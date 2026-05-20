using RuiSource.Models;

namespace RuiSource.Forms
{
    public sealed class ChannelsForm : Form
    {
        private static readonly Color HoverBackColor = Color.FromArgb(229, 241, 251);
        private static readonly Color NormalBackColor = SystemColors.Control;
        private readonly FlowLayoutPanel _rowsPanel;
        private readonly List<ChannelEditorRow> _rows = new();
        private int? _anchorIndex;

        public ChannelsForm(IReadOnlyList<EdfSignal> signals, IReadOnlySet<int> visibleChannelIndexes)
        {
            Text = "Channels";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(520, 520);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(8)
            };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var header = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 3,
                AutoSize = true
            };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            header.Controls.Add(CreateHeaderLabel("Index"), 0, 0);
            header.Controls.Add(CreateHeaderLabel("Visible"), 1, 0);
            header.Controls.Add(CreateHeaderLabel("Channel name"), 2, 0);

            _rowsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true
            };

            for (var index = 0; index < signals.Count; index++)
            {
                var row = new ChannelEditorRow(index, signals[index].Label, visibleChannelIndexes.Contains(index));
                row.IndexLabel.MouseEnter += (_, _) => SetHoverState(row, true);
                row.IndexLabel.MouseLeave += (_, _) => SetHoverState(row, false);
                row.IndexLabel.MouseClick += (_, e) => HandleIndexClick(row.Index, e);
                _rows.Add(row);
                _rowsPanel.Controls.Add(row.Container);
            }

            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true
            };

            var applyButton = new Button
            {
                Text = "Apply",
                DialogResult = DialogResult.OK,
                AutoSize = true
            };

            var cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                AutoSize = true
            };

            var reverseSelectionButton = new Button
            {
                Text = "Reverse Selection",
                AutoSize = true
            };
            reverseSelectionButton.Click += (_, _) => ReverseSelections();

            buttonPanel.Controls.Add(applyButton);
            buttonPanel.Controls.Add(cancelButton);
            buttonPanel.Controls.Add(reverseSelectionButton);

            root.Controls.Add(header, 0, 0);
            root.Controls.Add(_rowsPanel, 0, 1);
            root.Controls.Add(buttonPanel, 0, 2);

            Controls.Add(root);
            AcceptButton = applyButton;
            CancelButton = cancelButton;
        }

        public IReadOnlyList<ChannelEditResult> GetResults()
        {
            return _rows.Select(row => new ChannelEditResult(row.Index, row.IsVisible, row.ChannelName)).ToArray();
        }

        private void ReverseSelections()
        {
            foreach (var row in _rows)
            {
                row.VisibleCheckBox.Checked = !row.VisibleCheckBox.Checked;
            }
        }

        private void SetHoverState(ChannelEditorRow row, bool isHovered)
        {
            row.IndexLabel.BackColor = isHovered ? HoverBackColor : NormalBackColor;
        }

        private void HandleIndexClick(int clickedIndex, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            var modifiers = ModifierKeys;
            if ((modifiers & Keys.Shift) == Keys.Shift)
            {
                ApplyRangeSelection(clickedIndex, additive: (modifiers & Keys.Control) == Keys.Control);
                return;
            }

            if ((modifiers & Keys.Control) == Keys.Control)
            {
                _rows[clickedIndex].VisibleCheckBox.Checked = true;
                _anchorIndex = clickedIndex;
                return;
            }

            foreach (var row in _rows)
            {
                row.VisibleCheckBox.Checked = false;
            }

            _rows[clickedIndex].VisibleCheckBox.Checked = true;
            _anchorIndex = clickedIndex;
        }

        private void ApplyRangeSelection(int clickedIndex, bool additive)
        {
            var startIndex = _anchorIndex ?? clickedIndex;
            var minIndex = Math.Min(startIndex, clickedIndex);
            var maxIndex = Math.Max(startIndex, clickedIndex);

            if (!additive)
            {
                foreach (var row in _rows)
                {
                    row.VisibleCheckBox.Checked = false;
                }
            }

            for (var index = minIndex; index <= maxIndex; index++)
            {
                _rows[index].VisibleCheckBox.Checked = true;
            }

            _anchorIndex = clickedIndex;
        }

        private static Label CreateHeaderLabel(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold),
                Margin = new Padding(3, 0, 3, 3)
            };
        }

        public sealed record ChannelEditResult(int Index, bool IsVisible, string Name);

        private sealed class ChannelEditorRow
        {
            public ChannelEditorRow(int index, string name, bool isVisible)
            {
                Index = index;

                Container = new TableLayoutPanel
                {
                    ColumnCount = 3,
                    RowCount = 1,
                    Width = 470,
                    Height = 28,
                    Margin = new Padding(0, 0, 0, 3)
                };
                Container.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));
                Container.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
                Container.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

                var indexLabel = new Label
                {
                    Text = (index + 1).ToString(),
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft
                };

                var visibleCheckBox = new CheckBox
                {
                    Checked = isVisible,
                    Dock = DockStyle.None,
                    AutoSize = true,
                    Anchor = AnchorStyles.Left
                };

                var nameTextBox = new TextBox
                {
                    Text = name,
                    Dock = DockStyle.Fill
                };

                Container.Controls.Add(indexLabel, 0, 0);
                Container.Controls.Add(visibleCheckBox, 1, 0);
                Container.Controls.Add(nameTextBox, 2, 0);

                VisibleCheckBox = visibleCheckBox;
                IndexLabel = indexLabel;
                NameTextBox = nameTextBox;
            }

            public int Index { get; }

            public TableLayoutPanel Container { get; }

            public CheckBox VisibleCheckBox { get; }

            public Label IndexLabel { get; }

            public TextBox NameTextBox { get; }

            public bool IsVisible => VisibleCheckBox.Checked;

            public string ChannelName => string.IsNullOrWhiteSpace(NameTextBox.Text) ? $"Channel {Index + 1}" : NameTextBox.Text.Trim();
        }
    }
}
