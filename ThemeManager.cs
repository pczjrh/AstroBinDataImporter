using Syncfusion.WinForms.Controls;
using Syncfusion.WinForms.DataGrid;
using Syncfusion.WinForms.DataGrid.Styles;
using Syncfusion.WinForms.Themes;

namespace AstroBin_CSV_Importer
{
    public enum AppTheme
    {
        Light,
        Dark
    }

    /// <summary>
    /// Applies a light/dark theme: Syncfusion's SfSkinManager skins the Syncfusion controls,
    /// and the control tree is walked to style the standard WinForms controls to match.
    /// </summary>
    internal static class ThemeManager
    {
        public static Color DarkBackground { get; } = Color.FromArgb(24, 24, 28);
        public static Color DarkPanel { get; } = Color.FromArgb(34, 34, 40);
        public static Color DarkControl { get; } = Color.FromArgb(48, 48, 56);
        public static Color DarkText { get; } = Color.FromArgb(232, 232, 236);
        public static Color DarkGridLines { get; } = Color.FromArgb(54, 54, 62);
        public static Color DarkAccent { get; } = Color.FromArgb(99, 102, 241);

        public static Color LightBackground { get; } = Color.FromArgb(244, 245, 248);
        public static Color LightPanel { get; } = Color.White;
        public static Color LightControl { get; } = Color.FromArgb(232, 234, 240);
        public static Color LightText { get; } = Color.FromArgb(30, 32, 40);
        public static Color LightGridLines { get; } = Color.FromArgb(232, 234, 240);
        public static Color LightAccent { get; } = Color.FromArgb(79, 70, 229);

        private static bool _themesLoaded;

        public static void Apply(Form form, AppTheme theme)
        {
            bool dark = theme == AppTheme.Dark;

            if (!_themesLoaded)
            {
                SfSkinManager.LoadAssembly(typeof(Office2019Theme).Assembly);
                _themesLoaded = true;
            }

            // Syncfusion skins its own controls (SfButton); everything else is styled below.
            SfSkinManager.SetVisualStyle(form, dark ? "Office2019Black" : "Office2019Colorful");

            form.BackColor = dark ? DarkBackground : LightBackground;
            form.ForeColor = dark ? DarkText : LightText;

            ApplyToControls(form.Controls, dark);
        }

