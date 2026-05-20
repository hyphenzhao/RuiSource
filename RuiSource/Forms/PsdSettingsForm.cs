namespace RuiSource.Forms
{
    public sealed class PsdSettingsForm : Form
    {
        private readonly ComboBox _nFftComboBox;
        private readonly TextBox _fminTextBox;
        private readonly TextBox _fmaxTextBox;
        private readonly ComboBox _normalizationComboBox;

        public PsdSettingsForm(double sampleRate)
        {
            Text = "Compute PSD";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(360, 180);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 4,
                Padding = new Padding(12)
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            _nFftComboBox = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDown
            };
            _nFftComboBox.Items.AddRange(new object[] { "128", "256", "512", "1024", "2048" });
            _nFftComboBox.Text = "256";
            _nFftComboBox.KeyPress += IntegerOnlyKeyPress;

            _fminTextBox = CreateFloatTextBox("0");
            _fmaxTextBox = CreateFloatTextBox(Math.Min(sampleRate / 2d, 70d).ToString("F1"));
            _normalizationComboBox = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _normalizationComboBox.Items.AddRange(new object[] { "None", "Log10", "Z-score", "Percentage" });
            _normalizationComboBox.SelectedIndex = 0;

            root.Controls.Add(CreateLabel("n_fft"), 0, 0);
            root.Controls.Add(_nFftComboBox, 1, 0);
            root.Controls.Add(new Label(), 2, 0);
            root.Controls.Add(CreateLabel("fmin"), 0, 1);
            root.Controls.Add(_fminTextBox, 1, 1);
            root.Controls.Add(CreateUnitLabel(), 2, 1);
            root.Controls.Add(CreateLabel("fmax"), 0, 2);
            root.Controls.Add(_fmaxTextBox, 1, 2);
            root.Controls.Add(CreateUnitLabel(), 2, 2);
            root.Controls.Add(CreateLabel("Normalize"), 0, 3);
            root.Controls.Add(_normalizationComboBox, 1, 3);
            root.Controls.Add(new Label(), 2, 3);

            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft
            };

            var applyButton = new Button { Text = "Apply", DialogResult = DialogResult.OK, AutoSize = true };
            var cancelButton = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true };
            buttonPanel.Controls.Add(applyButton);
            buttonPanel.Controls.Add(cancelButton);
            root.Controls.Add(buttonPanel, 0, 4);
            root.SetColumnSpan(buttonPanel, 3);

            Controls.Add(root);
            AcceptButton = applyButton;
            CancelButton = cancelButton;
        }

        public int NFft => int.Parse(_nFftComboBox.Text);
        public double Fmin => double.Parse(_fminTextBox.Text);
        public double Fmax => double.Parse(_fmaxTextBox.Text);
        public string Normalization => _normalizationComboBox.SelectedItem?.ToString() ?? "None";

        private static Label CreateLabel(string text)
        {
            return new Label { Text = text, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
        }

        private static Label CreateUnitLabel()
        {
            return new Label { Text = "Hz", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
        }

        private static TextBox CreateFloatTextBox(string text)
        {
            var textBox = new TextBox { Dock = DockStyle.Fill, Text = text };
            textBox.KeyPress += FloatOnlyKeyPress;
            return textBox;
        }

        private static void FloatOnlyKeyPress(object? sender, KeyPressEventArgs e)
        {
            if (sender is not TextBox textBox)
            {
                return;
            }

            if (char.IsControl(e.KeyChar) || char.IsDigit(e.KeyChar))
            {
                return;
            }

            if (e.KeyChar == '.' && !textBox.Text.Contains('.'))
            {
                return;
            }

            e.Handled = true;
        }

        private static void IntegerOnlyKeyPress(object? sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }
    }
}
