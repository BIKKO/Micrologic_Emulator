using System.Net;
using static System.Net.Mime.MediaTypeNames;

namespace LogixForms.HelperClasses
{
    /// <summary>
    /// Режимы работы проверки
    /// </summary>
    public enum CheckingType
    {
        Local,
        Emulation
    }

    /// <summary>
    /// Класс - осуществляющий проверку правельности ранга
    /// </summary>
    public class CheckingTheCorrectness
    {
        private const ushort MSG_KEY = 0x800; //код для операций с message
        private const ushort TON_KEY = 0x400; //код для операций с тамером
        private const ushort ADD3_KEY = 0x200; //код для трех адресных операций
        private const ushort ADD2_KEY = 0x100; //код для двух адресных операций
        private const ushort BIT_KEY = 0x80; //код для битовых операций
        private const ushort ONS_KEY = 0x40; //код для подсчета ONS
        private const ushort BST_KEY = 0x21; //код для подсчета BST
        private const ushort BND_KEY = 0x11; //код для подсчета BND
        private const string sym_addr = ":";

        private ushort MAX_WORDS_NUM;
        private ushort MAX_MESS_SIZE;
        private ushort TIMER_MAX;

        private ushort[] CheckKey;
        private string[] com_str;
        private CheckingType checking;

        /// <summary>
        /// Основной конструктор класса
        /// </summary>
        /// <param name="MAX_WORDS_NUM">Максимальное количество слов в ранге</param>
        /// <param name="MAX_MESS_SIZE">Максимальный размер сообщения</param>
        /// <param name="TIMER_MAX"> - </param>
        /// <param name="checkingType">Режим работы проверки</param>
        public CheckingTheCorrectness(ushort MAX_WORDS_NUM, ushort MAX_MESS_SIZE, ushort TIMER_MAX, CheckingType checkingType)
        {
            this.MAX_WORDS_NUM = MAX_WORDS_NUM;
            this.MAX_MESS_SIZE = MAX_MESS_SIZE;
            this.TIMER_MAX = TIMER_MAX;
            checking = checkingType;
            CheckKey = new ushort[MAX_WORDS_NUM];
        }

