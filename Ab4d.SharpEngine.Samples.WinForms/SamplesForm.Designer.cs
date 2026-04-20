namespace Ab4d.SharpEngine.Samples.WinForms
{
    partial class SamplesForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SamplesForm));
            panel1 = new Panel();
            samplesListView = new ListView();
            diagnosticsButton = new Button();
            renderAsManyFramesAsPossibleCheckBox = new CheckBox();
            gpuInfoLabel = new Label();
            usedGpuTextLabel = new Label();
            pictureBox1 = new PictureBox();
            label1 = new Label();
            samplePanel = new Panel();
            panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // panel1
            // 
            panel1.Controls.Add(samplesListView);
            panel1.Controls.Add(diagnosticsButton);
            panel1.Controls.Add(renderAsManyFramesAsPossibleCheckBox);
            panel1.Controls.Add(gpuInfoLabel);
            panel1.Controls.Add(usedGpuTextLabel);
            panel1.Controls.Add(pictureBox1);
            panel1.Controls.Add(label1);
            panel1.Dock = DockStyle.Left;
            panel1.Location = new Point(0, 0);
            panel1.Margin = new Padding(3, 4, 3, 4);
            panel1.Name = "panel1";
            panel1.Size = new Size(466, 1280);
            panel1.TabIndex = 0;
            // 
            // samplesListView
            // 
            samplesListView.Dock = DockStyle.Fill;
            samplesListView.Location = new Point(0, 268);
            samplesListView.Margin = new Padding(3, 4, 3, 4);
            samplesListView.MultiSelect = false;
            samplesListView.Name = "samplesListView";
            samplesListView.ShowItemToolTips = true;
            samplesListView.Size = new Size(466, 1012);
            samplesListView.TabIndex = 0;
            samplesListView.UseCompatibleStateImageBehavior = false;
            samplesListView.View = View.Details;
            samplesListView.SelectedIndexChanged += samplesListView_SelectedIndexChanged;
            // 
            // diagnosticsButton
            // 
            diagnosticsButton.Dock = DockStyle.Top;
            diagnosticsButton.Image = (Image)resources.GetObject("diagnosticsButton.Image");
            diagnosticsButton.ImageAlign = ContentAlignment.MiddleLeft;
            diagnosticsButton.Location = new Point(0, 218);
            diagnosticsButton.Margin = new Padding(3, 4, 3, 4);
            diagnosticsButton.Name = "diagnosticsButton";
            diagnosticsButton.Padding = new Padding(10, 4, 10, 4);
            diagnosticsButton.Size = new Size(466, 50);
            diagnosticsButton.TabIndex = 4;
            diagnosticsButton.Text = "Diagnostics";
            diagnosticsButton.TextAlign = ContentAlignment.MiddleRight;
            diagnosticsButton.UseVisualStyleBackColor = true;
            // 
            // renderAsManyFramesAsPossibleCheckBox
            // 
            renderAsManyFramesAsPossibleCheckBox.AutoSize = true;
            renderAsManyFramesAsPossibleCheckBox.Checked = true;
            renderAsManyFramesAsPossibleCheckBox.CheckState = CheckState.Checked;
            renderAsManyFramesAsPossibleCheckBox.Dock = DockStyle.Top;
            renderAsManyFramesAsPossibleCheckBox.Location = new Point(0, 158);
            renderAsManyFramesAsPossibleCheckBox.Margin = new Padding(3, 4, 3, 4);
            renderAsManyFramesAsPossibleCheckBox.Name = "renderAsManyFramesAsPossibleCheckBox";
            renderAsManyFramesAsPossibleCheckBox.Padding = new Padding(10, 10, 0, 16);
            renderAsManyFramesAsPossibleCheckBox.Size = new Size(466, 60);
            renderAsManyFramesAsPossibleCheckBox.TabIndex = 5;
            renderAsManyFramesAsPossibleCheckBox.Text = "Render as many frames as possible";
            renderAsManyFramesAsPossibleCheckBox.UseVisualStyleBackColor = true;
            renderAsManyFramesAsPossibleCheckBox.CheckedChanged += renderAsManyFramesAsPossibleCheckBox_CheckedChanged;
            // 
            // gpuInfoLabel
            // 
            gpuInfoLabel.AutoSize = true;
            gpuInfoLabel.Dock = DockStyle.Top;
            gpuInfoLabel.Location = new Point(0, 128);
            gpuInfoLabel.Name = "gpuInfoLabel";
            gpuInfoLabel.Padding = new Padding(10, 0, 0, 0);
            gpuInfoLabel.Size = new Size(78, 30);
            gpuInfoLabel.TabIndex = 3;
            gpuInfoLabel.Text = "label3";
            // 
            // usedGpuTextLabel
            // 
            usedGpuTextLabel.AutoSize = true;
            usedGpuTextLabel.Dock = DockStyle.Top;
            usedGpuTextLabel.Location = new Point(0, 98);
            usedGpuTextLabel.Name = "usedGpuTextLabel";
            usedGpuTextLabel.Padding = new Padding(10, 0, 0, 0);
            usedGpuTextLabel.Size = new Size(204, 30);
            usedGpuTextLabel.TabIndex = 2;
            usedGpuTextLabel.Text = "Used graphics card:";
            // 
            // pictureBox1
            // 
            pictureBox1.Dock = DockStyle.Top;
            pictureBox1.Image = (Image)resources.GetObject("pictureBox1.Image");
            pictureBox1.Location = new Point(0, 42);
            pictureBox1.Margin = new Padding(3, 4, 3, 4);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Padding = new Padding(10, 4, 0, 0);
            pictureBox1.Size = new Size(466, 56);
            pictureBox1.TabIndex = 1;
            pictureBox1.TabStop = false;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Dock = DockStyle.Top;
            label1.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            label1.Location = new Point(0, 0);
            label1.Name = "label1";
            label1.Padding = new Padding(10, 4, 0, 0);
            label1.Size = new Size(310, 42);
            label1.TabIndex = 0;
            label1.Text = "Ab4d.SharpEngine by";
            // 
            // samplePanel
            // 
            samplePanel.Dock = DockStyle.Fill;
            samplePanel.Location = new Point(466, 0);
            samplePanel.Margin = new Padding(3, 4, 3, 4);
            samplePanel.Name = "samplePanel";
            samplePanel.Size = new Size(1907, 1280);
            samplePanel.TabIndex = 1;
            // 
            // SamplesForm
            // 
            AutoScaleDimensions = new SizeF(12F, 30F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(2373, 1280);
            Controls.Add(samplePanel);
            Controls.Add(panel1);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(3, 4, 3, 4);
            Name = "SamplesForm";
            Text = "Ab4d.SharpEngine WinForms samples";
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Panel panel1;
        private Panel samplePanel;
        private ListView samplesListView;
        private PictureBox pictureBox1;
        private Label label1;
        private Label usedGpuTextLabel;
        private Label gpuInfoLabel;
        private Button diagnosticsButton;
        private CheckBox renderAsManyFramesAsPossibleCheckBox;
    }
}