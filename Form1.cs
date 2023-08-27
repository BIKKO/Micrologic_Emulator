using EasyModbus;

namespace ModbasServer
{
    public partial class Form1 : Form
    {
        bool start_stop = false;
        ModbusServer server;

        

        public Form1()
        {
            InitializeComponent();
            Information.Text = "Статус: не активен";
        }

        private void transfer(string Text)
        {

            ModbusServer.HoldingRegisters reg = server.holdingRegisters;
            int count = 8001;
            var rang = File.ReadAllLines(Text);
            foreach (var line in rang)
            {
                short temp = 0;
                for (int i = 0; i < line.Length;)
                {
                    if (line[i] != 0) 
                    {   
                        temp = (short)line[i];
                        i++;
                    }
                    if (i < line.Length)
                    {
                        temp |= (short)((short)line[i] << 8);
                        i++;
                        reg[count] = temp;
                        count++;
                    }
                }

                //progressBar1.Value += 1;
            }
        }

        private void review_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.ShowDialog();
            //MessageBox.Show(ofd.FileName); 
            Location.Text = ofd.FileName;
        }

        private void StartStop_Click(object sender, EventArgs e)
        {
            if (Location.Text != "")
            {
                switch (start_stop)
                {
                    case true:
                        {
                            server.StopListening();
                            server = null;
                            Information.Text = "Статус: не активен";
                            start_stop = !start_stop;
                            break;
                        }
                    case false:
                        {
                            server = new ModbusServer();
                            server.Listen();/*
                            ModbusServer.HoldingRegisters reg = server.holdingRegisters;
                            reg[1] = [4324, 234234, 223];*/
                            Information.Text = "Статус: активен";
                            start_stop = !start_stop;
                            var trans = new Task(() =>
                            {
                                transfer(Location.Text);
                            });
                            trans.Start();
                            break;
                        }
                }
            }
            else
            {
                MessageBox.Show("Укажите файл!", "Ошибка!");
                review_Click(sender, e);
            }
        }
    }
}