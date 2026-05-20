namespace RuiSource.Forms
{
    public sealed class PsdPlotForm : Form
    {
        private readonly (string Name, double[] Frequencies, double[] Power)[] _baseSeries;
        private readonly Panel _plotPanel;
        private readonly FlowLayoutPanel _legendPanel;
        private readonly HashSet<int> _visibleSeriesIndexes;
        private string _normalization;

        public PsdPlotForm((string Name, double[] Frequencies, double[] Power)[] series, string normalization)
        {
            _baseSeries = series;
            _normalization = normalization;
            _visibleSeriesIndexes = Enumerable.Range(0, series.Length).ToHashSet();
            Text = "PSD";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(900, 520);

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));

            _plotPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            _plotPanel.Paint += Panel_Paint;
            _plotPanel.Resize += (_, _) => _plotPanel.Invalidate();

            var optionsPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(8),
                ColumnCount = 1,
                RowCount = 3
            };
            optionsPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            optionsPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            optionsPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var normalizationLabel = new Label
            {
                Text = "Normalization",
                AutoSize = true,
                Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold),
                Margin = new Padding(3, 3, 3, 8)
            };

            var normalizationPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true
            };

            foreach (var option in new[] { "None", "Log10", "Z-score", "Percentage" })
            {
                var button = new RadioButton
                {
                    Text = option,
                    AutoSize = true,
                    Checked = option == normalization,
                    Margin = new Padding(3, 3, 3, 6)
                };
                button.CheckedChanged += (_, _) =>
                {
                    if (!button.Checked)
                    {
                        return;
                    }

                    _normalization = option;
                    _plotPanel.Invalidate();
                };
                normalizationPanel.Controls.Add(button);
            }

            var legendTitle = new Label
            {
                Text = "Legend",
                AutoSize = true,
                Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold),
                Margin = new Padding(3, 10, 3, 6)
            };

            _legendPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(6)
            };

            optionsPanel.Controls.Add(normalizationLabel, 0, 0);
            optionsPanel.Controls.Add(normalizationPanel, 0, 1);
            optionsPanel.Controls.Add(CreateLegendHost(legendTitle, _legendPanel), 0, 2);

            RebuildLegend();

            mainLayout.Controls.Add(_plotPanel, 0, 0);
            mainLayout.Controls.Add(optionsPanel, 1, 0);
            Controls.Add(mainLayout);
        }

        private void Panel_Paint(object? sender, PaintEventArgs e)
        {
            var series = Normalize(_baseSeries, _normalization)
                .Select((item, index) => new { Item = item, Index = index })
                .Where(item => _visibleSeriesIndexes.Contains(item.Index))
                .Select(item => item.Item)
                .ToArray();
            var bounds = _plotPanel.ClientRectangle;
            e.Graphics.Clear(Color.White);
            if (series.Length == 0)
            {
                TextRenderer.DrawText(e.Graphics, "No PSD channels selected.", SystemFonts.DefaultFont, bounds, Color.DimGray,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                return;
            }

            const int left = 70;
            const int top = 20;
            const int rightPadding = 20;
            const int bottomPadding = 40;
            var width = Math.Max(1, bounds.Width - left - rightPadding);
            var height = Math.Max(1, bounds.Height - top - bottomPadding);
            using var axisPen = new Pen(Color.Gray, 1f);
            e.Graphics.DrawRectangle(axisPen, left, top, width, height);

            var minFreq = series.Min(s => s.Frequencies.FirstOrDefault());
            var maxFreq = series.Max(s => s.Frequencies.LastOrDefault());
            var minPower = series.Min(s => s.Power.DefaultIfEmpty(0d).Min());
            var maxPower = series.Max(s => s.Power.DefaultIfEmpty(1d).Max());
            var powerRange = Math.Max(double.Epsilon, maxPower - minPower);
            var colors = new[] { Color.MidnightBlue, Color.DarkGreen, Color.DarkRed, Color.DarkOrange, Color.Purple, Color.Teal, Color.Brown, Color.Magenta, Color.Navy, Color.Olive };

            for (var seriesIndex = 0; seriesIndex < series.Length; seriesIndex++)
            {
                var currentSeries = series[seriesIndex];
                if (currentSeries.Frequencies.Length < 2 || currentSeries.Power.Length < 2)
                {
                    continue;
                }

                var points = new PointF[currentSeries.Frequencies.Length];
                for (var i = 0; i < currentSeries.Frequencies.Length; i++)
                {
                    var x = left + (float)((currentSeries.Frequencies[i] - minFreq) / Math.Max(double.Epsilon, maxFreq - minFreq) * width);
                    var y = top + height - (float)((currentSeries.Power[i] - minPower) / powerRange * height);
                    points[i] = new PointF(x, y);
                }

                using var pen = new Pen(colors[seriesIndex % colors.Length], 1.2f);
                e.Graphics.DrawLines(pen, points);
            }

            var yAxisLabel = _normalization == "None" ? "Power" : _normalization;
            TextRenderer.DrawText(e.Graphics, yAxisLabel, SystemFonts.DefaultFont, new Point(6, top), Color.DimGray);
            TextRenderer.DrawText(e.Graphics, $"{minFreq:F1} Hz", SystemFonts.DefaultFont, new Point(left, top + height + 4), Color.DimGray);
            TextRenderer.DrawText(e.Graphics, $"{maxFreq:F1} Hz", SystemFonts.DefaultFont, new Point(left + width - 60, top + height + 4), Color.DimGray);
        }

        private Control CreateLegendHost(Control title, Control legendPanel)
        {
            var host = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };
            host.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            host.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            host.Controls.Add(title, 0, 0);
            host.Controls.Add(legendPanel, 0, 1);
            return host;
        }

        private void RebuildLegend()
        {
            _legendPanel.SuspendLayout();
            _legendPanel.Controls.Clear();
            var colors = new[] { Color.MidnightBlue, Color.DarkGreen, Color.DarkRed, Color.DarkOrange, Color.Purple, Color.Teal, Color.Brown, Color.Magenta, Color.Navy, Color.Olive };

            for (var index = 0; index < _baseSeries.Length; index++)
            {
                var legendIndex = index;
                var legendColor = colors[legendIndex % colors.Length];
                var legendName = _baseSeries[legendIndex].Name;

                var item = new CheckBox
                {
                    Width = 120,
                    Height = 22,
                    Margin = new Padding(0, 0, 0, 4),
                    Text = legendName,
                    Checked = true,
                    AutoEllipsis = true,
                    Padding = new Padding(18, 0, 0, 0)
                };

                item.Paint += (_, e) =>
                {
                    using var colorBrush = new SolidBrush(legendColor);
                    e.Graphics.FillRectangle(colorBrush, 1, 6, 12, 10);
                    e.Graphics.DrawRectangle(Pens.Gray, 1, 6, 12, 10);
                };
                item.CheckedChanged += (_, _) =>
                {
                    if (item.Checked)
                    {
                        _visibleSeriesIndexes.Add(legendIndex);
                    }
                    else
                    {
                        _visibleSeriesIndexes.Remove(legendIndex);
                    }

                    _plotPanel.Invalidate();
                };

                _legendPanel.Controls.Add(item);
            }

            _legendPanel.ResumeLayout();
        }

        private static (string Name, double[] Frequencies, double[] Power)[] Normalize((string Name, double[] Frequencies, double[] Power)[] series, string normalization)
        {
            return normalization switch
            {
                "Log10" => series.Select(s => (s.Name, s.Frequencies, s.Power.Select(p => Math.Log10(Math.Max(double.Epsilon, p))).ToArray())).ToArray(),
                "Z-score" => series.Select(s =>
                {
                    var mean = s.Power.Average();
                    var stdDev = Math.Sqrt(s.Power.Select(p => Math.Pow(p - mean, 2)).Average());
                    var normalized = stdDev > double.Epsilon ? s.Power.Select(p => (p - mean) / stdDev).ToArray() : s.Power.ToArray();
                    return (s.Name, s.Frequencies, normalized);
                }).ToArray(),
                "Percentage" => series.Select(s =>
                {
                    var sum = s.Power.Sum();
                    var normalized = sum > double.Epsilon ? s.Power.Select(p => (p / sum) * 100d).ToArray() : s.Power.ToArray();
                    return (s.Name, s.Frequencies, normalized);
                }).ToArray(),
                _ => series.Select(s => (s.Name, s.Frequencies, s.Power.ToArray())).ToArray()
            };
        }
    }
}
