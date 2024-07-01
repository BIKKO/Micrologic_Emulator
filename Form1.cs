using System.Net;
using System.Net.Sockets;
using Modbus.Device;
using Modbus.Data;
using System.Text.RegularExpressions;
using System.Diagnostics;


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
                    for(int el = 0; el < buf.Length; el++)
                    {
                        if (buf[el] == "TON")
                        {
                            int st = int.Parse(buf[el+1].Split(":")[1]);
                            TimeBase[st] = (int)(double.Parse(buf[el + 2].Replace('.',',')) * 100)!=0? (int)(double.Parse(buf[el + 2].Replace('.', ',')) * 100): 1;
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

        private void RangsInfo(string Text)
        {
            string[] com_str = Text.Trim().Split(' ');
            int C_GROPE_MAX = 8;
            int C_BRANCH_MAX = 16;
            int[] BST_Num = new int[com_str.Length];
            ushort[] MnemonicCod = new ushort[com_str.Length];
            int BranchNum = 0;
            int BST_Count = 0;
            int BranchInGropeCount = 0;
            int LogicGropeNum = 0;
            int[] BranchInGropeMax = new int[8];
            int[] BranchStart = new int[8];
            int[,] BranchInGropeNumbers = new int[10, 8];
            int[] BrunchCod = new int[32];
            int[] BranchEnd = new int[C_BRANCH_MAX];
            int CulckFlag = 0;
            int OnsInRang;
            int bn = 0;

            ushort BRANCHes_MASK = 0x80CF;
            ushort CULCK_LOGIC_MNEMON = 0x4000;
            ushort NXB_RESET_MNEMON = 0x80C9;

            ushort XIC_MNEMON = 0x8001; //логика НО
            ushort XIO_MNEMON = 0x8002;//логика НЗ
            ushort ONS_MNEMON = 0x8003;//по фронту

            ushort OTE_MNEMON = 0x8010;//присвоение
            ushort OTL_MNEMON = 0x8011;
            ushort OTU_MNEMON = 0x8012;

            ushort ADD_MNEMON = 0x8018; //сумма двух слов
            ushort SUB_MNEMON = 0x8019;	//разность
            ushort MOV_MNEMON = 0x801A; //присвоение

            ushort BST_MNEMON = 0x8020; //начало первой ветви
            ushort NXB_MNEMON = 0x8021; //начало другой ветви
            ushort BND_MNEMON = 0x8022; //конец всех предыдущих ветвей

            ushort EQU_MNEMON = 0x8040; //сравнение
            ushort NEQ_MNEMON = 0x8041;
            ushort LES_MNEMON = 0x8042;
            ushort GRT_MNEMON = 0x8043;
            ushort LEQ_MNEMON = 0x8044;
            ushort GEQ_MNEMON = 0x8045;
            ushort SCP_MNEMON = 0x801C;
            ushort DIV_MNEMON = 0x8017;
            ushort MUL_MNEMON = 0x8016;
            ushort ABS_MNEMON = 0x801C;
            ushort MSG_MNEMON = 0x8100;

            ushort TON_MNEMON = 0x8080;	//включение таймера по фронту


            for (; bn < com_str.Length; bn++)
            {
                if (com_str[bn] == "XIC")
                {
                    //vPrintDec("XIC ", 0); vPrintS("%s\n", com_str[k+1]);z
                    MnemonicCod[bn] = XIC_MNEMON;
                    BrunchCod[bn] = BranchNum | (LogicGropeNum << 4);
                    //if(BranchNum==0)
                    BranchEnd[BranchNum] = bn; //Оганичение на конфигурацию ветвей. Сначала линейный код, затем ветви.
                    bn++;
                    //GetBitAddr(com_str[bn], bn, RangNum);
                    //	BRANCH_MASK
                }
                else if (com_str[bn] == "XIO")
                {
                    //vPrintDec("XIO ", 0); vPrintS("%s\n", com_str[k+1]);
                    MnemonicCod[bn] = XIO_MNEMON;
                    BrunchCod[bn] = BranchNum | (LogicGropeNum << 4);
                    bn++;
                    //GetBitAddr(com_str[bn], bn, RangNum);
                }
                else if (com_str[bn] == "NEQ")
                {
                    //vPrintDec("XIO ", 0); vPrintS("%s\n", com_str[k+1]);
                    MnemonicCod[bn] = NEQ_MNEMON;
                    BrunchCod[bn] = BranchNum | (LogicGropeNum << 4);
                    bn++;
                    //GetCompareAddr(bn, RangNum);
                }
                else if (com_str[bn] == "EQU")
                {
                    //vPrintDec("XIO ", 0); vPrintS("%s\n", com_str[k+1]);
                    MnemonicCod[bn] = EQU_MNEMON;
                    BrunchCod[bn] = BranchNum | (LogicGropeNum << 4);
                    BranchEnd[BranchNum] = bn; //
                    bn++;
                    //GetCompareAddr(bn, RangNum);
                }
                else if (com_str[bn] == "LES")
                {
                    //vPrintDec("XIO ", 0); vPrintS("%s\n", com_str[k+1]);
                    MnemonicCod[bn] = LES_MNEMON;
                    BrunchCod[bn] = BranchNum | (LogicGropeNum << 4);
                    BranchEnd[BranchNum] = bn; //
                    bn++;
                    //GetCompareAddr(bn, RangNum);
                }
                else if (com_str[bn] == "GRT")
                {
                    //vPrintDec("XIO ", 0); vPrintS("%s\n", com_str[k+1]);
                    MnemonicCod[bn] = GRT_MNEMON;
                    BrunchCod[bn] = BranchNum | (LogicGropeNum << 4);
                    BranchEnd[BranchNum] = bn; //
                    bn++;
                    //GetCompareAddr(bn, RangNum);
                }
                else if (com_str[bn] == "LEQ")
                {
                    //vPrintDec("XIO ", 0); vPrintS("%s\n", com_str[k+1]);
                    MnemonicCod[bn] = LEQ_MNEMON;
                    BrunchCod[bn] = BranchNum | (LogicGropeNum << 4);
                    BranchEnd[BranchNum] = bn; //
                    bn++;
                    //GetCompareAddr(bn, RangNum);
                }
                else if (com_str[bn] == "GEQ")
                {
                    //vPrintDec("XIO ", 0); vPrintS("%s\n", com_str[k+1]);
                    MnemonicCod[bn] = GEQ_MNEMON;
                    BrunchCod[bn] = BranchNum | (LogicGropeNum << 4);
                    BranchEnd[BranchNum] = bn; //
                    bn++;
                    //GetCompareAddr(bn, RangNum);
                }
                else if (com_str[bn] == "MOV")
                {
                    //vPrintDec("XIO ", 0); vPrintS("%s\n", com_str[k+1]);
                    MnemonicCod[bn] = MOV_MNEMON;
                    MnemonicCod[bn] |= CULCK_LOGIC_MNEMON;
                    BrunchCod[bn] = BranchNum | (LogicGropeNum << 4);
                    bn++;
                    //GetCompareAddr(bn, RangNum);

                    if (!Convert.ToBoolean(CulckFlag)) CulckFlag = bn;
                }
                else if (com_str[bn] == "ABS")
                {
                    //vPrintDec("XIO ", 0); vPrintS("%s\n", com_str[k+1]);
                    MnemonicCod[bn] = ABS_MNEMON;
                    MnemonicCod[bn] |= CULCK_LOGIC_MNEMON;
                    BrunchCod[bn] = BranchNum | (LogicGropeNum << 4);
                    bn++;
                    //GetCompareAddr(bn, RangNum);

                    if (!Convert.ToBoolean(CulckFlag)) CulckFlag = bn;
                }
                else if (com_str[bn] == "MUL")
                {
                    //vPrintDec("XIO ", 0); vPrintS("%s\n", com_str[k+1]);
                    MnemonicCod[bn] = MUL_MNEMON;
                    MnemonicCod[bn] |= CULCK_LOGIC_MNEMON;
                    BrunchCod[bn] = BranchNum | (LogicGropeNum << 4);
                    bn++;
                    //Get3Addr(bn, RangNum);

                    if (!Convert.ToBoolean(CulckFlag)) CulckFlag = bn;
                }
                else if (com_str[bn] == "DIV")
                {
                    //vPrintDec("XIO ", 0); vPrintS("%s\n", com_str[k+1]);
                    MnemonicCod[bn] = DIV_MNEMON;
                    MnemonicCod[bn] |= CULCK_LOGIC_MNEMON;
                    BrunchCod[bn] = BranchNum | (LogicGropeNum << 4);
                    bn++;
                    //Get3Addr(bn, RangNum);

                    if (!Convert.ToBoolean(CulckFlag)) CulckFlag = bn;
                }
                else if (com_str[bn] == "ADD")
                {
                    //vPrintDec("XIO ", 0); vPrintS("%s\n", com_str[k+1]);
                    MnemonicCod[bn] = ADD_MNEMON;
                    MnemonicCod[bn] |= CULCK_LOGIC_MNEMON;
                    BrunchCod[bn] = BranchNum | (LogicGropeNum << 4);
                    bn++;
                    //Get3Addr(bn, RangNum);

                    if (!Convert.ToBoolean(CulckFlag)) CulckFlag = bn;
                }
                else if (com_str[bn] == "SUB")
                {
                    //vPrintDec("XIO ", 0); vPrintS("%s\n", com_str[k+1]);
                    MnemonicCod[bn] = SUB_MNEMON;
                    MnemonicCod[bn] |= CULCK_LOGIC_MNEMON;
                    BrunchCod[bn] = BranchNum | (LogicGropeNum << 4);
                    bn++;
                    //Get3Addr(bn, RangNum);

                    if (!Convert.ToBoolean(CulckFlag)) CulckFlag = bn;
                }
                else if (com_str[bn] == "TON")
                {
                    //vPrintDec("XIO ", 0); vPrintS("%s\n", com_str[k+1]);
                    MnemonicCod[bn] = TON_MNEMON;
                    MnemonicCod[bn] |= CULCK_LOGIC_MNEMON;
                    BrunchCod[bn] = BranchNum | (LogicGropeNum << 4);
                    bn++;
                    //SetTimerCod(bn, RangNum);

                    if (!Convert.ToBoolean(CulckFlag)) CulckFlag = bn;
                    //bn+=3;
                }
                else if (com_str[bn] == "SCP")
                {
                    MnemonicCod[bn] = SCP_MNEMON;
                    MnemonicCod[bn] |= CULCK_LOGIC_MNEMON;
                    BrunchCod[bn] = BranchNum | (LogicGropeNum << 4);
                    bn++;
                    //SetSCP_cod(bn, RangNum);
                    //bn+=15; // SCP N10:13 0 20000 0 800 N10:14
                }
                else if (com_str[bn] == "MSG")
                {
                    MnemonicCod[bn] = MSG_MNEMON;
                    MnemonicCod[bn] |= CULCK_LOGIC_MNEMON;
                    BrunchCod[bn] = BranchNum | (LogicGropeNum << 4);
                    bn++;
                    //SetMessCod(bn, RangNum);
                    //bn+=15; //MSG MG12:19  16 LOCAL 101 192.168.10.13 N44:0 1801 RI9:0 2 22 0 SLOT:0 0 5 502
                }
                else if (com_str[bn] == "OTE")
                {
                    MnemonicCod[bn] = OTE_MNEMON;
                    //if(CulckFlag)
                    MnemonicCod[bn] |= CULCK_LOGIC_MNEMON;
                    BrunchCod[bn] = BranchNum | (LogicGropeNum << 4);
                    BranchEnd[BranchNum] = bn; //Оганичение на конфигурацию ветвей. Сначала линейный код, затем ветви.
                    bn++;
                    //GetBitAddr(com_str[bn], bn, RangNum);

                    if (!Convert.ToBoolean(CulckFlag)) CulckFlag = bn;
                }
                else if (com_str[bn] == "OTL")
                {
                    MnemonicCod[bn] = OTL_MNEMON;
                    MnemonicCod[bn] |= CULCK_LOGIC_MNEMON;
                    BrunchCod[bn] = BranchNum | (LogicGropeNum << 4);
                    BranchEnd[BranchNum] = bn; //Оганичение на конфигурацию ветвей. Сначала линейный код, затем ветви.
                    bn++;
                    //GetBitAddr(com_str[bn], bn, RangNum);

                    if (!Convert.ToBoolean(CulckFlag)) CulckFlag = bn;
                }
                else if (com_str[bn] == "OTU")
                {
                    MnemonicCod[bn] = OTU_MNEMON;
                    MnemonicCod[bn] |= CULCK_LOGIC_MNEMON;
                    BrunchCod[bn] = BranchNum | (LogicGropeNum << 4);
                    BranchEnd[BranchNum] = bn; //Оганичение на конфигурацию ветвей. Сначала линейный код, затем ветви.
                    bn++;
                    //GetBitAddr(com_str[bn], bn, RangNum);

                    if (!Convert.ToBoolean(CulckFlag)) CulckFlag = bn;
                }
                else if (com_str[bn] == "ONS")
                {
                    MnemonicCod[bn] = ONS_MNEMON;
                    //if(CulckFlag)
                    MnemonicCod[bn] |= CULCK_LOGIC_MNEMON;
                    BrunchCod[bn] = BranchNum | (LogicGropeNum << 4);
                    bn++;
                    //GetBitAddr(com_str[bn], bn, RangNum);
                }

                else if (com_str[bn] == "BST")//branch start ///проверка на соответсятвие 
                {
                    BST_Count++;
                    BST_Num[bn] = BST_Count; //запомнить в массиве BST_Num

                    MnemonicCod[bn] = BST_MNEMON;
                    if (Convert.ToBoolean(CulckFlag)) MnemonicCod[bn] |= CULCK_LOGIC_MNEMON;

                    BranchInGropeCount = 0; //счет ветвей сначала
                    BranchInGropeMax[LogicGropeNum] = BranchInGropeCount;
                    if (bn != 0) //не первый элемент в ранге
                    {
                        if ((MnemonicCod[0] == BST_MNEMON) && (bn == 1)) ; //первый элемент и второй в ранге
                        else LogicGropeNum++;
                    }
                    BranchInGropeNumbers[LogicGropeNum, BranchInGropeCount] = BranchNum; //сохранить предыдущую номер ветви

                    if (MnemonicCod[bn - 1] == BND_MNEMON) ;//BND предыдущий
                    else
                    {
                        if (bn != 0) //не первый элемент в ранге
                            BranchNum++;
                    }
                    BranchStart[BranchNum] = bn + 1; //next Rung_logic

                }
                else if (com_str[bn] == "NXB")//next branch ///проверка на соответсятвие 
                {
                    MnemonicCod[bn] = NXB_MNEMON;

                    //найдем BST к которому относится NXB
                    //задача - определить - эта ветвь основная (независимая) ? NXB_RESET_MNEMON или локальная
                    int p, i, BND_Count, FindKey, FindPos = 0;

                    p = bn - 1; //начнем с текущей позиции к началу
                    FindKey = 0;
                    BND_Count = 0;
                    for (i = 0; i <= (bn - 1); i++, p--)
                    {
                        if ((MnemonicCod[p] & BRANCHes_MASK) == BND_MNEMON)
                        {
                            BND_Count++;
                        }
                        else if ((MnemonicCod[p] & BRANCHes_MASK) == BST_MNEMON)
                        {
                            if (BND_Count == 0)
                            {
                                if ((BST_Num[p] == 1) && (p == 0)) { FindKey = 1; FindPos = p; } //только номер 1 в позиции 0 укзывает на начало ранга
                                break; //остановка при первом встречном BST
                            }
                            else BND_Count--; //пропустить парный c BND BST
                        }
                    }
                    if (Convert.ToBoolean(FindKey))
                        if (BST_Num[FindPos] == 1) //if(BST_Count == 1 ) //IndBranchNum++; //переход на след. ветвь т.к. это основное ветвление
                            if (Convert.ToBoolean(CulckFlag)) //IndBranchNum++; //переход на след. ветвь т.к. вычисления уже прошли
                            {
                                MnemonicCod[bn] = NXB_RESET_MNEMON;

                                //предыдущие параметры обнуляются
                                CulckFlag = OnsInRang = 0;
                                LogicGropeNum = 0;
                                for (i = 0; i < C_BRANCH_MAX; i++) BranchInGropeMax[i] = 0;
                                for (i = 0; i < C_GROPE_MAX * C_BRANCH_MAX; i++) BranchInGropeMax[i] = 0;
                            }

                    if (Convert.ToBoolean(CulckFlag)) MnemonicCod[bn] |= CULCK_LOGIC_MNEMON;

                    BranchInGropeNumbers[LogicGropeNum, BranchInGropeCount] = BranchNum; //сохранить предыдущую номер ветви

                    if (MnemonicCod[bn] != NXB_RESET_MNEMON) //если не начало независимой ветви
                        BranchInGropeCount++;
                    BranchInGropeMax[LogicGropeNum] = BranchInGropeCount;

                    BranchNum++;
                    BranchStart[BranchNum] = bn + 1; //next Rung_logic
                }

                else if (com_str[bn] == "BND")// branch end ///проверка на соответсятвие 
                {

                    MnemonicCod[bn] = BND_MNEMON;
                    if (Convert.ToBoolean(CulckFlag)) MnemonicCod[bn] |= CULCK_LOGIC_MNEMON;

                    BranchInGropeNumbers[LogicGropeNum, BranchInGropeCount] = BranchNum; //сохранить предыдущую номер ветви

                    BranchInGropeMax[LogicGropeNum] = BranchInGropeCount;

                    if (bn != (com_str.Length - 1))  //not lust element
                    {
                        BranchNum++;
                        BranchStart[BranchNum] = bn + 1; //next Rung_logic
                    }
                }
            }
            CulckFlag--; //теперь указывает не на адресс, а на код

            //поиск ветвей при выполнении и исключение их из расчета логики ранга
            bn = CulckFlag - 1; //указывает на код ветвдения (возможно)

            if ((MnemonicCod[com_str.Length - 1]) == BND_MNEMON)//если последняя группа ветвей (исполняемые ветви)
                bn--;
            for (; bn > 0; bn--)
            {
                if ((MnemonicCod[bn] & BST_MNEMON) == BST_MNEMON)
                {
                    if ((MnemonicCod[bn]) != NXB_RESET_MNEMON) MnemonicCod[bn] |= CULCK_LOGIC_MNEMON;
                    break;
                }

            }

            MnemonicCod[CulckFlag] |= CULCK_LOGIC_MNEMON;
            Debug.Print("fffff");
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