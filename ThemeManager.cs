namespace AstroBin_CSV_Importer
{
    public enum AppTheme
    {
        Light,
        Dark
    }

    /// <summary>
    /// Applies a simple light/dark color palette to the form and its controls.
    /// WinForms has no built-in dark mode, so this hand-rolls one by walking the
    /// control tree and setting colors appropriate to each control type.
    /// </summary>
    internal static class ThemeManager
    {
        public static Color DarkBackground { get; } = Color.FromArgb(32, 32, 36);
        public static Color DarkPanel { get; } = Color.FromArgb(45, 45, 50);
        public static Color DarkControl { get; } = Color.FromArgb(60, 60, 66);
        public static Color DarkText { get; } = Color.FromArgb(230, 230, 230);
        public static Color DarkGridLines { get; } = Color.FromArgb(70, 70, 76);
        public static Color DarkAccent { get; } = Color.FromArgb(0, 150, 215);

        public static Color LightBackground { get; } = SystemColors.Control;
        public static Color LightText { get; } = Color.Black;
        public static Color LightAccent { get; } = Color.FromArgb(0, 120, 215);

        public static void Apply(Form form, AppTheme theme)
        {
            bool dark = theme == AppTheme.Dark;

            form.BackColor = dark ? DarkBackground : LightBackground;
            form.ForeColor = dark ? DarkText : LightText;

            ApplyToControls(form.Controls, dark);
        }

        private static void ApplyToControls(Control.ControlCollection controls, bool dark)
        {
            foreach (Control control in controls)
            {
                switch (control)
                {
                    case MenuStrip menuStrip:
                        menuStrip.BackColor = dark ? DarkPanel : SystemColors.Control;
                        menuStrip.ForeColor = dark ? DarkText : LightText;
                        menuStrip.Renderer = dark
                            ? new ToolStripProfessionalRenderer(new DarkMenuColorTable())
                            : new ToolStripProfessionalRenderer();
                        ApplyToToolStripItems(menuStrip.Items, dark);
                        break;

                    case DataGridView grid:
                        grid.BackgroundColor = dark ? DarkBackground : SystemColors.Window;
                        grid.GridColor = dark ? DarkGridLines : SystemColors.ControlLight;
                        grid.DefaultCellStyle.BackColor = dark ? DarkPanel : SystemColors.Window;
                        grid.DefaultCellStyle.ForeColor = dark ? DarkText : LightText;
                        grid.DefaultCellStyle.SelectionBackColor = dark ? DarkAccent : SystemColors.Highlight;
                        grid.DefaultCellStyle.SelectionForeColor = dark ? DarkText : SystemColors.HighlightText;
                        grid.ColumnHeadersDefaultCellStyle.BackColor = dark ? DarkControl : SystemColors.Control;
                        grid.ColumnHeadersDefaultCellStyle.ForeColor = dark ? DarkText : LightText;
                        grid.EnableHeadersVisualStyles = !dark;
                        grid.BorderStyle = BorderStyle.FixedSingle;
                        break;

                    case Button button:
                        button.FlatStyle = FlatStyle.Flat;
                        button.FlatAppearance.BorderColor = dark ? DarkAccent : LightAccent;
                        button.BackColor = dark ? DarkControl : SystemColors.Control;
                        button.ForeColor = dark ? DarkText : LightText;
                        break;

                    case Label label:
                        label.ForeColor = dark ? DarkText : LightText;
                        break;

                    case GroupBox groupBox:
                        groupBox.ForeColor = dark ? DarkText : LightText;
                        break;

                    case PieChartControl pieChart:
                        pieChart.DarkMode = dark;
                        break;

                    case Panel panel:
                        panel.BackColor = dark ? DarkPanel : SystemColors.Control;
                        break;

                    case ProgressBar:
                        break;
                }

                if (control.HasChildren)
                {
                    ApplyToControls(control.Controls, dark);
                }
            }
        }

        private static void ApplyToToolStripItems(ToolStripItemCollection items, bool dark)
        {
            foreach (ToolStripItem item in items)
            {
                item.ForeColor = dark ? DarkText : LightText;

                if (item is ToolStripMenuItem menuItem && menuItem.HasDropDownItems)
                {
                    ApplyToToolStripItems(menuItem.DropDownItems, dark);
                }
            }
        }

        private sealed class DarkMenuColorTable : ProfessionalColorTable
        {
            public override Color MenuItemSelected => DarkAccent;
            public override Color MenuItemSelectedGradientBegin => DarkAccent;
            public override Color MenuItemSelectedGradientEnd => DarkAccent;
            public override Color MenuItemPressedGradientBegin => DarkControl;
            public override Color MenuItemPressedGradientEnd => DarkControl;
            public override Color MenuStripGradientBegin => DarkPanel;
            public override Color MenuStripGradientEnd => DarkPanel;
            public override Color ToolStripDropDownBackground => DarkPanel;
            public override Color ImageMarginGradientBegin => DarkPanel;
            public override Color ImageMarginGradientMiddle => DarkPanel;
            public override Color ImageMarginGradientEnd => DarkPanel;
            public override Color MenuBorder => DarkGridLines;
            public override Color MenuItemBorder => DarkAccent;
            public override Color SeparatorDark => DarkGridLines;
            public override Color SeparatorLight => DarkGridLines;
        }
    }
}
