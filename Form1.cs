using System;
using System.Net;
using System.Net.Sockets;
using Modbus.Device;
using Modbus.Data;
using System.Threading;
using Microsoft.VisualBasic.Logging;
using System.Windows.Forms;
using System.Text.RegularExpressions;


namespace ModbasServer
{
    public partial class Form1 : Form
    {
        private bool start_stop = true;
        private DataStore DataValue;
        private DataStore[] DataRangs;// переделать на Dictionary
        private ModbusSlave mb_tcp_server;
        private Thread ListenThred;
        private Thread GenerateData;
        private int RangAdr;
        private int[] adres = new int[] { 1000, 600, 1200, 2000, 7200, 1300, 7000, 6800 };
        private Dictionary<string, int> Adreses;
        private Dictionary<string, ushort[]> Data;
        private string[] TextRangs;
        private byte[] IP;
        private int Port;
        private byte Slave;


        public Form1()
        {
            InitializeComponent();
            Adreses = new Dictionary<string, int>() { {"T4",1301},
                                                        {"T4_c",7001},
                                                        {"Timer_control",6801},
                                                        {"N13",1001},
                                                        {"N15",601},
                                                        {"N18",1201},
                                                        {"N40",2001},
                                                        {"B3",7201},
                                                        };

            string[] name = Properties.Settings.Default["AdresName"].ToString().Split(",");
            string[] adr = Properties.Settings.Default["Adres"].ToString().Split(",");
            RangAdr = int.Parse(Properties.Settings.Default["Rang"].ToString());

            if (name.Length > 1)
            {
                Adreses.Clear();
                for (int i = 0; i < adr.Length; i++)
                {
                    Adreses.Add(name[i], int.Parse(adr[i]));
                }
            }
            label1.Text = "";

            DataValue = DataStoreFactory.CreateDefaultDataStore();

            comboBox1.Items.Add("Расположение рангов");
            comboBox1.Items.AddRange(Adreses.Keys.ToArray());
            comboBox1.SelectedIndex = 0;
            IP = new byte[] { 0, 0, 0, 0 };
            Port = 502;
            Slave = 1;

            textBox2.PlaceholderText = Port.ToString();
            textBox3.PlaceholderText = Slave.ToString();

        }


        /// <summary>
        /// Конвертация и запись в регистры текста программы
        /// </summary>
        /// <param name="Text">Адрес на файл</param>
        private void transfer(string[] Text)
        {

            DataRangs = new DataStore[TextRangs.Length];
            string line;
            DataStore rang;
            //текст ранга
            for (int ind_Rang = 0; ind_Rang < TextRangs.Length; ind_Rang++)
            {
                int count = 8001 + ind_Rang;
                line = TextRangs[ind_Rang];
                rang = DataStoreFactory.CreateDefaultDataStore();
                ushort temp = 0;
                for (int i = 0; i < 240;)
                {
                    if (line[i] != 0 && i < line.Length)
                    {
                        temp = line[i];
                        i++;
                    }
                    if (i < line.Length)
                    {
                        temp |= (ushort)(line[i] << 8);
                        i++;
                        rang.HoldingRegisters[count] = temp;
                        count++;
                    }
                    if (i == line.Length) break;
                }
                DataRangs[ind_Rang] = rang;
            }
        }

        /// <summary>
        /// Открытие диологового окна
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void review_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "My files (*.LDF)|*.ldf|txt files (*.txt)|*.txt|All files (*.*)|*.*";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
                Location.Text = openFileDialog1.FileName;
        }

