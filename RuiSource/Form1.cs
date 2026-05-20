using RuiSource.Models;
using RuiSource.Forms;
using RuiSource.Services;

namespace RuiSource
{
    public partial class Form1 : Form
    {
        private const int VisibleChannelCount = 10;
        private const int LabelAreaWidth = 150;
        private const double VisibleTimeWindowSeconds = 10d;
        private const int ScaleRulerWidth = 42;
        private const double VoltageZoomStep = 1.2d;
        private EdfFile? _loadedFile;
        private int _channelStartIndex;
        private double _timeOffsetSeconds;
        private double _voltageZoom = 1d;
        private double? _lowCutHz;
        private double? _highCutHz;
        private double? _notchHz;
        private readonly Dictionary<string, double[]> _filteredSignals = new();
        private HashSet<int> _visibleChannelIndexes = new();
        private readonly Dictionary<int, string> _channelNames = new();
        private IReadOnlyList<ElectrodePosition> _matchedElectrodes = Array.Empty<ElectrodePosition>();
        private Image? _sourcePreviewImage;
        private string _sourcePreviewMessage = "Open an EDF file to render source preview.";
        private int _sourcePreviewRenderVersion;
        private bool _standardMriLoaded;
        private AppConfiguration _configuration;
        private bool _isSelectingRange;
        private double? _selectionStartSeconds;
        private double? _selectionEndSeconds;

        public Form1()
        {
            InitializeComponent();
            _configuration = AppConfigurationService.Load();
        }

        private async void openEdfToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Filter = "EDF files (*.edf)|*.edf|All files (*.*)|*.*",
                Title = "Open EDF file"
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            openEdfToolStripMenuItem.Enabled = false;
            statusLabel.Text = $"Loading {Path.GetFileName(dialog.FileName)}...";

            try
            {
                var loadedFile = await Task.Run(() => EdfLoader.Load(dialog.FileName));
                _loadedFile = loadedFile;
                _channelStartIndex = 0;
                _timeOffsetSeconds = 0;
                _voltageZoom = 1d;
                _filteredSignals.Clear();
                SetDefaultFilterValues();
                _lowCutHz = null;
                _highCutHz = null;
                _notchHz = null;
                _visibleChannelIndexes = Enumerable.Range(0, loadedFile.Signals.Count).ToHashSet();
                _channelNames.Clear();
                _selectionStartSeconds = null;
                _selectionEndSeconds = null;
                computePsdButton.Visible = false;
                computeTfrButton.Visible = false;
                for (var index = 0; index < loadedFile.Signals.Count; index++)
                {
                    _channelNames[index] = loadedFile.Signals[index].Label;
                }

                LoadStandardSourceViews(loadedFile);
                _ = RenderMneSourcePreviewAsync(loadedFile, ++_sourcePreviewRenderVersion);

                statusLabel.Text = BuildStatusText();
                plotPanel.Invalidate();
            }
            catch (Exception ex)
            {
                _loadedFile = null;
                _channelStartIndex = 0;
                _timeOffsetSeconds = 0;
                _voltageZoom = 1d;
                _filteredSignals.Clear();
                _visibleChannelIndexes.Clear();
                _channelNames.Clear();
                ClearSourceViews();
                _selectionStartSeconds = null;
                _selectionEndSeconds = null;
                computePsdButton.Visible = false;
                computeTfrButton.Visible = false;
                plotPanel.Invalidate();
                statusLabel.Text = $"Failed to load EDF: {ex.Message}";
            }
            finally
            {
                openEdfToolStripMenuItem.Enabled = true;
            }
        }

        private void plotPanel_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.Clear(Color.White);

            var bounds = plotPanel.ClientRectangle;
            if (bounds.Width <= 1 || bounds.Height <= 1)
            {
                return;
            }

            using var axisPen = new Pen(Color.LightGray, 1f);
            var plotLeft = LabelAreaWidth;
            var plotTop = 20;
            var plotWidth = Math.Max(1, bounds.Width - plotLeft - 20);
            var plotHeight = Math.Max(1, bounds.Height - 50);
            e.Graphics.DrawRectangle(axisPen, plotLeft, plotTop, plotWidth, plotHeight);
            e.Graphics.DrawLine(axisPen, LabelAreaWidth, 0, LabelAreaWidth, bounds.Height);