        /// <summary>
        /// Проверка на правильность составления Messeg
        /// </summary>
        /// <param name="bn">Порядкоый номер в тексте ранга</param>
        /// <returns>Код ошибки</returns>
        private byte CheckMSG(byte bn)
        {
            int len_word;
            string addr_str;
            char grope = ' ', grope1 = ' ', grope_num = ' ';
            byte word_num = 0;
            int Preset;
            byte ErrKey = 0;
            byte out_log = 0;

            addr_str = com_str[bn]; // Первый аргумент

            // MG12:0 10 LOCAL 101 192.168.11.48 N15:16 1601 RI9:0 2 220 0 SLOT:0 0 5 503

            // MG12:0
            len_word = addr_str.IndexOf(':'); // Количество символов до ':'
            if (len_word != 0)
            {
                grope = addr_str[0];
                grope1 = addr_str[1];
                grope_num = (char)int.Parse(addr_str.Substring(2, len_word - 2));

                string rest = addr_str.Substring(len_word + 1);
                word_num = byte.Parse(rest.Split(' ')[0]);
            }

            if ((grope == 'M') && (grope1 == 'G'))
            {
                if (grope_num == 12)
                {
                    if (word_num >= MAX_MESS_SIZE)
                        ErrKey = 3; // Количество должно быть 0-MAX_MESS_SIZE
                    else
                        CheckKey[bn] = 1;
                }
                else
                    ErrKey = 5; // Нет такой группы в MG
            }
            else
                ErrKey = 5; // Нет такой группы

            bn++;

            // 10
            Preset = int.Parse(com_str[bn]);
            if (Preset == 16)
                CheckKey[bn] = 1;
            else if (Preset == 3)
                CheckKey[bn] = 1;
            else
                ErrKey = 12;
            bn++;

            // LOCAL
            if (com_str[bn] == "LOCAL")
                CheckKey[bn] = 1;
            else
                ErrKey = 13;
            bn++;

            // 101 (в 16-ричной системе)
            Preset = Convert.ToInt32(com_str[bn], 16);
            if (Preset == 0x101)
                CheckKey[bn] = 1;
            else if (Preset == 0x1)
                CheckKey[bn] = 1;
            else
                ErrKey = 13;
            bn++;

            // 192.168.11.48 (проверка IP)
            string ipStr = com_str[bn];
            if (ipStr.Length < 7 || ipStr.Length > 15)
                ErrKey = 14;
            else
            {
                if (IPAddress.TryParse(ipStr, out _))
                    CheckKey[bn] = 1;
                else
                    ErrKey = 14;
            }
            bn++;

            // N15:16
            addr_str = com_str[bn];
            len_word = addr_str.IndexOf(':');
            if (len_word != 0)
            {
                if (addr_str[0] > '9') // Буква
                {
                    grope = addr_str[0];
                    grope_num = (char)int.Parse(addr_str.Substring(1, len_word - 1));
                    word_num = byte.Parse(addr_str.Substring(len_word + 1));
                }
            }

            if (grope == 'N')
            {
                if (grope_num == 13)
                {
                    if (word_num > 2)
                        out_log = 3; // Должно быть 0-2
                    else
                    {
                        CheckKey[bn] = 1;
                        out_log = 0;
                    }
                }
                else if (grope_num == 15)
                {
                    if (word_num > 79)
                        out_log = 3; // Должно быть 0-79
                    else
                    {
                        CheckKey[bn] = 1;
                        out_log = 0;
                    }
                }
                else if (grope_num == 18)
                {
                    if (word_num > 31)
                        out_log = 3; // Должно быть 0-31
                    else
                    {
                        CheckKey[bn] = 1;
                        out_log = 0;
                    }
                }
                else if (grope_num == 40)
                {
                    if (word_num > 0)
                        out_log = 3; // Должно быть 0
                    else
                    {
                        CheckKey[bn] = 1;
                        out_log = 0;
                    }
                }
                else
                    out_log = 5; // Нет такой группы в N
            }
            else if (grope == 'B')
            {
                if (grope_num == 13)
                {
                    if (word_num > 31)
                        ErrKey = 3; // Должно быть 0-31
                    else
                    {
                        CheckKey[bn] = 1;
                        out_log = 0;
                    }
                }
            }

            if (out_log != 0)
                return out_log;
            bn++;

            // 1601 MB addr
            Preset = int.Parse(com_str[bn]);
            if (Preset <= 0x0)
                ErrKey = 15;
            else if (Preset > 65535)
                ErrKey = 15;
            else
                CheckKey[bn] = 1;
            bn++;

            // RI9:0
            CheckKey[bn] = 1;
            bn++;

            // 2 len
            Preset = int.Parse(com_str[bn]);
            if (Preset <= 0x1)
                ErrKey = 16;
            else if (Preset > 255)
                ErrKey = 16;
            else
                CheckKey[bn] = 1;
            bn++;

            // 220 MTO
            Preset = int.Parse(com_str[bn]);
            if (Preset < 0)
                ErrKey = 17;
            else if (Preset > 32000)
                ErrKey = 17;
            else
                CheckKey[bn] = 1;
            bn++;

            // 0
            CheckKey[bn] = 1;
            bn++;

            // SLOT:0
            CheckKey[bn] = 1;
            bn++;

            // 0
            CheckKey[bn] = 1;
            bn++;

            // 5 NOD
            Preset = int.Parse(com_str[bn]);
            if (Preset < 0)
                ErrKey = 18;
            else if (Preset > 255)
                ErrKey = 18;
            else
                CheckKey[bn] = 1;
            bn++;

            // 503 Port
            Preset = int.Parse(com_str[bn]);
            if (Preset < 0)
                ErrKey = 19;
            else if (Preset > 65535)
                ErrKey = 19;
            else
                CheckKey[bn] = 1;

            return ErrKey;
        }