        /// <summary>
        /// Запуск и остановка сервера
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StartStop_Click(object sender, EventArgs e)
        {
            try
            {
                switch (start_stop)
                {
                    case false:
                        {
                            mb_tcp_server.ModbusSlaveRequestReceived -= Mb_tcp_server_ModbusSlaveRequestReceived;
                            mb_tcp_server.Dispose();

                            StartStop.BackColor = Color.Green;
                            StartStop.Text = "Старт";
                            start_stop = !start_stop;
                            label1.Text = "";
                            try
                            {
                                GenerateData.Abort(100);
                                ListenThred.Abort(100);
                            }
                            catch
                            {
                                ListenThred.Join();
                                listlog.Items.Add("Server stoped");
                                ListenThred = null;
                                GenerateData = null;
                            }

                            break;
                        }
                    case true:
                        {
                            if (Location.Text != "")
                            {
                                mb_tcp_server = ModbusTcpSlave.CreateTcp(Slave, new TcpListener(new IPAddress(IP), Port));
                                mb_tcp_server.DataStore = DataValue;
                                mb_tcp_server.ModbusSlaveRequestReceived += Mb_tcp_server_ModbusSlaveRequestReceived;

                                start_stop = !start_stop;
                                StartStop.BackColor = Color.Red;
                                StartStop.Text = "Стоп";
                                label1.Text = string.Join(".",IP)=="0.0.0.0"?"127.0.0.1:"+Port: string.Join(".", IP)+':'+Port;



                                if (ListenThred == null)
                                    ListenThred = new Thread(() =>
                                    {
                                        try
                                        {
                                            mb_tcp_server.ListenAsync();
                                        }
                                        catch (ThreadAbortException ex)
                                        {
                                            BeginInvoke(new MethodInvoker(() => { listlog.Items.Add("Thread(th_0) is aborted " + ex.ExceptionState); }));
                                        }
                                    })
                                    {
                                        IsBackground = true,
                                    };
                                ListenThred.Name = "Listen";
                                ListenThred.SetApartmentState(ApartmentState.STA);

                                if (GenerateData == null)
                                    GenerateData = new Thread(() =>
                                    {
                                        try
                                        {
                                            SetData();
                                        }
                                        catch (ThreadAbortException ex)
                                        {
                                            BeginInvoke(new MethodInvoker(() => { listlog.Items.Add("Thread(th_1) is aborted " + ex.ExceptionState); }));
                                        }
                                    })
                                    {
                                        IsBackground = true,
                                    };

                                GenerateData.Name = "SetData";

                                ListenThred.Start();
                                GenerateData.Start();

                                listlog.Items.Add("Server started");
                                if (Location.Text.Contains("ldf"))
                                {
                                    if (TextRangs == null)
                                    {
                                        Data = CreateFile.GetData(Location.Text);
                                        TextRangs = CreateFile.Load(Location.Text, Type.RANG);
                                    }
                                    else if (TextRangs != CreateFile.Load(Location.Text, Type.RANG)) TextRangs = CreateFile.Load(Location.Text, Type.RANG);
                                }
                                else
                                {
                                    if (TextRangs == null) TextRangs = File.ReadAllLines(Location.Text);
                                    else if (TextRangs != File.ReadAllLines(Location.Text)) TextRangs = File.ReadAllLines(Location.Text);
                                }
                                transfer(TextRangs);

                            }
                            break;
                        }
                }
            }
            catch (Exception ex) { MessageBox.Show(ListenThred.ThreadState.ToString() + " " + ex.Message); }
        }

        /// <summary>
        /// Задача и обновление регистров
        /// </summary>
        public void SetData()
        {
            Random random = new Random();
            while (true)
            {
                Thread.Sleep(200);
                foreach (int ad in adres)
                {
                    for (int i = 0; i < 100; i++)
                    {
                        Thread.Sleep(100);
                        DataValue.HoldingRegisters[ad + 1 + i] = (ushort)Math.Abs(random.Next(0, ushort.MaxValue));
                    }
                }
            }
        }

        /// <summary>
        /// Формирование ответа, в зависиости от полученного зпроса
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Mb_tcp_server_ModbusSlaveRequestReceived(object sender, ModbusSlaveRequestEventArgs e)
        {
            //(e.Message.MessageFrame -> byte[])ind:0 - SlaveID; ind:1 - FunCod; ind:2,3 - adres; ind:4,5 - len
            BeginInvoke(new MethodInvoker(() =>
            {
                listlog.Items.Add(e.Message);
                if (listlog.Items.Count > 14) listlog.Items.Remove(listlog.Items[0]);
                listlog.SelectedIndex = listlog.Items.Count - 1;
            }));

            if (((e.Message.MessageFrame[2] * 256 + e.Message.MessageFrame[3]) / 1000 == 8) && (e.Message.FunctionCode == 3))
            {
                if ((e.Message.MessageFrame[2] * 256 + e.Message.MessageFrame[3]) % 100 < DataRangs.Length && (e.Message.MessageFrame[2] * 256 + e.Message.MessageFrame[3]) % 100 >= 0)
                    mb_tcp_server.DataStore = DataRangs[(e.Message.MessageFrame[2] * 256 + e.Message.MessageFrame[3]) % 100];
                else
                    mb_tcp_server.DataStore = DataValue;
            }
            else if (DataRangs.Contains(mb_tcp_server.DataStore)) mb_tcp_server.DataStore = DataValue;
        }