            if (_loadedFile is null || _loadedFile.Signals.Count == 0)
            {
                TextRenderer.DrawText(e.Graphics, "Open an EDF file from File > Open EDF.", Font, bounds, Color.DimGray,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                return;
            }

            var visibleSignals = _loadedFile.Signals
                .Select((signal, index) => new { Signal = signal, Index = index })
                .Where(item => _visibleChannelIndexes.Contains(item.Index))
                .Skip(_channelStartIndex)
                .Take(VisibleChannelCount)
                .ToArray();

            if (visibleSignals.Length == 0)
            {
                TextRenderer.DrawText(e.Graphics, "No visible channels selected. Use Channels... to choose channels.", Font, bounds, Color.DimGray,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                return;
            }

            var laneHeight = plotHeight / (float)Math.Max(1, visibleSignals.Length);
            using var signalPen = new Pen(Color.MidnightBlue, 1.1f);
            using var baselinePen = new Pen(Color.Gainsboro, 1f);
            using var rulerPen = new Pen(Color.DimGray, 1f);
            using var selectionBrush = new SolidBrush(Color.FromArgb(72, Color.Orange));

            DrawSelectionOverlay(e.Graphics, plotLeft, plotTop, plotWidth, plotHeight, selectionBrush);

            for (var channelOffset = 0; channelOffset < visibleSignals.Length; channelOffset++)
            {
                var signal = visibleSignals[channelOffset].Signal;
                var signalIndex = visibleSignals[channelOffset].Index;
                var plottedSamples = GetDisplayedSamples(signal);
                if (plottedSamples.Length == 0)
                {
                    continue;
                }

                var startSampleIndex = signal.SamplesPerSecond > 0
                    ? Math.Clamp((int)Math.Floor(_timeOffsetSeconds * signal.SamplesPerSecond), 0, Math.Max(0, plottedSamples.Length - 1))
                    : 0;
                var requestedSampleCount = signal.SamplesPerSecond > 0
                    ? Math.Max(2, (int)Math.Ceiling(VisibleTimeWindowSeconds * signal.SamplesPerSecond))
                    : plottedSamples.Length;
                var endSampleIndexExclusive = Math.Min(plottedSamples.Length, startSampleIndex + requestedSampleCount);
                var visibleSampleCount = endSampleIndexExclusive - startSampleIndex;
                if (visibleSampleCount < 2)
                {
                    continue;
                }

                var laneTop = plotTop + (channelOffset * laneHeight);
                var laneCenter = laneTop + (laneHeight / 2f);
                e.Graphics.DrawLine(baselinePen, plotLeft, laneCenter, plotLeft + plotWidth, laneCenter);

                TextRenderer.DrawText(e.Graphics, GetChannelDisplayName(signalIndex, signal.Label),
                    Font,
                    new Rectangle(ScaleRulerWidth + 8, (int)laneTop, LabelAreaWidth - ScaleRulerWidth - 12, (int)laneHeight),
                    Color.Black,
                    TextFormatFlags.EndEllipsis | TextFormatFlags.Right | TextFormatFlags.VerticalCenter);

                var min = plottedSamples[startSampleIndex];
                var max = plottedSamples[startSampleIndex];
                for (var sampleIndex = startSampleIndex + 1; sampleIndex < endSampleIndexExclusive; sampleIndex++)
                {
                    var sampleValue = plottedSamples[sampleIndex];
                    if (sampleValue < min)
                    {
                        min = sampleValue;
                    }

                    if (sampleValue > max)
                    {
                        max = sampleValue;
                    }
                }

                var range = max - min;
                if (range <= 0)
                {
                    range = 1;
                }

                var displayedRange = range / _voltageZoom;
                var laneMidValue = (max + min) / 2d;
                var displayedMax = laneMidValue + (displayedRange / 2d);
                var displayedMin = laneMidValue - (displayedRange / 2d);

                DrawScaleRuler(e.Graphics, rulerPen, laneTop, laneHeight, displayedMax, displayedMin);

                var pointCount = Math.Min(plotWidth, visibleSampleCount);
                if (pointCount < 2)
                {
                    continue;
                }

                var points = new PointF[pointCount];
                var stride = visibleSampleCount / (double)pointCount;
                var amplitudeScale = Math.Max(1f, (laneHeight - 10f) / 2f);

                for (var pointIndex = 0; pointIndex < pointCount; pointIndex++)
                {
                    var sampleIndex = Math.Min(endSampleIndexExclusive - 1, startSampleIndex + (int)Math.Floor(pointIndex * stride));
                    var value = plottedSamples[sampleIndex];
                    var normalized = (value - laneMidValue) / Math.Max(double.Epsilon, displayedRange / 2d);
                    normalized = Math.Clamp(normalized, -1d, 1d);
                    var x = plotLeft + (pointIndex / (float)(pointCount - 1)) * plotWidth;
                    var y = laneCenter - (float)normalized * amplitudeScale;
                    points[pointIndex] = new PointF(x, y);
                }

                e.Graphics.DrawLines(signalPen, points);
            }

            var firstSignal = visibleSignals[0].Signal;
            var totalDurationSeconds = firstSignal.SamplesPerSecond > 0 ? firstSignal.Samples.Length / firstSignal.SamplesPerSecond : 0;
            var visibleEndTimeSeconds = Math.Min(totalDurationSeconds, _timeOffsetSeconds + VisibleTimeWindowSeconds);
            var visibleChannelCount = _visibleChannelIndexes.Count;
            var visibleStart = visibleChannelCount == 0 ? 0 : _channelStartIndex + 1;
            var visibleEnd = Math.Min(visibleChannelCount, _channelStartIndex + visibleSignals.Length);
            TextRenderer.DrawText(e.Graphics,
                $"Channels {visibleStart}-{visibleEnd} of {visibleChannelCount}",
                Font,
                new Point(plotLeft, 0),
                Color.Black);
            TextRenderer.DrawText(e.Graphics, $"{_timeOffsetSeconds:F2} s", Font, new Point(plotLeft, plotTop + plotHeight + 4), Color.DimGray);
            TextRenderer.DrawText(e.Graphics, $"{visibleEndTimeSeconds:F2} s", Font, new Point(plotLeft + plotWidth - 70, plotTop + plotHeight + 4), Color.DimGray);
        }

        private void plotPanel_Resize(object sender, EventArgs e)
        {
            plotPanel.Invalidate();
        }

        private void electrodeViewPanel_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.Clear(Color.White);
            if (_loadedFile is null || _matchedElectrodes.Count == 0)
            {
                return;
            }

            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            var bounds = electrodeViewPanel.ClientRectangle;
            if (bounds.Width <= 1 || bounds.Height <= 1)
            {
                return;
            }

            var center = new PointF(bounds.Width / 2f, bounds.Height / 2f + 6f);
            var radius = Math.Max(20f, Math.Min(bounds.Width, bounds.Height) * 0.34f);
            using var headPen = new Pen(Color.LightSlateGray, 1.2f);
            using var gridPen = new Pen(Color.Gainsboro, 1f);
            using var electrodeBrush = new SolidBrush(Color.RoyalBlue);
            using var labelBrush = new SolidBrush(Color.Black);
            using var axisBrush = new SolidBrush(Color.DimGray);

            e.Graphics.DrawEllipse(headPen, center.X - radius, center.Y - radius, radius * 2f, radius * 2f);
            e.Graphics.DrawEllipse(gridPen, center.X - radius * 0.72f, center.Y - radius * 0.72f, radius * 1.44f, radius * 1.44f);
            e.Graphics.DrawLine(gridPen, center.X, center.Y - radius, center.X, center.Y + radius);
            e.Graphics.DrawLine(gridPen, center.X - radius, center.Y, center.X + radius, center.Y);
            DrawNose(e.Graphics, headPen, center, radius);

            foreach (var electrode in _matchedElectrodes.OrderBy(item => item.Z))
            {
                var point = ProjectElectrode(electrode, center, radius);
                e.Graphics.FillEllipse(electrodeBrush, point.X - 3.5f, point.Y - 3.5f, 7f, 7f);
                e.Graphics.DrawEllipse(Pens.White, point.X - 3.5f, point.Y - 3.5f, 7f, 7f);
                TextRenderer.DrawText(e.Graphics, electrode.CanonicalName, Font, new Point((int)point.X + 4, (int)point.Y - 7), Color.Black);
            }

            TextRenderer.DrawText(e.Graphics, $"Matched {_matchedElectrodes.Count}/{_loadedFile.Signals.Count}", Font, new Point(6, 6), Color.DimGray);
            TextRenderer.DrawText(e.Graphics, "L", Font, new Point((int)(center.X - radius - 16), (int)center.Y - 8), Color.DimGray);
            TextRenderer.DrawText(e.Graphics, "R", Font, new Point((int)(center.X + radius + 5), (int)center.Y - 8), Color.DimGray);
            TextRenderer.DrawText(e.Graphics, "Nasion", Font, new Point((int)center.X - 22, (int)(center.Y - radius - 24)), Color.DimGray);
        }

