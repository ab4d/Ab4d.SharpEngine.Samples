using Ab4d.SharpEngine.Common;

namespace Ab4d.SharpEngine.Samples.WinForms.CameraControllers
{
    partial class PointerCameraControllerSample
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            panel1 = new Panel();
            maxCameraDistanceComboBox = new ComboBox();
            mouseMoveThresholdComboBox = new ComboBox();
            label16 = new Label();
            label15 = new Label();
            mouseWheelDistanceChangeFactorComboBox = new ComboBox();
            label14 = new Label();
            isMouseWheelZoomEnabledCheckBox = new CheckBox();
            useMousePositionForMovementSpeedCheckBox = new CheckBox();
            isYAxisInvertedCheckBox = new CheckBox();
            isXAxisInvertedCheckBox = new CheckBox();
            zoomModeComboBox = new ComboBox();
            label13 = new Label();
            rotateAroundMousePositionCheckBox = new CheckBox();
            altKeyCheckBox3 = new CheckBox();
            controlKeyCheckBox3 = new CheckBox();
            shiftKeyCheckBox3 = new CheckBox();
            rightButtonCheckBox3 = new CheckBox();
            middleButtonCheckBox3 = new CheckBox();
            leftButtonCheckBox3 = new CheckBox();
            label9 = new Label();
            label10 = new Label();
            label11 = new Label();
            label12 = new Label();
            altKeyCheckBox2 = new CheckBox();
            controlKeyCheckBox2 = new CheckBox();
            shiftKeyCheckBox2 = new CheckBox();
            rightButtonCheckBox2 = new CheckBox();
            middleButtonCheckBox2 = new CheckBox();
            leftButtonCheckBox2 = new CheckBox();
            label5 = new Label();
            label6 = new Label();
            label7 = new Label();
            label8 = new Label();
            altKeyCheckBox1 = new CheckBox();
            controlKeyCheckBox1 = new CheckBox();
            shiftKeyCheckBox1 = new CheckBox();
            rightButtonCheckBox1 = new CheckBox();
            middleButtonCheckBox1 = new CheckBox();
            leftButtonCheckBox1 = new CheckBox();
            label4 = new Label();
            label3 = new Label();
            label2 = new Label();
            label1 = new Label();
            panel2 = new Panel();
            label18 = new Label();
            label17 = new Label();
            mainSceneView = new SharpEngine.WinForms.SharpEngineSceneView();
            panel1.SuspendLayout();
            panel2.SuspendLayout();
            SuspendLayout();
            // 
            // panel1
            // 
            panel1.BackColor = SystemColors.Window;
            panel1.Controls.Add(maxCameraDistanceComboBox);
            panel1.Controls.Add(mouseMoveThresholdComboBox);
            panel1.Controls.Add(label16);
            panel1.Controls.Add(label15);
            panel1.Controls.Add(mouseWheelDistanceChangeFactorComboBox);
            panel1.Controls.Add(label14);
            panel1.Controls.Add(isMouseWheelZoomEnabledCheckBox);
            panel1.Controls.Add(useMousePositionForMovementSpeedCheckBox);
            panel1.Controls.Add(isYAxisInvertedCheckBox);
            panel1.Controls.Add(isXAxisInvertedCheckBox);
            panel1.Controls.Add(zoomModeComboBox);
            panel1.Controls.Add(label13);
            panel1.Controls.Add(rotateAroundMousePositionCheckBox);
            panel1.Controls.Add(altKeyCheckBox3);
            panel1.Controls.Add(controlKeyCheckBox3);
            panel1.Controls.Add(shiftKeyCheckBox3);
            panel1.Controls.Add(rightButtonCheckBox3);
            panel1.Controls.Add(middleButtonCheckBox3);
            panel1.Controls.Add(leftButtonCheckBox3);
            panel1.Controls.Add(label9);
            panel1.Controls.Add(label10);
            panel1.Controls.Add(label11);
            panel1.Controls.Add(label12);
            panel1.Controls.Add(altKeyCheckBox2);
            panel1.Controls.Add(controlKeyCheckBox2);
            panel1.Controls.Add(shiftKeyCheckBox2);
            panel1.Controls.Add(rightButtonCheckBox2);
            panel1.Controls.Add(middleButtonCheckBox2);
            panel1.Controls.Add(leftButtonCheckBox2);
            panel1.Controls.Add(label5);
            panel1.Controls.Add(label6);
            panel1.Controls.Add(label7);
            panel1.Controls.Add(label8);
            panel1.Controls.Add(altKeyCheckBox1);
            panel1.Controls.Add(controlKeyCheckBox1);
            panel1.Controls.Add(shiftKeyCheckBox1);
            panel1.Controls.Add(rightButtonCheckBox1);
            panel1.Controls.Add(middleButtonCheckBox1);
            panel1.Controls.Add(leftButtonCheckBox1);
            panel1.Controls.Add(label4);
            panel1.Controls.Add(label3);
            panel1.Controls.Add(label2);
            panel1.Controls.Add(label1);
            panel1.Dock = DockStyle.Right;
            panel1.Location = new Point(398, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(363, 772);
            panel1.TabIndex = 0;
            // 
            // maxCameraDistanceComboBox
            // 
            maxCameraDistanceComboBox.FormattingEnabled = true;
            maxCameraDistanceComboBox.Items.AddRange(new object[] { "float.NaN", "500", "1000", "5000" });
            maxCameraDistanceComboBox.Location = new Point(190, 686);
            maxCameraDistanceComboBox.Name = "maxCameraDistanceComboBox";
            maxCameraDistanceComboBox.Size = new Size(151, 28);
            maxCameraDistanceComboBox.TabIndex = 42;
            maxCameraDistanceComboBox.SelectedIndexChanged += MaxCameraDistanceComboBox_SelectedIndexChanged;
            // 
            // mouseMoveThresholdComboBox
            // 
            mouseMoveThresholdComboBox.FormattingEnabled = true;
            mouseMoveThresholdComboBox.Location = new Point(190, 652);
            mouseMoveThresholdComboBox.Name = "mouseMoveThresholdComboBox";
            mouseMoveThresholdComboBox.Size = new Size(151, 28);
            mouseMoveThresholdComboBox.TabIndex = 41;
            mouseMoveThresholdComboBox.SelectedIndexChanged += PointerMoveThresholdComboBox_SelectedIndexChanged;
            // 
            // label16
            // 
            label16.AutoSize = true;
            label16.Location = new Point(11, 689);
            label16.Name = "label16";
            label16.Size = new Size(148, 20);
            label16.TabIndex = 40;
            label16.Text = "MaxCameraDistance:";
            // 
            // label15
            // 
            label15.AutoSize = true;
            label15.Location = new Point(11, 655);
            label15.Name = "label15";
            label15.Size = new Size(160, 20);
            label15.TabIndex = 39;
            label15.Text = "PointerMoveThreshold:";
            // 
            // mouseWheelDistanceChangeFactorComboBox
            // 
            mouseWheelDistanceChangeFactorComboBox.FormattingEnabled = true;
            mouseWheelDistanceChangeFactorComboBox.Location = new Point(262, 618);
            mouseWheelDistanceChangeFactorComboBox.Name = "mouseWheelDistanceChangeFactorComboBox";
            mouseWheelDistanceChangeFactorComboBox.Size = new Size(79, 28);
            mouseWheelDistanceChangeFactorComboBox.TabIndex = 38;
            mouseWheelDistanceChangeFactorComboBox.SelectedIndexChanged += PointerWheelDistanceChangeFactorComboBox_SelectedIndexChanged;
            // 
            // label14
            // 
            label14.AutoSize = true;
            label14.Location = new Point(11, 621);
            label14.Name = "label14";
            label14.Size = new Size(247, 20);
            label14.TabIndex = 37;
            label14.Text = "PointerWheelDistanceChangeFactor:";
            // 
            // isMouseWheelZoomEnabledCheckBox
            // 
            isMouseWheelZoomEnabledCheckBox.AutoSize = true;
            isMouseWheelZoomEnabledCheckBox.Checked = true;
            isMouseWheelZoomEnabledCheckBox.CheckState = CheckState.Checked;
            isMouseWheelZoomEnabledCheckBox.Location = new Point(11, 582);
            isMouseWheelZoomEnabledCheckBox.Name = "isMouseWheelZoomEnabledCheckBox";
            isMouseWheelZoomEnabledCheckBox.Size = new Size(223, 24);
            isMouseWheelZoomEnabledCheckBox.TabIndex = 36;
            isMouseWheelZoomEnabledCheckBox.Text = "IsPointerWheelZoomEnabled";
            isMouseWheelZoomEnabledCheckBox.UseVisualStyleBackColor = true;
            isMouseWheelZoomEnabledCheckBox.CheckedChanged += IsPointerWheelZoomEnabledCheckBox_CheckedChanged;
            // 
            // useMousePositionForMovementSpeedCheckBox
            // 
            useMousePositionForMovementSpeedCheckBox.AutoSize = true;
            useMousePositionForMovementSpeedCheckBox.Checked = true;
            useMousePositionForMovementSpeedCheckBox.CheckState = CheckState.Checked;
            useMousePositionForMovementSpeedCheckBox.Location = new Point(11, 556);
            useMousePositionForMovementSpeedCheckBox.Name = "useMousePositionForMovementSpeedCheckBox";
            useMousePositionForMovementSpeedCheckBox.Size = new Size(287, 24);
            useMousePositionForMovementSpeedCheckBox.TabIndex = 35;
            useMousePositionForMovementSpeedCheckBox.Text = "UsePointerPositionForMovementSpeed";
            useMousePositionForMovementSpeedCheckBox.UseVisualStyleBackColor = true;
            useMousePositionForMovementSpeedCheckBox.CheckedChanged += UsePointerPositionForMovementSpeedCheckBox_CheckedChanged;
            // 
            // isYAxisInvertedCheckBox
            // 
            isYAxisInvertedCheckBox.AutoSize = true;
            isYAxisInvertedCheckBox.Location = new Point(11, 524);
            isYAxisInvertedCheckBox.Name = "isYAxisInvertedCheckBox";
            isYAxisInvertedCheckBox.Size = new Size(129, 24);
            isYAxisInvertedCheckBox.TabIndex = 34;
            isYAxisInvertedCheckBox.Text = "IsYAxisInverted";
            isYAxisInvertedCheckBox.UseVisualStyleBackColor = true;
            isYAxisInvertedCheckBox.CheckedChanged += IsYAxisInvertedCheckBox_CheckedChanged;
            // 
            // isXAxisInvertedCheckBox
            // 
            isXAxisInvertedCheckBox.AutoSize = true;
            isXAxisInvertedCheckBox.Location = new Point(11, 498);
            isXAxisInvertedCheckBox.Name = "isXAxisInvertedCheckBox";
            isXAxisInvertedCheckBox.Size = new Size(131, 24);
            isXAxisInvertedCheckBox.TabIndex = 33;
            isXAxisInvertedCheckBox.Text = "IsXAxisInverted";
            isXAxisInvertedCheckBox.UseVisualStyleBackColor = true;
            isXAxisInvertedCheckBox.CheckedChanged += IsXAxisInvertedCheckBox_CheckedChanged;
            // 
            // zoomModeComboBox
            // 
            zoomModeComboBox.FormattingEnabled = true;
            zoomModeComboBox.Items.AddRange(new object[] { "ViewCenter", "CameraRotationCenterPosition", "PointerPosition" });
            zoomModeComboBox.Location = new Point(108, 456);
            zoomModeComboBox.Name = "zoomModeComboBox";
            zoomModeComboBox.Size = new Size(233, 28);
            zoomModeComboBox.TabIndex = 32;
            zoomModeComboBox.SelectedIndexChanged += ZoomModeComboBox_SelectedIndexChanged;
            // 
            // label13
            // 
            label13.AutoSize = true;
            label13.Location = new Point(11, 459);
            label13.Name = "label13";
            label13.Size = new Size(91, 20);
            label13.TabIndex = 31;
            label13.Text = "ZoomMode:";
            // 
            // rotateAroundMousePositionCheckBox
            // 
            rotateAroundMousePositionCheckBox.AutoSize = true;
            rotateAroundMousePositionCheckBox.Checked = true;
            rotateAroundMousePositionCheckBox.CheckState = CheckState.Checked;
            rotateAroundMousePositionCheckBox.Location = new Point(11, 432);
            rotateAroundMousePositionCheckBox.Name = "rotateAroundMousePositionCheckBox";
            rotateAroundMousePositionCheckBox.Size = new Size(222, 24);
            rotateAroundMousePositionCheckBox.TabIndex = 30;
            rotateAroundMousePositionCheckBox.Text = "RotateAroundPointerPosition";
            rotateAroundMousePositionCheckBox.UseVisualStyleBackColor = true;
            rotateAroundMousePositionCheckBox.CheckedChanged += RotateAroundPointerPositionCheckBox_CheckedChanged;
            // 
            // altKeyCheckBox3
            // 
            altKeyCheckBox3.AutoSize = true;
            altKeyCheckBox3.Location = new Point(144, 395);
            altKeyCheckBox3.Name = "altKeyCheckBox3";
            altKeyCheckBox3.Size = new Size(50, 24);
            altKeyCheckBox3.TabIndex = 29;
            altKeyCheckBox3.Text = "Alt";
            altKeyCheckBox3.UseVisualStyleBackColor = true;
            altKeyCheckBox3.CheckedChanged += OnQuickZoomCheckBoxChanged;
            // 
            // controlKeyCheckBox3
            // 
            controlKeyCheckBox3.AutoSize = true;
            controlKeyCheckBox3.Location = new Point(144, 375);
            controlKeyCheckBox3.Name = "controlKeyCheckBox3";
            controlKeyCheckBox3.Size = new Size(80, 24);
            controlKeyCheckBox3.TabIndex = 28;
            controlKeyCheckBox3.Text = "Control";
            controlKeyCheckBox3.UseVisualStyleBackColor = true;
            controlKeyCheckBox3.CheckedChanged += OnQuickZoomCheckBoxChanged;
            // 
            // shiftKeyCheckBox3
            // 
            shiftKeyCheckBox3.AutoSize = true;
            shiftKeyCheckBox3.Location = new Point(144, 355);
            shiftKeyCheckBox3.Name = "shiftKeyCheckBox3";
            shiftKeyCheckBox3.Size = new Size(61, 24);
            shiftKeyCheckBox3.TabIndex = 27;
            shiftKeyCheckBox3.Text = "Shift";
            shiftKeyCheckBox3.UseVisualStyleBackColor = true;
            shiftKeyCheckBox3.CheckedChanged += OnQuickZoomCheckBoxChanged;
            // 
            // rightButtonCheckBox3
            // 
            rightButtonCheckBox3.AutoSize = true;
            rightButtonCheckBox3.Checked = true;
            rightButtonCheckBox3.CheckState = CheckState.Checked;
            rightButtonCheckBox3.Location = new Point(11, 395);
            rightButtonCheckBox3.Name = "rightButtonCheckBox3";
            rightButtonCheckBox3.Size = new Size(66, 24);
            rightButtonCheckBox3.TabIndex = 26;
            rightButtonCheckBox3.Text = "Right";
            rightButtonCheckBox3.UseVisualStyleBackColor = true;
            rightButtonCheckBox3.CheckedChanged += OnQuickZoomCheckBoxChanged;
            // 
            // middleButtonCheckBox3
            // 
            middleButtonCheckBox3.AutoSize = true;
            middleButtonCheckBox3.Location = new Point(11, 375);
            middleButtonCheckBox3.Name = "middleButtonCheckBox3";
            middleButtonCheckBox3.Size = new Size(78, 24);
            middleButtonCheckBox3.TabIndex = 25;
            middleButtonCheckBox3.Text = "Middle";
            middleButtonCheckBox3.UseVisualStyleBackColor = true;
            middleButtonCheckBox3.CheckedChanged += OnQuickZoomCheckBoxChanged;
            // 
            // leftButtonCheckBox3
            // 
            leftButtonCheckBox3.AutoSize = true;
            leftButtonCheckBox3.Checked = true;
            leftButtonCheckBox3.CheckState = CheckState.Checked;
            leftButtonCheckBox3.Location = new Point(11, 355);
            leftButtonCheckBox3.Name = "leftButtonCheckBox3";
            leftButtonCheckBox3.Size = new Size(56, 24);
            leftButtonCheckBox3.TabIndex = 24;
            leftButtonCheckBox3.Text = "Left";
            leftButtonCheckBox3.UseVisualStyleBackColor = true;
            leftButtonCheckBox3.CheckedChanged += OnQuickZoomCheckBoxChanged;
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Font = new Font("Segoe UI", 9F, FontStyle.Underline);
            label9.Location = new Point(139, 332);
            label9.Name = "label9";
            label9.Size = new Size(101, 20);
            label9.TabIndex = 23;
            label9.Text = "Modifier keys:";
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Font = new Font("Segoe UI", 9F, FontStyle.Underline);
            label10.Location = new Point(6, 332);
            label10.Name = "label10";
            label10.Size = new Size(104, 20);
            label10.TabIndex = 22;
            label10.Text = "Mouse button:";
            // 
            // label11
            // 
            label11.AutoSize = true;
            label11.Location = new Point(6, 312);
            label11.Name = "label11";
            label11.Size = new Size(132, 20);
            label11.TabIndex = 21;
            label11.Text = "(default: Disabled)";
            // 
            // label12
            // 
            label12.AutoSize = true;
            label12.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            label12.Location = new Point(6, 292);
            label12.Name = "label12";
            label12.Size = new Size(168, 20);
            label12.TabIndex = 20;
            label12.Text = "QuickZoomConditions:";
            // 
            // altKeyCheckBox2
            // 
            altKeyCheckBox2.AutoSize = true;
            altKeyCheckBox2.Location = new Point(144, 254);
            altKeyCheckBox2.Name = "altKeyCheckBox2";
            altKeyCheckBox2.Size = new Size(50, 24);
            altKeyCheckBox2.TabIndex = 19;
            altKeyCheckBox2.Text = "Alt";
            altKeyCheckBox2.UseVisualStyleBackColor = true;
            altKeyCheckBox2.CheckedChanged += OnMoveCheckBoxChanged;
            // 
            // controlKeyCheckBox2
            // 
            controlKeyCheckBox2.AutoSize = true;
            controlKeyCheckBox2.Checked = true;
            controlKeyCheckBox2.CheckState = CheckState.Checked;
            controlKeyCheckBox2.Location = new Point(144, 234);
            controlKeyCheckBox2.Name = "controlKeyCheckBox2";
            controlKeyCheckBox2.Size = new Size(80, 24);
            controlKeyCheckBox2.TabIndex = 18;
            controlKeyCheckBox2.Text = "Control";
            controlKeyCheckBox2.UseVisualStyleBackColor = true;
            controlKeyCheckBox2.CheckedChanged += OnMoveCheckBoxChanged;
            // 
            // shiftKeyCheckBox2
            // 
            shiftKeyCheckBox2.AutoSize = true;
            shiftKeyCheckBox2.Location = new Point(144, 214);
            shiftKeyCheckBox2.Name = "shiftKeyCheckBox2";
            shiftKeyCheckBox2.Size = new Size(61, 24);
            shiftKeyCheckBox2.TabIndex = 17;
            shiftKeyCheckBox2.Text = "Shift";
            shiftKeyCheckBox2.UseVisualStyleBackColor = true;
            shiftKeyCheckBox2.CheckedChanged += OnMoveCheckBoxChanged;
            // 
            // rightButtonCheckBox2
            // 
            rightButtonCheckBox2.AutoSize = true;
            rightButtonCheckBox2.Location = new Point(11, 254);
            rightButtonCheckBox2.Name = "rightButtonCheckBox2";
            rightButtonCheckBox2.Size = new Size(66, 24);
            rightButtonCheckBox2.TabIndex = 16;
            rightButtonCheckBox2.Text = "Right";
            rightButtonCheckBox2.UseVisualStyleBackColor = true;
            rightButtonCheckBox2.CheckedChanged += OnMoveCheckBoxChanged;
            // 
            // middleButtonCheckBox2
            // 
            middleButtonCheckBox2.AutoSize = true;
            middleButtonCheckBox2.Location = new Point(11, 234);
            middleButtonCheckBox2.Name = "middleButtonCheckBox2";
            middleButtonCheckBox2.Size = new Size(78, 24);
            middleButtonCheckBox2.TabIndex = 15;
            middleButtonCheckBox2.Text = "Middle";
            middleButtonCheckBox2.UseVisualStyleBackColor = true;
            middleButtonCheckBox2.CheckedChanged += OnMoveCheckBoxChanged;
            // 
            // leftButtonCheckBox2
            // 
            leftButtonCheckBox2.AutoSize = true;
            leftButtonCheckBox2.Checked = true;
            leftButtonCheckBox2.CheckState = CheckState.Checked;
            leftButtonCheckBox2.Location = new Point(11, 214);
            leftButtonCheckBox2.Name = "leftButtonCheckBox2";
            leftButtonCheckBox2.Size = new Size(56, 24);
            leftButtonCheckBox2.TabIndex = 14;
            leftButtonCheckBox2.Text = "Left";
            leftButtonCheckBox2.UseVisualStyleBackColor = true;
            leftButtonCheckBox2.CheckedChanged += OnMoveCheckBoxChanged;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Font = new Font("Segoe UI", 9F, FontStyle.Underline);
            label5.Location = new Point(139, 191);
            label5.Name = "label5";
            label5.Size = new Size(101, 20);
            label5.TabIndex = 13;
            label5.Text = "Modifier keys:";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Font = new Font("Segoe UI", 9F, FontStyle.Underline);
            label6.Location = new Point(6, 191);
            label6.Name = "label6";
            label6.Size = new Size(104, 20);
            label6.TabIndex = 12;
            label6.Text = "Mouse button:";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(6, 171);
            label7.Name = "label7";
            label7.Size = new Size(235, 20);
            label7.TabIndex = 11;
            label7.Text = "(default: Ctrl + Left mouse button)";
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            label8.Location = new Point(6, 151);
            label8.Name = "label8";
            label8.Size = new Size(176, 20);
            label8.TabIndex = 10;
            label8.Text = "MoveCameraConditions";
            // 
            // altKeyCheckBox1
            // 
            altKeyCheckBox1.AutoSize = true;
            altKeyCheckBox1.Location = new Point(144, 117);
            altKeyCheckBox1.Name = "altKeyCheckBox1";
            altKeyCheckBox1.Size = new Size(50, 24);
            altKeyCheckBox1.TabIndex = 9;
            altKeyCheckBox1.Text = "Alt";
            altKeyCheckBox1.UseVisualStyleBackColor = true;
            altKeyCheckBox1.CheckedChanged += OnRotateCheckBoxChanged;
            // 
            // controlKeyCheckBox1
            // 
            controlKeyCheckBox1.AutoSize = true;
            controlKeyCheckBox1.Location = new Point(144, 97);
            controlKeyCheckBox1.Name = "controlKeyCheckBox1";
            controlKeyCheckBox1.Size = new Size(80, 24);
            controlKeyCheckBox1.TabIndex = 8;
            controlKeyCheckBox1.Text = "Control";
            controlKeyCheckBox1.UseVisualStyleBackColor = true;
            controlKeyCheckBox1.CheckedChanged += OnRotateCheckBoxChanged;
            // 
            // shiftKeyCheckBox1
            // 
            shiftKeyCheckBox1.AutoSize = true;
            shiftKeyCheckBox1.Location = new Point(144, 77);
            shiftKeyCheckBox1.Name = "shiftKeyCheckBox1";
            shiftKeyCheckBox1.Size = new Size(61, 24);
            shiftKeyCheckBox1.TabIndex = 7;
            shiftKeyCheckBox1.Text = "Shift";
            shiftKeyCheckBox1.UseVisualStyleBackColor = true;
            shiftKeyCheckBox1.CheckedChanged += OnRotateCheckBoxChanged;
            // 
            // rightButtonCheckBox1
            // 
            rightButtonCheckBox1.AutoSize = true;
            rightButtonCheckBox1.Location = new Point(11, 117);
            rightButtonCheckBox1.Name = "rightButtonCheckBox1";
            rightButtonCheckBox1.Size = new Size(66, 24);
            rightButtonCheckBox1.TabIndex = 6;
            rightButtonCheckBox1.Text = "Right";
            rightButtonCheckBox1.UseVisualStyleBackColor = true;
            rightButtonCheckBox1.CheckedChanged += OnRotateCheckBoxChanged;
            // 
            // middleButtonCheckBox1
            // 
            middleButtonCheckBox1.AutoSize = true;
            middleButtonCheckBox1.Location = new Point(11, 97);
            middleButtonCheckBox1.Name = "middleButtonCheckBox1";
            middleButtonCheckBox1.Size = new Size(78, 24);
            middleButtonCheckBox1.TabIndex = 5;
            middleButtonCheckBox1.Text = "Middle";
            middleButtonCheckBox1.UseVisualStyleBackColor = true;
            middleButtonCheckBox1.CheckedChanged += OnRotateCheckBoxChanged;
            // 
            // leftButtonCheckBox1
            // 
            leftButtonCheckBox1.AutoSize = true;
            leftButtonCheckBox1.Checked = true;
            leftButtonCheckBox1.CheckState = CheckState.Checked;
            leftButtonCheckBox1.Location = new Point(11, 77);
            leftButtonCheckBox1.Name = "leftButtonCheckBox1";
            leftButtonCheckBox1.Size = new Size(56, 24);
            leftButtonCheckBox1.TabIndex = 4;
            leftButtonCheckBox1.Text = "Left";
            leftButtonCheckBox1.UseVisualStyleBackColor = true;
            leftButtonCheckBox1.CheckedChanged += OnRotateCheckBoxChanged;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Font = new Font("Segoe UI", 9F, FontStyle.Underline);
            label4.Location = new Point(139, 54);
            label4.Name = "label4";
            label4.Size = new Size(101, 20);
            label4.TabIndex = 3;
            label4.Text = "Modifier keys:";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Segoe UI", 9F, FontStyle.Underline);
            label3.Location = new Point(6, 54);
            label3.Name = "label3";
            label3.Size = new Size(104, 20);
            label3.TabIndex = 2;
            label3.Text = "Mouse button:";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(6, 34);
            label2.Name = "label2";
            label2.Size = new Size(194, 20);
            label2.TabIndex = 1;
            label2.Text = "(default: Left mouse button)";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            label1.Location = new Point(6, 14);
            label1.Name = "label1";
            label1.Size = new Size(188, 20);
            label1.TabIndex = 0;
            label1.Text = "RotateCameraConditions:";
            // 
            // panel2
            // 
            panel2.Controls.Add(label18);
            panel2.Controls.Add(label17);
            panel2.Controls.Add(mainSceneView);
            panel2.Dock = DockStyle.Fill;
            panel2.Location = new Point(0, 0);
            panel2.Name = "panel2";
            panel2.Size = new Size(398, 772);
            panel2.TabIndex = 1;
            // 
            // label18
            // 
            label18.AutoSize = true;
            label18.Font = new Font("Segoe UI", 10F);
            label18.Location = new Point(7, 43);
            label18.Name = "label18";
            label18.Size = new Size(788, 23);
            label18.TabIndex = 2;
            label18.Text = "PointerCameraController enables rotating, moving and zooming the camera with the mouse or touch.";
            // 
            // label17
            // 
            label17.AutoSize = true;
            label17.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            label17.Location = new Point(3, 9);
            label17.Name = "label17";
            label17.Size = new Size(299, 32);
            label17.TabIndex = 1;
            label17.Text = "PointerCameraController";
            // 
            // mainSceneView
            // 
            mainSceneView.Dock = DockStyle.Fill;
            mainSceneView.Location = new Point(0, 0);
            mainSceneView.Name = "mainSceneView";
            mainSceneView.MultisampleCount = 4;
            mainSceneView.PresentationType = PresentationTypes.SharedTexture;
            mainSceneView.Size = new Size(398, 772);
            mainSceneView.StopRenderingWhenHidden = true;
            mainSceneView.TabIndex = 0;
            mainSceneView.WaitForVSync = false;
            // 
            // PointerCameraControllerSample
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(panel2);
            Controls.Add(panel1);
            Name = "PointerCameraControllerSample";
            Size = new Size(761, 772);
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            panel2.ResumeLayout(false);
            panel2.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private Panel panel1;
        private CheckBox rightButtonCheckBox1;
        private CheckBox middleButtonCheckBox1;
        private CheckBox leftButtonCheckBox1;
        private Label label4;
        private Label label3;
        private Label label2;
        private Label label1;
        private Panel panel2;
        private SharpEngine.WinForms.SharpEngineSceneView mainSceneView;
        private CheckBox altKeyCheckBox1;
        private CheckBox controlKeyCheckBox1;
        private CheckBox shiftKeyCheckBox1;
        private CheckBox altKeyCheckBox3;
        private CheckBox controlKeyCheckBox3;
        private CheckBox shiftKeyCheckBox3;
        private CheckBox rightButtonCheckBox3;
        private CheckBox middleButtonCheckBox3;
        private CheckBox leftButtonCheckBox3;
        private Label label9;
        private Label label10;
        private Label label11;
        private Label label12;
        private CheckBox altKeyCheckBox2;
        private CheckBox controlKeyCheckBox2;
        private CheckBox shiftKeyCheckBox2;
        private CheckBox rightButtonCheckBox2;
        private CheckBox middleButtonCheckBox2;
        private CheckBox leftButtonCheckBox2;
        private Label label5;
        private Label label6;
        private Label label7;
        private Label label8;
        private CheckBox rotateAroundMousePositionCheckBox;
        private ComboBox zoomModeComboBox;
        private Label label13;
        private CheckBox isMouseWheelZoomEnabledCheckBox;
        private CheckBox useMousePositionForMovementSpeedCheckBox;
        private CheckBox isYAxisInvertedCheckBox;
        private CheckBox isXAxisInvertedCheckBox;
        private ComboBox mouseWheelDistanceChangeFactorComboBox;
        private Label label14;
        private ComboBox maxCameraDistanceComboBox;
        private ComboBox mouseMoveThresholdComboBox;
        private Label label16;
        private Label label15;
        private Label label18;
        private Label label17;
    }
}