        /// <summary>
        /// Добавление нового адреса
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DoneAdd_Click(object sender, EventArgs e)
        {
            Adreses.Add(NameAdd.Text, int.Parse(AdresAdd.Text) + 1);
            comboBox1.Items.Add(NameAdd.Text);
            NameAdd.Clear();
            AdresAdd.Clear();
        }

        /// <summary>
        /// Очистка полей добавления
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClearAdd_Click(object sender, EventArgs e)
        {
            NameAdd.Clear();
            AdresAdd.Clear();
        }

        /// <summary>
        /// Обновление значений
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DoneUpdete_Click(object sender, EventArgs e)
        {
            if (comboBox1.SelectedItem.ToString() == "Расположение рангов")
            {
                RangAdr = int.Parse(AdresUpdate.Text) + 1;
                return;
            }
            Adreses[comboBox1.SelectedItem.ToString()] = int.Parse(AdresUpdate.Text) + 1;
            Save();
        }

        /// <summary>
        /// Удаление адреса
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Delete_Click(object sender, EventArgs e)
        {
            if (comboBox1.SelectedItem.ToString() == "Расположение рангов") return;
            Adreses.Remove(comboBox1.SelectedItem.ToString());
            comboBox1.Items.Remove(comboBox1.SelectedItem);
            AdresUpdate.Clear();
            comboBox1.SelectedIndex = 0;

            Save();
        }