        /// <summary>
        /// Проверка на правильность соствления Timer
        /// </summary>
        /// <param name="bn">Порядкоый номер в тексте ранга</param>
        /// <returns>Код ошибки</returns>
        private byte CheckTON(byte bn)
        {
            int len_word;
            string addr_str;
            char grope = ' ';
            char grope_num = ' ';
            byte word_num = 0;
            short Preset, Accum;
            byte ErrKey = 0;

            addr_str = com_str[bn]; // Первый аргумент

            // Обработка формата типа "T4:10"
            len_word = addr_str.IndexOf(':'); // Количество символов до ':'
            if (len_word != 0)
            {
                grope = addr_str[0];
                grope_num = (char)int.Parse(addr_str.Substring(1, len_word - 1));

                string rest = addr_str.Substring(len_word + 1);
                word_num = byte.Parse(rest.Split(' ')[0]);
            }

            if (grope == 'T')
            {
                if (grope_num == 4)
                {
                    if (word_num >= TIMER_MAX)
                        ErrKey = 3; // Количество должно быть 0-TIMER_MAX
                    else
                        CheckKey[bn] = 1;
                }
                else
                    ErrKey = 5; // Нет такой группы в T
            }
            else
                ErrKey = 5; // Нет такой группы

            bn++;

            // Проверка значений 1.0, 0.01, 0.001
            if (com_str[bn] == "1.0")
                CheckKey[bn] = 1;
            else if (com_str[bn] == "0.01")
                CheckKey[bn] = 1;
            else if (com_str[bn] == "0.001")
                CheckKey[bn] = 1;
            else
                ErrKey = 9;

            bn++;

            // Проверка Preset
            Preset = short.Parse(com_str[bn]);
            if (Preset == 0)
                ErrKey = 10;
            else
                CheckKey[bn] = 1;
            bn++;

            // Проверка Accum
            Accum = short.Parse(com_str[bn]);
            CheckKey[bn] = 1;

            return ErrKey;
        }

