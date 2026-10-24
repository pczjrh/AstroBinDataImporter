using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace AstroBin_CSV_Importer
{
    /// <summary>
    /// A single slice of the pie chart: a label, a count (used to compute the slice
    /// proportion), and the color to draw it with.
    /// </summary>
    internal sealed record PieSlice(string Label, int Count, Color Color);

    /// <summary>
    /// Small, dependency-free pie chart control drawn with GDI+. Shows a proportional
    /// pie plus a legend listing each slice's label and frame count. Used for the
    /// "Overview" panel to visualize the distribution of light frames by filter.
    /// </summary>
    internal sealed class PieChartControl : Control
    {
        private List<PieSlice> _slices = new();

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public bool DarkMode { get; set; }

        public PieChartControl()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            DoubleBuffered = true;
        }

        public void SetData(IEnumerable<PieSlice> slices)
        {
            _slices = slices.Where(s => s.Count > 0).ToList();
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            Color backColor = DarkMode ? ThemeManager.DarkPanel : BackColor;
            Color textColor = DarkMode ? ThemeManager.DarkText : ForeColor;
            using (var backBrush = new SolidBrush(backColor))
            {
                g.FillRectangle(backBrush, ClientRectangle);
            }

            if (_slices.Count == 0)
            {
                using var emptyBrush = new SolidBrush(textColor);
                using var emptyFormat = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString("No data", Font, emptyBrush, ClientRectangle, emptyFormat);
                return;
            }

            int total = _slices.Sum(s => s.Count);
            if (total == 0)
            {
                return;
            }

            const int legendWidth = 130;
            int pieDiameter = Math.Min(Height - 20, Width - legendWidth - 20);
            pieDiameter = Math.Max(pieDiameter, 20);

            var pieRect = new Rectangle(10, (Height - pieDiameter) / 2, pieDiameter, pieDiameter);

            float startAngle = -90f;
            using var outlinePen = new Pen(backColor, 2f);

            foreach (PieSlice slice in _slices)
            {
                float sweep = 360f * slice.Count / total;
                using var brush = new SolidBrush(slice.Color);
                g.FillPie(brush, pieRect, startAngle, sweep);
                g.DrawPie(outlinePen, pieRect, startAngle, sweep);
                startAngle += sweep;
            }

            // Legend
            int legendX = pieRect.Right + 20;
            int legendY = pieRect.Top;
            const int swatchSize = 14;
            const int rowHeight = 20;

            using var legendBrush = new SolidBrush(textColor);

            int row = 0;
            foreach (PieSlice slice in _slices)
            {
                int y = legendY + row * rowHeight;
                if (y + rowHeight > Height)
                {
                    break;
                }

                using (var swatchBrush = new SolidBrush(slice.Color))
                {
                    g.FillRectangle(swatchBrush, legendX, y + 2, swatchSize, swatchSize);
                }

                double percent = 100.0 * slice.Count / total;
                g.DrawString($"{slice.Label} ({slice.Count}, {percent:F0}%)", Font, legendBrush, legendX + swatchSize + 6, y);

                row++;
            }
        }
    }
}
