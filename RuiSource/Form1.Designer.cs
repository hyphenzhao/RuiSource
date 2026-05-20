namespace RuiSource
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            menuStrip1 = new MenuStrip();
            fileToolStripMenuItem = new ToolStripMenuItem();
            openEdfToolStripMenuItem = new ToolStripMenuItem();
            configureToolStripMenuItem = new ToolStripMenuItem();
            channelsToolStripMenuItem = new ToolStripMenuItem();
            filtersToolStripMenuItem = new ToolStripMenuItem();
            filterToolStrip = new ToolStrip();
            lowCutLabel = new ToolStripLabel();
            lowCutComboBox = new ToolStripComboBox();
            lowCutUnitLabel = new ToolStripLabel();
            highCutLabel = new ToolStripLabel();
            highCutComboBox = new ToolStripComboBox();
            highCutUnitLabel = new ToolStripLabel();
            notchLabel = new ToolStripLabel();
            notchComboBox = new ToolStripComboBox();
            notchUnitLabel = new ToolStripLabel();
            applyFilterButton = new ToolStripButton();
            computePsdButton = new ToolStripButton();
            computeTfrButton = new ToolStripButton();
            mainLayoutPanel = new TableLayoutPanel();
            plotPanel = new Panel();
            sourcePreviewLayoutPanel = new TableLayoutPanel();
            electrodeViewGroupBox = new GroupBox();
            electrodeViewPanel = new Panel();
            mriViewGroupBox = new GroupBox();
            mriViewPanel = new Panel();
            sourcePreviewPictureBox = new PictureBox();
            statusLabel = new Label();
            menuStrip1.SuspendLayout();
            filterToolStrip.SuspendLayout();
            mainLayoutPanel.SuspendLayout();
            sourcePreviewLayoutPanel.SuspendLayout();
            electrodeViewGroupBox.SuspendLayout();
            mriViewGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)sourcePreviewPictureBox).BeginInit();
            SuspendLayout();

            menuStrip1.ImageScalingSize = new Size(24, 24);
            menuStrip1.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem, configureToolStripMenuItem, channelsToolStripMenuItem, filtersToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(800, 33);
            menuStrip1.TabIndex = 0;
            menuStrip1.Text = "menuStrip1";

            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { openEdfToolStripMenuItem });
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new Size(54, 29);
            fileToolStripMenuItem.Text = "File";

            openEdfToolStripMenuItem.Name = "openEdfToolStripMenuItem";
            openEdfToolStripMenuItem.Size = new Size(180, 34);
            openEdfToolStripMenuItem.Text = "Open EDF";
            openEdfToolStripMenuItem.Click += openEdfToolStripMenuItem_Click;

            configureToolStripMenuItem.Name = "configureToolStripMenuItem";
            configureToolStripMenuItem.Size = new Size(99, 29);
            configureToolStripMenuItem.Text = "Configure...";
            configureToolStripMenuItem.Click += configureToolStripMenuItem_Click;

            channelsToolStripMenuItem.Name = "channelsToolStripMenuItem";
            channelsToolStripMenuItem.Size = new Size(89, 29);
            channelsToolStripMenuItem.Text = "Channels...";
            channelsToolStripMenuItem.Click += channelsToolStripMenuItem_Click;

            filtersToolStripMenuItem.Name = "filtersToolStripMenuItem";
            filtersToolStripMenuItem.Size = new Size(73, 29);
            filtersToolStripMenuItem.Text = "Filters...";
            filtersToolStripMenuItem.Click += filtersToolStripMenuItem_Click;

            filterToolStrip.ImageScalingSize = new Size(24, 24);
            filterToolStrip.Items.AddRange(new ToolStripItem[] { lowCutLabel, lowCutComboBox, lowCutUnitLabel, highCutLabel, highCutComboBox, highCutUnitLabel, notchLabel, notchComboBox, notchUnitLabel, applyFilterButton, computePsdButton, computeTfrButton });
            filterToolStrip.Location = new Point(0, 33);
            filterToolStrip.Name = "filterToolStrip";
            filterToolStrip.Size = new Size(800, 34);
            filterToolStrip.TabIndex = 1;

            lowCutLabel.Name = "lowCutLabel";
            lowCutLabel.Size = new Size(68, 29);
            lowCutLabel.Text = "Low cut";

            lowCutComboBox.AutoSize = false;
            lowCutComboBox.DropDownStyle = ComboBoxStyle.DropDown;
            lowCutComboBox.Items.AddRange(new object[] { "0.5", "1", "4" });
            lowCutComboBox.Name = "lowCutComboBox";
            lowCutComboBox.Size = new Size(70, 33);
            lowCutComboBox.KeyPress += frequencyComboBox_KeyPress;

            lowCutUnitLabel.Name = "lowCutUnitLabel";
            lowCutUnitLabel.Size = new Size(29, 29);
            lowCutUnitLabel.Text = "Hz";

            highCutLabel.Name = "highCutLabel";
            highCutLabel.Size = new Size(72, 29);
            highCutLabel.Text = "High cut";

            highCutComboBox.AutoSize = false;
            highCutComboBox.DropDownStyle = ComboBoxStyle.DropDown;
            highCutComboBox.Items.AddRange(new object[] { "30", "70", "150" });
            highCutComboBox.Name = "highCutComboBox";
            highCutComboBox.Size = new Size(70, 33);
            highCutComboBox.KeyPress += frequencyComboBox_KeyPress;

            highCutUnitLabel.Name = "highCutUnitLabel";
            highCutUnitLabel.Size = new Size(29, 29);
            highCutUnitLabel.Text = "Hz";

            notchLabel.Name = "notchLabel";
            notchLabel.Size = new Size(52, 29);
            notchLabel.Text = "Notch";

            notchComboBox.AutoSize = false;
            notchComboBox.DropDownStyle = ComboBoxStyle.DropDown;
            notchComboBox.Items.AddRange(new object[] { "50", "60" });
            notchComboBox.Name = "notchComboBox";
            notchComboBox.Size = new Size(70, 33);
            notchComboBox.KeyPress += frequencyComboBox_KeyPress;

            notchUnitLabel.Name = "notchUnitLabel";
            notchUnitLabel.Size = new Size(29, 29);
            notchUnitLabel.Text = "Hz";

            applyFilterButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
            applyFilterButton.Name = "applyFilterButton";
            applyFilterButton.Size = new Size(61, 29);
            applyFilterButton.Text = "Apply";
            applyFilterButton.Click += applyFilterButton_Click;

            computePsdButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
            computePsdButton.Name = "computePsdButton";
            computePsdButton.Size = new Size(109, 29);
            computePsdButton.Text = "Compute PSD";
            computePsdButton.Visible = false;
            computePsdButton.Click += computePsdButton_Click;

            computeTfrButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
            computeTfrButton.Name = "computeTfrButton";
            computeTfrButton.Size = new Size(108, 29);
            computeTfrButton.Text = "Compute TFR";
            computeTfrButton.Visible = false;
            computeTfrButton.Click += computeTfrButton_Click;

            mainLayoutPanel.ColumnCount = 2;
            mainLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            mainLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300F));
            mainLayoutPanel.Controls.Add(plotPanel, 0, 0);
            mainLayoutPanel.Controls.Add(sourcePreviewLayoutPanel, 1, 0);
            mainLayoutPanel.Dock = DockStyle.Fill;
            mainLayoutPanel.Location = new Point(0, 67);
            mainLayoutPanel.Name = "mainLayoutPanel";
            mainLayoutPanel.RowCount = 1;
            mainLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            mainLayoutPanel.Size = new Size(800, 336);
            mainLayoutPanel.TabIndex = 2;

            plotPanel.BackColor = Color.White;
            plotPanel.Dock = DockStyle.Fill;
            plotPanel.Location = new Point(3, 3);
            plotPanel.Name = "plotPanel";
            plotPanel.Size = new Size(494, 330);
            plotPanel.TabIndex = 0;
            plotPanel.Paint += plotPanel_Paint;
            plotPanel.Resize += plotPanel_Resize;
            plotPanel.MouseWheel += plotPanel_MouseWheel;
            plotPanel.MouseEnter += plotPanel_MouseEnter;
            plotPanel.MouseDown += plotPanel_MouseDown;
            plotPanel.MouseMove += plotPanel_MouseMove;
            plotPanel.MouseUp += plotPanel_MouseUp;

            sourcePreviewLayoutPanel.ColumnCount = 1;
            sourcePreviewLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            sourcePreviewLayoutPanel.Controls.Add(electrodeViewGroupBox, 0, 0);
            sourcePreviewLayoutPanel.Controls.Add(mriViewGroupBox, 0, 1);
            sourcePreviewLayoutPanel.Dock = DockStyle.Fill;
            sourcePreviewLayoutPanel.Location = new Point(503, 3);
            sourcePreviewLayoutPanel.Name = "sourcePreviewLayoutPanel";
            sourcePreviewLayoutPanel.RowCount = 2;
            sourcePreviewLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            sourcePreviewLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            sourcePreviewLayoutPanel.Size = new Size(294, 330);
            sourcePreviewLayoutPanel.TabIndex = 1;

            electrodeViewGroupBox.Controls.Add(electrodeViewPanel);
            electrodeViewGroupBox.Dock = DockStyle.Fill;
            electrodeViewGroupBox.Enabled = false;
            electrodeViewGroupBox.Location = new Point(3, 3);
            electrodeViewGroupBox.Name = "electrodeViewGroupBox";
            electrodeViewGroupBox.Padding = new Padding(6, 6, 6, 6);
            electrodeViewGroupBox.Size = new Size(288, 159);
            electrodeViewGroupBox.TabIndex = 0;
            electrodeViewGroupBox.TabStop = false;
            electrodeViewGroupBox.Text = "EEG electrodes 3D position";

            electrodeViewPanel.BackColor = Color.White;
            electrodeViewPanel.Dock = DockStyle.Fill;
            electrodeViewPanel.Location = new Point(6, 30);
            electrodeViewPanel.Name = "electrodeViewPanel";
            electrodeViewPanel.Size = new Size(276, 123);
            electrodeViewPanel.TabIndex = 0;
            electrodeViewPanel.Paint += electrodeViewPanel_Paint;
            electrodeViewPanel.Resize += electrodeViewPanel_Resize;

            mriViewGroupBox.Controls.Add(sourcePreviewPictureBox);
            mriViewGroupBox.Controls.Add(mriViewPanel);
            mriViewGroupBox.Dock = DockStyle.Fill;
            mriViewGroupBox.Enabled = false;
            mriViewGroupBox.Location = new Point(3, 168);
            mriViewGroupBox.Name = "mriViewGroupBox";
            mriViewGroupBox.Padding = new Padding(6, 6, 6, 6);
            mriViewGroupBox.Size = new Size(288, 159);
            mriViewGroupBox.TabIndex = 1;
            mriViewGroupBox.TabStop = false;
            mriViewGroupBox.Text = "Standard MRI 3D view";

            mriViewPanel.BackColor = Color.White;
            mriViewPanel.Dock = DockStyle.Fill;
            mriViewPanel.Location = new Point(6, 30);
            mriViewPanel.Name = "mriViewPanel";
            mriViewPanel.Size = new Size(276, 123);
            mriViewPanel.TabIndex = 0;
            mriViewPanel.Paint += mriViewPanel_Paint;
            mriViewPanel.Resize += mriViewPanel_Resize;

            sourcePreviewPictureBox.BackColor = Color.White;
            sourcePreviewPictureBox.Dock = DockStyle.Fill;
            sourcePreviewPictureBox.Location = new Point(6, 30);
            sourcePreviewPictureBox.Name = "sourcePreviewPictureBox";
            sourcePreviewPictureBox.Size = new Size(276, 123);
            sourcePreviewPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            sourcePreviewPictureBox.TabIndex = 1;
            sourcePreviewPictureBox.TabStop = false;
            sourcePreviewPictureBox.Visible = false;

            statusLabel.Dock = DockStyle.Bottom;
            statusLabel.Location = new Point(0, 403);
            statusLabel.Name = "statusLabel";
            statusLabel.Padding = new Padding(8, 0, 8, 0);
            statusLabel.Size = new Size(800, 47);
            statusLabel.TabIndex = 3;
            statusLabel.Text = "Open an EDF file to view signals.";
            statusLabel.TextAlign = ContentAlignment.MiddleLeft;

            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(mainLayoutPanel);
            Controls.Add(statusLabel);
            Controls.Add(filterToolStrip);
            Controls.Add(menuStrip1);
            MainMenuStrip = menuStrip1;
            MinimumSize = new Size(900, 500);
            Name = "Form1";
            Text = "RuiSource";
            ((System.ComponentModel.ISupportInitialize)sourcePreviewPictureBox).EndInit();
            mriViewGroupBox.ResumeLayout(false);
            electrodeViewGroupBox.ResumeLayout(false);
            sourcePreviewLayoutPanel.ResumeLayout(false);
            mainLayoutPanel.ResumeLayout(false);
            filterToolStrip.ResumeLayout(false);
            filterToolStrip.PerformLayout();
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private MenuStrip menuStrip1;
        private ToolStripMenuItem fileToolStripMenuItem;
        private ToolStripMenuItem openEdfToolStripMenuItem;
        private ToolStripMenuItem configureToolStripMenuItem;
        private ToolStripMenuItem channelsToolStripMenuItem;
        private ToolStripMenuItem filtersToolStripMenuItem;
        private ToolStrip filterToolStrip;
        private ToolStripLabel lowCutLabel;
        private ToolStripComboBox lowCutComboBox;
        private ToolStripLabel lowCutUnitLabel;
        private ToolStripLabel highCutLabel;
        private ToolStripComboBox highCutComboBox;
        private ToolStripLabel highCutUnitLabel;
        private ToolStripLabel notchLabel;
        private ToolStripComboBox notchComboBox;
        private ToolStripLabel notchUnitLabel;
        private ToolStripButton applyFilterButton;
        private ToolStripButton computePsdButton;
        private ToolStripButton computeTfrButton;
        private TableLayoutPanel mainLayoutPanel;
        private Panel plotPanel;
        private TableLayoutPanel sourcePreviewLayoutPanel;
        private GroupBox electrodeViewGroupBox;
        private Panel electrodeViewPanel;
        private GroupBox mriViewGroupBox;
        private Panel mriViewPanel;
        private PictureBox sourcePreviewPictureBox;
        private Label statusLabel;
    }
}