        private void electrodeViewPanel_Resize(object sender, EventArgs e)
        {
            electrodeViewPanel.Invalidate();
        }

        private void mriViewPanel_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.Clear(Color.White);
            var bounds = mriViewPanel.ClientRectangle;
            if (bounds.Width <= 1 || bounds.Height <= 1)
            {
                return;
            }

            if (_loadedFile is null || !_standardMriLoaded)
            {
                TextRenderer.DrawText(e.Graphics, _sourcePreviewMessage, Font, bounds, Color.DimGray,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak);
                return;
            }

            if (_sourcePreviewImage is not null)
            {
                TextRenderer.DrawText(e.Graphics, _sourcePreviewMessage, Font, new Point(6, 6), Color.DimGray);
                return;
            }

            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            var center = new PointF(bounds.Width / 2f, bounds.Height / 2f + 4f);
            var scale = Math.Max(20f, Math.Min(bounds.Width, bounds.Height) * 0.34f);
            var headBounds = new RectangleF(center.X - scale * 0.72f, center.Y - scale, scale * 1.44f, scale * 1.90f);
            var brainBounds = new RectangleF(center.X - scale * 0.54f, center.Y - scale * 0.65f, scale * 1.08f, scale * 1.12f);

            using var headBrush = new SolidBrush(Color.FromArgb(232, 232, 232));
            using var headPen = new Pen(Color.Gray, 1.2f);
            using var brainBrush = new SolidBrush(Color.FromArgb(210, 225, 245));
            using var brainPen = new Pen(Color.SteelBlue, 1.2f);
            using var midlinePen = new Pen(Color.FromArgb(120, Color.DimGray), 1f) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };

            e.Graphics.FillEllipse(headBrush, headBounds);
            e.Graphics.DrawEllipse(headPen, headBounds);
            e.Graphics.FillEllipse(brainBrush, brainBounds);
            e.Graphics.DrawEllipse(brainPen, brainBounds);
            e.Graphics.DrawLine(midlinePen, center.X, headBounds.Top + 8f, center.X, headBounds.Bottom - 8f);
            e.Graphics.DrawArc(brainPen, brainBounds.Left + brainBounds.Width * 0.14f, brainBounds.Top + brainBounds.Height * 0.18f, brainBounds.Width * 0.72f, brainBounds.Height * 0.34f, 200f, 140f);
            e.Graphics.DrawArc(brainPen, brainBounds.Left + brainBounds.Width * 0.16f, brainBounds.Top + brainBounds.Height * 0.50f, brainBounds.Width * 0.68f, brainBounds.Height * 0.30f, 20f, 140f);

            DrawOrientationAxes(e.Graphics, bounds);
            TextRenderer.DrawText(e.Graphics, _sourcePreviewMessage, Font, new Rectangle(6, 6, bounds.Width - 12, Font.Height * 3), Color.DimGray,
                TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis);
            TextRenderer.DrawText(e.Graphics, "Native fallback preview", Font, new Point(6, bounds.Height - Font.Height - 6), Color.DimGray);
        }

        private void mriViewPanel_Resize(object sender, EventArgs e)
        {
            mriViewPanel.Invalidate();
        }

        private void plotPanel_MouseEnter(object sender, EventArgs e)
        {
            plotPanel.Focus();
        }

        private void plotPanel_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || _loadedFile is null || e.X <= LabelAreaWidth)
            {
                return;
            }

            _isSelectingRange = true;
            var time = GetTimeFromPlotX(e.X);
            _selectionStartSeconds = time;
            _selectionEndSeconds = time;
            computePsdButton.Visible = false;
            computeTfrButton.Visible = false;
            plotPanel.Invalidate();
        }

        private void plotPanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isSelectingRange)
            {
                return;
            }

            _selectionEndSeconds = GetTimeFromPlotX(e.X);
            plotPanel.Invalidate();
        }

        private void plotPanel_MouseUp(object sender, MouseEventArgs e)
        {
            if (!_isSelectingRange)
            {
                return;
            }

            _isSelectingRange = false;
            _selectionEndSeconds = GetTimeFromPlotX(e.X);
            if (!HasValidSelection())
            {
                _selectionStartSeconds = null;
                _selectionEndSeconds = null;
                computePsdButton.Visible = false;
                computeTfrButton.Visible = false;
            }
            else
            {
                computePsdButton.Visible = true;
                computeTfrButton.Visible = true;
            }

            statusLabel.Text = BuildStatusText();
            plotPanel.Invalidate();
        }

        private void plotPanel_MouseWheel(object sender, MouseEventArgs e)
        {
            if (_loadedFile is null)
            {
                return;
            }

            var delta = e.Delta > 0 ? -1 : 1;

            if (e.X <= LabelAreaWidth)
            {
                var visibleChannelCount = _visibleChannelIndexes.Count;
                if (visibleChannelCount <= VisibleChannelCount)
                {
                    return;
                }

                var maxStartIndex = Math.Max(0, visibleChannelCount - VisibleChannelCount);
                var newStartIndex = Math.Clamp(_channelStartIndex + delta, 0, maxStartIndex);
                if (newStartIndex == _channelStartIndex)
                {
                    return;
                }

                _channelStartIndex = newStartIndex;
                statusLabel.Text = BuildStatusText();
                plotPanel.Invalidate();
                return;
            }

            if ((ModifierKeys & Keys.Control) == Keys.Control)
            {
                _voltageZoom = delta < 0
                    ? Math.Min(100d, _voltageZoom * VoltageZoomStep)
                    : Math.Max(0.1d, _voltageZoom / VoltageZoomStep);
                statusLabel.Text = BuildStatusText();
                plotPanel.Invalidate();
                return;
            }

            var maxTimeOffsetSeconds = GetMaxTimeOffsetSeconds();
            var newTimeOffsetSeconds = Math.Clamp(_timeOffsetSeconds + delta, 0, maxTimeOffsetSeconds);
            if (Math.Abs(newTimeOffsetSeconds - _timeOffsetSeconds) < double.Epsilon)
            {
                return;
            }

            _timeOffsetSeconds = newTimeOffsetSeconds;
            statusLabel.Text = BuildStatusText();
            plotPanel.Invalidate();
        }

        private void computePsdButton_Click(object sender, EventArgs e)
        {
            if (_loadedFile is null || !HasValidSelection())
            {
                return;
            }

            var sampleRate = _loadedFile.Signals.FirstOrDefault()?.SamplesPerSecond ?? 0;
            using var dialog = new PsdSettingsForm(sampleRate);
            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            var selectedSignals = _loadedFile.Signals
                .Select((signal, index) => new { signal, index })
                .Where(item => _visibleChannelIndexes.Contains(item.index))
                .Skip(_channelStartIndex)
                .Take(VisibleChannelCount)
                .ToArray();

            var selectionStart = Math.Min(_selectionStartSeconds!.Value, _selectionEndSeconds!.Value);
            var selectionEnd = Math.Max(_selectionStartSeconds!.Value, _selectionEndSeconds!.Value);
            var series = new List<(string Name, double[] Frequencies, double[] Power)>();

            foreach (var item in selectedSignals)
            {
                var samples = GetDisplayedSamples(item.signal);
                var startIndex = Math.Clamp((int)Math.Floor(selectionStart * item.signal.SamplesPerSecond), 0, Math.Max(0, samples.Length - 1));
                var endIndex = Math.Clamp((int)Math.Ceiling(selectionEnd * item.signal.SamplesPerSecond), startIndex + 1, samples.Length);
                var length = endIndex - startIndex;
                if (length < 8)
                {
                    continue;
                }

                var segment = new double[length];
                Array.Copy(samples, startIndex, segment, 0, length);
                var (frequencies, power) = PsdService.ComputeWelch(segment, item.signal.SamplesPerSecond, dialog.NFft, dialog.Fmin, dialog.Fmax);
                if (frequencies.Length == 0)
                {
                    continue;
                }

                series.Add((GetChannelDisplayName(item.index, item.signal.Label), frequencies, power));
            }

            if (series.Count == 0)
            {
                statusLabel.Text = "Selected range is too short for PSD.";
                return;
            }

            using var plotForm = new PsdPlotForm(series.ToArray(), dialog.Normalization);
            plotForm.ShowDialog(this);
        }

        private async void computeTfrButton_Click(object sender, EventArgs e)
        {
            if (_loadedFile is null || !HasValidSelection())
            {
                return;
            }

            var selectedSignals = _loadedFile.Signals
                .Select((signal, index) => new { signal, index })
                .Where(item => _visibleChannelIndexes.Contains(item.index))
                .Skip(_channelStartIndex)
                .Take(VisibleChannelCount)
                .ToArray();

            if (selectedSignals.Length == 0)
            {
                statusLabel.Text = "No visible channel available for TFR.";
                return;
            }

            using var dialog = new TfrSettingsForm(selectedSignals[0].signal.SamplesPerSecond);
            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            try
            {
                computeTfrButton.Enabled = false;
                statusLabel.Text = "Computing TFR...";

                var selectionStart = Math.Min(_selectionStartSeconds!.Value, _selectionEndSeconds!.Value);
                var selectionEnd = Math.Max(_selectionStartSeconds!.Value, _selectionEndSeconds!.Value);
                var channelPayload = new List<(string ChannelName, double[] Samples, double SampleRate)>();
                foreach (var selectedSignal in selectedSignals)
                {
                    var samples = GetDisplayedSamples(selectedSignal.signal);
                    var startIndex = Math.Clamp((int)Math.Floor(selectionStart * selectedSignal.signal.SamplesPerSecond), 0, Math.Max(0, samples.Length - 1));
                    var endIndex = Math.Clamp((int)Math.Ceiling(selectionEnd * selectedSignal.signal.SamplesPerSecond), startIndex + 1, samples.Length);
                    var length = endIndex - startIndex;
                    if (length < 8)
                    {
                        continue;
                    }

                    var segment = new double[length];
                    Array.Copy(samples, startIndex, segment, 0, length);
                    channelPayload.Add((GetChannelDisplayName(selectedSignal.index, selectedSignal.signal.Label), segment, selectedSignal.signal.SamplesPerSecond));
                }

                if (channelPayload.Count == 0)
                {
                    statusLabel.Text = "Selected range is too short for TFR.";
                    return;
                }

                var projectRoot = Directory.GetParent(AppContext.BaseDirectory)?.Parent?.Parent?.Parent?.FullName ?? Directory.GetCurrentDirectory();
                var pythonPath = ResolvePythonPath(projectRoot);
                var scriptPath = ResolveTfrScriptPath(projectRoot);
                var workingDirectory = Path.GetDirectoryName(scriptPath) ?? Path.Combine(projectRoot, "RuiSource", "Python");
                var result = await PythonTfrService.ComputeMorletTfrAsync(
                    pythonPath,
                    scriptPath,
                    workingDirectory,
                    channelPayload.ToArray(),
                    selectionStart,
                    selectionEnd,
                    dialog.Fmin,
                    dialog.Fmax,
                    dialog.Fstep,
                    dialog.UseAutomaticNCycles,
                    dialog.UseAutomaticNCycles ? null : dialog.ManualNCycles);

                using var plotForm = new TfrPlotForm(result);
                statusLabel.Text = BuildStatusText();
                plotForm.ShowDialog(this);
            }
            catch (Exception ex)
            {
                statusLabel.Text = $"Failed to compute TFR: {ex.Message}";
            }
            finally
            {
                computeTfrButton.Enabled = true;
            }
        }

        private void configureToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using var dialog = new ConfigurationForm(_configuration.PythonPath, _configuration.PythonScriptsFolder);
            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            _configuration.PythonPath = dialog.PythonPath;
            _configuration.PythonScriptsFolder = dialog.PythonScriptsFolder;
            AppConfigurationService.Save(_configuration);
            statusLabel.Text = "Configuration saved.";
        }

        private async void applyFilterButton_Click(object sender, EventArgs e)
        {
            await ApplyCurrentFiltersAsync();
        }

        private void channelsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_loadedFile is null)
            {
                statusLabel.Text = "Open an EDF file before editing channels.";
                return;
            }

            using var dialog = new ChannelsForm(_loadedFile.Signals, _visibleChannelIndexes);
            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            var results = dialog.GetResults();
            var visibleIndexes = results.Where(result => result.IsVisible).Select(result => result.Index).ToHashSet();
            if (visibleIndexes.Count == 0)
            {
                statusLabel.Text = "At least one channel must remain visible.";
                return;
            }

            _visibleChannelIndexes = visibleIndexes;
            _channelNames.Clear();
            foreach (var result in results)
            {
                _channelNames[result.Index] = result.Name;
            }

            var maxStartIndex = Math.Max(0, _visibleChannelIndexes.Count - VisibleChannelCount);
            _channelStartIndex = Math.Clamp(_channelStartIndex, 0, maxStartIndex);
            RefreshStandardElectrodeMatches();
            statusLabel.Text = BuildStatusText();
            plotPanel.Invalidate();
        }

        private async void filtersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using var dialog = new FilterSettingsForm(lowCutComboBox.Text, highCutComboBox.Text, notchComboBox.Text);
            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            lowCutComboBox.Text = dialog.LowCutText;
            highCutComboBox.Text = dialog.HighCutText;
            notchComboBox.Text = dialog.NotchText;
            await ApplyCurrentFiltersAsync();
        }

        private async Task ApplyCurrentFiltersAsync()
        {
            if (_loadedFile is null)
            {
                statusLabel.Text = "Open an EDF file before applying filters.";
                return;
            }

            try
            {
                var lowCut = ParseFrequency(lowCutComboBox.Text, "low cut");
                var highCut = ParseFrequency(highCutComboBox.Text, "high cut");
                var notch = ParseFrequency(notchComboBox.Text, "notch");

                if (lowCut is not null && highCut is not null && lowCut >= highCut)
                {
                    throw new InvalidOperationException("Low cut must be lower than high cut.");
                }

                applyFilterButton.Enabled = false;
                statusLabel.Text = "Applying filters...";

                var filtered = await Task.Run(() => _loadedFile.Signals.ToDictionary(
                    signal => signal.Label,
                    signal => SignalFilterService.ApplyFilters(signal, lowCut, highCut, notch)));

                _lowCutHz = lowCut;
                _highCutHz = highCut;
                _notchHz = notch;
                _filteredSignals.Clear();
                foreach (var pair in filtered)
                {
                    _filteredSignals[pair.Key] = pair.Value;
                }

                statusLabel.Text = BuildStatusText();
                plotPanel.Invalidate();
            }
            catch (Exception ex)
            {
                statusLabel.Text = $"Failed to apply filters: {ex.Message}";
            }
            finally
            {
                applyFilterButton.Enabled = true;
            }
        }

        private string BuildStatusText()
        {
            if (_loadedFile is null)
            {
                return "Open an EDF file to view signals.";
            }

            var visibleChannelCount = _visibleChannelIndexes.Count;
            var visibleEnd = Math.Min(visibleChannelCount, _channelStartIndex + VisibleChannelCount);
            var visibleEndTimeSeconds = Math.Min(GetPrimarySignalDurationSeconds(), _timeOffsetSeconds + VisibleTimeWindowSeconds);
            var selectionText = HasValidSelection()
                ? $" | selection {Math.Min(_selectionStartSeconds!.Value, _selectionEndSeconds!.Value):F2}-{Math.Max(_selectionStartSeconds!.Value, _selectionEndSeconds!.Value):F2} s"
                : string.Empty;
            return $"Loaded {Path.GetFileName(_loadedFile.FilePath)} | channels {_channelStartIndex + 1}-{visibleEnd} of {visibleChannelCount} | time {_timeOffsetSeconds:F2}-{visibleEndTimeSeconds:F2} s{selectionText} | voltage zoom x{_voltageZoom:F2} | filters {FormatFilterSummary()}. Scroll over the left label area to browse channels, over the signals to move through time, drag to select time, and use Ctrl+wheel to change voltage scale.";
        }

        private double GetPrimarySignalDurationSeconds()
        {
            if (_loadedFile is null || _loadedFile.Signals.Count == 0)
            {
                return 0;
            }

            var signal = _loadedFile.Signals[0];
            return signal.SamplesPerSecond > 0 ? signal.Samples.Length / signal.SamplesPerSecond : 0;
        }

        private double GetMaxTimeOffsetSeconds()
        {
            return Math.Max(0, GetPrimarySignalDurationSeconds() - VisibleTimeWindowSeconds);
        }

        private void DrawScaleRuler(Graphics graphics, Pen rulerPen, float laneTop, float laneHeight, double maxValue, double minValue)
        {
            var rulerX = ScaleRulerWidth - 14;
            var rulerTop = laneTop + 6f;
            var rulerBottom = laneTop + laneHeight - 6f;
            var rulerCenter = laneTop + (laneHeight / 2f);

            graphics.DrawLine(rulerPen, rulerX, rulerTop, rulerX, rulerBottom);
            graphics.DrawLine(rulerPen, rulerX - 4, rulerTop, rulerX + 4, rulerTop);
            graphics.DrawLine(rulerPen, rulerX - 4, rulerCenter, rulerX + 4, rulerCenter);
            graphics.DrawLine(rulerPen, rulerX - 4, rulerBottom, rulerX + 4, rulerBottom);

            TextRenderer.DrawText(graphics,
                $"{maxValue:F1}",
                Font,
                new Rectangle(0, (int)rulerTop - 8, ScaleRulerWidth - 6, Font.Height),
                Color.DimGray,
                TextFormatFlags.Right);

            TextRenderer.DrawText(graphics,
                $"{minValue:F1}",
                Font,
                new Rectangle(0, (int)rulerBottom - Font.Height + 8, ScaleRulerWidth - 6, Font.Height),
                Color.DimGray,
                TextFormatFlags.Right);
        }

        private double[] GetDisplayedSamples(EdfSignal signal)
        {
            if (_filteredSignals.TryGetValue(signal.Label, out var filteredSamples))
            {
                return filteredSamples;
            }

            return signal.Samples;
        }

        private static double? ParseFrequency(string? text, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            var normalizedText = text.Trim();

            if (double.TryParse(normalizedText, out var value) && value > 0)
            {
                return value;
            }

            throw new InvalidOperationException($"Invalid {fieldName} frequency.");
        }

        private string FormatFilterSummary()
        {
            var lowCut = _lowCutHz is null ? "off" : $"LP>{_lowCutHz:F1}Hz";
            var highCut = _highCutHz is null ? "off" : $"HP<{_highCutHz:F1}Hz";
            var notch = _notchHz is null ? "off" : $"notch {_notchHz:F1}Hz";
            return $"low {lowCut}, high {highCut}, {notch}";
        }

        private void SetDefaultFilterValues()
        {
            lowCutComboBox.Text = "0.5";
            highCutComboBox.Text = "70";
            notchComboBox.Text = "50";
        }

        private string GetChannelDisplayName(int index, string fallbackName)
        {
            return _channelNames.TryGetValue(index, out var name) ? name : fallbackName;
        }

        private void LoadStandardSourceViews(EdfFile loadedFile)
        {
            DisposeSourcePreviewImage();
            sourcePreviewPictureBox.Visible = false;
            _sourcePreviewMessage = "Rendering MNE source preview...";

            for (var index = 0; index < loadedFile.Signals.Count; index++)
            {
                if (StandardElectrodePositionService.TryNormalizeChannelName(loadedFile.Signals[index].Label, out var canonicalName))
                {
                    _channelNames[index] = canonicalName;
                }
            }

            _matchedElectrodes = StandardElectrodePositionService.MatchChannels(_channelNames.OrderBy(pair => pair.Key).Select(pair => pair.Value));
            _standardMriLoaded = true;
            electrodeViewGroupBox.Enabled = true;
            mriViewGroupBox.Enabled = true;
            mriViewGroupBox.Text = "MNE source-space preview";
            electrodeViewPanel.Invalidate();
            mriViewPanel.Invalidate();
        }

        private void ClearSourceViews()
        {
            _sourcePreviewRenderVersion++;
            DisposeSourcePreviewImage();
            sourcePreviewPictureBox.Visible = false;
            _sourcePreviewMessage = "Open an EDF file to render source preview.";
            _matchedElectrodes = Array.Empty<ElectrodePosition>();
            _standardMriLoaded = false;
            electrodeViewGroupBox.Enabled = false;
            mriViewGroupBox.Enabled = false;
            mriViewGroupBox.Text = "MNE source-space preview";
            electrodeViewPanel.Invalidate();
            mriViewPanel.Invalidate();
        }

        private void RefreshStandardElectrodeMatches()
        {
            if (_loadedFile is null)
            {
                ClearSourceViews();
                return;
            }

            _matchedElectrodes = StandardElectrodePositionService.MatchChannels(_channelNames.OrderBy(pair => pair.Key).Select(pair => pair.Value));
            electrodeViewPanel.Invalidate();
        }

        private async Task RenderMneSourcePreviewAsync(EdfFile loadedFile, int renderVersion)
        {
            try
            {
                var projectRoot = Directory.GetParent(AppContext.BaseDirectory)?.Parent?.Parent?.Parent?.FullName ?? Directory.GetCurrentDirectory();
                var pythonPath = ResolvePythonPath(projectRoot);
                var scriptPath = ResolveSourcePreviewScriptPath(projectRoot);
                var workingDirectory = Path.GetDirectoryName(scriptPath) ?? Path.Combine(projectRoot, "RuiSource", "Python");
                var channelNames = loadedFile.Signals
                    .Select((signal, index) => GetChannelDisplayName(index, signal.Label))
                    .ToArray();

                var outputPath = await PythonSourcePreviewService.RenderPreviewAsync(
                    pythonPath,
                    scriptPath,
                    workingDirectory,
                    channelNames);

                if (renderVersion != _sourcePreviewRenderVersion || !ReferenceEquals(loadedFile, _loadedFile))
                {
                    return;
                }

                using var loadedImage = Image.FromFile(outputPath);
                SetSourcePreviewImage(new Bitmap(loadedImage));
                _sourcePreviewMessage = "MNE source preview rendered.";
            }
            catch (Exception ex)
            {
                if (renderVersion != _sourcePreviewRenderVersion || !ReferenceEquals(loadedFile, _loadedFile))
                {
                    return;
                }

                _sourcePreviewMessage = $"MNE preview unavailable: {ex.Message}";
                sourcePreviewPictureBox.Visible = false;
                mriViewPanel.Invalidate();
            }
        }

        private void SetSourcePreviewImage(Image image)
        {
            DisposeSourcePreviewImage();
            _sourcePreviewImage = image;
            sourcePreviewPictureBox.Image = _sourcePreviewImage;
            sourcePreviewPictureBox.Visible = true;
            mriViewPanel.Invalidate();
        }

        private void DisposeSourcePreviewImage()
        {
            sourcePreviewPictureBox.Image = null;
            _sourcePreviewImage?.Dispose();
            _sourcePreviewImage = null;
        }

        private static PointF ProjectElectrode(ElectrodePosition electrode, PointF center, float radius)
        {
            var depthScale = 0.82f + (float)Math.Clamp(electrode.Z, -0.2d, 1d) * 0.10f;
            var x = center.X + (float)electrode.X * radius * depthScale;
            var y = center.Y - (float)electrode.Y * radius * depthScale;
            return new PointF(x, y);
        }

        private static void DrawNose(Graphics graphics, Pen pen, PointF center, float radius)
        {
            var nose = new[]
            {
                new PointF(center.X - radius * 0.12f, center.Y - radius * 0.96f),
                new PointF(center.X, center.Y - radius * 1.16f),
                new PointF(center.X + radius * 0.12f, center.Y - radius * 0.96f)
            };
            graphics.DrawLines(pen, nose);
        }

        private void DrawOrientationAxes(Graphics graphics, Rectangle bounds)
        {
            var origin = new Point(bounds.Right - 62, bounds.Bottom - 42);
            using var axisPen = new Pen(Color.DimGray, 1.1f)
            {
                CustomEndCap = new System.Drawing.Drawing2D.AdjustableArrowCap(3f, 4f)
            };

            graphics.DrawLine(axisPen, origin, new Point(origin.X + 36, origin.Y));
            graphics.DrawLine(axisPen, origin, new Point(origin.X, origin.Y - 30));
            TextRenderer.DrawText(graphics, "R", Font, new Point(origin.X + 39, origin.Y - 9), Color.DimGray);
            TextRenderer.DrawText(graphics, "S", Font, new Point(origin.X - 5, origin.Y - 48), Color.DimGray);
        }

        private double GetTimeFromPlotX(int x)
        {
            var plotWidth = Math.Max(1, plotPanel.ClientRectangle.Width - LabelAreaWidth - 20);
            var normalized = Math.Clamp((x - LabelAreaWidth) / (double)plotWidth, 0d, 1d);
            return _timeOffsetSeconds + (normalized * VisibleTimeWindowSeconds);
        }

        private bool HasValidSelection()
        {
            return _selectionStartSeconds is not null && _selectionEndSeconds is not null && Math.Abs(_selectionEndSeconds.Value - _selectionStartSeconds.Value) > 0.01d;
        }

        private void DrawSelectionOverlay(Graphics graphics, int plotLeft, int plotTop, int plotWidth, int plotHeight, Brush selectionBrush)
        {
            if (!HasValidSelection())
            {
                return;
            }

            var selectionStart = Math.Min(_selectionStartSeconds!.Value, _selectionEndSeconds!.Value);
            var selectionEnd = Math.Max(_selectionStartSeconds!.Value, _selectionEndSeconds!.Value);
            var leftRatio = (selectionStart - _timeOffsetSeconds) / VisibleTimeWindowSeconds;
            var rightRatio = (selectionEnd - _timeOffsetSeconds) / VisibleTimeWindowSeconds;
            var left = plotLeft + (int)(Math.Clamp(leftRatio, 0d, 1d) * plotWidth);
            var right = plotLeft + (int)(Math.Clamp(rightRatio, 0d, 1d) * plotWidth);
            if (right <= left)
            {
                return;
            }

            graphics.FillRectangle(selectionBrush, left, plotTop, right - left, plotHeight);
        }

        private string ResolvePythonPath(string projectRoot)
        {
            if (!string.IsNullOrWhiteSpace(_configuration.PythonPath))
            {
                return _configuration.PythonPath;
            }

            return Path.Combine(projectRoot, "venv", "Scripts", "python.exe");
        }

        private string ResolveTfrScriptPath(string projectRoot)
        {
            if (!string.IsNullOrWhiteSpace(_configuration.PythonScriptsFolder))
            {
                var configuredScriptPath = Path.Combine(_configuration.PythonScriptsFolder, "compute_tfr.py");
                if (File.Exists(configuredScriptPath))
                {
                    return configuredScriptPath;
                }
            }

            var projectScriptPath = Path.Combine(projectRoot, "RuiSource", "Python", "compute_tfr.py");
            if (File.Exists(projectScriptPath))
            {
                return projectScriptPath;
            }

            return Path.Combine(AppContext.BaseDirectory, "Python", "compute_tfr.py");
        }

        private string ResolveSourcePreviewScriptPath(string projectRoot)
        {
            if (!string.IsNullOrWhiteSpace(_configuration.PythonScriptsFolder))
            {
                var configuredScriptPath = Path.Combine(_configuration.PythonScriptsFolder, "render_source_preview.py");
                if (File.Exists(configuredScriptPath))
                {
                    return configuredScriptPath;
                }
            }

            var projectScriptPath = Path.Combine(projectRoot, "RuiSource", "Python", "render_source_preview.py");
            if (File.Exists(projectScriptPath))
            {
                return projectScriptPath;
            }

            return Path.Combine(AppContext.BaseDirectory, "Python", "render_source_preview.py");
        }

        private void frequencyComboBox_KeyPress(object? sender, KeyPressEventArgs e)
        {
            if (sender is not ToolStripComboBox comboBox)
            {
                return;
            }

            if (char.IsControl(e.KeyChar) || char.IsDigit(e.KeyChar))
            {
                return;
            }

            if (e.KeyChar == '.' && !comboBox.Text.Contains('.'))
            {
                return;
            }

            e.Handled = true;
        }
    }
}
