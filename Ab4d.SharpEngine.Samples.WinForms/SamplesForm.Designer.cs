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
            panel3 = new Panel();
            diagnosticsButton = new Button();
            gpuInfoLabel = new Label();
            usedGpuTextLabel = new Label();
            pictureBox1 = new PictureBox();
            label1 = new Label();
            samplePanel = new Panel();
            panel1.SuspendLayout();
            panel3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // panel1
            // 
            panel1.Controls.Add(samplesListView);
            panel1.Controls.Add(panel3);
            panel1.Dock = DockStyle.Left;
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(294, 853);
            panel1.TabIndex = 0;
            // 
            // samplesListView
            // 
            samplesListView.Dock = DockStyle.Fill;
            samplesListView.Location = new Point(0, 175);
            samplesListView.MultiSelect = false;
            samplesListView.Name = "samplesListView";
            samplesListView.Size = new Size(294, 678);
            samplesListView.TabIndex = 0;
            samplesListView.UseCompatibleStateImageBehavior = false;
            samplesListView.View = View.Details;
            samplesListView.SelectedIndexChanged += samplesListView_SelectedIndexChanged;
            // 
            // panel3
            // 
            panel3.BackColor = SystemColors.Control;
            panel3.Controls.Add(diagnosticsButton);
            panel3.Controls.Add(gpuInfoLabel);
            panel3.Controls.Add(usedGpuTextLabel);
            panel3.Controls.Add(pictureBox1);
            panel3.Controls.Add(label1);
            panel3.Dock = DockStyle.Top;
            panel3.Location = new Point(0, 0);
            panel3.Name = "panel3";
            panel3.Size = new Size(294, 175);
            panel3.TabIndex = 0;
            // 
            // diagnosticsButton
            // 
            diagnosticsButton.Image = (Image)resources.GetObject("diagnosticsButton.Image");
            diagnosticsButton.ImageAlign = ContentAlignment.MiddleLeft;
            diagnosticsButton.Location = new Point(12, 134);
            diagnosticsButton.Name = "diagnosticsButton";
            diagnosticsButton.Size = new Size(121, 33);
            diagnosticsButton.TabIndex = 4;
            diagnosticsButton.Text = "Diagnostics";
            diagnosticsButton.TextAlign = ContentAlignment.MiddleRight;
            diagnosticsButton.UseVisualStyleBackColor = true;
            // 
            // gpuInfoLabel
            // 
            gpuInfoLabel.AutoSize = true;
            gpuInfoLabel.Location = new Point(12, 106);
            gpuInfoLabel.Name = "gpuInfoLabel";
            gpuInfoLabel.Size = new Size(50, 20);
            gpuInfoLabel.TabIndex = 3;
            gpuInfoLabel.Text = "label3";
            // 
            // usedGpuTextLabel
            // 
            usedGpuTextLabel.AutoSize = true;
            usedGpuTextLabel.Location = new Point(12, 86);
            usedGpuTextLabel.Name = "usedGpuTextLabel";
            usedGpuTextLabel.Size = new Size(138, 20);
            usedGpuTextLabel.TabIndex = 2;
            usedGpuTextLabel.Text = "Used graphics card:";
            // 
            // pictureBox1
            // 
            pictureBox1.Image = (Image)resources.GetObject("pictureBox1.Image");
            pictureBox1.Location = new Point(12, 40);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(138, 38);
            pictureBox1.TabIndex = 1;
            pictureBox1.TabStop = false;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            label1.Location = new Point(12, 9);
            label1.Name = "label1";
            label1.Size = new Size(214, 28);
            label1.TabIndex = 0;
            label1.Text = "Ab4d.SharpEngine by";
            // 
            // samplePanel
            // 
            samplePanel.Dock = DockStyle.Fill;
            samplePanel.Location = new Point(294, 0);
            samplePanel.Name = "samplePanel";
            samplePanel.Size = new Size(1288, 853);
            samplePanel.TabIndex = 1;
            // 
            // SamplesForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1582, 853);
            Controls.Add(samplePanel);
            Controls.Add(panel1);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "SamplesForm";
            Text = "Ab4d.SharpEngine WinForms samples";
            panel1.ResumeLayout(false);
            panel3.ResumeLayout(false);
            panel3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Panel panel1;
        private Panel samplePanel;
        private Panel panel3;
        private ListView samplesListView;
        private PictureBox pictureBox1;
        private Label label1;
        private Label usedGpuTextLabel;
        private Label gpuInfoLabel;
        private Button diagnosticsButton;
    }
}