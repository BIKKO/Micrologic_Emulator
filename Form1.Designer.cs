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
            components = new System.ComponentModel.Container();
            panel1 = new Panel();
            tabControl1 = new TabControl();
            tabPage1 = new TabPage();
            SaveTextRang = new Button();
            listlog = new ListBox();
            label1 = new Label();
            review = new Button();
            Location = new TextBox();
            StartStop = new Button();
            tabPage2 = new TabPage();
            tabControl2 = new TabControl();
            tabPage3 = new TabPage();
            groupBox1 = new GroupBox();
            ClearAdd = new Button();
            DoneAdd = new Button();
            AdresAdd = new TextBox();
            NameAdd = new TextBox();
            label3 = new Label();
            label2 = new Label();
            groupBox2 = new GroupBox();
            DoneUpdete = new Button();
            AdresUpdate = new TextBox();
            Delete = new Button();
            comboBox1 = new ComboBox();
            tabPage4 = new TabPage();
            IP_4 = new TextBox();
            IP_3 = new TextBox();
            IP_2 = new TextBox();
            ServerOk = new Button();
            label6 = new Label();
            label5 = new Label();
            label4 = new Label();
            textBox3 = new TextBox();
            textBox2 = new TextBox();
            IP_1 = new TextBox();
            saveFileDialog1 = new SaveFileDialog();
            openFileDialog1 = new OpenFileDialog();
            TONupdate = new System.Windows.Forms.Timer(components);
            panel1.SuspendLayout();
            tabControl1.SuspendLayout();
            tabPage1.SuspendLayout();
            tabPage2.SuspendLayout();
            tabControl2.SuspendLayout();
            tabPage3.SuspendLayout();
            groupBox1.SuspendLayout();
            groupBox2.SuspendLayout();
            tabPage4.SuspendLayout();
            SuspendLayout();
            // 
            // panel1
            // 
            panel1.Controls.Add(tabControl1);
            panel1.Dock = DockStyle.Fill;
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(390, 465);
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
            tabControl1.Size = new Size(390, 465);
            tabControl1.TabIndex = 0;
            // 
            // tabPage1
            // 
            tabPage1.BorderStyle = BorderStyle.FixedSingle;
            tabPage1.Controls.Add(SaveTextRang);
            tabPage1.Controls.Add(listlog);
            tabPage1.Controls.Add(label1);
            tabPage1.Controls.Add(review);
            tabPage1.Controls.Add(Location);
            tabPage1.Controls.Add(StartStop);
            tabPage1.Location = new Point(4, 29);
            tabPage1.Name = "tabPage1";
            tabPage1.Padding = new Padding(3);
            tabPage1.Size = new Size(382, 432);
            tabPage1.TabIndex = 0;
            tabPage1.Text = "Главная";
            tabPage1.UseVisualStyleBackColor = true;
            // 
            // SaveTextRang
            // 
            SaveTextRang.Location = new Point(289, 88);
            SaveTextRang.Name = "SaveTextRang";
            SaveTextRang.Size = new Size(92, 30);
            SaveTextRang.TabIndex = 21;
            SaveTextRang.Text = "Сохранить";
            SaveTextRang.UseVisualStyleBackColor = true;
            SaveTextRang.Click += SaveTextRang_Click;
            // 
            // listlog
            // 
            listlog.Dock = DockStyle.Bottom;
            listlog.FormattingEnabled = true;
            listlog.ItemHeight = 20;
            listlog.Location = new Point(3, 163);
            listlog.Name = "listlog";
            listlog.Size = new Size(374, 264);
            listlog.TabIndex = 20;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 15F, FontStyle.Regular, GraphicsUnit.Point);
            label1.Location = new Point(104, 3);
            label1.Name = "label1";
            label1.Size = new Size(0, 35);
            label1.TabIndex = 19;
            // 
            // review
            // 
            review.Location = new Point(289, 124);
            review.Name = "review";
            review.Size = new Size(92, 30);
            review.TabIndex = 18;
            review.Text = "Обзор";
            review.UseVisualStyleBackColor = true;
            review.Click += review_Click;
            // 
            // Location
            // 
            Location.Location = new Point(8, 126);
            Location.Name = "Location";
            Location.Size = new Size(275, 27);
            Location.TabIndex = 17;
            // 
            // StartStop
            // 
            StartStop.BackColor = Color.Green;
            StartStop.Location = new Point(35, 48);
            StartStop.Name = "StartStop";
            StartStop.Size = new Size(218, 70);
            StartStop.TabIndex = 16;
            StartStop.Text = "Старт";
            StartStop.UseVisualStyleBackColor = false;
            StartStop.Click += StartStop_Click;
            // 
            // tabPage2
            // 
            tabPage2.BorderStyle = BorderStyle.FixedSingle;
            tabPage2.Controls.Add(tabControl2);
            tabPage2.Location = new Point(4, 29);
            tabPage2.Name = "tabPage2";
            tabPage2.Padding = new Padding(3);
            tabPage2.Size = new Size(382, 432);
            tabPage2.TabIndex = 1;
            tabPage2.Text = "Настройни";
            tabPage2.UseVisualStyleBackColor = true;
            // 
            // tabControl2
            // 
            tabControl2.Controls.Add(tabPage3);
            tabControl2.Controls.Add(tabPage4);
            tabControl2.Dock = DockStyle.Fill;
            tabControl2.Location = new Point(3, 3);
            tabControl2.Name = "tabControl2";
            tabControl2.SelectedIndex = 0;
            tabControl2.Size = new Size(374, 424);
            tabControl2.TabIndex = 0;
            // 
            // tabPage3
            // 
            tabPage3.Controls.Add(groupBox1);
            tabPage3.Controls.Add(groupBox2);
            tabPage3.Location = new Point(4, 29);
            tabPage3.Name = "tabPage3";
            tabPage3.Padding = new Padding(3);
            tabPage3.Size = new Size(366, 391);
            tabPage3.TabIndex = 0;
            tabPage3.Text = "Адреса";
            tabPage3.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(ClearAdd);
            groupBox1.Controls.Add(DoneAdd);
            groupBox1.Controls.Add(AdresAdd);
            groupBox1.Controls.Add(NameAdd);
            groupBox1.Controls.Add(label3);
            groupBox1.Controls.Add(label2);
            groupBox1.Dock = DockStyle.Bottom;
            groupBox1.Location = new Point(3, 160);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(360, 228);
            groupBox1.TabIndex = 5;
            groupBox1.TabStop = false;
            groupBox1.Text = "Добавление";
            // 
            // ClearAdd
            // 
            ClearAdd.Location = new Point(279, 184);
            ClearAdd.Name = "ClearAdd";
            ClearAdd.Size = new Size(76, 26);
            ClearAdd.TabIndex = 5;
            ClearAdd.Text = "Очистка";
            ClearAdd.UseVisualStyleBackColor = true;
            ClearAdd.Click += ClearAdd_Click;
            // 
            // DoneAdd
            // 
            DoneAdd.Location = new Point(185, 185);
            DoneAdd.Name = "DoneAdd";
            DoneAdd.Size = new Size(76, 26);
            DoneAdd.TabIndex = 4;
            DoneAdd.Text = "Готово";
            DoneAdd.UseVisualStyleBackColor = true;
            DoneAdd.Click += DoneAdd_Click;
            // 
            // AdresAdd
            // 
            AdresAdd.Location = new Point(36, 135);
            AdresAdd.Name = "AdresAdd";
            AdresAdd.Size = new Size(188, 27);
            AdresAdd.TabIndex = 3;
            // 
            // NameAdd
            // 
            NameAdd.Location = new Point(36, 63);
            NameAdd.Name = "NameAdd";
            NameAdd.Size = new Size(188, 27);
            NameAdd.TabIndex = 2;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(36, 112);
            label3.Name = "label3";
            label3.Size = new Size(51, 20);
            label3.TabIndex = 1;
            label3.Text = "Адрес";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(36, 40);
            label2.Name = "label2";
            label2.Size = new Size(77, 20);
            label2.TabIndex = 0;
            label2.Text = "Название";
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(DoneUpdete);
            groupBox2.Controls.Add(AdresUpdate);
            groupBox2.Controls.Add(Delete);
            groupBox2.Controls.Add(comboBox1);
            groupBox2.Dock = DockStyle.Fill;
            groupBox2.Location = new Point(3, 3);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new Size(360, 385);
            groupBox2.TabIndex = 6;
            groupBox2.TabStop = false;
            groupBox2.Text = "Редактирование";
            // 
            // DoneUpdete
            // 
            DoneUpdete.Location = new Point(189, 99);
            DoneUpdete.Name = "DoneUpdete";
            DoneUpdete.Size = new Size(80, 27);
            DoneUpdete.TabIndex = 3;
            DoneUpdete.Text = "Готово";
            DoneUpdete.UseVisualStyleBackColor = true;
            DoneUpdete.Click += DoneUpdete_Click;
            // 
            // AdresUpdate
            // 
            AdresUpdate.Location = new Point(36, 99);
            AdresUpdate.Name = "AdresUpdate";
            AdresUpdate.Size = new Size(147, 27);
            AdresUpdate.TabIndex = 1;
            // 
            // Delete
            // 
            Delete.Location = new Point(275, 99);
            Delete.Name = "Delete";
            Delete.Size = new Size(80, 27);
            Delete.TabIndex = 2;
            Delete.Text = "Удалить";
            Delete.UseVisualStyleBackColor = true;
            Delete.Click += Delete_Click;
            // 
            // comboBox1
            // 
            comboBox1.FormattingEnabled = true;
            comboBox1.Location = new Point(36, 48);
            comboBox1.Name = "comboBox1";
            comboBox1.Size = new Size(319, 28);
            comboBox1.TabIndex = 0;
            comboBox1.SelectedIndexChanged += comboBox1_SelectedIndexChanged;
            // 
            // tabPage4
            // 
            tabPage4.Controls.Add(IP_4);
            tabPage4.Controls.Add(IP_3);
            tabPage4.Controls.Add(IP_2);
            tabPage4.Controls.Add(ServerOk);
            tabPage4.Controls.Add(label6);
            tabPage4.Controls.Add(label5);
            tabPage4.Controls.Add(label4);
            tabPage4.Controls.Add(textBox3);
            tabPage4.Controls.Add(textBox2);
            tabPage4.Controls.Add(IP_1);
            tabPage4.Location = new Point(4, 29);
            tabPage4.Name = "tabPage4";
            tabPage4.Padding = new Padding(3);
            tabPage4.Size = new Size(366, 391);
            tabPage4.TabIndex = 1;
            tabPage4.Text = "Сервер";
            tabPage4.UseVisualStyleBackColor = true;
            // 
            // IP_4
            // 
            IP_4.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            IP_4.Location = new Point(148, 46);
            IP_4.MaxLength = 3;
            IP_4.Name = "IP_4";
            IP_4.PlaceholderText = "1";
            IP_4.Size = new Size(37, 27);
            IP_4.TabIndex = 9;
            IP_4.TextAlign = HorizontalAlignment.Center;
            IP_4.TextChanged += textBox_TextChanged;
            // 
            // IP_3
            // 
            IP_3.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            IP_3.Location = new Point(105, 46);
            IP_3.MaxLength = 3;
            IP_3.Name = "IP_3";
            IP_3.PlaceholderText = "0";
            IP_3.Size = new Size(37, 27);
            IP_3.TabIndex = 8;
            IP_3.TextAlign = HorizontalAlignment.Center;
            IP_3.TextChanged += textBox_TextChanged;
            // 
            // IP_2
            // 
            IP_2.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            IP_2.Location = new Point(62, 46);
            IP_2.MaxLength = 3;
            IP_2.Name = "IP_2";
            IP_2.PlaceholderText = "0";
            IP_2.Size = new Size(37, 27);
            IP_2.TabIndex = 7;
            IP_2.TextAlign = HorizontalAlignment.Center;
            IP_2.TextChanged += textBox_TextChanged;
            // 
            // ServerOk
            // 
            ServerOk.Location = new Point(263, 356);
            ServerOk.Name = "ServerOk";
            ServerOk.Size = new Size(97, 29);
            ServerOk.TabIndex = 6;
            ServerOk.Text = "Применить";
            ServerOk.UseVisualStyleBackColor = true;
            ServerOk.Click += ServerOk_Click;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(19, 184);
            label6.Name = "label6";
            label6.Size = new Size(59, 20);
            label6.TabIndex = 5;
            label6.Text = "SlaveID";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(19, 100);
            label5.Name = "label5";
            label5.Size = new Size(35, 20);
            label5.TabIndex = 4;
            label5.Text = "Port";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(19, 19);
            label4.Name = "label4";
            label4.Size = new Size(21, 20);
            label4.TabIndex = 3;
            label4.Text = "IP";
            // 
            // textBox3
            // 
            textBox3.Location = new Point(19, 207);
            textBox3.Name = "textBox3";
            textBox3.Size = new Size(95, 27);
            textBox3.TabIndex = 2;
            // 
            // textBox2
            // 
            textBox2.Location = new Point(19, 123);
            textBox2.Name = "textBox2";
            textBox2.Size = new Size(95, 27);
            textBox2.TabIndex = 1;
            // 
            // IP_1
            // 
            IP_1.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            IP_1.Location = new Point(19, 46);
            IP_1.MaxLength = 3;
            IP_1.Name = "IP_1";
            IP_1.PlaceholderText = "127";
            IP_1.Size = new Size(37, 27);
            IP_1.TabIndex = 0;
            IP_1.TextAlign = HorizontalAlignment.Center;
            IP_1.TextChanged += textBox_TextChanged;
            // 
            // openFileDialog1
            // 
            openFileDialog1.FileName = "openFileDialog1";
            // 
            // TONupdate
            // 
            TONupdate.Interval = 10;
            TONupdate.Tick += TONupdate_Tick;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(390, 465);
            Controls.Add(panel1);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Name = "Form1";
            ShowIcon = false;
            Text = "Симулятор";
            FormClosing += Form1_FormClosing;
            panel1.ResumeLayout(false);
            tabControl1.ResumeLayout(false);
            tabPage1.ResumeLayout(false);
            tabPage1.PerformLayout();
            tabPage2.ResumeLayout(false);
            tabControl2.ResumeLayout(false);
            tabPage3.ResumeLayout(false);
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            tabPage4.ResumeLayout(false);
            tabPage4.PerformLayout();
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
        private Button SaveTextRang;
        private SaveFileDialog saveFileDialog1;
        private OpenFileDialog openFileDialog1;
        private TabControl tabControl2;
        private TabPage tabPage3;
        private GroupBox groupBox1;
        private Button ClearAdd;
        private Button DoneAdd;
        private TextBox AdresAdd;
        private TextBox NameAdd;
        private Label label3;
        private Label label2;
        private GroupBox groupBox2;
        private Button DoneUpdete;
        private TextBox AdresUpdate;
        private Button Delete;
        private ComboBox comboBox1;
        private TabPage tabPage4;
        private TextBox textBox3;
        private TextBox textBox2;
        private TextBox IP_1;
        private Label label6;
        private Label label5;
        private Label label4;
        private Button ServerOk;
        private TextBox IP_4;
        private TextBox IP_3;
        private TextBox IP_2;
        private System.Windows.Forms.Timer TONupdate;
    }
}