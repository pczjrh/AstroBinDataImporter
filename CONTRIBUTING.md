# Contributing

## FITS Header Keyword Conventions

This project reads FITS file headers via `cfitsio`-based native interop. The following header keywords are used:

| Purpose            | Keyword(s)              |
|--------------------|--------------------------|
| Observation date   | `DATE-OB`               |
| Exposure time (s)  | `EXPOSURE`              |
| CCD temperature    | `CCD-TEMP`              |
| Filter             | `FILTER`                |
| Camera / CCD name  | `INSTRUME` or `INSTRUMENT` (checked in that order) |

Only the **calendar date** portion of `DATE-OB` is used when grouping/aggregating summary data — time-of-day is ignored.