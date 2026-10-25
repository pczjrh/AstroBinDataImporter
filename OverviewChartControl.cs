using System.ComponentModel;
using Syncfusion.Drawing;
using Syncfusion.Windows.Forms.Chart;

namespace AstroBin_CSV_Importer
{
    /// <summary>
    /// A single slice of the pie chart: a label, a count (used to compute the slice
    /// proportion), and the color to draw it with.
    /// </summary>
    internal sealed record PieSlice(string Label, int Count, Color Color);

    /// <summary>
    /// Syncfusion <see cref="ChartControl"/> configured as a pie chart with a legend. Used for the
    /// "Overview" panel to visualize the distribution of light frames by filter.
    /// </summary>
    internal sealed class OverviewChartControl : ChartControl
    {
        /// <summary>True when the dark theme is active; used to pick slice border colours.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public bool DarkMode { get; private set; }

        private List<PieSlice> _slices = new();

        public OverviewChartControl()
        {
            ShowLegend = false;

            Title.Visible = false;
            AllowGradientPalette = false;
            ChartArea.BorderWidth = 0;
            ChartArea.BorderColor = Color.Transparent;
            ElementsSpacing = 12;
            Series3D = false;
            ShowToolTips = true;
            ChartArea.BackInterior = new BrushInfo(Color.Transparent);
        }

        /// <summary>Re-colours the chart background and redraws the current data for the theme.</summary>
        public void ApplyTheme(bool dark)
        {
            DarkMode = dark;

            Color back = dark ? ThemeManager.DarkPanel : ThemeManager.LightPanel;

            BackInterior = new BrushInfo(back);
            ChartArea.BackInterior = new BrushInfo(back);

            SetData(_slices);
        }

        /// <summary>Replaces the chart contents; slices with a zero count are skipped.</summary>
        public void SetData(IEnumerable<PieSlice> slices)
        {
            List<PieSlice> data = slices.Where(s => s.Count > 0).ToList();
            _slices = data;

            Series.Clear();
            if (data.Count == 0)
            {
                return;
            }

            var series = new ChartSeries("Filters", ChartSeriesType.Pie);

            for (int i = 0; i < data.Count; i++)
            {
                PieSlice slice = data[i];
                series.Points.Add(i, slice.Count);
                series.Styles[i].ToolTip = $"{slice.Label} ({slice.Count})";
                series.Styles[i].Interior = new BrushInfo(slice.Color);
                series.Styles[i].Border.Color = DarkMode ? ThemeManager.DarkPanel : ThemeManager.LightPanel;
                series.Styles[i].Border.Width = 2;
            }

            series.Style.DisplayText = false;
            Series.Add(series);
            Refresh();
        }
    }
}
