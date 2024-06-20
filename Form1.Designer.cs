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
            tabControl1 = new TabControl();
            tabPage1 = new TabPage();
            tabPage2 = new TabPage();
            listlog = new ListBox();
            label1 = new Label();
            review = new Button();
            Location = new TextBox();
            StartStop = new Button();
            panel1.SuspendLayout();
            tabControl1.SuspendLayout();
            tabPage1.SuspendLayout();
            SuspendLayout();
            // 
            // panel1
            // 
            panel1.Controls.Add(tabControl1);
            panel1.Dock = DockStyle.Fill;
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(390, 492);
            panel1.TabIndex = 0;
            // 
            // tabControl1
            // 
            tabControl1.Controls.Add(tabPage1);
            tabControl1.Controls.Add(tabPage2);
            tabControl1.Dock = DockStyle.Fill;
            tabControl1.Location = new Point(0, 0);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(390, 492);
            tabControl1.TabIndex = 0;
            // 
            // tabPage1
            // 
            tabPage1.Controls.Add(listlog);
            tabPage1.Controls.Add(label1);
            tabPage1.Controls.Add(review);
            tabPage1.Controls.Add(Location);
            tabPage1.Controls.Add(StartStop);
            tabPage1.Location = new Point(4, 29);
            tabPage1.Name = "tabPage1";
            tabPage1.Padding = new Padding(3);
            tabPage1.Size = new Size(382, 459);
            tabPage1.TabIndex = 0;
            tabPage1.Text = "tabPage1";
            tabPage1.UseVisualStyleBackColor = true;
            // 
            // tabPage2
            // 
            tabPage2.Location = new Point(4, 29);
            tabPage2.Name = "tabPage2";
            tabPage2.Padding = new Padding(3);
            tabPage2.Size = new Size(0, 0);
            tabPage2.TabIndex = 1;
            tabPage2.Text = "tabPage2";
            tabPage2.UseVisualStyleBackColor = true;
            // 
            // listlog
            // 
            listlog.Dock = DockStyle.Bottom;
            listlog.FormattingEnabled = true;
            listlog.ItemHeight = 20;
            listlog.Location = new Point(3, 172);
            listlog.Name = "listlog";
            listlog.Size = new Size(376, 284);
            listlog.TabIndex = 20;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 15F, FontStyle.Regular, GraphicsUnit.Point);
            label1.Location = new Point(146, 3);
            label1.Name = "label1";
            label1.Size = new Size(81, 35);
            label1.TabIndex = 19;
            label1.Text = "label1";
            // 
            // review
            // 
            review.Location = new Point(289, 124);
            review.Name = "review";
            review.Size = new Size(94, 29);
            review.TabIndex = 18;
            review.Text = "Обзор";
            review.UseVisualStyleBackColor = true;
            review.Click += review_Click;
            // 
            // Location
            // 
            Location.Location = new Point(8, 124);
            Location.Name = "Location";
            Location.Size = new Size(275, 27);
            Location.TabIndex = 17;
            // 
            // StartStop
            // 
            StartStop.BackColor = Color.Green;
            StartStop.Location = new Point(75, 48);
            StartStop.Name = "StartStop";
            StartStop.Size = new Size(218, 70);
            StartStop.TabIndex = 16;
            StartStop.Text = "Старт";
            StartStop.UseVisualStyleBackColor = false;
            StartStop.Click += StartStop_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(390, 492);
            Controls.Add(panel1);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Name = "Form1";
            ShowIcon = false;
            Text = "Симулятор";
            panel1.ResumeLayout(false);
            tabControl1.ResumeLayout(false);
            tabPage1.ResumeLayout(false);
            tabPage1.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private Panel panel1;
        private TabControl tabControl1;
        private TabPage tabPage1;
        private ListBox listlog;
        private Label label1;
        private Button review;
        private new TextBox Location;
        private Button StartStop;
        private TabPage tabPage2;
    }
}