        private static void ApplyToControls(Control.ControlCollection controls, bool dark)
        {
            Color panel = dark ? DarkPanel : LightPanel;
            Color text = dark ? DarkText : LightText;
            Color accent = dark ? DarkAccent : LightAccent;

            foreach (Control control in controls)
            {
                switch (control)
                {
                    case MenuStrip menuStrip:
                        menuStrip.BackColor = dark ? DarkPanel : LightPanel;
                        menuStrip.ForeColor = text;
                        menuStrip.Padding = new Padding(8, 3, 0, 3);
                        menuStrip.Renderer = new ToolStripProfessionalRenderer(new MenuColorTable(dark))
                        {
                            RoundedEdges = false
                        };
                        ApplyToToolStripItems(menuStrip.Items, dark);
                        break;

                    case SfDataGrid grid:
                        grid.Style.BorderColor = Color.Transparent;
                        grid.Style.CellStyle.BackColor = panel;
                        grid.Style.CellStyle.TextColor = text;
                        grid.Style.CellStyle.Borders.All = new GridBorder(GridBorderStyle.None, Color.Transparent, GridBorderWeight.Thin);
                        grid.Style.CellStyle.Borders.Bottom = new GridBorder(GridBorderStyle.Solid, dark ? DarkGridLines : LightGridLines, GridBorderWeight.Thin);
                        grid.Style.BorderStyle = BorderStyle.None;
                        grid.RowHeight = 28;
                        grid.HeaderRowHeight = 34;
                        grid.Style.HeaderStyle.BackColor = dark ? DarkControl : LightControl;
                        grid.Style.HeaderStyle.TextColor = text;
                        grid.Style.HeaderStyle.FilterIconColor = text;
                        grid.Style.HeaderStyle.SortIconColor = text;
                        grid.Style.HeaderStyle.Font.Bold = true;
                        grid.Style.HeaderStyle.Borders.All = new GridBorder(GridBorderStyle.None, Color.Transparent, GridBorderWeight.Thin);
                        grid.Style.SelectionStyle.BackColor = accent;
                        grid.Style.SelectionStyle.TextColor = Color.White;
                        grid.Style.CurrentCellStyle.BorderColor = Color.Transparent;
                        grid.Style.CurrentCellStyle.BackColor = accent;
                        grid.Style.CurrentCellStyle.TextColor = Color.White;
                        break;

                    case SfButton sfButton:
                        bool primary = sfButton.Tag as string == "primary";
                        sfButton.Style.BackColor = primary ? accent : (dark ? DarkControl : LightControl);
                        sfButton.Style.ForeColor = primary ? Color.White : text;
                        sfButton.Style.HoverBackColor = primary
                            ? ControlPaint.Light(accent, 0.15f)
                            : (dark ? Color.FromArgb(62, 62, 72) : Color.FromArgb(218, 221, 230));
                        sfButton.Style.HoverForeColor = primary ? Color.White : text;
                        sfButton.Style.PressedBackColor = primary
                            ? ControlPaint.Dark(accent, 0.1f)
                            : (dark ? Color.FromArgb(40, 40, 46) : Color.FromArgb(205, 208, 220));
                        sfButton.Style.PressedForeColor = primary ? Color.White : text;
                        sfButton.Style.DisabledBackColor = dark ? DarkPanel : LightControl;
                        sfButton.Style.DisabledForeColor = dark ? Color.FromArgb(110, 110, 120) : Color.FromArgb(160, 164, 176);
                        sfButton.Style.FocusedBackColor = sfButton.Style.BackColor;
                        sfButton.Style.FocusedForeColor = sfButton.Style.ForeColor;
                        sfButton.Style.Border = new Pen(Color.Transparent);
                        sfButton.Style.HoverBorder = new Pen(Color.Transparent);
                        sfButton.Style.PressedBorder = new Pen(Color.Transparent);
                        sfButton.Style.FocusedBorder = new Pen(Color.Transparent);
                        sfButton.Style.DisabledBorder = new Pen(Color.Transparent);
                        sfButton.Font = new Font("Segoe UI Semibold", 9F);
                        sfButton.Cursor = Cursors.Hand;
                        break;

                    case Label label:
                        label.ForeColor = text;
                        if (label.Name == "lbl_TotalIntTimeResult")
                        {
                            label.ForeColor = accent;
                            label.Font = new Font("Segoe UI Semibold", 11F);
                        }
                        else
                        {
                            label.Font = new Font("Segoe UI", 9.5F);
                        }
                        break;

                    case GroupBox groupBox:
                        groupBox.ForeColor = text;
                        groupBox.BackColor = panel;
                        groupBox.Font = new Font("Segoe UI Semibold", 9.5F);
                        break;

                    case OverviewChartControl pieChart:
                        pieChart.ApplyTheme(dark);
                        break;

                    case Panel panelControl:
                        panelControl.BackColor = panel;
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

        private sealed class MenuColorTable : ProfessionalColorTable
        {
            private readonly bool _dark;

            public MenuColorTable(bool dark)
            {
                _dark = dark;
                UseSystemColors = false;
            }

            private Color Panel => _dark ? DarkPanel : LightPanel;
            private Color Hover => _dark ? DarkControl : LightControl;
            private Color Line => _dark ? DarkGridLines : LightGridLines;

            public override Color MenuItemSelected => Hover;
            public override Color MenuItemSelectedGradientBegin => Hover;
            public override Color MenuItemSelectedGradientEnd => Hover;
            public override Color MenuItemPressedGradientBegin => Hover;
            public override Color MenuItemPressedGradientEnd => Hover;
            public override Color MenuStripGradientBegin => Panel;
            public override Color MenuStripGradientEnd => Panel;
            public override Color ToolStripDropDownBackground => Panel;
            public override Color ImageMarginGradientBegin => Panel;
            public override Color ImageMarginGradientMiddle => Panel;
            public override Color ImageMarginGradientEnd => Panel;
            public override Color MenuBorder => Line;
            public override Color MenuItemBorder => Hover;
            public override Color SeparatorDark => Line;
            public override Color SeparatorLight => Line;
        }
    }
}
