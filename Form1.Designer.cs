namespace ModbasServer
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
            panel1 = new Panel();
            progressBar1 = new ProgressBar();
            review = new Button();
            Location = new TextBox();
            StartStop = new Button();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // panel1
            // 
            panel1.Controls.Add(progressBar1);
            panel1.Controls.Add(review);
            panel1.Controls.Add(Location);
            panel1.Controls.Add(StartStop);
            panel1.Dock = DockStyle.Fill;
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(554, 288);
            panel1.TabIndex = 0;
            // 
            // progressBar1
            // 
            progressBar1.Location = new Point(68, 238);
            progressBar1.Name = "progressBar1";
            progressBar1.Size = new Size(396, 15);
            progressBar1.TabIndex = 3;
            // 
            // review
            // 
            review.Location = new Point(419, 181);
            review.Name = "review";
            review.Size = new Size(94, 29);
            review.TabIndex = 2;
            review.Text = "Обзор";
            review.UseVisualStyleBackColor = true;
            review.Click += review_Click;
            // 
            // Location
            // 
            Location.Location = new Point(68, 183);
            Location.Name = "Location";
            Location.Size = new Size(345, 27);
            Location.TabIndex = 1;
            // 
            // StartStop
            // 
            StartStop.BackColor = Color.Green;
            StartStop.Location = new Point(68, 33);
            StartStop.Name = "StartStop";
            StartStop.Size = new Size(445, 118);
            StartStop.TabIndex = 0;
            StartStop.Text = "Старт";
            StartStop.UseVisualStyleBackColor = false;
            StartStop.Click += StartStop_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(554, 288);
            Controls.Add(panel1);
            Name = "Form1";
            Text = "Form1";
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private Panel panel1;
        private Button StartStop;
        private TextBox Location;
        private ProgressBar progressBar1;
        private Button review;
    }
}