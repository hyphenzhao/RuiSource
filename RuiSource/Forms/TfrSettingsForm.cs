namespace RuiSource.Forms
{
    public sealed class TfrSettingsForm : Form
    {
        private readonly TextBox _fminTextBox;
        private readonly TextBox _fmaxTextBox;
        private readonly TextBox _fstepTextBox;
        private readonly RadioButton _autoCyclesRadioButton;
        private readonly RadioButton _manualCyclesRadioButton;
        private readonly TextBox _nCyclesTextBox;

        public TfrSettingsForm(double sampleRate)
        {
            Text = "Compute TFR";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(400, 220);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 6,
                Padding = new Padding(12)
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40));

            _fminTextBox = CreateFloatTextBox("1.0");
            _fmaxTextBox = CreateFloatTextBox(Math.Min(200d, sampleRate / 2d).ToString("F1"));
            _fstepTextBox = CreateFloatTextBox("0.5");
            _autoCyclesRadioButton = new RadioButton { Text = "Automatic", Checked = true, AutoSize = true };
            _manualCyclesRadioButton = new RadioButton { Text = "Manual", AutoSize = true };
            _nCyclesTextBox = CreateFloatTextBox("7.0");
            _nCyclesTextBox.Enabled = false;

            _autoCyclesRadioButton.CheckedChanged += (_, _) => _nCyclesTextBox.Enabled = _manualCyclesRadioButton.Checked;
            _manualCyclesRadioButton.CheckedChanged += (_, _) => _nCyclesTextBox.Enabled = _manualCyclesRadioButton.Checked;

            root.Controls.Add(CreateLabel("fmin"), 0, 0);
            root.Controls.Add(_fminTextBox, 1, 0);
            root.Controls.Add(CreateUnitLabel(), 2, 0);
            root.Controls.Add(CreateLabel("fmax"), 0, 1);
            root.Controls.Add(_fmaxTextBox, 1, 1);
            root.Controls.Add(CreateUnitLabel(), 2, 1);
            root.Controls.Add(CreateLabel("fstep"), 0, 2);
            root.Controls.Add(_fstepTextBox, 1, 2);
            root.Controls.Add(CreateUnitLabel(), 2, 2);

            var cyclePanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true };
            cyclePanel.Controls.Add(_autoCyclesRadioButton);
            cyclePanel.Controls.Add(_manualCyclesRadioButton);
            root.Controls.Add(CreateLabel("n_cycles"), 0, 3);
            root.Controls.Add(cyclePanel, 1, 3);
            root.SetColumnSpan(cyclePanel, 2);

            root.Controls.Add(CreateLabel("Value"), 0, 4);
            root.Controls.Add(_nCyclesTextBox, 1, 4);
            root.Controls.Add(new Label(), 2, 4);

            var buttonPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
            var applyButton = new Button { Text = "Apply", DialogResult = DialogResult.OK, AutoSize = true };
            var cancelButton = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true };
            buttonPanel.Controls.Add(applyButton);
            buttonPanel.Controls.Add(cancelButton);
            root.Controls.Add(buttonPanel, 0, 5);
            root.SetColumnSpan(buttonPanel, 3);

            Controls.Add(root);
            AcceptButton = applyButton;
            CancelButton = cancelButton;
        }

        public double Fmin => double.Parse(_fminTextBox.Text);
        public double Fmax => double.Parse(_fmaxTextBox.Text);
        public double Fstep => double.Parse(_fstepTextBox.Text);
        public bool UseAutomaticNCycles => _autoCyclesRadioButton.Checked;
        public double ManualNCycles => double.Parse(_nCyclesTextBox.Text);

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
            textBox.KeyPress += (_, e) =>
            {
                if (char.IsControl(e.KeyChar) || char.IsDigit(e.KeyChar))
                {
                    return;
                }

                if (e.KeyChar == '.' && !textBox.Text.Contains('.'))
                {
                    return;
                }

                e.Handled = true;
            };
            return textBox;
        }
    }
}