        /// <summary>
        /// Проверка на правильость составления комманд с тримя парраметрами
        /// </summary>
        /// <param name="bn">Порядкоый номер в тексте ранга</param>
        /// <returns>Код ошибки</returns>
        private byte CheckAddr3(byte bn)
        {
            int len_word;
            string addr_str;
            char grope = '\0', grope_num = '\0', word_num = '\0';
            byte dig_addr = 0; // 1 - цифра, 0 - адрес
            byte ErrKey = 0;

            // Первый аргумент
            addr_str = com_str[bn];

            // Обработка формата типа "N13:1"
            len_word = addr_str.IndexOf(sym_addr);
            if (len_word != 0)
            {
                if (addr_str[0] > '9') // Буква
                {
                    grope = addr_str[0];
                    grope_num = (char)int.Parse(addr_str.Substring(1, len_word - 1));
                    word_num = (char)int.Parse(addr_str.Substring(len_word + 1));
                }
                else // Цифра
                {
                    dig_addr = 1;
                    CheckKey[bn] = 1;
                    ErrKey = 0;
                }
            }

            if (dig_addr == 0 && grope == 'N')
            {
                switch ((ushort)grope_num)
                {
                    case 13:
                        if (word_num > 2) ErrKey = 3; // Должно быть 0-2
                        else CheckKey[bn] = 1;
                        break;
                    case 11:
                        if (word_num > 63) ErrKey = 3; // 0-63
                        else CheckKey[bn] = 1;
                        break;
                    case 15:
                        if (word_num > 79) ErrKey = 3; // 0-79
                        else CheckKey[bn] = 1;
                        break;
                    case 18:
                        if (word_num > 31) ErrKey = 3; // 0-31
                        else CheckKey[bn] = 1;
                        break;
                    case 26:
                        if (word_num > 127) ErrKey = 3; // 0-127
                        else CheckKey[bn] = 1;
                        break;
                    case 40:
                        if (word_num > 0) ErrKey = 3; // Должно быть 0
                        else CheckKey[bn] = 1;
                        break;
                    default:
                        ErrKey = 5; // Нет такой группы в N
                        break;
                }
            }
            else if (dig_addr == 0 && grope == 'B')
            {
                if (grope_num == 3)
                {
                    if (word_num > 31) ErrKey = 3; // 0-31
                    else CheckKey[bn] = 1;
                }
            }

            if (ErrKey != 0) return ErrKey;

            // Второй аргумент
            bn++;
            addr_str = com_str[bn];
            dig_addr = 0;

            len_word = addr_str.IndexOf(sym_addr);
            if (len_word != 0)
            {
                if (addr_str[0] > '9') // Буква
                {
                    grope = addr_str[0];
                    grope_num = (char)int.Parse(addr_str.Substring(1, len_word - 1));
                    word_num = (char)int.Parse(addr_str.Substring(len_word + 1));
                }
                else // Цифра
                {
                    dig_addr = 1;
                    CheckKey[bn] = 1;
                    ErrKey = 0;
                }
            }

            // Повторяем те же проверки для второго аргумента
            if (dig_addr == 0 && grope == 'N')
            {
                switch ((ushort)grope_num)
                {
                    case 13:
                        if (word_num > 2) ErrKey = 3; // Должно быть 0-2
                        else CheckKey[bn] = 1;
                        break;
                    case 11:
                        if (word_num > 63) ErrKey = 3; // 0-63
                        else CheckKey[bn] = 1;
                        break;
                    case 15:
                        if (word_num > 79) ErrKey = 3; // 0-79
                        else CheckKey[bn] = 1;
                        break;
                    case 18:
                        if (word_num > 31) ErrKey = 3; // 0-31
                        else CheckKey[bn] = 1;
                        break;
                    case 26:
                        if (word_num > 127) ErrKey = 3; // 0-127
                        else CheckKey[bn] = 1;
                        break;
                    case 40:
                        if (word_num > 0) ErrKey = 3; // Должно быть 0
                        else CheckKey[bn] = 1;
                        break;
                    default:
                        ErrKey = 5; // Нет такой группы в N
                        break;
                }
            }
            else if (dig_addr == 0 && grope == 'B')
            {
                if (grope_num == 3)
                {
                    if (word_num > 31) ErrKey = 3; // 0-31
                    else CheckKey[bn] = 1;
                }
            }

            // Третий аргумент
            bn++;
            addr_str = com_str[bn];
            dig_addr = 0;

            len_word = addr_str.IndexOf(sym_addr);
            if (len_word != 0)
            {
                if (addr_str[0] > '9') // Буква
                {
                    grope = addr_str[0];
                    grope_num = (char)int.Parse(addr_str.Substring(1, len_word - 1));
                    word_num = (char)int.Parse(addr_str.Substring(len_word + 1));
                }
                else // Цифра
                {
                    dig_addr = 1;
                    CheckKey[bn] = 0; // Цифра не может быть приемником
                    ErrKey = 6;
                }
            }

            if (dig_addr == 0 && grope == 'N')
            {
                switch ((ushort)grope_num)
                {
                    case 13:
                        if (word_num > 2) ErrKey = 3; // Должно быть 0-2
                        else CheckKey[bn] = 1;
                        break;
                    case 11:
                        if (word_num > 63) ErrKey = 3; // 0-63
                        else CheckKey[bn] = 1;
                        break;
                    case 15:
                        if (word_num > 79) ErrKey = 3; // 0-79
                        else CheckKey[bn] = 1;
                        break;
                    case 18:
                        if (word_num > 31) ErrKey = 3; // 0-31
                        else CheckKey[bn] = 1;
                        break;
                    case 26:
                        if (word_num > 127) ErrKey = 3; // 0-127
                        else CheckKey[bn] = 1;
                        break;
                    case 40:
                        if (word_num > 0) ErrKey = 3; // Должно быть 0
                        else CheckKey[bn] = 1;
                        break;
                    default:
                        ErrKey = 5; // Нет такой группы в N
                        break;
                }
            }
            else if (dig_addr == 0 && grope == 'B')
            {
                if (grope_num == 3)
                {
                    if (word_num > 31) ErrKey = 3; // 0-31
                    else CheckKey[bn] = 1;
                }
            }

            return ErrKey;
        }

