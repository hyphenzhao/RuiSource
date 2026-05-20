namespace RuiSource.Forms
{
    public sealed class ConfigurationForm : Form
    {
        private readonly TextBox _pythonPathTextBox;
        private readonly TextBox _pythonScriptsFolderTextBox;

        public ConfigurationForm(string? pythonPath, string? pythonScriptsFolder)
        {
            Text = "Configure";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(640, 190);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 4,
                Padding = new Padding(12)
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));

            _pythonPathTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Text = pythonPath ?? string.Empty
            };

            _pythonScriptsFolderTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Text = pythonScriptsFolder ?? string.Empty
            };

            var browsePythonButton = new Button
            {
                Text = "Browse...",
                Dock = DockStyle.Fill
            };
            browsePythonButton.Click += BrowsePythonButton_Click;

            var browseScriptsButton = new Button
            {
                Text = "Browse...",
                Dock = DockStyle.Fill
            };
            browseScriptsButton.Click += BrowseScriptsButton_Click;

            root.Controls.Add(new Label
            {
                Text = "Python executable",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, 0);
            root.Controls.Add(_pythonPathTextBox, 1, 0);
            root.Controls.Add(browsePythonButton, 2, 0);

            root.Controls.Add(new Label
            {
                Text = "Python scripts folder",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, 1);
            root.Controls.Add(_pythonScriptsFolderTextBox, 1, 1);
            root.Controls.Add(browseScriptsButton, 2, 1);

            var descriptionLabel = new Label
            {
                Text = "Select the Python interpreter and the folder that contains compute_tfr.py.",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            root.Controls.Add(descriptionLabel, 0, 2);
            root.SetColumnSpan(descriptionLabel, 3);

            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft
            };

            var applyButton = new Button { Text = "Apply", DialogResult = DialogResult.OK, AutoSize = true };
            var cancelButton = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true };
            buttonPanel.Controls.Add(applyButton);
            buttonPanel.Controls.Add(cancelButton);

            root.Controls.Add(buttonPanel, 0, 3);
            root.SetColumnSpan(buttonPanel, 3);

            Controls.Add(root);
            AcceptButton = applyButton;
            CancelButton = cancelButton;
        }

        public string PythonPath => _pythonPathTextBox.Text.Trim();
        public string PythonScriptsFolder => _pythonScriptsFolderTextBox.Text.Trim();

        private void BrowsePythonButton_Click(object? sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Filter = "Python executable|python.exe|Executable files (*.exe)|*.exe|All files (*.*)|*.*",
                Title = "Select Python executable"
            };

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                _pythonPathTextBox.Text = dialog.FileName;
            }
        }

        private void BrowseScriptsButton_Click(object? sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "Select Python scripts folder"
            };

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                _pythonScriptsFolderTextBox.Text = dialog.SelectedPath;
            }
        }
    }
}
