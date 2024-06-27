using System.Net;
using System.Net.Sockets;
using Modbus.Device;
using Modbus.Data;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Data;


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
        private Thread DataTon;
        private int RangAdr;
        private Dictionary<string, int> Adreses;
        private Dictionary<string, ushort[]> Data;
        private string[] TextRangs;
        private byte[] IP;
        private int Port;
        private byte Slave;
        private Dictionary<string, string[]> Tegs;
        private int[] Timer_control = new int[32];


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

            DataValue = DataStoreFactory.CreateDefaultDataStore(0, 0, ushort.MaxValue, 0);

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
                rang = DataStoreFactory.CreateDefaultDataStore(0, 0, ushort.MaxValue, 0);
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
                                DataTon.Abort(100);
                            }
                            catch
                            {
                                ListenThred.Join();
                                listlog.Items.Add("Server stoped");
                                ListenThred = null;
                                GenerateData = null;
                                DataTon = null;
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
                                label1.Text = string.Join(".", IP) == "0.0.0.0" ? "127.0.0.1:" + Port : string.Join(".", IP) + ':' + Port;



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

                                if (DataTon == null)
                                {
                                    DataTon = new Thread(() =>
                                    {
                                        try
                                        {
                                            SetDataTon();
                                        }
                                        catch (ThreadAbortException ex)
                                        {
                                            BeginInvoke(new MethodInvoker(() => { listlog.Items.Add("Thread(th_1) is aborted " + ex.ExceptionState); }));
                                        }
                                    })
                                    {
                                        IsBackground = true,
                                    };
                                }

                                DataTon.Name = "Timers";

                                //DataTon.Start();
                                ListenThred.Start();
                                GenerateData.Start();

                                listlog.Items.Add("Server started");
                                if (Location.Text.Contains("ldf"))
                                {
                                    Data = CreateFile.GetData(Location.Text);
                                    SetFDataToDatMB();
                                    TextRangs = CreateFile.Load(Location.Text, Type.RANG);
                                    Tegs = CreateFile.GetTegs(Location.Text);
                                }
                                else
                                {
                                    TextRangs = File.ReadAllLines(Location.Text);
                                }
                                transfer(TextRangs);
                                IsnensRangs(TextRangs[0]);
                            }
                            break;
                        }
                }
            }
            catch (Exception ex) { MessageBox.Show(ListenThred.ThreadState.ToString() + " " + ex.Message); }
        }

        /// <summary>
        /// Ввод данных из массива в регистрыы
        /// </summary>
        private void SetFDataToDatMB()
        {
            string[] datakey = Data.Keys.ToArray();
            foreach (string key in datakey)
            {
                for (int index = 0; index < Data[key].Length; index++)
                {
                    DataValue.HoldingRegisters[Adreses[key] + index] = Data[key][index];
                }
            }
        }

        /// <summary>
        /// Вывод даных из регистров в массив
        /// </summary>
        private void GetDataMBtoDataF()
        {
            string[] _keys = Data.Keys.ToArray();
            foreach (string _key in _keys)
            {
                for(int i = 0; i < Data[_key].Length; i++)
                {
                    Data[_key][i] = DataValue.HoldingRegisters[Adreses[_key] + i];
                }
            }
        }

        /// <summary>
        /// Метод обратотки таймеров
        /// </summary>
        public void SetDataTon()
        {

        }

        /// <summary>
        /// Задача и обновление регистров
        /// </summary>
        public void SetData()
        {
            while (!start_stop)
            {
                int num = 1;
                Thread.Sleep(100);
                foreach (string item in TextRangs)
                {
#if DEBUG
                    Debug.Print("Ранг №"+num);
#endif
                    IsnensRangs(item);
                    num++;
                    //Thread.Sleep(200);
                } 
            }
        }

        /// <summary>
        /// Проверка истиности ранга
        /// </summary>
        /// <param name="_Rang">Текст ранга</param>
        public void IsnensRangs(string _Rang)
        {
            short ist = 1;
            string[] rang_text = _Rang.Trim().Split(' ');
            short CountBranch = 0;

            for (int i = 0; i < rang_text.Length; i++)
            {
                string el = rang_text[i];
                if (el == "BST")
                {
                    CountBranch++;
                    continue;
                }
                else if (el == "NXB")
                {
                    short[] info = InsnensBranch(rang_text, CountBranch, i);
                    ist = (short)(info[0] | ist);
                    i = info[2];
                    continue;
                }
                else
                {
                    switch (el)
                    {
                        case "TON":
                            i += 4;
                            break;
                        case "MOV":
                            i += 2;
                            break;
                        case "ADD":
                            i += 3;
                            break;
                        case "DIV":
                            i += 3;
                            break;
                        case "MUL":
                            i += 3;
                            break;
                        case "ABS":
                            i += 2;
                            break;
                        case "SCP":
                            i += 6;
                            break;
                        case "MSG":
                            i += 15;
                            break;
                        default:
                            if (el == "XIO") ist = (short)(ist & Convert.ToInt16(!Adres(rang_text[i + 1])));
                            else ist = (short)(ist & Convert.ToInt16(Adres(rang_text[i + 1])));
                            i++;
                            break;
                    }
                }
            }
            #if DEBUG
            Debug.Print("Ранг " + (ist == 1 ? "истин" : "ложен"));
            #endif
        }

        /// <summary>
        /// Проверка истиности ветви
        /// </summary>
        /// <param name="_Rangs">Текст ранга, разбитый по пробелам</param>
        /// <param name="_CountBranch">Счетчик ветвей</param>
        /// <param name="_i">Индекс в текстре ранга</param>
        /// <returns>истиность;кол-во ветвей;индекс нахождения конца ветви</returns>
        /// <exception cref="Exception"></exception>
        private short[] InsnensBranch(string[] _Rangs, short _CountBranch, int _i)
        {
            short ist = 1;
            short CountBranch = _CountBranch;
            for (int i = _i + 1; i < _Rangs.Length; i++)
            {
                string el = _Rangs[i];
                if (el == "BST")
                {
                    CountBranch++;
                    continue;
                }
                else if (el == "NXB")
                {
                    short[] info = InsnensBranch(_Rangs, CountBranch, i);
                    if (info[1] == 0) return info;
                    ist = (short)(info[0] | ist);
                    i = info[2];
                    continue;
                }
                else if (el == "BND")
                {
                    return new short[] { ist, (short)(CountBranch -1), (short)i };
                }
                else
                {
                    switch (el)
                    {
                        case "TON":
                            i += 4;
                            break;
                        case "MOV":
                            i += 2;
                            break;
                        case "ADD":
                            i += 3;
                            break;
                        case "DIV":
                            i += 3;
                            break;
                        case "MUL":
                            i += 3;
                            break;
                        case "ABS":
                            i += 2;
                            break;
                        case "SCP":
                            i += 6;
                            break;
                        case "MSG":
                            i += 15;
                            break;
                        default:
                            if (el == "XIO") ist = (short)(ist & Convert.ToInt16(!Adres(_Rangs[i + 1])));
                            else ist = (short)(ist & Convert.ToInt16(Adres(_Rangs[i + 1])));
                            i++;
                            break;
                    }
                }
            }
            throw new Exception();
        }

        /// <summary>
        /// Проверка истиности элеента
        /// </summary>
        /// <param name="st">Адрес</param>
        /// <returns></returns>
        private bool Adres(string st)
        {
            try
            {
                string mas = new Regex(@":\w*(/(\w*)?)?").Replace(st, "");
                string[] k = new string[2];
                int Bitmask = 0;
                int ind_1;
                int adr;

                if (st.Contains("N13")) k = st.Replace("N13:", "").Split('/');
                if (st.Contains("N15")) k = st.Replace("N15:", "").Split('/');
                if (st.Contains("N18")) k = st.Replace("N18:", "").Split('/');
                if (st.Contains("N40")) k = st.Replace("N40:", "").Split('/');
                if (st.Contains("B3")) k = st.Replace("B3:", "").Split('/');
                if (st.Contains("T4")) k = st.Replace("T4:", "").Split('/');

                if (k.Contains("EN"))
                {
                    Bitmask = 1;
                    ind_1 = int.Parse(k[0]);
                    adr = Timer_control[ind_1];

                    if ((adr & Bitmask) == Bitmask) return true;
                    return false;
                }
                else if (k.Contains("DN"))
                {
                    Bitmask = 2;
                    ind_1 = int.Parse(k[0]);
                    adr = Timer_control[ind_1];

                    if ((adr & Bitmask) == Bitmask) return true;
                    return false;
                }
                else if (k.Contains("TT"))
                {
                    Bitmask = 4;
                    ind_1 = int.Parse(k[0]);
                    adr = Timer_control[ind_1];

                    if ((adr & Bitmask) == Bitmask) return true;
                    return false;
                }
                else
                {
                    Bitmask = 1 << int.Parse(k[1]);

                    ind_1 = int.Parse(k[0]);
                    adr = DataValue.HoldingRegisters[ind_1 + Adreses[mas]];

                    if ((adr & Bitmask) == Bitmask) return true;
                    return false;
                }
            }
            catch (Exception)
            {
                return false;
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
                //if (listlog.Items.Count > 14) listlog.Items.Remove(listlog.Items[0]);
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
                GetDataMBtoDataF();
                if (saveFileDialog1.FileName.Contains(".ldf"))
                {
                    sw.WriteLine(CreateFile.Create(TextRangs, CreateFile.CreateDATA(Data), CreateFile.CreateTEGS(Tegs)));
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
                if (int.TryParse(IP_1.Text, out u) && u < 256 && u >= 0) IP[0] = (byte)u;
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
                if (int.TryParse(textBox2.Text, out u)) Port = u;
                else
                {
                    MessageBox.Show("Не верно указан порт");
                    textBox2.Focus();
                    return;
                }
            }
            if (textBox3.Text != null && textBox3.Text != "")
            {
                if (int.TryParse(textBox3.Text, out u)) Slave = (byte)u;
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