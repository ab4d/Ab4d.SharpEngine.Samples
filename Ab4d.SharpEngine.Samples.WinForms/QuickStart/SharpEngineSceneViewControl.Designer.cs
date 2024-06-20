using Ab4d.SharpEngine.Common;

namespace Ab4d.SharpEngine.Samples.WinForms.QuickStart
{
    partial class SharpEngineSceneViewControl
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        // Moved to user code
        ///// <summary> 
        ///// Clean up any resources being used.
        ///// </summary>
        ///// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        //protected override void Dispose(bool disposing)
        //{
        //    if (disposing && (components != null))
        //    {
        //        components.Dispose();
        //    }
        //    base.Dispose(disposing);
        //}

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            panel1 = new Panel();
            renderToBitmapButton = new Button();
            changeBackgroundButton = new Button();
            changeMaterial2Button = new Button();
            changeMaterial1Button = new Button();
            removeButton = new Button();
            addNewButton = new Button();
            panel2 = new Panel();
            mainSceneView = new SharpEngine.WinForms.SharpEngineSceneView();
            label18 = new Label();
            label17 = new Label();
            panel1.SuspendLayout();
            panel2.SuspendLayout();
            SuspendLayout();
            // 
            // panel1
            // 
            panel1.Controls.Add(renderToBitmapButton);
            panel1.Controls.Add(changeBackgroundButton);
            panel1.Controls.Add(changeMaterial2Button);
            panel1.Controls.Add(changeMaterial1Button);
            panel1.Controls.Add(removeButton);
            panel1.Controls.Add(addNewButton);
            panel1.Dock = DockStyle.Bottom;
            panel1.Location = new Point(0, 499);
            panel1.Name = "panel1";
            panel1.Size = new Size(997, 45);
            panel1.TabIndex = 0;
            // 
            // renderToBitmapButton
            // 
            renderToBitmapButton.Location = new Point(833, 6);
            renderToBitmapButton.Name = "renderToBitmapButton";
            renderToBitmapButton.Size = new Size(160, 29);
            renderToBitmapButton.TabIndex = 5;
            renderToBitmapButton.Text = "Render to bitmap";
            renderToBitmapButton.UseVisualStyleBackColor = true;
            renderToBitmapButton.Click += renderToBitmapButton_Click;
            // 
            // changeBackgroundButton
            // 
            changeBackgroundButton.Location = new Point(667, 6);
            changeBackgroundButton.Name = "changeBackgroundButton";
            changeBackgroundButton.Size = new Size(160, 29);
            changeBackgroundButton.TabIndex = 4;
            changeBackgroundButton.Text = "Change background";
            changeBackgroundButton.UseVisualStyleBackColor = true;
            changeBackgroundButton.Click += changeBackgroundButton_Click;
            // 
            // changeMaterial2Button
            // 
            changeMaterial2Button.Location = new Point(501, 6);
            changeMaterial2Button.Name = "changeMaterial2Button";
            changeMaterial2Button.Size = new Size(160, 29);
            changeMaterial2Button.TabIndex = 3;
            changeMaterial2Button.Text = "Change material 2";
            changeMaterial2Button.UseVisualStyleBackColor = true;
            changeMaterial2Button.Click += changeMaterial2Button_Click;
            // 
            // changeMaterial1Button
            // 
            changeMaterial1Button.Location = new Point(335, 6);
            changeMaterial1Button.Name = "changeMaterial1Button";
            changeMaterial1Button.Size = new Size(160, 29);
            changeMaterial1Button.TabIndex = 2;
            changeMaterial1Button.Text = "Change material 1";
            changeMaterial1Button.UseVisualStyleBackColor = true;
            changeMaterial1Button.Click += changeMaterial1Button_Click;
            // 
            // removeButton
            // 
            removeButton.Location = new Point(169, 6);
            removeButton.Name = "removeButton";
            removeButton.Size = new Size(160, 29);
            removeButton.TabIndex = 1;
            removeButton.Text = "Remove";
            removeButton.UseVisualStyleBackColor = true;
            removeButton.Click += removeButton_Click;
            // 
            // addNewButton
            // 
            addNewButton.Location = new Point(3, 6);
            addNewButton.Name = "addNewButton";
            addNewButton.Size = new Size(160, 29);
            addNewButton.TabIndex = 0;
            addNewButton.Text = "Add new";
            addNewButton.UseVisualStyleBackColor = true;
            addNewButton.Click += addNewButton_Click;
            // 
            // panel2
            // 
            panel2.Controls.Add(label18);
            panel2.Controls.Add(label17);
            panel2.Controls.Add(mainSceneView);
            panel2.Dock = DockStyle.Fill;
            panel2.Location = new Point(0, 0);
            panel2.Name = "panel2";
            panel2.Size = new Size(997, 499);
            panel2.TabIndex = 1;
            // 
            // mainSceneView
            // 
            mainSceneView.Dock = DockStyle.Fill;
            mainSceneView.Location = new Point(0, 0);
            mainSceneView.Name = "mainSceneView";
            mainSceneView.PreferredMultiSampleCount = 4;
            mainSceneView.PresentationType = PresentationTypes.SharedTexture;
            mainSceneView.Size = new Size(997, 499);
            mainSceneView.StopRenderingWhenHidden = true;
            mainSceneView.TabIndex = 0;
            mainSceneView.WaitForVSync = false;
            // 
            // label18
            // 
            label18.AutoSize = true;
            label18.Font = new Font("Segoe UI", 10F);
            label18.Location = new Point(7, 43);
            label18.Name = "label18";
            label18.Size = new Size(329, 92);
            label18.TabIndex = 4;
            label18.Text = "Rotate camera: left mouse button\r\nMove camera: CTRL + left mouse button\r\nChange distance: mouse wheel\r\nQuick zoom: left and right mouse buttons";
            // 
            // label17
            // 
            label17.AutoSize = true;
            label17.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            label17.Location = new Point(3, 9);
            label17.Name = "label17";
            label17.Size = new Size(368, 32);
            label17.TabIndex = 3;
            label17.Text = "SharpEngineSceneView control";
            // 
            // SharpEngineSceneViewControl
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(panel2);
            Controls.Add(panel1);
            Name = "SharpEngineSceneViewControl";
            Size = new Size(997, 544);
            panel1.ResumeLayout(false);
            panel2.ResumeLayout(false);
            panel2.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private Panel panel1;
        private Panel panel2;
        private Button changeBackgroundButton;
        private Button changeMaterial2Button;
        private Button changeMaterial1Button;
        private Button removeButton;
        private Button addNewButton;
        private Button renderToBitmapButton;
        private SharpEngine.WinForms.SharpEngineSceneView mainSceneView;
        private Label label18;
        private Label label17;
    }
}
