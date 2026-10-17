using System.Globalization;

namespace AstroBin_CSV_Importer
{
    /// <summary>
    /// Presents one numeric input per distinct filter found in the current database/records,
    /// allowing each filter to be mapped to a numerical code (e.g. for AstroBin export).
    /// </summary>
    public sealed class FilterMappingForm : Form
    {
        private readonly Dictionary<string, NumericUpDown> _filterInputs = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Populated with the Filter -> code values the user confirmed, once the dialog
        /// closes with <see cref="DialogResult.OK"/>.
        /// </summary>
        public Dictionary<string, int> Mappings { get; private set; } = new(StringComparer.OrdinalIgnoreCase);

        public FilterMappingForm(IEnumerable<string> filters, IReadOnlyDictionary<string, int> existingMappings)
        {
            Text = "Map Filters";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            AutoScaleMode = AutoScaleMode.Font;

            string[] sortedFilters = filters
                .Where(f => !string.IsNullOrWhiteSpace(f))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 2,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(12),
                RowCount = sortedFilters.Length
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));

            foreach (string filter in sortedFilters)
            {
                var label = new Label
                {
                    Text = filter,
                    AutoSize = true,
                    Anchor = AnchorStyles.Left,
                    Margin = new Padding(3, 8, 3, 3)
                };

                var numericUpDown = new NumericUpDown
                {
                    Minimum = 0,
                    Maximum = 999,
                    Width = 80,
                    Anchor = AnchorStyles.Left,
                    Value = existingMappings.TryGetValue(filter, out int code)
                        ? code
                        : 0
                };

                _filterInputs[filter] = numericUpDown;

                layout.Controls.Add(label);
                layout.Controls.Add(numericUpDown);
            }

            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                Padding = new Padding(12)
            };

            var okButton = new Button { Text = "OK", DialogResult = DialogResult.OK, AutoSize = true };
            var cancelButton = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true };

            okButton.Click += (_, _) =>
            {
                Mappings = _filterInputs.ToDictionary(
                    kvp => kvp.Key,
                    kvp => (int)kvp.Value.Value,
                    StringComparer.OrdinalIgnoreCase);
            };

            buttonPanel.Controls.Add(cancelButton);
            buttonPanel.Controls.Add(okButton);

            AcceptButton = okButton;
            CancelButton = cancelButton;

            Controls.Add(layout);
            Controls.Add(buttonPanel);

            ClientSize = new Size(320, Math.Min(500, 70 + (sortedFilters.Length * 32) + 60));
            AutoScroll = sortedFilters.Length > 10;
        }
    }
}