        /// <summary>
        /// Проверка на правильость составления комманд с двумя парраметрами
        /// </summary>
        /// <param name="bn">Порядкоый номер в тексте ранга</param>
        /// <returns>Код ошибки</returns>
        private byte CheckAddr2(byte bn)
        {
            int len_word;
            string addr_str;
            char grope = '\0', grope_num = '\0', word_num = '\0';
            byte dig_addr = 0; // 1 - цифра, 0 - адрес
            byte ErrKey = 0;

            // Первый аргумент
            addr_str = com_str[bn];

            // Обработка формата типа "N13:1"
            len_word = addr_str.IndexOf(sym_addr);
            if (len_word != 0)
            {
                if (addr_str[0] > '9') // Буква
                {
                    grope = addr_str[0];
                    grope_num = (char)int.Parse(addr_str.Substring(1, len_word - 1));
                    word_num = (char)int.Parse(addr_str.Substring(len_word + 1));
                }
                else // Цифра
                {
                    dig_addr = 1;
                    CheckKey[bn] = 1;
                    ErrKey = 0;
                }
            }

            // Проверки для группы N
            if (dig_addr == 0 && grope == 'N')
            {
                switch (grope_num)
                {
                    case (char)13:
                        if (word_num > 2) ErrKey = 3; // Должно быть 0-2
                        else CheckKey[bn] = 1;
                        break;
                    case (char)11:
                        if (word_num > 63) ErrKey = 3; // 0-63
                        else CheckKey[bn] = 1;
                        break;
                    case (char)15:
                        if (word_num > 79) ErrKey = 3; // 0-79
                        else CheckKey[bn] = 1;
                        break;
                    case (char)18:
                        if (word_num > 31) ErrKey = 3; // 0-31
                        else CheckKey[bn] = 1;
                        break;
                    case (char)26:
                        if (word_num > 127) ErrKey = 3; // 0-127
                        else CheckKey[bn] = 1;
                        break;
                    case (char)40:
                        if (word_num > 0) ErrKey = 3; // Должно быть 0
                        else CheckKey[bn] = 1;
                        break;
                    default:
                        ErrKey = 5; // Нет такой группы в N
                        break;
                }
            }
            // Проверки для группы B
            else if (dig_addr == 0 && grope == 'B')
            {
                if (grope_num == (char)3)
                {
                    if (word_num > 31) ErrKey = 3; // 0-31
                    else CheckKey[bn] = 1;
                }
            }

            if (ErrKey != 0) return ErrKey;

            // Второй аргумент
            bn++;
            addr_str = com_str[bn];
            dig_addr = 0;

            len_word = addr_str.IndexOf(sym_addr);
            if (len_word != 0)
            {
                if (addr_str[0] > '9') // Буква
                {
                    grope = addr_str[0];
                    grope_num = (char)int.Parse(addr_str.Substring(1, len_word - 1));
                    word_num = (char)int.Parse(addr_str.Substring(len_word + 1));
                }
                else // Цифра
                {
                    dig_addr = 1;
                    CheckKey[bn] = 0; // Цифра не может быть приемником
                    ErrKey = 6;
                }
            }

            // Проверки для второго аргумента (группа N)
            if (dig_addr == 0 && grope == 'N')
            {
                switch (grope_num)
                {
                    case (char)13:
                        if (word_num > 2) ErrKey = 3;
                        else
                        {
                            CheckKey[bn] = 1;
                            ErrKey = 0;
                            Console.WriteLine("CheckKey {0}", CheckKey[bn]);
                        }
                        break;
                    case (char)11:
                        if (word_num > 63) ErrKey = 3;
                        else CheckKey[bn] = 1;
                        break;
                    case (char)15:
                        if (word_num > 79) ErrKey = 3;
                        else CheckKey[bn] = 1;
                        break;
                    case (char)18:
                        if (word_num > 31) ErrKey = 3;
                        else CheckKey[bn] = 1;
                        break;
                    case (char)26:
                        if (word_num > 127) ErrKey = 3;
                        else CheckKey[bn] = 1;
                        break;
                    case (char)40:
                        if (word_num > 0) ErrKey = 3;
                        else CheckKey[bn] = 1;
                        break;
                    default:
                        ErrKey = 5;
                        break;
                }
            }
            // Проверки для второго аргумента (группа B)
            else if (dig_addr == 0 && grope == 'B')
            {
                if (grope_num == (char)3)
                {
                    if (word_num > 31) ErrKey = 3;
                    else CheckKey[bn] = 1;
                }
            }

            return ErrKey;
        }

