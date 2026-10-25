using System.ComponentModel;
using Syncfusion.WinForms.Controls;
using Syncfusion.WinForms.DataGrid;
using Syncfusion.WinForms.DataGrid.Enums;
using Syncfusion.WinForms.GridCommon.ScrollAxis;

namespace AstroBin_CSV_Importer
{
    /// <summary>
    /// Allows the Filter Name -> numeric Code list to be viewed, edited, added to, and
    /// removed from, via an editable Syncfusion grid. Changes are only persisted when the
    /// user confirms with OK (see <see cref="Mappings"/>).
    /// </summary>
    public sealed class FilterMappingForm : Form
    {
        /// <summary>Editable row model bound to the grid.</summary>
        public sealed class MappingRow
        {
            public string Name { get; set; } = string.Empty;
            public int Code { get; set; }
        }

        private readonly SfDataGrid _grid;
        private readonly BindingList<MappingRow> _rows;
        private readonly Label _countLabel;

        /// <summary>
        /// Populated with the Name -> Code values the user confirmed, once the dialog
        /// closes with <see cref="DialogResult.OK"/>.
        /// </summary>
        public List<(string Name, int Code)> Mappings { get; private set; } = new();

        public FilterMappingForm(IEnumerable<(string Name, int Code)> existingMappings)
        {
            Text = "Map Filters";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(420, 500);
            MinimumSize = new Size(360, 360);

            _rows = new BindingList<MappingRow>(
                existingMappings.Select(m => new MappingRow { Name = m.Name, Code = m.Code }).ToList());

            var titleLabel = new Label
            {
                Text = "Filter mappings",
                Font = new Font("Segoe UI Semibold", 14F),
                AutoSize = true,
                Dock = DockStyle.Top,
                Padding = new Padding(0, 0, 0, 2)
            };

            var hintLabel = new Label
            {
                Text = "Map each filter name to its AstroBin filter number. Double-click a cell to edit.",
                AutoSize = false,
                Height = 38,
                Dock = DockStyle.Top
            };

            _countLabel = new Label { AutoSize = true, Dock = DockStyle.Bottom, Padding = new Padding(0, 6, 0, 0) };

            _grid = new SfDataGrid
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                AllowEditing = true,
                AllowResizingColumns = false,
                AllowSorting = true,
                AllowDraggingColumns = false,
                EditMode = EditMode.DoubleClick,
                SelectionMode = GridSelectionMode.Extended,
                AutoSizeColumnsMode = AutoSizeColumnsMode.Fill,
                ShowRowHeader = false,
                DataSource = _rows
            };
            _grid.Columns.Add(new GridTextColumn { MappingName = nameof(MappingRow.Name), HeaderText = "Name", MinimumWidth = 120 });
            _grid.Columns.Add(new GridNumericColumn
            {
                MappingName = nameof(MappingRow.Code),
                HeaderText = "Number",
                NumberFormatInfo = new System.Globalization.NumberFormatInfo { NumberDecimalDigits = 0 },
                MinimumWidth = 90,
                Width = 100,
                AutoSizeColumnsMode = AutoSizeColumnsMode.None
            });
            _rows.ListChanged += (_, _) => UpdateCount();
            UpdateCount();

            var addButton = new SfButton { Text = "Add", Size = new Size(80, 32), Tag = "primary" };
            addButton.Click += (_, _) => AddRow();

            var removeButton = new SfButton { Text = "Remove Selected", Size = new Size(140, 32) };
            removeButton.Click += (_, _) => RemoveSelected();

            var toolbar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.LeftToRight,
                Height = 44,
                Padding = new Padding(0, 4, 0, 4)
            };
            toolbar.Controls.Add(addButton);
            toolbar.Controls.Add(removeButton);

            var okButton = new SfButton { Text = "OK", DialogResult = DialogResult.OK, Size = new Size(90, 34), Tag = "primary" };
            var cancelButton = new SfButton { Text = "Cancel", DialogResult = DialogResult.Cancel, Size = new Size(90, 34) };

            okButton.Click += (_, _) =>
            {
                _grid.CurrentCell?.EndEdit();

                if (!TryBuildMappings(out List<(string Name, int Code)> mappings, out string? error))
                {
                    MessageBox.Show(this, error, "Invalid Entry", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    DialogResult = DialogResult.None;
                    return;
                }

                Mappings = mappings;
            };

            var bottomButtonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Height = 50,
                Padding = new Padding(0, 8, 0, 0)
            };
            bottomButtonPanel.Controls.Add(cancelButton);
            bottomButtonPanel.Controls.Add(okButton);

            AcceptButton = okButton;
            CancelButton = cancelButton;

            // Container gives the content breathing room from the form edges.
            var content = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16) };
            content.Controls.Add(_grid);
            content.Controls.Add(_countLabel);
            content.Controls.Add(toolbar);
            content.Controls.Add(hintLabel);
            content.Controls.Add(titleLabel);
            content.Controls.Add(bottomButtonPanel);
            Controls.Add(content);

            ThemeManager.Apply(this, AppSettingsStore.LoadTheme());
            titleLabel.Font = new Font("Segoe UI Semibold", 14F);
            // Panels are painted with the "panel" colour by ThemeManager; match the form instead
            // so the dialog reads as one surface with the grid as the only raised element.
            content.BackColor = BackColor;
            toolbar.BackColor = BackColor;
            bottomButtonPanel.BackColor = BackColor;
        }

        private void UpdateCount() =>
            _countLabel.Text = _rows.Count == 1 ? "1 filter" : $"{_rows.Count} filters";

        private void AddRow()
        {
            _grid.CurrentCell?.EndEdit();

            var row = new MappingRow();
            _rows.Add(row);

            int rowIndex = _grid.TableControl.ResolveToRowIndex(row);
            _grid.TableControl.ScrollRows.ScrollInView(rowIndex);
            _grid.SelectedItems.Clear();
            _grid.SelectedItem = row;
            _grid.MoveToCurrentCell(new RowColumnIndex(rowIndex, 0));
            _grid.CurrentCell?.BeginEdit();
        }

        private void RemoveSelected()
        {
            _grid.CurrentCell?.EndEdit();

            foreach (MappingRow row in _grid.SelectedItems.OfType<MappingRow>().ToList())
            {
                _rows.Remove(row);
            }
        }

        private bool TryBuildMappings(out List<(string Name, int Code)> mappings, out string? error)
        {
            mappings = new List<(string Name, int Code)>();
            error = null;

            var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (MappingRow row in _rows)
            {
                string name = row.Name?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(name))
                {
                    error = "Each entry must have a name.";
                    return false;
                }

                // Names are stored in a "Name,Code" CSV file, so a comma would corrupt the line.
                if (name.Contains(','))
                {
                    error = $"Filter name '{name}' cannot contain a comma.";
                    return false;
                }

                if (!seenNames.Add(name))
                {
                    error = $"Duplicate filter name: '{name}'.";
                    return false;
                }

                mappings.Add((name, row.Code));
            }

            return true;
        }
    }
}
