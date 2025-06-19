using System.Net;
using System.Net.Sockets;
using Modbus.Device;
using Modbus.Data;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Xml.Linq;
using System.Windows.Forms;
using System.Diagnostics.Metrics;
using LogixForms.HelperClasses;


namespace ModbasServer
{
    public partial class Form1 : Form
    {
        private bool start_stop = true;
        private DataStore DataValue;
        private DataStore VoidData;
        private DataStore NewRangData;
        private DataStore[] DataRangs;// переделать на Dictionary
        private DataStore[] CfgRangs;
        private ModbusSlave mb_tcp_server;
        private Thread ListenThred;
        private Thread GenerateData;
        private Thread TonDeta;
        private int RangAdr;
        private Dictionary<string, int> Adreses;
        private Dictionary<string, ushort[]> Data;
        private string[] TextRangs;
        private byte[] IP;
        private int Port;
        private byte Slave;
        private Dictionary<string, string[]> Tegs;
        private const int timer_max = 24;
        private int[] TimeBase = new int[timer_max];
        private int[] Timers_ms_count = new int[timer_max];

        private string[] k = new string[2];
        private int Bitmask = 0;
        private int ind_1;
        private int adr;
        private int ConfigAdr;
        private int SaveaNewRang;
        private Dictionary<string, byte> DataType;


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

            string[] name = Properties.Settings.Default.AdresName.Split(",");
            string[] adr = Properties.Settings.Default.Adres.Split(",");
            RangAdr = Properties.Settings.Default.Rang;
            ConfigAdr = Properties.Settings.Default.ConfigAdr;
            SaveaNewRang = Properties.Settings.Default.SaveaNewRang;
            NewRangData = DataStoreFactory.CreateDefaultDataStore(0, 0, ushort.MaxValue, 0);

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
            VoidData = DataStoreFactory.CreateDefaultDataStore(0, 0, ushort.MaxValue, 0);

            comboBox1.Items.Add("Расположение рангов");
            comboBox1.Items.Add("Конфигурация");
            comboBox1.Items.AddRange(Adreses.Keys.ToArray());
            comboBox1.SelectedIndex = 0;
            IP = new byte[] { 0, 0, 0, 0 };
            Port = 502;
            Slave = 1;

            textBox2.PlaceholderText = Port.ToString();
            textBox3.PlaceholderText = Slave.ToString();
            //GetsNewRang = false;

