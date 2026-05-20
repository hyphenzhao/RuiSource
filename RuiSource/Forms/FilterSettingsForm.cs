namespace RuiSource.Forms
{
    public sealed class FilterSettingsForm : Form
    {
        private readonly ComboBox _lowCutComboBox;
        private readonly ComboBox _highCutComboBox;
        private readonly ComboBox _notchComboBox;

        public FilterSettingsForm(string? lowCutText, string? highCutText, string? notchText)
        {
            Text = "Filters";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(380, 190);

            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 4,
                Padding = new Padding(12)
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            _lowCutComboBox = CreateComboBox(new[] { "0.5", "1", "4" }, lowCutText);
            _highCutComboBox = CreateComboBox(new[] { "30", "70", "150" }, highCutText);
            _notchComboBox = CreateComboBox(new[] { "50", "60" }, notchText);

            table.Controls.Add(CreateLabel("Low cut"), 0, 0);
            table.Controls.Add(_lowCutComboBox, 1, 0);
            table.Controls.Add(CreateUnitLabel(), 2, 0);
            table.Controls.Add(CreateLabel("High cut"), 0, 1);
            table.Controls.Add(_highCutComboBox, 1, 1);
            table.Controls.Add(CreateUnitLabel(), 2, 1);
            table.Controls.Add(CreateLabel("Notch"), 0, 2);
            table.Controls.Add(_notchComboBox, 1, 2);
            table.Controls.Add(CreateUnitLabel(), 2, 2);

            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft
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

            buttonPanel.Controls.Add(applyButton);
            buttonPanel.Controls.Add(cancelButton);
            table.Controls.Add(buttonPanel, 0, 3);
            table.SetColumnSpan(buttonPanel, 3);

            Controls.Add(table);
            AcceptButton = applyButton;
            CancelButton = cancelButton;
        }

        public string LowCutText => _lowCutComboBox.Text;

        public string HighCutText => _highCutComboBox.Text;

        public string NotchText => _notchComboBox.Text;

        private static Label CreateLabel(string text)
        {
            return new Label
            {
                Text = text,
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill
            };
        }

        private static Label CreateUnitLabel()
        {
            return new Label
            {
                Dock = DockStyle.Fill,
                Text = "Hz",
                TextAlign = ContentAlignment.MiddleLeft
            };
        }

        private static ComboBox CreateComboBox(IEnumerable<string> items, string? initialText)
        {
            var comboBox = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDown,
                Text = initialText ?? string.Empty
            };

            comboBox.Items.AddRange(items.Cast<object>().ToArray());
            comboBox.KeyPress += (_, e) =>
            {
                if (char.IsControl(e.KeyChar))
                {
                    return;
                }

                if (char.IsDigit(e.KeyChar))
                {
                    return;
                }

                if (e.KeyChar == '.' && !comboBox.Text.Contains('.'))
                {
                    return;
                }

                e.Handled = true;
            };

            return comboBox;
        }
    }
}
