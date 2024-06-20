using System;
using System.Net;
using System.Net.Sockets;
using Modbus.Device;
using Modbus.Data;
using System.Threading;
using Microsoft.VisualBasic.Logging;


namespace ModbasServer
{
    public partial class Form1 : Form
    {
        private bool start_stop = true;
        private static DataStore DataValue;
        private static DataStore[] DataRangs;
        private static ModbusSlave mb_tcp_server;
        private static Thread ListenThred;
        private static Thread GenerateData;


        public Form1()
        {
            InitializeComponent();
            label1.Text = "";

            DataValue = DataStoreFactory.CreateDefaultDataStore();
        }


        /// <summary>
        /// Конвертация и запись в регистры текста программы
        /// </summary>
        /// <param name="Text">Адрес на файл</param>
        private void transfer(string Text)
        {
            var rangs = File.ReadAllLines(Text);
            DataRangs = new DataStore[rangs.Length];
            string line;
            DataStore rang;
            //текст ранга
            for (int ind_Rang = 0; ind_Rang < rangs.Length; ind_Rang++)
            {
                int count = 8001+ind_Rang;
                line = rangs[ind_Rang];
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
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.ShowDialog();
            Location.Text = ofd.FileName;
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
                            mb_tcp_server = ModbusTcpSlave.CreateTcp(2, new TcpListener(new IPAddress(new byte[] { 0, 0, 0, 0 }), 502));
                            mb_tcp_server.DataStore = DataValue;
                            mb_tcp_server.ModbusSlaveRequestReceived += Mb_tcp_server_ModbusSlaveRequestReceived;

                            start_stop = !start_stop;
                            StartStop.BackColor = Color.Red;
                            StartStop.Text = "Стоп";
                            label1.Text = "127.0.0.1:502";

                            

                            if(ListenThred == null)
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

                            if(GenerateData == null)
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
                            if (Location.Text != "") transfer(Location.Text);
                            break;
                        }
                    }
            }
            catch(Exception ex) { MessageBox.Show(ListenThred.ThreadState.ToString()+" "+ex.Message); }
        }

        /// <summary>
        /// Задача и обновление регистров
        /// </summary>
        public static void SetData()
        {
            int[] adres = new int[] { 1000, 600, 1200, 2000, 7200, 1300, 7000, 6800 };
            Random random = new Random();
            while (true)
            {
                Thread.Sleep(200);
                foreach (int ad in adres)
                {
                    for (int i = 0; i < 100; i++)
                    {
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

            if (((e.Message.MessageFrame[2] * 256 + e.Message.MessageFrame[3]) /1000 == 8) && (e.Message.FunctionCode == 3))
            {
                if ((e.Message.MessageFrame[2] * 256 + e.Message.MessageFrame[3]) % 100 < DataRangs.Length && (e.Message.MessageFrame[2] * 256 + e.Message.MessageFrame[3]) % 100 >= 0)
                    mb_tcp_server.DataStore = DataRangs[(e.Message.MessageFrame[2] * 256 + e.Message.MessageFrame[3]) % 100];
                else
                    mb_tcp_server.DataStore = DataValue;
            }
            else if(DataRangs.Contains(mb_tcp_server.DataStore)) mb_tcp_server.DataStore = DataValue;
        }
    }
}