        /// <summary>
        /// Получение значения адреса регистра, в зависимости от выбранного адреса
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox1.SelectedItem.ToString() == "Расположение рангов")
            {
                Delete.Enabled = false;
                AdresUpdate.Text = (RangAdr - 1).ToString();
                AdresUpdate.Focus();
                return;
            }
            if (!Delete.Enabled) Delete.Enabled = true;
            AdresUpdate.Text = (Adreses[comboBox1.SelectedItem.ToString()] - 1).ToString();
            AdresUpdate.Focus();
        }

        /// <summary>
        /// Сохранение адресов при закрытии
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Save();
        }

        /// <summary>
        /// сохранение в config
        /// </summary>
        private void Save()
        {
            Properties.Settings.Default["Rang"] = RangAdr;
            Properties.Settings.Default["Adres"] = string.Join(",", Adreses.Values.ToArray());
            Properties.Settings.Default["AdresName"] = string.Join(",", Adreses.Keys.ToArray());

            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// Открытие диологового окна на сохранение
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveTextRang_Click(object sender, EventArgs e)
        {
            saveFileDialog1.InitialDirectory = Location.Text;
            saveFileDialog1.RestoreDirectory = true;
            saveFileDialog1.DefaultExt = "RangsSave";
            saveFileDialog1.Filter = "My files (*.ldf)|*.ldf|txt files (*.txt)|*.txt";
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                Stream file = saveFileDialog1.OpenFile();
                StreamWriter sw = new StreamWriter(file);
                if (saveFileDialog1.FileName.Contains(".ldf"))
                {
                    sw.WriteLine(CreateFile.Create(TextRangs, CreateFile.CreateDATA(Data)));
                    sw.Close();
                    file.Close();
                    return;
                }
                foreach (string rang in TextRangs)
                    sw.WriteLine(rang);
                sw.Close();
                file.Close();
                file.Dispose();
                sw.Dispose();
            }
        }

        /// <summary>
        /// Сохранение изменений параметров сервера
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ServerOk_Click(object sender, EventArgs e)
        {
            int u;
            if (IP_1.Text != null && IP_1.Text != "" && (IP_2.Text != null && IP_2.Text != "") && (IP_3.Text != null && IP_3.Text != "") && (IP_4.Text != null && IP_4.Text != ""))
            {
                IP = new byte[4];
                if (int.TryParse(IP_1.Text, out u) && u<256 && u>=0) IP[0] = (byte)u;
                else
                {
                    IP_1.Text = "";
                    MessageBox.Show("Не верно указан IP адрес");
                    IP_1.Focus();

                    IP_1.PlaceholderText = "127";
                    IP_2.PlaceholderText = "0";
                    IP_3.PlaceholderText = "0";
                    IP_4.PlaceholderText = "1";
                    IP = new byte[] { 0, 0, 0, 0 };
                    return;
                }
                if (int.TryParse(IP_2.Text, out u) && u < 256 && u >= 0) IP[1] = (byte)u;
                else
                {
                    IP_2.Text = "";
                    MessageBox.Show("Не верно указан IP адрес");
                    IP_2.Focus();

                    IP_1.PlaceholderText = "127";
                    IP_2.PlaceholderText = "0";
                    IP_3.PlaceholderText = "0";
                    IP_4.PlaceholderText = "1";
                    IP = new byte[] { 0, 0, 0, 0 };
                    return;
                }
                if (int.TryParse(IP_3.Text, out u) && u < 256 && u >= 0) IP[2] = (byte)u;
                else
                {
                    IP_3.Text = "";
                    MessageBox.Show("Не верно указан IP адрес");
                    IP_3.Focus();

                    IP_1.PlaceholderText = "127";
                    IP_2.PlaceholderText = "0";
                    IP_3.PlaceholderText = "0";
                    IP_4.PlaceholderText = "1";
                    IP = new byte[] { 0, 0, 0, 0 };
                    return;
                }
                if (int.TryParse(IP_4.Text, out u) && u < 256 && u >= 0) IP[3] = (byte)u;
                else
                {
                    IP_4.Text = "";
                    MessageBox.Show("Не верно указан IP адрес");
                    IP_4.Focus();

                    IP_1.PlaceholderText = "127";
                    IP_2.PlaceholderText = "0";
                    IP_3.PlaceholderText = "0";
                    IP_4.PlaceholderText = "1";
                    IP = new byte[] { 0, 0, 0, 0 };
                    return;
                }
                IP_1.PlaceholderText = IP[0].ToString();
                IP_2.PlaceholderText = IP[1].ToString();
                IP_3.PlaceholderText = IP[2].ToString();
                IP_4.PlaceholderText = IP[3].ToString();
            }
            if (textBox2.Text != null && textBox2.Text != "")
            {
                if(int.TryParse(textBox2.Text, out u)) Port = u;
                else
                {
                    MessageBox.Show("Не верно указан порт");
                    textBox2.Focus();
                    return;
                }
            }
            if (textBox3.Text != null && textBox3.Text != "")
            {
                if(int.TryParse(textBox3.Text, out u)) Slave = (byte)u;
                else
                {
                    MessageBox.Show("Не верно указан SlaveID");
                    textBox3.Focus();
                    return;
                }
            }

            textBox2.PlaceholderText = Port.ToString();
            textBox3.PlaceholderText = Slave.ToString();

            IP_1.Clear();
            IP_2.Clear();
            IP_3.Clear();
            IP_4.Clear();
            textBox2.Clear();
            textBox3.Clear();
        }

        /// <summary>
        /// Перепрыгивание курсора, при заполнении полей ввода IP адресса
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBox_TextChanged(object sender, EventArgs e)
        {
            TextBox a = sender as TextBox;
            if (a.Text.Length == a.MaxLength)
            {
                switch (a.Name)
                {
                    case "IP_1":
                        {
                            IP_2.Focus();
                            break;
                        }
                    case "IP_2":
                        {
                            IP_3.Focus();
                            break;
                        }
                    case "IP_3":
                        {
                            IP_4.Focus();
                            break;
                        }
                    case "IP_4":
                        {
                            IP_1.Focus();
                            break;
                        }
                }
            }
        }
    }
}