        /// <summary>
        /// Проверка нового текста ранга на правильность составления
        /// </summary>
        /// <param name="RangStr">Текст ранга</param>
        /// <returns>Код ошибки</returns>
        public sbyte CheckRangText(string RangStr)
        {
            int word_count = 0;
            byte ErrKey = 0;

            if (RangStr[0] != '%' && checking == CheckingType.Emulation) return 11;

            // Извлечение номера ранга
            if (checking == CheckingType.Emulation)
            {
                RangStr = RangStr.Substring(1);
                int spaceIndex = RangStr.IndexOf(' ');
                RangStr = RangStr.Substring(spaceIndex + 1);
            }

            // Разбивка на слова
            string[] words = RangStr.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            word_count = Math.Min(words.Length, MAX_WORDS_NUM);
            com_str = new string[word_count];
            Array.Copy(words, com_str, word_count);

            Array.Clear(CheckKey, 0, CheckKey.Length);

            // Проверка команд
            for (byte bn = 0; bn < word_count; bn++)
            {
                switch (com_str[bn])
                {
                    case "XIC":
                    case "XIO":
                    case "OTE":
                    case "OTL":
                    case "OTU":
                        CheckKey[bn] = BIT_KEY;
                        break;

                    case "NEQ":
                    case "EQU":
                    case "LES":
                    case "GRT":
                    case "LEQ":
                    case "GEQ":
                    case "MOV":
                        CheckKey[bn] = ADD2_KEY;
                        break;

                    case "ADD":
                    case "SUB":
                        CheckKey[bn] = ADD3_KEY;
                        break;

                    case "TON":
                        CheckKey[bn] = TON_KEY;
                        break;

                    case "MSG":
                        CheckKey[bn] = MSG_KEY;
                        break;

                    case "ONS":
                        if (CheckKey[bn] == 0)
                            CheckKey[bn] = ONS_KEY | BIT_KEY;
                        CheckKey[bn]++;
                        break;

                    case "BST":
                        if (CheckKey[bn] == 0)
                            CheckKey[bn] = BST_KEY;
                        CheckKey[bn]++;
                        break;

                    case "NXB":
                        CheckKey[bn] = 1;
                        break;

                    case "BND":
                        if (CheckKey[bn] == 0)
                            CheckKey[bn] = BND_KEY;
                        CheckKey[bn]++;
                        break;
                }
            }

            // Проверка ветвей
            byte BST_KEY_Count = 0, BND_KEY_Count = 0, NXB_KEY_Count = 0;
            for (byte bn = 0; bn < word_count; bn++)
            {
                if ((CheckKey[bn] & ONS_KEY) == ONS_KEY)
                    if ((CheckKey[bn] & 0xF) > 1) ErrKey = 2;
            }

            foreach (string item in RangStr.Trim().Split(" "))
            {
                if (item == "BST") BST_KEY_Count++;
                if (item == "BND") BND_KEY_Count++;
                if (item == "NXB") NXB_KEY_Count++;
            }

            if (BST_KEY_Count != BND_KEY_Count) ErrKey = 1;
            if (BST_KEY_Count > 0 && NXB_KEY_Count == 0) ErrKey = 1;

            // Проверка адресов
            for (byte bn = 0; bn < word_count; bn++)
            {
                if ((CheckKey[bn] & BIT_KEY) == BIT_KEY)
                {
                    bn++;
                    string addr_str = com_str[bn];
                    char grope = addr_str[0];

                    int colonPos = addr_str.IndexOf(':');
                    if (colonPos == -1)
                    {
                        ErrKey = 5;
                        continue;
                    }

                    char grope_num = (char)int.Parse(addr_str.Substring(1, colonPos - 1));
                    byte word_num = byte.Parse(addr_str.Split("/")[0].Substring(colonPos + 1));

                    if (grope == 'N')
                    {
                        switch (grope_num)
                        {
                            case (char)13:
                                if (word_num > 2) ErrKey = 3;
                                break;
                            case (char)11:
                                if (word_num > 63) ErrKey = 3;
                                break;
                            case (char)15:
                                if (word_num > 79) ErrKey = 3;
                                break;
                            case (char)18:
                                if (word_num > 31) ErrKey = 3;
                                break;
                            case (char)26:
                                if (word_num > 127) ErrKey = 3;
                                break;
                            case (char)40:
                                if (word_num > 0) ErrKey = 3;
                                break;
                            default:
                                ErrKey = 5;
                                break;
                        }

                        if (ErrKey == 0)
                        {
                            int slashPos = addr_str.IndexOf('/');
                            if (slashPos != -1)
                            {
                                byte bit_num = byte.Parse(addr_str.Substring(slashPos + 1));
                                if (bit_num > 15) ErrKey = 4;
                            }
                        }
                    }
                    else if (grope == 'B')
                    {
                        if (grope_num == (char)3)
                        {
                            if (word_num > 31) ErrKey = 3;

                            if (ErrKey == 0)
                            {
                                int slashPos = addr_str.IndexOf('/');
                                if (slashPos != -1)
                                {
                                    byte bit_num = byte.Parse(addr_str.Substring(slashPos + 1));
                                    if (bit_num > 15) ErrKey = 4;
                                }
                            }
                        }
                        else
                        {
                            ErrKey = 5;
                        }
                    }
                    else if (grope == 'T')
                    {
                        if (grope_num == 4)
                        {
                            if (word_num >= TIMER_MAX) ErrKey = 3;

                            if (ErrKey == 0)
                            {
                                string suffix = addr_str.Substring(addr_str.IndexOf('/'));
                                if (suffix != "/DN" && suffix != "/EN" && suffix != "/TT")
                                    ErrKey = 4;
                            }
                        }
                        else
                        {
                            ErrKey = 5;
                        }
                    }
                    else
                    {
                        ErrKey = 5;
                    }

                    if (ErrKey == 0) CheckKey[bn] = 1;
                }
                else if ((CheckKey[bn] & ADD2_KEY) == ADD2_KEY)
                {
                    bn++;
                    ErrKey = CheckAddr2(bn);
                    bn++;
                }
                else if ((CheckKey[bn] & ADD3_KEY) == ADD3_KEY)
                {
                    bn++;
                    ErrKey = CheckAddr3(bn);
                    bn++;
                    bn++;
                }
                else if ((CheckKey[bn] & TON_KEY) == TON_KEY)
                {
                    bn++;
                    ErrKey = CheckTON(bn);
                    bn++;
                    bn++;
                    bn++;
                }
                else if ((CheckKey[bn] & MSG_KEY) == MSG_KEY)
                {
                    bn++;
                    ErrKey = CheckMSG(bn);
                    bn += 15;
                }
            }

            // Финальная проверка
            bool hasUnchecked = false;
            for (byte bn = 0; bn < word_count; bn++)
            {
                if (CheckKey[bn] == 0)
                {
                    hasUnchecked = true;
                    break;
                }
            }

            if (hasUnchecked && ErrKey == 0) return 8;
            return (sbyte)ErrKey;
        }
    }
}
