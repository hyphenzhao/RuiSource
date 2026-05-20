namespace RuiSource.Forms
{
    public sealed class TfrPlotForm : Form
    {
        private readonly Models.TfrResult[] _results;
        private readonly FlowLayoutPanel _plotsPanel;
        private readonly Panel _colorBarPanel;
        private readonly Dictionary<string, Bitmap> _bitmapCache = new();
        private string _normalization = "None";

        public TfrPlotForm(Models.TfrResultSet resultSet)
        {
            _results = resultSet.Results;
            Text = "TFR";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(1200, 760);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));

            _plotsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(8)
            };

            var sidePanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(8)
            };

            sidePanel.Controls.Add(new Label
            {
                Text = "Normalization",
                AutoSize = true,
                Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold),
                Margin = new Padding(3, 3, 3, 8)
            });

            foreach (var option in new[] { "None", "Log10", "Z-score", "Percentage" })
            {
                var button = new RadioButton
                {
                    Text = option,
                    AutoSize = true,
                    Checked = option == _normalization,
                    Margin = new Padding(3, 3, 3, 6)
                };
                button.CheckedChanged += (_, _) =>
                {
                    if (!button.Checked)
                    {
                        return;
                    }

                    _normalization = option;
                    ClearBitmapCache();
                    RebuildPlots();
                    _colorBarPanel.Invalidate();
                };
                sidePanel.Controls.Add(button);
            }

            sidePanel.Controls.Add(new Label
            {
                Text = "Color bar",
                AutoSize = true,
                Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold),
                Margin = new Padding(3, 10, 3, 6)
            });

            _colorBarPanel = new Panel
            {
                Width = 140,
                Height = 240,
                Margin = new Padding(3)
            };
            _colorBarPanel.Paint += ColorBarPanel_Paint;
            sidePanel.Controls.Add(_colorBarPanel);

            root.Controls.Add(_plotsPanel, 0, 0);
            root.Controls.Add(sidePanel, 1, 0);
            Controls.Add(root);

            RebuildPlots();
        }

        private void RebuildPlots()
        {
            _plotsPanel.SuspendLayout();
            _plotsPanel.Controls.Clear();

            foreach (var result in _results)
            {
                _plotsPanel.Controls.Add(CreateChannelPanel(result));
            }

            _plotsPanel.ResumeLayout();
        }

        private Control CreateChannelPanel(Models.TfrResult result)
        {
            var host = new TableLayoutPanel
            {
                Width = Math.Max(860, _plotsPanel.ClientSize.Width - 30),
                Height = 240,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, 10)
            };
            host.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            host.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 42));

            var plotPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Tag = result
            };
            plotPanel.Paint += ChannelPlotPanel_Paint;
            plotPanel.Resize += (_, _) => plotPanel.Invalidate();

            var magnifyButton = new Button
            {
                Dock = DockStyle.Top,
                Text = "🔍",
                Width = 34,
                Height = 30,
                Margin = new Padding(4)
            };
            magnifyButton.Click += (_, _) =>
            {
                using var dialog = new TfrSinglePlotForm(result, _normalization);
                dialog.ShowDialog(this);
            };

            host.Controls.Add(plotPanel, 0, 0);
            host.Controls.Add(magnifyButton, 1, 0);
            return host;
        }

        private void ChannelPlotPanel_Paint(object? sender, PaintEventArgs e)
        {
            if (sender is not Panel panel || panel.Tag is not Models.TfrResult result)
            {
                return;
            }

            DrawCachedTfr(e.Graphics, panel.ClientRectangle, result, _normalization, true);
        }

        private void ColorBarPanel_Paint(object? sender, PaintEventArgs e)
        {
            var bounds = ((Control)sender!).ClientRectangle;
            e.Graphics.Clear(SystemColors.Control);
            var barBounds = new Rectangle(20, 10, 24, Math.Max(1, bounds.Height - 40));
            for (var y = 0; y < barBounds.Height; y++)
            {
                var normalized = 1d - (y / (double)Math.Max(1, barBounds.Height - 1));
                using var pen = new Pen(GetSpectralRColor(normalized));
                e.Graphics.DrawLine(pen, barBounds.Left, barBounds.Top + y, barBounds.Right, barBounds.Top + y);
            }

            e.Graphics.DrawRectangle(Pens.Gray, barBounds);
            TextRenderer.DrawText(e.Graphics, "High", SystemFonts.DefaultFont, new Point(54, barBounds.Top - 2), Color.DimGray);
            TextRenderer.DrawText(e.Graphics, "Low", SystemFonts.DefaultFont, new Point(54, barBounds.Bottom - 16), Color.DimGray);
            TextRenderer.DrawText(e.Graphics, _normalization, SystemFonts.DefaultFont, new Point(20, barBounds.Bottom + 6), Color.Black);
        }

        internal static void DrawTfr(Graphics graphics, Rectangle bounds, Models.TfrResult result, string normalization, bool drawAxes)
        {
            graphics.Clear(Color.White);
            if (result.Frequencies.Length == 0 || result.Times.Length == 0 || result.Power.Length == 0)
            {
                return;
            }

            const int left = 70;
            const int top = 20;
            const int rightPadding = 20;
            const int bottomPadding = 45;
            var plotWidth = Math.Max(1, bounds.Width - left - rightPadding);
            var plotHeight = Math.Max(1, bounds.Height - top - bottomPadding);

            var normalizedPower = NormalizePower(result.Power, normalization);
            var minPower = normalizedPower.SelectMany(row => row).Min();
            var maxPower = normalizedPower.SelectMany(row => row).Max();
            var range = Math.Max(double.Epsilon, maxPower - minPower);

            for (var f = 0; f < result.Frequencies.Length; f++)
            {
                var row = normalizedPower[f];
                for (var t = 0; t < Math.Min(result.Times.Length, row.Length); t++)
                {
                    var normalized = (row[t] - minPower) / range;
                    using var brush = new SolidBrush(GetSpectralRColor(normalized));
                    var x = left + (int)Math.Floor(t * plotWidth / (double)result.Times.Length);
                    var y = top + plotHeight - (int)Math.Ceiling((f + 1) * plotHeight / (double)result.Frequencies.Length);
                    var cellWidth = Math.Max(1, (int)Math.Ceiling(plotWidth / (double)result.Times.Length));
                    var cellHeight = Math.Max(1, (int)Math.Ceiling(plotHeight / (double)result.Frequencies.Length));
                    graphics.FillRectangle(brush, x, y, cellWidth, cellHeight);
                }
            }

            if (!drawAxes)
            {
                return;
            }

            using var axisPen = new Pen(Color.Gray, 1f);
            graphics.DrawRectangle(axisPen, left, top, plotWidth, plotHeight);
            TextRenderer.DrawText(graphics, result.ChannelName, SystemFonts.DefaultFont, new Point(left, 0), Color.Black);
            TextRenderer.DrawText(graphics, $"{result.Times.First():F2} s", SystemFonts.DefaultFont, new Point(left, top + plotHeight + 4), Color.DimGray);
            TextRenderer.DrawText(graphics, $"{result.Times.Last():F2} s", SystemFonts.DefaultFont, new Point(left + plotWidth - 60, top + plotHeight + 4), Color.DimGray);
            TextRenderer.DrawText(graphics, $"{result.Frequencies.First():F1} Hz", SystemFonts.DefaultFont, new Point(4, top + plotHeight - 18), Color.DimGray);
            TextRenderer.DrawText(graphics, $"{result.Frequencies.Last():F1} Hz", SystemFonts.DefaultFont, new Point(4, top), Color.DimGray);
        }

        private void DrawCachedTfr(Graphics graphics, Rectangle bounds, Models.TfrResult result, string normalization, bool drawAxes)
        {
            var cacheKey = $"{result.ChannelName}|{normalization}|{bounds.Width}|{bounds.Height}|{drawAxes}";
            if (!_bitmapCache.TryGetValue(cacheKey, out var bitmap))
            {
                bitmap = new Bitmap(Math.Max(1, bounds.Width), Math.Max(1, bounds.Height));
                using var bitmapGraphics = Graphics.FromImage(bitmap);
                DrawTfr(bitmapGraphics, new Rectangle(Point.Empty, bitmap.Size), result, normalization, drawAxes);
                _bitmapCache[cacheKey] = bitmap;
            }

            graphics.DrawImageUnscaled(bitmap, bounds.Location);
        }

        private void ClearBitmapCache()
        {
            foreach (var bitmap in _bitmapCache.Values)
            {
                bitmap.Dispose();
            }

            _bitmapCache.Clear();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ClearBitmapCache();
            }

            base.Dispose(disposing);
        }

        private static double[][] NormalizePower(double[][] power, string normalization)
        {
            var flattened = power.SelectMany(row => row).ToArray();
            var mean = flattened.Length > 0 ? flattened.Average() : 0d;
            var stdDev = flattened.Length > 0 ? Math.Sqrt(flattened.Select(value => Math.Pow(value - mean, 2)).Average()) : 0d;
            var sum = flattened.Sum();

            return normalization switch
            {
                "Log10" => power.Select(row => row.Select(value => Math.Log10(Math.Max(double.Epsilon, value))).ToArray()).ToArray(),
                "Z-score" => stdDev > double.Epsilon
                    ? power.Select(row => row.Select(value => (value - mean) / stdDev).ToArray()).ToArray()
                    : power.Select(row => row.ToArray()).ToArray(),
                "Percentage" => sum > double.Epsilon
                    ? power.Select(row => row.Select(value => (value / sum) * 100d).ToArray()).ToArray()
                    : power.Select(row => row.ToArray()).ToArray(),
                _ => power.Select(row => row.ToArray()).ToArray()
            };
        }

        private static Color GetSpectralRColor(double value)
        {
            value = Math.Clamp(value, 0d, 1d);
            var palette = new[]
            {
                Color.FromArgb(94, 79, 162),
                Color.FromArgb(50, 136, 189),
                Color.FromArgb(102, 194, 165),
                Color.FromArgb(171, 221, 164),
                Color.FromArgb(230, 245, 152),
                Color.FromArgb(255, 255, 191),
                Color.FromArgb(254, 224, 139),
                Color.FromArgb(253, 174, 97),
                Color.FromArgb(244, 109, 67),
                Color.FromArgb(213, 62, 79),
                Color.FromArgb(158, 1, 66)
            };

            var scaled = value * (palette.Length - 1);
            var index = Math.Min(palette.Length - 2, (int)Math.Floor(scaled));
            var fraction = scaled - index;
            var start = palette[index];
            var end = palette[index + 1];
            return Color.FromArgb(
                (int)(start.R + ((end.R - start.R) * fraction)),
                (int)(start.G + ((end.G - start.G) * fraction)),
                (int)(start.B + ((end.B - start.B) * fraction)));
        }
    }

    internal sealed class TfrSinglePlotForm : Form
    {
        private readonly Models.TfrResult _result;
        private readonly string _normalization;
        private Bitmap? _bitmap;

        public TfrSinglePlotForm(Models.TfrResult result, string normalization)
        {
            _result = result;
            _normalization = normalization;
            Text = $"TFR - {result.ChannelName}";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(1100, 760);

            var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            panel.Paint += (_, e) =>
            {
                var size = panel.ClientSize;
                if (_bitmap is null || _bitmap.Width != Math.Max(1, size.Width) || _bitmap.Height != Math.Max(1, size.Height))
                {
                    _bitmap?.Dispose();
                    _bitmap = new Bitmap(Math.Max(1, size.Width), Math.Max(1, size.Height));
                    using var bitmapGraphics = Graphics.FromImage(_bitmap);
                    TfrPlotForm.DrawTfr(bitmapGraphics, new Rectangle(Point.Empty, _bitmap.Size), _result, _normalization, true);
                }

                e.Graphics.DrawImageUnscaled(_bitmap, Point.Empty);
            };
            panel.Resize += (_, _) => panel.Invalidate();
            Controls.Add(panel);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _bitmap?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
