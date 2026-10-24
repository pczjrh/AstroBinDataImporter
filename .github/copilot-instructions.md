# Copilot Instructions — AstroBin Data Importer

This file gives GitHub Copilot (and other AI coding assistants) the context needed to work
productively on this project without re-discovering it from scratch each session.

## What this project is

A Windows Forms (.NET 9, `net9.0-windows`) desktop app that:
1. Reads FITS (`.fits`/`.fit`/`.fts`) and XISF (`.xisf`) astrophotography image headers.
2. Aggregates exposure data (frames/filter/date/exposure time/temperature/etc.) into a summary table.
3. Persists the data to a per-project SQLite database.
4. Exports the aggregated data as a CSV formatted for import into AstroBin's
   "Acquisition sessions" bulk-upload feature, and copies it to the clipboard.

Root namespace: `AstroBin_CSV_Importer`. Main project file: `AstroBin-CSV-Importer.csproj`.

## Solution layout (key files)

| File | Responsibility |
|---|---|
| `Program.cs` | Entry point, launches `Form1`. |
| `Form1.cs` / `Form1.Designer.cs` | Main UI, event handlers, and (currently) also contains the core domain types: `FitsRecord`, `FrameType`, `FitsReader`, `XisfReader`, `FitsRecordDeduplicator`, `HeaderValueHelpers`. These are defined at the bottom of `Form1.cs` below the `Form1` class — **not** in separate files, despite the project otherwise using one-class-per-file. Keep this in mind when searching for these types. |
| `FitsDatabase.cs` | All SQLite persistence (`Microsoft.Data.Sqlite`). Schema creation, migration (`ALTER TABLE ADD COLUMN` guarded by `PRAGMA table_info`), load/save/clear of `FitsRecords`. |
| `FilterMappingStore.cs` | Persists the Name→numeric-code filter list as a CSV (`FilterCodes.txt`) in `%AppData%\AstroBinDataImporter\`, seeded from the `FilterCodes.txt` shipped next to the exe on first run. **Independent of any project database** — this is global, not per-project. |
| `FilterMappingForm.cs` | Editable grid (add/remove rows) dialog for the Name→Code list, built entirely in code (no `.Designer.cs`). |
| `FilterAliasResolver.cs` | Normalizes filter name aliases (e.g. `R`/`Red`, `L`/`Lum`/`Luminance`) to a canonical display name, used for both on-screen aggregation and export grouping. |
| `AstroBinExporter.cs` | Builds the AstroBin CSV export from `FitsRecord` data + `FilterMappingStore` + `FilterAliasResolver`. |
| `FilterCodes.txt` | Seed data for `FilterMappingStore` (CSV, header row `Name,Code`), copied to output dir via `.csproj`. |
| `cfitsio.dll` | Native dependency for FITS reading, copied to output dir via `.csproj`. |
| `ThemeManager.cs` | Applies a hand-rolled light/dark color palette by walking the form's control tree (no external theming library/dark-mode API used). |
| `AppSettingsStore.cs` | Persists the chosen theme (`AppTheme.Light`/`Dark`) as a single-line text file in `%AppData%\AstroBinDataImporter\Settings.txt`, mirroring `FilterMappingStore`'s storage pattern. Independent of the per-project database. |
| `PieChartControl.cs` | Dependency-free GDI+ pie chart (`Control` subclass, custom `OnPaint`) + legend, used by the "Overview" panel to show Light-frame distribution by canonical filter name. |

## Key domain concepts

- **`FitsRecord`**: one row = one exposure frame's extracted header data. Fields include
  `Filter`, `DateObs`/`DateOnly`, `ExposureSeconds`, `CcdTemp`, `CcdName`, `ImageType` (`FrameType`),
  `Iso`, `Binning`, `Gain`, `FNumber`, `AmbientTemp`, `MeanFwhm`, `MeanSqm`.
- **`FrameType`** (`Light`, `Dark`, `Bias`, `Flat`, `FlatDark`, `Unknown`): derived from the FITS
  `IMAGETYP` keyword via `HeaderValueHelpers.NormalizeImageType` (prefix-matched, case-insensitive).
  Only `Light` frames appear in the on-screen summary/report; calibration frames are counted
  separately and rolled into the export's `darks`/`flats`/`flatDarks`/`bias` columns.
- **FITS header keyword mapping** (read by both `FitsReader` and `XisfReader` — keep them in sync
  when adding new fields!):

  | Field | Keyword(s) checked, in order |
  |---|---|
  | Observation date | `DATE-OBS` |
  | Exposure time (s) | `EXPOSURE` (FITS), `EXPOSURE`/`EXPTIME` (XISF) |
  | CCD temperature | `CCD-TEMP` |
  | Filter | `FILTER` |
  | Camera/CCD name | `INSTRUME`, then `INSTRUMENT` |
  | Frame type | `IMAGETYP` |
  | ISO | `ISO` only |
  | Gain | `GAIN`, then `EGAIN`, then `ISOSPEED` — **`ISOSPEED` is a gain value for these cameras, not an ISO value.** Do not map it to the `Iso` field. |
  | Binning | `XBINNING` |
  | f-number | `FOCRATIO` |
  | Ambient temp | `AMBTEMP`, then `OUTTEMP` |
  | Mean FWHM | `FWHM` |
  | Mean SQM | `SQM` |

  Only the **calendar date** portion of `DATE-OBS` (first 10 chars, `yyyy-MM-dd`) is used for
  grouping/aggregation (`FitsRecord.DateOnly`) — time-of-day is ignored.

- **Deduplication** (`FitsRecordDeduplicator`): two-pass — first by base file name (catches
  FITS/XISF pairs of the same frame), then by a header-content key
  (`DateObs`, `Filter`, `ExposureSeconds`, `CcdTemp`, `ImageType`) with a small numeric tolerance.

- **Filter aliasing**: filter names found in FITS headers (`R`, `G`, `B`, `L`, `Luminance`, `Ha`,
  `H-Alpha`, `Oiii`, `O-III`, `Sii`, `S-II`, etc.) are normalized to canonical display names
  (`Red`, `Green`, `Blue`, `Lum`, `Ha`, `Oiii`, `Sii`) via `FilterAliasResolver.ToCanonicalName`
  wherever data is grouped for display, export, or charting. The raw value from the header is
  still stored as-is in the DB; only the reporting/export/chart layer normalizes it.
- **Filter colors**: `FilterAliasResolver.ToColor(canonicalName)` maps each canonical filter name
  to a fixed `Color` used by the Overview pie chart: `Red`/`Green`/`Blue` → pure RGB primaries,
  `RGB` (OSC/Bayer frames, see `HeaderValueHelpers.ResolveFilter`) → white, `Lum` → near-white,
  `Ha` → a distinct orange-red (deliberately different from `Red`), `Oiii` → a distinct cyan-blue
  (deliberately different from `Blue`), `Sii` → dark red. Unrecognized filter names fall back to a
  small deterministic (hash-based) palette so colors stay stable across refreshes. **When adding a
  new narrowband/broadband alias, add both the alias→canonical mapping and a color entry** so the
  chart doesn't silently fall back to the generic palette.

## Database (`FitsDatabase.cs`)

- SQLite file: `AstroBinData.sqlite`, stored in a **user-chosen project folder** (selected via
  `FolderBrowserDialog` — there's no single fixed location; each "project" is its own folder).
- Single table `FitsRecords` with a `UNIQUE (DateObs, Filter, ExposureSeconds, CcdTemp)` constraint;
  saves use `INSERT OR IGNORE` so re-saving previously-imported data is a no-op.
- **Schema migrations**: SQLite has no `ADD COLUMN IF NOT EXISTS`, so `MigrateSchema` reads
  `PRAGMA table_info(FitsRecords)` and only runs `ALTER TABLE ADD COLUMN` for columns that don't
  already exist. **When adding a new `FitsRecord` field that should be persisted, you must update
  three places in `FitsDatabase.cs`**: the `CREATE TABLE` DDL, the `newColumns` array in
  `MigrateSchema`, and both `LoadAll`/`SaveRecords`.
- `ClearAll(folder)` empties the table (`DELETE FROM FitsRecords`) without dropping the file/schema.

## AstroBin export (`AstroBinExporter.cs`)

Produces a CSV matching AstroBin's "Acquisition sessions" bulk-upload format:
`date,filter,number,duration,iso,binning,gain,sensorCooling,fNumber,darks,flats,flatDarks,bias,bortle,meanSqm,meanFwhm,temperature`.

- Rows are grouped from **Light** frames only, keyed by date + canonical filter name + exposure
  duration + camera settings (ISO/binning/gain/temp/etc.) — one row per distinct combination.
- `filter` is exported as the **numeric code** resolved via `FilterMappingStore`, keyed by the
  canonical filter name (so `R`/`Red` both resolve through the `Red` mapping entry, etc.).
- `darks`/`flats`/`flatDarks`/`bias` counts are computed from actual calibration frames in the
  loaded data, matched by date (flats are additionally matched by filter, since flats are
  filter-specific; darks/bias/flat-darks are not).
- **Columns that are blank across every row are omitted from the CSV entirely**, except
  `date`/`filter`/`number`/`duration` which are always included (see `RequiredColumns`).
- `bortle` is currently never populated (not derivable from FITS/XISF headers) — it will always
  be blank/omitted unless a UI is added to capture it manually.

## UI conventions

- Menu structure: `menuStrip1` has two top-level menus:
  - **Database** (`databaseToolStripMenuItem`): **Load a DB**, **Save to Database**, **Map
    Filters**, **Clear Database**.
  - **View** (`viewToolStripMenuItem`): **Dark Mode** (`darkModeToolStripMenuItem`, a
    `CheckOnClick` checkbox item) toggling the app theme.
- `FilterMappingForm` is built entirely in code (`DataGridView` + Add/Remove buttons), not via the
  WinForms designer — there's no matching `.Designer.cs` for it.
- **Notifications**: informational/warning/success messages use `ShowNotification(title, message)`,
  which shows a system tray balloon tip via `notifyIcon1` (a `NotifyIcon` added in
  `Form1.Designer.cs`, icon set to `SystemIcons.Application` in the `Form1` constructor since the
  app ships no custom `.ico`). **Do not use `MessageBox.Show` for routine info/warning/error
  feedback** — reserve `MessageBox` only for cases requiring an explicit user decision (e.g. the
  Yes/No confirmation before `Clear Database`, which is destructive/irreversible).

## Theming and the Overview chart

- **`ThemeManager.Apply(Form, AppTheme)`**: walks `form.Controls` recursively and sets colors on
  known control types (`MenuStrip` incl. a custom `ProfessionalColorTable` for dark dropdowns,
  `DataGridView` cell/header styles, `Button` via `FlatStyle.Flat`, `Label`, `GroupBox`, `Panel`,
  `PieChartControl.DarkMode`). **When adding a new control type to `Form1`, add a corresponding
  `case` in `ThemeManager.ApplyToControls` if it needs theme-aware colors** — otherwise it will
  keep its default WinForms colors even in dark mode (easy to miss).
- Theme preference (`AppTheme.Light`/`Dark`) is persisted by `AppSettingsStore` to
  `%AppData%\AstroBinDataImporter\Settings.txt` (plain text, just the enum name) — a separate,
  independent store from `FilterMappingStore`/`FitsDatabase`. Loaded in the `Form1` constructor and
  applied before the form is shown; saved on every toggle of the **View → Dark Mode** menu item.
- **`PieChartControl`** (`Control` subclass): dependency-free GDI+ donut-free pie chart + legend.
  Call `SetData(IEnumerable<PieSlice>)` to update it; `PieSlice` is a `record(string Label, int
  Count, Color Color)`. It lives inside `grp_Overview` (a `GroupBox` titled "Overview", docked to
  the right of `dgv_SummaryData` in `Form1.Designer.cs`).
- **`Form1.UpdateOverviewChart(List<FitsRecord> lightRecords)`**: called at the end of
  `PopulateSummary` (i.e. every time the grid is refreshed — after loading files, opening a
  database, etc.). Groups **Light** frames only by `FilterAliasResolver.ToCanonicalName`, builds
  one `PieSlice` per distinct filter with its color from `FilterAliasResolver.ToColor`, and feeds
  the list to `pieChart.SetData`. If you add a new place that mutates `_currentRecords` without
  going through `PopulateSummary`, remember the chart won't refresh unless you call this too.

## Things to watch out for

- `Form1.Designer.cs` edits must keep `InitializeComponent()` internally consistent (field
  declarations at the bottom of the file, instantiation + `.Click +=` wiring inside the method,
  and `components = new System.ComponentModel.Container()` must exist for `NotifyIcon` to work).
- When editing generated-looking designer code by hand, always re-run a build afterward —
  mismatched `using var ... = new Foo()` ordering or stray directives are easy to introduce.
- `FilterMappingStore` and `FitsDatabase` are two **separate** persistence stores with different
  lifetimes/locations — don't conflate "the project database" with "the global filter code list".
- `FitsReader` (cfitsio P/Invoke) and `XisfReader` (hand-rolled XISF/XML header parser) must be
  kept in sync: any new header field added to one should be added to the other too.
- `Form1.Designer.cs` currently has **two theme-affecting control collections** to remember when
  adding controls: the themed tree walked by `ThemeManager` (automatic, as long as the control type
  has a `case`) and the `grp_Overview`/`pieChart` pairing, which is anchored `Top|Bottom|Right` so
  it stays docked to the right edge as the form resizes — if you add more Overview-panel controls,
  anchor/dock them relative to `grp_Overview`, not the form.
- Running `run_build` after any `ThemeManager`/`PieChartControl`/Designer edit is especially
  important: WinForms analyzers (e.g. `WFO1000`) flag public properties on custom controls that
  look like they should be designer-serializable (like `PieChartControl.DarkMode`) — mark such
  properties `[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]` and
  `[Browsable(false)]` if they're meant to be set purely in code.