            DataType = new Dictionary<string, byte>
            {
                {"B", 1 },
                {"T", 2 },
                {"C", 3 },
                {"R", 4 },
                {"N", 5 },
                {"F", 6 },
                {"S", 7 },
                {"L", 8 },
                {"MG", 9 },
                {"RI", 10 },
                {"T_c", 11 },
                {"Timer_control", 12 },
            };
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
            int count;
            ushort temp;
            //List<ushort> sss = new List<ushort>();
            //текст ранга
            for (int ind_Rang = 0; ind_Rang < TextRangs.Length; ind_Rang++)
            {
                count = RangAdr + ind_Rang;
                line = TextRangs[ind_Rang];
                rang = DataStoreFactory.CreateDefaultDataStore(0, 0, ushort.MaxValue, 0);
                temp = 0;
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
                        //sss.Add(temp);
                    }
                    if (i == line.Length) break;
                }
                DataRangs[ind_Rang] = rang;
            }

            CfgRangs = new DataStore[Adreses.Count + 1];
            string name;
            string _type;
            rang = DataStoreFactory.CreateDefaultDataStore(0, 0, ushort.MaxValue, 0);
            rang.HoldingRegisters[ConfigAdr] = 0xffff;
            rang.HoldingRegisters[ConfigAdr + 1] = (ushort)(RangAdr - 1);
            CfgRangs[0] = rang;
            for (int ind = 1; ind < Adreses.Count + 1; ++ind)
            {
                count = ConfigAdr + ind;
                rang = DataStoreFactory.CreateDefaultDataStore(0, 0, ushort.MaxValue, 0);

                name = Adreses.Keys.ToArray()[ind - 1];
                _type = new Regex(@"\d{1,2}").Replace(name, "");

                temp = (ushort)(DataType[_type] << 8);
                if (name != "Timer_control" && name != "T4_c")
                    temp |= ushort.Parse(name.Replace(_type, ""));

                rang.HoldingRegisters[count] = temp;
                rang.HoldingRegisters[count + 1] = (ushort)(Adreses[name] - 1);
                try
                {
                    rang.HoldingRegisters[count + 2] = (ushort)Data[name].Length;
                }
                catch { }

                CfgRangs[ind] = rang;
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
                            try
                            {
                                GenerateData.Abort(100);
                                ListenThred.Abort(100);
                                TonDeta.Abort(100);
                            }
                            catch
                            {
                                ListenThred.Join();
                                listlog.Items.Add("Server stoped");
                                ListenThred = null;
                                GenerateData = null;
                                TonDeta = null;
                            }

                            StartStop.BackColor = Color.Green;
                            StartStop.Text = "Старт";
                            start_stop = !start_stop;
                            label1.Text = "";

                            break;
                        }
                    case true:
                        {
                            if (Location.Text != "")
                            {
                                mb_tcp_server = ModbusTcpSlave.CreateTcp(Slave, new TcpListener(new IPAddress(IP), Port));
                                mb_tcp_server.DataStore = DataValue;
                                mb_tcp_server.ModbusSlaveRequestReceived += Mb_tcp_server_ModbusSlaveRequestReceived;

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

                                if (TonDeta == null)
                                    TonDeta = new Thread(() =>
                                    {
                                        try
                                        {
                                            TimerUp();
                                        }
                                        catch (ThreadAbortException ex)
                                        {
                                            BeginInvoke(new MethodInvoker(() => { listlog.Items.Add("Thread(th_1) is aborted " + ex.ExceptionState); }));
                                        }
                                    })
                                    {
                                        IsBackground = true,
                                    };

                                TonDeta.Name = "Timer";
                                try
                                {
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

                                    ListenThred.Start();
                                    GenerateData.Start();
                                    TonDeta.Start();

                                    start_stop = !start_stop;

                                    transfer(TextRangs);
                                    //IsnensRangs(TextRangs[0]);
                                }
                                catch
                                {
                                    comboBox1.SelectedIndex = comboBox1.Items.Count - 1;
                                    tabControl1.SelectedIndex = 1;
                                }
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
            bool addnew = false;
            string[] datakey = Data.Keys.ToArray();
            foreach (string key in datakey)
            {
                for (int index = 0; index < Data[key].Length; index++)
                {
                    if (!Adreses.ContainsKey(key))
                    {
                        var mbox = MessageBox.Show($"Имя {key} нет в таблице регистров.\nДобавить?", "Неизвестное имя", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                        if (mbox == DialogResult.Yes)
                        {
                            Adreses.Add(key, 1);
                            comboBox1.Items.Add(key);
                            DataValue.HoldingRegisters[Adreses[key] + index] = Data[key][index];
                            addnew = true;
                        }
                        else if (mbox == DialogResult.No)
                        {
                            break;
                        }
                    }
                    else
                        DataValue.HoldingRegisters[Adreses[key] + index] = Data[key][index];
                }
            }
            if (addnew && MessageBox.Show("Необходима настройка новых имен", "", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.OK) throw new Exception();
        }

        /// <summary>
        /// Вывод даных из регистров в массив
        /// </summary>
        private void GetDataMBtoDataF()
        {
            string[] _keys = Data.Keys.ToArray();
            foreach (string _key in _keys)
            {
                for (int i = 0; i < Data[_key].Length; i++)
                {
                    Data[_key][i] = DataValue.HoldingRegisters[Adreses[_key] + i];
                }
            }
        }

        /// <summary>
        /// Метод обратотки таймеров таймером
        /// </summary>
        public void TimerUp()
        {
            while (true)
            {
                for (int i = 0; i < timer_max; i++)
                {
                    if (TimeBase[i] != 0)
                    {
                        if ((DataValue.HoldingRegisters[Adreses["Timer_control"] + i] & 1) == 1)
                        {
                            if (++Timers_ms_count[i] >= TimeBase[i])
                            {
                                DataValue.HoldingRegisters[Adreses["T4_c"] + i] += 1;
                                Timers_ms_count[i] = 0;
                            }
                        }
                        else
                        {
                            DataValue.HoldingRegisters[Adreses["T4_c"] + i] = 0;
                        }

                        if (DataValue.HoldingRegisters[Adreses["T4_c"] + i] >= DataValue.HoldingRegisters[Adreses["T4"] + i])
                        {
                            DataValue.HoldingRegisters[Adreses["Timer_control"] + i] |= 2;
                        }
                        if ((DataValue.HoldingRegisters[Adreses["Timer_control"] + i] & (1 | 2)) == 1)
                        {
                            DataValue.HoldingRegisters[Adreses["Timer_control"] + i] |= 4;
                        }
                        else DataValue.HoldingRegisters[Adreses["Timer_control"] + i] &= 0b011;
                    }
                }
                Thread.Sleep(10);
            }
        }

        /// <summary>
        /// Задача и обновление регистров
        /// </summary>
        public void SetData()
        {
            string[] buf;
            //Thread.Sleep(100);
            for (int i = 0; i < TextRangs.Length; i++)
            {
                if (TextRangs[i].Contains("TON"))
                {
                    buf = TextRangs[i].Split(' ');
                    for (int el = 0; el < buf.Length; el++)
                    {
                        if (buf[el] == "TON")
                        {
                            int st = int.Parse(buf[el + 1].Split(":")[1]);
                            TimeBase[st] = (int)(double.Parse(buf[el + 2].Replace('.', ',')) * 100) != 0 ? (int)(double.Parse(buf[el + 2].Replace('.', ',')) * 100) : 1;
                            DataValue.HoldingRegisters[Adreses["T4"] + st] = ushort.Parse(buf[el + 3]);
                            DataValue.HoldingRegisters[Adreses["T4_c"] + st] = ushort.Parse(buf[el + 4]);
                            break;
                        }
                    }
                }
            }
            buf = null;

            BeginInvoke(new MethodInvoker(() =>
            {
                StartStop.BackColor = Color.Red;
                StartStop.Text = "Стоп";
                label1.Text = string.Join(".", IP) == "0.0.0.0" ? "127.0.0.1:" + Port : string.Join(".", IP) + ':' + Port;

                listlog.Items.Add("Server started");
            }));
            //TONupdate.Enabled = true;
            while (!start_stop)
            {
                int num = 1;
                foreach (string item in TextRangs)
                {
#if DEBUG
                    Debug.Print("Ранг №" + num);
#endif
                    IsnensRangs(item);
                    num++;
                    Task.Delay(2);
                }
            }
        }

        /// <summary>
        /// Проверка истиности ранга
        /// </summary>
        /// <param name="_Rang">Текст ранга</param>
        public void IsnensRangs(string _Rang)
        {
            //RangsInfo(_Rang);
            short ist = 1;
            string[] rang_text = _Rang.Trim().Split(' ');
            short CountBranch = 0;
            bool ONS = false;

            for (int i = 0; i < rang_text.Length; i++)//Test(1).ldf ONS не работат
            {
                string el = rang_text[i];
                if (el == "BST")
                {
                    CountBranch++;
                    short[] info = InsnensBranch(rang_text, CountBranch, i, ist);
                    ist = (short)(info[0] & ist);
                    i = info[2];
                    CountBranch = info[1];
                    continue;
                }
                else
                {
                    string[] buf;
                    string name;
                    int str;
                    ushort u;

                    switch (el)
                    {
                        case "OTE"://OTE если ранг истина, в указанный адрес 1, иначе 0
                            if (ist == 1) SetAdresBit(rang_text[i + 1], 1);
                            else SetAdresBit(rang_text[i + 1], 0);
                            i++;
                            break;
                        case "OTL"://OTL если ранг истина, в указанный адрес 1
                            if (ist == 1) SetAdresBit(rang_text[i + 1], 1);
                            i++;
                            break;
                        case "OTU"://OTU если ранг истина, в указанный адрес 0
                            if (ist == 1) SetAdresBit(rang_text[i + 1], 0);
                            i++;
                            break;
                        case "ONS"://ONS после него ранг истин, если его адрес перешёл в 1 (соб_бит = ист_л)(0->1)[дальше можно не идти]
                            bool bit = GetAdresBit(rang_text[i + 1]);
                            if (ist != Convert.ToInt16(bit))
                            {
                                SetAdresBit(rang_text[i + 1], (byte)ist);
                                bit = GetAdresBit(rang_text[i + 1]);
                                if (Convert.ToInt16(bit) == 1) ist = 1;
                            }
                            i++;
                            break;
                        case "TON":
                            if (ist == 1) DataValue.HoldingRegisters[Adreses["Timer_control"] + int.Parse(rang_text[i + 1].Split(':')[1])] |= 1;
                            else
                                DataValue.HoldingRegisters[Adreses["Timer_control"] + int.Parse(rang_text[i + 1].Split(':')[1])] = 0;
                            i += 4;
                            break;
                        case "MOV":
                            if (ist == 1)
                            {

                                if (ushort.TryParse(rang_text[i + 1], out u))
                                {
                                    SetAdresValue(rang_text[i + 2], u);
                                }
                                else
                                {
                                    buf = rang_text[i + 1].Split(':');
                                    name = buf[0];
                                    buf = buf[1].Split("/");
                                    str = int.Parse(buf[0]);
                                    SetAdresValue(rang_text[i + 2], DataValue.HoldingRegisters[Adreses[name] + str]);
                                }
                            }
                            i += 2;
                            break;
                        case "ADD"://A+B=C
                            if (ist == 1)
                            {
                                if (ushort.TryParse(rang_text[i + 1], out u))
                                {
                                    buf = rang_text[i + 2].Split(':');
                                    name = buf[0];
                                    buf = buf[1].Split("/");
                                    str = int.Parse(buf[0]);
                                    SetAdresValue(rang_text[i + 3], (ushort)(u + DataValue.HoldingRegisters[Adreses[name] + str]));
                                }
                                else if (ushort.TryParse(rang_text[i + 2], out u))
                                {
                                    buf = rang_text[i + 1].Split(':');
                                    name = buf[0];
                                    buf = buf[1].Split("/");
                                    str = int.Parse(buf[0]);
                                    SetAdresValue(rang_text[i + 3], (ushort)(u + DataValue.HoldingRegisters[Adreses[name] + str]));
                                }
                                else
                                {
                                    buf = rang_text[i + 1].Split(':');
                                    name = buf[0];
                                    buf = buf[1].Split("/");
                                    str = int.Parse(buf[0]);

                                    u = DataValue.HoldingRegisters[Adreses[name] + str];

                                    buf = rang_text[i + 2].Split(':');
                                    name = buf[0];
                                    buf = buf[1].Split("/");
                                    str = int.Parse(buf[0]);
                                    SetAdresValue(rang_text[i + 2], (ushort)(u + DataValue.HoldingRegisters[Adreses[name] + str]));
                                }
                            }
                            i += 3;
                            break;
                        case "GEQ":
                            if (ushort.TryParse(rang_text[i + 2], out u))
                            {
                                buf = rang_text[i + 1].Split(':');
                                name = buf[0];
                                str = int.Parse(buf[1]);
                                //SetAdresValue(rang_text[i + 3], (ushort)(u + DataValue.HoldingRegisters[Adreses[name] + str]));
                                if (DataValue.HoldingRegisters[Adreses[name] + str] >= int.Parse(rang_text[i + 2])) ist = 1;
                                else ist = 0;
                            }
                            else
                            {
                                buf = rang_text[i + 1].Split(':');
                                name = buf[0];
                                str = int.Parse(buf[1]);

                                u = DataValue.HoldingRegisters[Adreses[name] + str];

                                buf = rang_text[i + 2].Split(':');
                                name = buf[0];
                                str = int.Parse(buf[1]);

                                if (u >= DataValue.HoldingRegisters[Adreses[name] + str]) ist = 1;
                                else ist = 0;
                            }
                            i += 2;
                            break;
                        case "GRT":
                            if (ushort.TryParse(rang_text[i + 2], out u))
                            {
                                buf = rang_text[i + 1].Split(':');
                                name = buf[0];
                                str = int.Parse(buf[1]);
                                //SetAdresValue(rang_text[i + 3], (ushort)(u + DataValue.HoldingRegisters[Adreses[name] + str]));
                                if (DataValue.HoldingRegisters[Adreses[name] + str] > int.Parse(rang_text[i + 2])) ist = 1;
                                else ist = 0;
                            }
                            else
                            {
                                buf = rang_text[i + 1].Split(':');
                                name = buf[0];
                                str = int.Parse(buf[1]);

                                u = DataValue.HoldingRegisters[Adreses[name] + str];

                                buf = rang_text[i + 2].Split(':');
                                name = buf[0];
                                str = int.Parse(buf[1]);

                                if (u > DataValue.HoldingRegisters[Adreses[name] + str]) ist = 1;
                                else ist = 0;
                            }
                            i += 2;
                            break;
                        case "EQU":
                            if (ushort.TryParse(rang_text[i + 2], out u))
                            {
                                buf = rang_text[i + 1].Split(':');
                                name = buf[0];
                                str = int.Parse(buf[1]);
                                //SetAdresValue(rang_text[i + 3], (ushort)(u + DataValue.HoldingRegisters[Adreses[name] + str]));
                                if (DataValue.HoldingRegisters[Adreses[name] + str] == int.Parse(rang_text[i + 2])) ist = 1;
                                else ist = 0;
                            }
                            else
                            {
                                buf = rang_text[i + 1].Split(':');
                                name = buf[0];
                                str = int.Parse(buf[1]);

                                u = DataValue.HoldingRegisters[Adreses[name] + str];

                                buf = rang_text[i + 2].Split(':');
                                name = buf[0];
                                str = int.Parse(buf[1]);

                                if (u == DataValue.HoldingRegisters[Adreses[name] + str]) ist = 1;
                                else ist = 0;
                            }
                            i += 2;
                            break;
                        case "NEQ":
                            if (ushort.TryParse(rang_text[i + 2], out u))
                            {
                                buf = rang_text[i + 1].Split(':');
                                name = buf[0];
                                str = int.Parse(buf[1]);
                                //SetAdresValue(rang_text[i + 3], (ushort)(u + DataValue.HoldingRegisters[Adreses[name] + str]));
                                if (DataValue.HoldingRegisters[Adreses[name] + str] != int.Parse(rang_text[i + 2])) ist = 1;
                                else ist = 0;
                            }
                            else
                            {
                                buf = rang_text[i + 1].Split(':');
                                name = buf[0];
                                str = int.Parse(buf[1]);

                                u = DataValue.HoldingRegisters[Adreses[name] + str];

                                buf = rang_text[i + 2].Split(':');
                                name = buf[0];
                                str = int.Parse(buf[1]);

                                if (u != DataValue.HoldingRegisters[Adreses[name] + str]) ist = 1;
                                else ist = 0;
                            }
                            i += 2;
                            break;
                        case "LES":
                            if (ushort.TryParse(rang_text[i + 2], out u))
                            {
                                buf = rang_text[i + 1].Split(':');
                                name = buf[0];
                                str = int.Parse(buf[1]);
                                //SetAdresValue(rang_text[i + 3], (ushort)(u + DataValue.HoldingRegisters[Adreses[name] + str]));
                                if (DataValue.HoldingRegisters[Adreses[name] + str] < int.Parse(rang_text[i + 2])) ist = 1;
                                else ist = 0;
                            }
                            else
                            {
                                buf = rang_text[i + 1].Split(':');
                                name = buf[0];
                                str = int.Parse(buf[1]);

                                u = DataValue.HoldingRegisters[Adreses[name] + str];

                                buf = rang_text[i + 2].Split(':');
                                name = buf[0];
                                str = int.Parse(buf[1]);

                                if (u < DataValue.HoldingRegisters[Adreses[name] + str]) ist = 1;
                                else ist = 0;
                            }
                            i += 2;
                            break;
                        case "LEQ":
                            if (ushort.TryParse(rang_text[i + 2], out u))
                            {
                                buf = rang_text[i + 1].Split(':');
                                name = buf[0];
                                str = int.Parse(buf[1]);
                                //SetAdresValue(rang_text[i + 3], (ushort)(u + DataValue.HoldingRegisters[Adreses[name] + str]));
                                if (DataValue.HoldingRegisters[Adreses[name] + str] <= int.Parse(rang_text[i + 2])) ist = 1;
                                else ist = 0;
                            }
                            else
                            {
                                buf = rang_text[i + 1].Split(':');
                                name = buf[0];
                                str = int.Parse(buf[1]);

                                u = DataValue.HoldingRegisters[Adreses[name] + str];

                                buf = rang_text[i + 2].Split(':');
                                name = buf[0];
                                str = int.Parse(buf[1]);

                                if (u <= DataValue.HoldingRegisters[Adreses[name] + str]) ist = 1;
                                else ist = 0;
                            }
                            i += 2;
                            break;
                        case "DIV"://Ц/Ц=Ц
                            if (ist == 1)
                            {
                                if (ushort.TryParse(rang_text[i + 1], out u))
                                {
                                    buf = rang_text[i + 2].Split(':');
                                    name = buf[0];
                                    buf = buf[1].Split("/");
                                    str = int.Parse(buf[0]);
                                    SetAdresValue(rang_text[i + 3], (ushort)(u / DataValue.HoldingRegisters[Adreses[name] + str]));
                                }
                                else if (ushort.TryParse(rang_text[i + 2], out u))
                                {
                                    buf = rang_text[i + 1].Split(':');
                                    name = buf[0];
                                    buf = buf[1].Split("/");
                                    str = int.Parse(buf[0]);
                                    SetAdresValue(rang_text[i + 3], (ushort)(u / DataValue.HoldingRegisters[Adreses[name] + str]));
                                }
                                else
                                {
                                    buf = rang_text[i + 1].Split(':');
                                    name = buf[0];
                                    buf = buf[1].Split("/");
                                    str = int.Parse(buf[0]);

                                    u = DataValue.HoldingRegisters[Adreses[name] + str];

                                    buf = rang_text[i + 2].Split(':');
                                    name = buf[0];
                                    buf = buf[1].Split("/");
                                    str = int.Parse(buf[0]);
                                    SetAdresValue(rang_text[i + 2], (ushort)(u / DataValue.HoldingRegisters[Adreses[name] + str]));
                                }
                            }
                            i += 3;
                            break;
                        case "MUL"://A*B=C
                            if (ist == 1)
                            {
                                if (ushort.TryParse(rang_text[i + 1], out u))
                                {
                                    buf = rang_text[i + 2].Split(':');
                                    name = buf[0];
                                    buf = buf[1].Split("/");
                                    str = int.Parse(buf[0]);
                                    SetAdresValue(rang_text[i + 3], (ushort)(u * DataValue.HoldingRegisters[Adreses[name] + str]));
                                }
                                else if (ushort.TryParse(rang_text[i + 2], out u))
                                {
                                    buf = rang_text[i + 1].Split(':');
                                    name = buf[0];
                                    buf = buf[1].Split("/");
                                    str = int.Parse(buf[0]);
                                    SetAdresValue(rang_text[i + 3], (ushort)(u * DataValue.HoldingRegisters[Adreses[name] + str]));
                                }
                                else
                                {
                                    buf = rang_text[i + 1].Split(':');
                                    name = buf[0];
                                    buf = buf[1].Split("/");
                                    str = int.Parse(buf[0]);

                                    u = DataValue.HoldingRegisters[Adreses[name] + str];

                                    buf = rang_text[i + 2].Split(':');
                                    name = buf[0];
                                    buf = buf[1].Split("/");
                                    str = int.Parse(buf[0]);
                                    SetAdresValue(rang_text[i + 2], (ushort)(u * DataValue.HoldingRegisters[Adreses[name] + str]));
                                }
                            }
                            i += 3;
                            break;
                        case "ABS":
                            if (ist == 1)
                            {
                                buf = rang_text[i + 1].Split(':');
                                name = buf[0];
                                buf = buf[1].Split("/");
                                str = int.Parse(buf[0]);
                                SetAdresValue(rang_text[i + 2], (ushort)Math.Abs(DataValue.HoldingRegisters[Adreses[name] + str]));
                            }
                            i += 2;
                            break;
                        case "SCP":
                            i += 6;
                            break;
                        case "MSG":
                            i += 15;
                            break;
                        default:
                            if (el == "XIO") ist = (short)(ist & Convert.ToInt16(!GetAdresBit(rang_text[i + 1])));
                            else ist = (short)(ist & Convert.ToInt16(GetAdresBit(rang_text[i + 1])));
                            i++;
                            break;
                    }
                }
            }
#if DEBUG
            Debug.Print(ist == 1 ? "истин" : "ложен");
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
        private short[] InsnensBranch(string[] _Rangs, short _CountBranch, int _i, short _ist)
        {
            short ist = 1;
            short CountBranch = _CountBranch;
            for (int i = _i + 1; i < _Rangs.Length; i++)
            {
                string el = _Rangs[i];
                if (el == "BST")
                {
                    CountBranch++;
                    short[] info = InsnensBranch(_Rangs, CountBranch, i, ist);
                    if (info[1] == 0) return info;
                    ist = (short)(info[0] & ist);
                    i = info[2];
                    CountBranch = info[1];
                    continue;
                }
                else if (el == "NXB")
                {
                    short[] info = InsnensBranch(_Rangs, CountBranch, i, _ist);
                    ist = (short)(info[0] | ist);
                    i = info[2];
                    CountBranch = info[1];
                    if (info[1] == 0) return new short[] { ist, info[1], (short)i };
                    continue;
                }
                else if (el == "BND")
                {
                    return new short[] { ist, (short)(CountBranch - 1), (short)i };
                }
                else
                {
                    string[] buf;
                    string name;
                    int str;
                    ushort u;

                    switch (el)
                    {
                        case "OTE"://OTE если ранг истина, в указанный адрес 1, иначе 0
                            if ((_ist & ist) == 1) SetAdresBit(_Rangs[i + 1], 1);
                            else SetAdresBit(_Rangs[i + 1], 0);
                            i++;
                            break;
                        case "OTL"://OTL если ранг истина, в указанный адрес 1
                            if ((_ist & ist) == 1) SetAdresBit(_Rangs[i + 1], 1);
                            i++;
                            break;
                        case "OTU"://OTU если ранг истина, в указанный адрес 0
                            if ((_ist & ist) == 1) SetAdresBit(_Rangs[i + 1], 0);
                            i++;
                            break;
                        case "ONS"://ONS после него ранг истин, если его адрес перешёл в 1 (соб_бит = ист_л)(0->1)[дальше можно не идти]
                            bool bit = GetAdresBit(_Rangs[i + 1]);
                            if (_ist != Convert.ToInt16(bit))
                            {
                                SetAdresBit(_Rangs[i + 1], (byte)_ist);
                                bit = GetAdresBit(_Rangs[i + 1]);
                                if (Convert.ToInt16(bit) == 1) ist = 1;
                            }
                            i++;
                            break;
                        case "TON":
                            if ((_ist & ist) == 1) DataValue.HoldingRegisters[Adreses["Timer_control"] + int.Parse(_Rangs[i + 1].Split(':')[1])] |= 1;
                            else
                                DataValue.HoldingRegisters[Adreses["Timer_control"] + int.Parse(_Rangs[i + 1].Split(':')[1])] = 0;
                            ist = (short)(_ist & ist);
                            i += 4;
                            break;
                        case "MOV":
                            if (ist == 1)
                            {

                                if (ushort.TryParse(_Rangs[i + 1], out u))
                                {
                                    SetAdresValue(_Rangs[i + 2], u);
                                }
                                else
                                {
                                    buf = _Rangs[i + 1].Split(':');
                                    name = buf[0];
                                    buf = buf[1].Split("/");
                                    str = int.Parse(buf[0]);
                                    SetAdresValue(_Rangs[i + 2], DataValue.HoldingRegisters[Adreses[name] + str]);
                                }
                            }
                            i += 2;
                            break;
                        case "ADD"://A+B=C
                            if (ist == 1)
                            {

                                if (ushort.TryParse(_Rangs[i + 1], out u))
                                {
                                    buf = _Rangs[i + 2].Split(':');
                                    name = buf[0];
                                    buf = buf[1].Split("/");
                                    str = int.Parse(buf[0]);
                                    SetAdresValue(_Rangs[i + 3], (ushort)(u + DataValue.HoldingRegisters[Adreses[name] + str]));
                                }
                                else if (ushort.TryParse(_Rangs[i + 2], out u))
                                {
                                    buf = _Rangs[i + 1].Split(':');
                                    name = buf[0];
                                    buf = buf[1].Split("/");
                                    str = int.Parse(buf[0]);
                                    SetAdresValue(_Rangs[i + 3], (ushort)(u + DataValue.HoldingRegisters[Adreses[name] + str]));
                                }
                                else
                                {
                                    buf = _Rangs[i + 1].Split(':');
                                    name = buf[0];
                                    buf = buf[1].Split("/");
                                    str = int.Parse(buf[0]);

                                    u = DataValue.HoldingRegisters[Adreses[name] + str];

                                    buf = _Rangs[i + 2].Split(':');
                                    name = buf[0];
                                    buf = buf[1].Split("/");
                                    str = int.Parse(buf[0]);
                                    SetAdresValue(_Rangs[i + 2], (ushort)(u + DataValue.HoldingRegisters[Adreses[name] + str]));
                                }
                            }
                            i += 3;
                            break;
                        case "GEQ":
                            if (ushort.TryParse(_Rangs[i + 2], out u))
                            {
                                buf = _Rangs[i + 1].Split(':');
                                name = buf[0];
                                str = int.Parse(buf[1]);
                                //SetAdresValue(_Rangs[i + 3], (ushort)(u + DataValue.HoldingRegisters[Adreses[name] + str]));
                                if (DataValue.HoldingRegisters[Adreses[name] + str] >= int.Parse(_Rangs[i + 2])) ist = 1;
                                else ist = 0;
                            }
                            else
                            {
                                buf = _Rangs[i + 1].Split(':');
                                name = buf[0];
                                str = int.Parse(buf[1]);

                                u = DataValue.HoldingRegisters[Adreses[name] + str];

                                buf = _Rangs[i + 2].Split(':');
                                name = buf[0];
                                str = int.Parse(buf[1]);

                                if (u >= DataValue.HoldingRegisters[Adreses[name] + str]) ist = 1;
                                else ist = 0;
                            }
                            i += 2;
                            break;
                        case "GRT":
                            if (ushort.TryParse(_Rangs[i + 2], out u))
                            {
                                buf = _Rangs[i + 1].Split(':');
                                name = buf[0];
                                str = int.Parse(buf[1]);
                                //SetAdresValue(_Rangs[i + 3], (ushort)(u + DataValue.HoldingRegisters[Adreses[name] + str]));
                                if (DataValue.HoldingRegisters[Adreses[name] + str] > int.Parse(_Rangs[i + 2])) ist = 1;
                                else ist = 0;
                            }
                            else
                            {
                                buf = _Rangs[i + 1].Split(':');
                                name = buf[0];
                                str = int.Parse(buf[1]);

                                u = DataValue.HoldingRegisters[Adreses[name] + str];

                                buf = _Rangs[i + 2].Split(':');
                                name = buf[0];
                                str = int.Parse(buf[1]);

                                if (u > DataValue.HoldingRegisters[Adreses[name] + str]) ist = 1;
                                else ist = 0;
                            }
                            i += 2;
                            break;
                        case "EQU":
                            if (ushort.TryParse(_Rangs[i + 2], out u))
                            {
                                buf = _Rangs[i + 1].Split(':');
                                name = buf[0];
                                str = int.Parse(buf[1]);
                                //SetAdresValue(_Rangs[i + 3], (ushort)(u + DataValue.HoldingRegisters[Adreses[name] + str]));
                                if (DataValue.HoldingRegisters[Adreses[name] + str] == int.Parse(_Rangs[i + 2])) ist = 1;
                                else ist = 0;
                            }
                            else
                            {
                                buf = _Rangs[i + 1].Split(':');
                                name = buf[0];
                                str = int.Parse(buf[1]);

                                u = DataValue.HoldingRegisters[Adreses[name] + str];

                                buf = _Rangs[i + 2].Split(':');
                                name = buf[0];
                                str = int.Parse(buf[1]);

                                if (u == DataValue.HoldingRegisters[Adreses[name] + str]) ist = 1;
                                else ist = 0;
                            }
                            i += 2;
                            break;
                        case "NEQ":
                            if (ushort.TryParse(_Rangs[i + 2], out u))
                            {
                                buf = _Rangs[i + 1].Split(':');
                                name = buf[0];
                                str = int.Parse(buf[1]);
                                //SetAdresValue(_Rangs[i + 3], (ushort)(u + DataValue.HoldingRegisters[Adreses[name] + str]));
                                if (DataValue.HoldingRegisters[Adreses[name] + str] != int.Parse(_Rangs[i + 2])) ist = 1;
                                else ist = 0;
                            }
                            else
                            {
                                buf = _Rangs[i + 1].Split(':');
                                name = buf[0];
                                str = int.Parse(buf[1]);

                                u = DataValue.HoldingRegisters[Adreses[name] + str];

                                buf = _Rangs[i + 2].Split(':');
                                name = buf[0];
                                str = int.Parse(buf[1]);

                                if (u != DataValue.HoldingRegisters[Adreses[name] + str]) ist = 1;
                                else ist = 0;
                            }
                            i += 2;
                            break;
                        case "LES":
                            if (ushort.TryParse(_Rangs[i + 2], out u))
                            {
                                buf = _Rangs[i + 1].Split(':');
                                name = buf[0];
                                str = int.Parse(buf[1]);
                                //SetAdresValue(_Rangs[i + 3], (ushort)(u + DataValue.HoldingRegisters[Adreses[name] + str]));
                                if (DataValue.HoldingRegisters[Adreses[name] + str] < int.Parse(_Rangs[i + 2])) ist = 1;
                                else ist = 0;
                            }
                            else
                            {
                                buf = _Rangs[i + 1].Split(':');
                                name = buf[0];
                                str = int.Parse(buf[1]);

                                u = DataValue.HoldingRegisters[Adreses[name] + str];

                                buf = _Rangs[i + 2].Split(':');
                                name = buf[0];
                                str = int.Parse(buf[1]);

                                if (u < DataValue.HoldingRegisters[Adreses[name] + str]) ist = 1;
                                else ist = 0;
                            }
                            i += 2;
                            break;
                        case "LEQ":
                            if (ushort.TryParse(_Rangs[i + 2], out u))
                            {
                                buf = _Rangs[i + 1].Split(':');
                                name = buf[0];
                                str = int.Parse(buf[1]);
                                //SetAdresValue(_Rangs[i + 3], (ushort)(u + DataValue.HoldingRegisters[Adreses[name] + str]));
                                if (DataValue.HoldingRegisters[Adreses[name] + str] <= int.Parse(_Rangs[i + 2])) ist = 1;
                                else ist = 0;
                            }
                            else
                            {
                                buf = _Rangs[i + 1].Split(':');
                                name = buf[0];
                                str = int.Parse(buf[1]);

                                u = DataValue.HoldingRegisters[Adreses[name] + str];

                                buf = _Rangs[i + 2].Split(':');
                                name = buf[0];
                                str = int.Parse(buf[1]);

                                if (u <= DataValue.HoldingRegisters[Adreses[name] + str]) ist = 1;
                                else ist = 0;
                            }
                            i += 2;
                            break;
                        case "DIV"://Ц/Ц=Ц
                            if (ist == 1)
                            {

                                if (ushort.TryParse(_Rangs[i + 1], out u))
                                {
                                    buf = _Rangs[i + 2].Split(':');
                                    name = buf[0];
                                    buf = buf[1].Split("/");
                                    str = int.Parse(buf[0]);
                                    SetAdresValue(_Rangs[i + 3], (ushort)(u / DataValue.HoldingRegisters[Adreses[name] + str]));
                                }
                                else if (ushort.TryParse(_Rangs[i + 2], out u))
                                {
                                    buf = _Rangs[i + 1].Split(':');
                                    name = buf[0];
                                    buf = buf[1].Split("/");
                                    str = int.Parse(buf[0]);
                                    SetAdresValue(_Rangs[i + 3], (ushort)(u / DataValue.HoldingRegisters[Adreses[name] + str]));
                                }
                                else
                                {
                                    buf = _Rangs[i + 1].Split(':');
                                    name = buf[0];
                                    buf = buf[1].Split("/");
                                    str = int.Parse(buf[0]);

                                    u = DataValue.HoldingRegisters[Adreses[name] + str];

                                    buf = _Rangs[i + 2].Split(':');
                                    name = buf[0];
                                    buf = buf[1].Split("/");
                                    str = int.Parse(buf[0]);
                                    SetAdresValue(_Rangs[i + 2], (ushort)(u / DataValue.HoldingRegisters[Adreses[name] + str]));
                                }
                            }
                            i += 3;
                            break;
                        case "MUL"://A*B=C
                            if (ist == 1)
                            {

                                if (ushort.TryParse(_Rangs[i + 1], out u))
                                {
                                    buf = _Rangs[i + 2].Split(':');
                                    name = buf[0];
                                    buf = buf[1].Split("/");
                                    str = int.Parse(buf[0]);
                                    SetAdresValue(_Rangs[i + 3], (ushort)(u * DataValue.HoldingRegisters[Adreses[name] + str]));
                                }
                                else if (ushort.TryParse(_Rangs[i + 2], out u))
                                {
                                    buf = _Rangs[i + 1].Split(':');
                                    name = buf[0];
                                    buf = buf[1].Split("/");
                                    str = int.Parse(buf[0]);
                                    SetAdresValue(_Rangs[i + 3], (ushort)(u * DataValue.HoldingRegisters[Adreses[name] + str]));
                                }
                                else
                                {
                                    buf = _Rangs[i + 1].Split(':');
                                    name = buf[0];
                                    buf = buf[1].Split("/");
                                    str = int.Parse(buf[0]);

                                    u = DataValue.HoldingRegisters[Adreses[name] + str];

                                    buf = _Rangs[i + 2].Split(':');
                                    name = buf[0];
                                    buf = buf[1].Split("/");
                                    str = int.Parse(buf[0]);
                                    SetAdresValue(_Rangs[i + 2], (ushort)(u * DataValue.HoldingRegisters[Adreses[name] + str]));
                                }
                            }
                            i += 3;
                            break;
                        case "ABS":
                            if (ist == 1)
                            {
                                buf = _Rangs[i + 1].Split(':');
                                name = buf[0];
                                buf = buf[1].Split("/");
                                str = int.Parse(buf[0]);
                                SetAdresValue(_Rangs[i + 2], (ushort)Math.Abs(DataValue.HoldingRegisters[Adreses[name] + str]));
                            }
                            i += 2;
                            break;
                        case "SCP":
                            i += 6;
                            break;
                        case "MSG":
                            i += 15;
                            break;
                        default:
                            if (el == "XIO") ist = (short)(ist & Convert.ToInt16(!GetAdresBit(_Rangs[i + 1])));
                            else ist = (short)(ist & Convert.ToInt16(GetAdresBit(_Rangs[i + 1])));
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
        private bool GetAdresBit(string st)
        {
            try
            {
                string mas = new Regex(@":\w*(/(\w*)?)?").Replace(st, "");
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
                    adr = DataValue.HoldingRegisters[Adreses["Timer_control"] + ind_1];

                    if ((adr & Bitmask) == Bitmask) return true;
                    return false;
                }
                else if (k.Contains("DN"))
                {
                    Bitmask = 2;
                    ind_1 = int.Parse(k[0]);
                    adr = DataValue.HoldingRegisters[Adreses["Timer_control"] + ind_1];

                    if ((adr & Bitmask) == Bitmask) return true;
                    return false;
                }
                else if (k.Contains("TT"))
                {
                    Bitmask = 4;
                    ind_1 = int.Parse(k[0]);
                    adr = DataValue.HoldingRegisters[Adreses["Timer_control"] + ind_1];

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
        /// Установить значение бита
        /// </summary>
        /// <param name="adr">Адрес изменения</param>
        /// <param name="bit">на что менять (0 или 1)</param>
        private void SetAdresBit(string adr, byte bit)
        {
            if (bit != 1 && bit != 0) return;
            string[] str_bits = { "EN", "DN", "TT" };
            string[] buf = adr.Split(':');
            string name = buf[0];
            buf = buf[1].Split("/");
            int str = int.Parse(buf[0]);
            if (!str_bits.Contains(buf[1]))
            {
                if (bit == 1)
                {
                    DataValue.HoldingRegisters[Adreses[name] + str] = (ushort)(DataValue.HoldingRegisters[Adreses[name] + str] | 1 << int.Parse(buf[1]));
                }
                else
                {
                    if ((DataValue.HoldingRegisters[Adreses[name] + str] & 1 << int.Parse(buf[1])) == 1 << int.Parse(buf[1]))
                        DataValue.HoldingRegisters[Adreses[name] + str] = (ushort)(DataValue.HoldingRegisters[Adreses[name] + str] ^ 1 << int.Parse(buf[1]));
                }
            }
            else
            {
                if (bit == 1)
                {
                    switch (buf[1])
                    {
                        case "DN":
                            DataValue.HoldingRegisters[Adreses[name] + str] = (ushort)(DataValue.HoldingRegisters[Adreses[name] + str] | 1 << 2);
                            break;
                        case "EN":
                            DataValue.HoldingRegisters[Adreses[name] + str] = (ushort)(DataValue.HoldingRegisters[Adreses[name] + str] | 1 << 1);
                            break;
                        case "TT":
                            DataValue.HoldingRegisters[Adreses[name] + str] = (ushort)(DataValue.HoldingRegisters[Adreses[name] + str] | 1 << 4);
                            break;
                        default:
                            break;
                    }

                }
                else
                {
                    switch (buf[1])
                    {
                        case "DN":
                            if ((DataValue.HoldingRegisters[Adreses[name] + str] & 1 << 2) == 1 << 2)
                                DataValue.HoldingRegisters[Adreses[name] + str] = (ushort)(DataValue.HoldingRegisters[Adreses[name] + str] | 1 << 2);
                            break;
                        case "EN":
                            if ((DataValue.HoldingRegisters[Adreses[name] + str] & 1 << 1) == 1 << int.Parse(buf[1]))
                                DataValue.HoldingRegisters[Adreses[name] + str] = (ushort)(DataValue.HoldingRegisters[Adreses[name] + str] | 1 << 1);
                            break;
                        case "TT":
                            if ((DataValue.HoldingRegisters[Adreses[name] + str] & 1 << 4) == 1 << 4)
                                DataValue.HoldingRegisters[Adreses[name] + str] = (ushort)(DataValue.HoldingRegisters[Adreses[name] + str] | 1 << 4);
                            break;
                        default:
                            break;
                    }

                }
            }
        }

        /// <summary>
        /// Установить значение бита
        /// </summary>
        /// <param name="adr">Адрес изменения</param>
        /// <param name="bit">на что менять (0 или 1)</param>
        private void SetAdresValue(string adr, ushort value)
        {
            string[] buf = adr.Split(':');
            string name = buf[0];
            buf = buf[1].Split("/");
            int str = int.Parse(buf[0]);
            DataValue.HoldingRegisters[Adreses[name] + str] = value;
        }

        /// <summary>
        /// Сохранение нового ранга
        /// </summary>
        private void SaveRang()
        {
            ushort pass = DataValue.HoldingRegisters[8201];

            if (pass != 54321) return;

                DataValue.HoldingRegisters[8201] = 0;

            if (DataValue.HoldingRegisters[8600] != 0) return;

            DataValue.HoldingRegisters[8600] = 0;

            string g = DecodingWritesRegister();

            string[] num_text = { "", "" };

            num_text[0] = g.Substring(1, g.IndexOf(' ') - 1);
            num_text[1] = g.Substring(g.IndexOf(' '));

            TextRangs[int.Parse(num_text[0])] = num_text[1];
            transfer(TextRangs);
        }

        /// <summary>
        /// Расшифровка полученного ранга
        /// </summary>
        /// <returns>Новый ранг</returns>
        private string DecodingWritesRegister()
        {
            ushort[] inputs = new ushort[240];

            string g = "";
            int len = 0;
            int buf;

            for (int i = 0; i < 240; i++)
            {
                if (DataValue.HoldingRegisters[SaveaNewRang + i] == 0) break;
                inputs[i] = DataValue.HoldingRegisters[SaveaNewRang + i];
            }

            for (int i = 0; i < 240; i++)
            {
                if (inputs[i] != 0)
                {
                    buf = (inputs[i] & 0xff);
                    if (buf != 0)
                    {
                        g += (char)((char)inputs[i] & 0xff);
                        len++;
                    }
                    buf = (inputs[i] >> 8);
                    if (buf != 0)
                    {
                        g += (char)((char)inputs[i] >> 8);
                        len++;
                    }
                }
                else break;
            }

            return g;
        }

        /// <summary>
        /// Тестирование полученного рага
        /// </summary>
        private void TestNewRang()
        {
            CheckingTheCorrectness checking = new(100, 100, 100, CheckingType.Emulation);
            Thread.Sleep(25);
            string str_from_check = DecodingWritesRegister();

            ushort result = (ushort)checking.CheckRangText(str_from_check);
            DataValue.HoldingRegisters[8600] = result;

            if (result != 0)
                for (int regist = 8400; regist < 8600; regist++)
                    DataValue.HoldingRegisters[regist] = 0;
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
                string mess = e.Message + " " + e.Message.FunctionCode;
                //if (listlog.Items.Count > 14) listlog.Items.Remove(listlog.Items[2]);
                if (!listlog.Items.Contains(mess))
                {
                    listlog.Items.Add(mess);
                    listlog.SelectedIndex = listlog.Items.Count - 1;
                }
            }));
            if (e.Message.FunctionCode == 16)
            {
                //mb_tcp_server.DataStore = NewRangData;
                if ((e.Message.MessageFrame[2] * 256 + e.Message.MessageFrame[3]) / 100 == 82)
                {
                    new Thread(() => { SaveRang(); }).Start();
                }
                if ((e.Message.MessageFrame[2] * 256 + e.Message.MessageFrame[3]) / 100 == 84)
                {
                    new Thread(() => { TestNewRang(); }).Start();
                }
            }
            if (e.Message.SlaveAddress < 5)
            {
                // Регистры
                if (((e.Message.MessageFrame[2] * 256 + e.Message.MessageFrame[3]) / 1000 == RangAdr / 1000) && (e.Message.FunctionCode == 3))
                {
                    if ((e.Message.MessageFrame[2] * 256 + e.Message.MessageFrame[3]) % 100 < DataRangs.Length && (e.Message.MessageFrame[2] * 256 + e.Message.MessageFrame[3]) % 100 >= 0)
                        mb_tcp_server.DataStore = DataRangs[(e.Message.MessageFrame[2] * 256 + e.Message.MessageFrame[3]) % 100];
                    else
                        mb_tcp_server.DataStore = DataValue;
                }
                else if (DataRangs.Contains(mb_tcp_server.DataStore)) mb_tcp_server.DataStore = DataValue;

            }
            else
            {
                // Конфигурация
                if (((e.Message.MessageFrame[2] * 256 + e.Message.MessageFrame[3]) / 1000 == ConfigAdr / 1000) && (e.Message.FunctionCode == 3))
                {
                    if ((e.Message.MessageFrame[2] * 256 + e.Message.MessageFrame[3]) % 100 < CfgRangs.Length && (e.Message.MessageFrame[2] * 256 + e.Message.MessageFrame[3]) % 100 >= 0)
                        mb_tcp_server.DataStore = CfgRangs[(e.Message.MessageFrame[2] * 256 + e.Message.MessageFrame[3]) % 100];
                    else
                        mb_tcp_server.DataStore = VoidData;
                }
                else if (DataRangs.Contains(mb_tcp_server.DataStore)) mb_tcp_server.DataStore = VoidData;
            }

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
            else if (comboBox1.SelectedItem.ToString() == "Конфигурация")
            {
                ConfigAdr = int.Parse(AdresUpdate.Text) + 1;
                return;
            }
            else if (comboBox1.SelectedItem.ToString() == "Запись ранга")
            {
                SaveaNewRang = int.Parse(AdresUpdate.Text) + 1;
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
            int index = comboBox1.SelectedIndex;
            comboBox1.Items.Remove(comboBox1.SelectedItem);
            AdresUpdate.Clear();
            comboBox1.SelectedIndex = index % comboBox1.Items.Count;

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
                if (Delete.Enabled) Delete.Enabled = false;
                AdresUpdate.Text = (RangAdr - 1).ToString();
                AdresUpdate.Focus();
                return;
            }
            else if (comboBox1.SelectedItem.ToString() == "Конфигурация")
            {
                if (Delete.Enabled) Delete.Enabled = false;
                AdresUpdate.Text = (ConfigAdr - 1).ToString();
                AdresUpdate.Focus();
                return;
            }
            else if (comboBox1.SelectedItem.ToString() == "Запись ранга")
            {
                if (Delete.Enabled) Delete.Enabled = false;
                AdresUpdate.Text = (SaveaNewRang - 1).ToString();
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
            Properties.Settings.Default.Rang = RangAdr;
            Properties.Settings.Default.Adres = string.Join(",", Adreses.Values.ToArray());
            Properties.Settings.Default.AdresName = string.Join(",", Adreses.Keys.ToArray());
            Properties.Settings.Default.ConfigAdr = ConfigAdr;
            Properties.Settings.Default.SaveaNewRang = SaveaNewRang;

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

        /// <summary>
        /// Выгрузить регистры с именами
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UPload_Click(object sender, EventArgs e)
        {
            string _data = "";

            _data += "RANDS:" + (RangAdr - 1) + "\n";
            _data += "CONFIG:" + (ConfigAdr - 1) + "\n";
            _data += "SAVERANG:" + (SaveaNewRang - 1) + "\n";

            foreach (string key in Adreses.Keys)
            {
                _data += key + ":" + (Adreses[key] - 1) + ';' + Data[key].Length + "\n";
            }
            saveFileDialog1.InitialDirectory = Location.Text;
            saveFileDialog1.RestoreDirectory = true;
            saveFileDialog1.Filter = "ModBus config (*.MBcfg)|*.MBcfg";
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                CreateFile.ToFile(saveFileDialog1.FileName, _data);
            }
        }

        /// <summary>
        /// Загрузить регистры с именами
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Load_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "ModBus config (*.MBcfg)|*.MBcfg";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string[] _data = File.ReadAllLines(openFileDialog1.FileName).Where(x => x != "").ToArray();
                string[] buf;
                var mbres = MessageBox.Show("Заменить имеющиеся данные на полученные из файла?", "Внимание!", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (mbres == DialogResult.Yes)
                {
                    RangAdr = int.Parse(_data[0].Split(':')[1]) + 1;
                    ConfigAdr = int.Parse(_data[1].Split(':')[1]) + 1;
                    SaveaNewRang = int.Parse(_data[2].Split(':')[1]) + 1;
                    Adreses.Clear();
                    for (int i = 2; i < _data.Length; i++)
                    {
                        buf = _data[i].Split(":");
                        Adreses.Add(buf[0], int.Parse(buf[1].Split(";")[0]) + 1);
                    }
                    comboBox1.Items.Clear();
                    comboBox1.Items.Add("Расположение рангов");
                    comboBox1.Items.Add("Конфигурация");
                    comboBox1.Items.Add("Запись ранга");
                    comboBox1.Items.AddRange(Adreses.Keys.ToArray());
                }
                else if (mbres == DialogResult.No)
                {
                    for (int i = 2; i < _data.Length; i++)
                    {
                        buf = _data[i].Split(":");
                        if (!Adreses.ContainsKey(buf[0]))
                        {
                            Adreses.Add(buf[0], int.Parse(buf[1].Split(";")[0]) + 1);
                            comboBox1.Items.Add(buf[0]);
                        }
                    }
                }
            }
        }
    }
}