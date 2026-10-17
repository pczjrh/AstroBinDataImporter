namespace AstroBin_CSV_Importer
{
    /// <summary>
    /// Allows the Filter Name -> numeric Code list to be viewed, edited, added to, and
    /// removed from, via an editable grid. Changes are only persisted when the user
    /// confirms with OK (see <see cref="Mappings"/>).
    /// </summary>
    public sealed class FilterMappingForm : Form
    {
        private readonly DataGridView _grid;
        private readonly DataGridViewTextBoxColumn _nameColumn;
        private readonly DataGridViewTextBoxColumn _codeColumn;

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
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(360, 420);
            MinimumSize = new Size(320, 300);

            _nameColumn = new DataGridViewTextBoxColumn
            {
                HeaderText = "Name",
                Name = "Name",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            };

            _codeColumn = new DataGridViewTextBoxColumn
            {
                HeaderText = "Number",
                Name = "Code",
                Width = 90,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None
            };

            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = true,
                AutoGenerateColumns = false
            };
            _grid.Columns.AddRange(_nameColumn, _codeColumn);

            foreach ((string name, int code) in existingMappings)
            {
                _grid.Rows.Add(name, code);
            }

            var addButton = new Button { Text = "Add", AutoSize = true };
            addButton.Click += (_, _) =>
            {
                int rowIndex = _grid.Rows.Add(string.Empty, 0);
                _grid.ClearSelection();
                _grid.Rows[rowIndex].Selected = true;
                _grid.CurrentCell = _grid.Rows[rowIndex].Cells[0];
                _grid.BeginEdit(true);
            };

            var removeButton = new Button { Text = "Remove Selected", AutoSize = true };
            removeButton.Click += (_, _) =>
            {
                foreach (DataGridViewRow row in _grid.SelectedRows.Cast<DataGridViewRow>().ToList())
                {
                    if (!row.IsNewRow)
                    {
                        _grid.Rows.Remove(row);
                    }
                }
            };

            var topButtonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                Padding = new Padding(8, 8, 8, 0)
            };
            topButtonPanel.Controls.Add(addButton);
            topButtonPanel.Controls.Add(removeButton);

            var bottomButtonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                Padding = new Padding(8)
            };

            var okButton = new Button { Text = "OK", DialogResult = DialogResult.OK, AutoSize = true };
            var cancelButton = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true };

            okButton.Click += (_, e) =>
            {
                if (!TryBuildMappings(out List<(string Name, int Code)> mappings, out string? error))
                {
                    MessageBox.Show(this, error, "Invalid Entry", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    DialogResult = DialogResult.None;
                    return;
                }

                Mappings = mappings;
            };

            bottomButtonPanel.Controls.Add(cancelButton);
            bottomButtonPanel.Controls.Add(okButton);

            AcceptButton = okButton;
            CancelButton = cancelButton;

            Controls.Add(_grid);
            Controls.Add(topButtonPanel);
            Controls.Add(bottomButtonPanel);
        }

        private bool TryBuildMappings(out List<(string Name, int Code)> mappings, out string? error)
        {
            mappings = new List<(string Name, int Code)>();
            error = null;

            var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (DataGridViewRow row in _grid.Rows)
            {
                if (row.IsNewRow)
                {
                    continue;
                }

                string name = Convert.ToString(row.Cells[_nameColumn.Index].Value)?.Trim() ?? string.Empty;
                string codeText = Convert.ToString(row.Cells[_codeColumn.Index].Value)?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(name))
                {
                    error = "Each entry must have a name.";
                    return false;
                }

                if (!int.TryParse(codeText, out int code))
                {
                    error = $"'{name}' has an invalid numeric value: '{codeText}'.";
                    return false;
                }

                if (!seenNames.Add(name))
                {
                    error = $"Duplicate filter name: '{name}'.";
                    return false;
                }

                mappings.Add((name, code));
            }

            return true;
        }
    }
}