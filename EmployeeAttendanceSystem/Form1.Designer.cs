namespace EmployeeAttendanceSystem
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
            pictureBox1 = new PictureBox();
            labelUpload = new Label();
            buttonUpload = new Button();
            openFileDialog1 = new OpenFileDialog();
            buttonTrain = new Button();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // pictureBox1
            // 
            pictureBox1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            pictureBox1.BorderStyle = BorderStyle.FixedSingle;
            pictureBox1.Location = new Point(124, 120);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(664, 257);
            pictureBox1.TabIndex = 3;
            pictureBox1.TabStop = false;
            // 
            // labelUpload
            // 
            labelUpload.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            labelUpload.AutoSize = true;
            labelUpload.Location = new Point(35, 39);
            labelUpload.Name = "labelUpload";
            labelUpload.Size = new Size(168, 15);
            labelUpload.TabIndex = 6;
            labelUpload.Text = "Upload an Image to Recognize";
            // 
            // buttonUpload
            // 
            buttonUpload.Location = new Point(62, 64);
            buttonUpload.Name = "buttonUpload";
            buttonUpload.Size = new Size(93, 23);
            buttonUpload.TabIndex = 7;
            buttonUpload.Text = "Upload Image";
            buttonUpload.UseVisualStyleBackColor = true;
            buttonUpload.Click += buttonUpload_Click;
            // 
            // openFileDialog1
            // 
            openFileDialog1.FileName = "openFileDialog1";
            // 
            // buttonTrain
            // 
            buttonTrain.Location = new Point(320, 406);
            buttonTrain.Name = "buttonTrain";
            buttonTrain.Size = new Size(171, 23);
            buttonTrain.TabIndex = 9;
            buttonTrain.Text = "Train Model";
            buttonTrain.UseVisualStyleBackColor = true;
            buttonTrain.Click += buttonTrain_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(878, 533);
            Controls.Add(buttonTrain);
            Controls.Add(buttonUpload);
            Controls.Add(labelUpload);
            Controls.Add(pictureBox1);
            Name = "Form1";
            Text = "Facial Recognition";
            Load += Form1_Load;
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private PictureBox pictureBox1;
        private Label labelUpload;
        private Button buttonUpload;
        private OpenFileDialog openFileDialog1;
        private Button buttonTrain;
    }
}