using Microsoft.VisualBasic.FileIO;
using System.Text;
using System.Text.RegularExpressions;

namespace ModbasServer
{
    public enum Type
    {
        RANG,
        TEGS,
        DATA
    }

    public static class CreateFile
    {
        private static readonly string[] _type = new string[] { "[RANGS]", "[TEGS]", "[DATA]" };

        /// <summary>
        /// Чтение файла
        /// </summary>
        /// <param name="path"></param>
        public static string[] Load(string path, Type type_ret)
        {
            if (!path.Contains(".ldf")) return null;
            string[] readFile = File.ReadAllLines(path);

            for (int i = 0; i < readFile.Length; i++)
            {
                if (readFile[i].Contains(_type[(int)type_ret])) return GetDataOfFile(readFile, i);
            }

            return null;
        }

        /// <summary>
        /// Выделение необходимых строк из файла
        /// </summary>
        /// <param name="_load">Массив считанных строк</param>
        /// <param name="_index">Индекст старта чтения</param>
        /// <returns>Массив нужных строк</returns>
        private static string[] GetDataOfFile(string[] _load, int _index)
        {
            int index = _index + 1;
            int count = int.Parse(new Regex(@"\A\[\w*\]:").Replace(_load[_index], ""));
            if (!Convert.ToBoolean(count)) return null;
            string[] _out = new string[count];
            for (int i = 0; i < count; i++)
            {
                if (new Regex(@"\A\[/\w*\]").IsMatch(_load[index])) return _out;
                _out[i] = new Regex(@"\ASOR|EOR\z").Replace(_load[index], "");
                index++;
            }
            return _out;
        }

        /// <summary>
        /// Создание файла
        /// </summary>
        /// <param name="_Rangs">Текст рангов</param>
        /// <param name="_Data">Директория: название - массив данных</param>
        /// <returns>Строка, с готовым форматом для записи</returns>
        public static string Create(string[] _Rangs, Dictionary<string, ushort[]> _Data)
        {
            string[] Data = CreateDATA(_Data);
            string Out = $"[RANGS]:{_Rangs.Length}\nSOR";
            Out += string.Join("EOR\nSOR", _Rangs) + "EOR";
            Out += $"\n[/RANGS]\n[TEGS]:0\n[/TEGS]\n[DATA]:{Data.Length}\n";
            Out += string.Join("\n", Data);
            return Out + "\n[/DATA]";
        }

        /// <summary>
        /// Создание файла
        /// </summary>
        /// <param name="_Rangs">Текст рангов</param>
        /// <param name="_Data">Сфомированные данные</param>
        /// <returns>Строка, с готовым форматом для записи</returns>
        public static string Create(string[] _Rangs, string[] _Data)
        {
            string Out = $"[RANGS]:{_Rangs.Length}\nSOR";
            Out += string.Join("EOR\nSOR", _Rangs) + "EOR";
            Out += $"\n[/RANGS]\n[TEGS]:0\n[/TEGS]\n[DATA]:{_Data.Length}\n";
            Out += string.Join("\n", _Data);
            return Out + "\n[/DATA]";
        }

        /// <summary>
        /// Создание файла
        /// </summary>
        /// <param name="_Rangs">Текст рангов</param>
        /// <param name="_Data">Сфомированные данные</param>
        /// <param name="_Tegs">Сформированные теги</param>
        /// <returns>Строка, с готовым форматом для записи</returns>
        public static string Create(string[] _Rangs, string[] _Data, string[] _Tegs)
        {
            string Out = $"[RANGS]:{_Rangs.Length}\nSOR";
            Out += string.Join("EOR\nSOR", _Rangs) + "EOR";
            Out += $"\n[/RANGS]\n[TEGS]:{_Tegs.Length}\n";
            Out += string.Join("\n", _Tegs);
            Out += $"\n[/TEGS]\n[DATA]:{_Data.Length}\n";
            Out += string.Join("\n", _Data);
            return Out + "\n[/DATA]";
        }

        /// <summary>
        /// Создание файла
        /// </summary>
        /// <param name="_Rangs">Текст рангов</param>
        /// <param name="_Data">Директория: название - массив данных</param>
        /// <param name="_Tegs">Сформированные теги</param>
        /// <returns>Строка, с готовым форматом для записи</returns>
        public static string Create(string[] _Rangs, Dictionary<string, ushort[]> _Data, string[] _Tegs)
        {
            string[] Data = CreateDATA(_Data);
            string Out = $"[RANGS]:{_Rangs.Length}\nSOR";
            Out += string.Join("EOR\nSOR", _Rangs) + "EOR";
            Out += $"\n[/RANGS]\n[TEGS]:{_Tegs.Length}\n";
            Out += string.Join("\n", _Tegs);
            Out += $"\n[/TEGS]\n[DATA]:{Data.Length}\n";
            Out += string.Join("\n", Data);
            return Out + "\n[/DATA]";
        }

        /// <summary>
        /// Создание файла
        /// </summary>
        /// <param name="_Rangs">Текст рангов</param>
        /// <param name="_Data">Директория: название - массив данных</param>
        /// <param name="_Tegs">Директория, содержащаяя нужную информацию</param>
        /// <returns>Строка, с готовым форматом для записи</returns>
        public static string Create(string[] _Rangs, Dictionary<string, ushort[]> _Data, Dictionary<string, string[]> _Tegs)
        {
            string[] Data = CreateDATA(_Data);
            string[] Tegs = CreateTEGS(_Tegs);
            string Out = $"[RANGS]:{_Rangs.Length}\nSOR";
            Out += string.Join("EOR\nSOR", _Rangs) + "EOR";
            Out += $"\n[/RANGS]\n[TEGS]:{Tegs.Length}\n";
            Out += string.Join("\n", Tegs);
            Out += $"\n[/TEGS]\n[DATA]:{Data.Length}\n";
            Out += string.Join("\n", Data);
            return Out + "\n[/DATA]";
        }

        /// <summary>
        /// Конвертация директории в необходимый строковый формат
        /// </summary>
        /// <param name="_Data">Директория, содержащаяя нужную информацию</param>
        /// <returns>Строки, имеющий необходимы формат для записи</returns>
        public static string[] CreateDATA(Dictionary<string, ushort[]> _Data)
        {
            string[] Out = new string[_Data.Count];
            string[] keys = _Data.Keys.ToArray();

            for (int i = 0; i < keys.Length; i++) Out[i] = $"<{keys[i]}>[{_Data[keys[i]].Length}]:" + '{' + string.Join(",", _Data[keys[i]]) + '}';
            return Out;
        }

        /// <summary>
        /// Конвертация директории в необходимый строковый формат
        /// </summary>
        /// <param name="_Keys">Именна адресов</param>
        /// <param name="_value">Множество массивов с данныйми</param>
        /// <returns>Строки, имеющий необходимы формат для записи</returns>
        public static string[] CreateDATA(string[] _Keys, ushort[][] _value)
        {
            string[] Out = new string[_Keys.Length];
            string[] keys = _Keys;

            for (int i = 0; i < keys.Length; i++) Out[i] = $"<{keys[i]}>[{_value[i].Length}]:" + '{' + string.Join(",", keys[i]) + '}';
            return Out;
        }

        /// <summary>
        /// Создание файла без тегов
        /// </summary>
        /// <param name="_Rangs">Текст рангов</param>
        /// <param name="_Data">Сфомированные данные</param>
        /// <returns>Строка, с готовым форматом для записи</returns>
        public static string CreateNoTEGS(string[] _Rangs, string[] _Data)
        {
            string Out = $"[RANGS]:{_Rangs.Length}\nSOR";
            Out += string.Join("EOR\nSOR", _Rangs) + "EOR";
            Out += $"\n[/RANGS]\n[DATA]:{_Data.Length}\n";
            Out += string.Join("\n", _Data);
            return Out + "\n[/DATA]";
        }

        /// <summary>
        /// Конвертация CSV файла в строковый массив
        /// </summary>
        /// <param name="_path">Путь к CSV файлу</param>
        /// <returns>Массив строк, определенного формата</returns>
        public static string[] CreateTEGS(string _path)
        {
            List<string> Out = new List<string>();
            using (TextFieldParser tfp = new TextFieldParser(_path, Encoding.UTF8))
            {
                tfp.TextFieldType = FieldType.Delimited;
                tfp.SetDelimiters(",");
                while (!tfp.EndOfData)
                {
                    string[] str = tfp.ReadFields();
                    Out.Add('{' + str[0] + ',' + str[2] + $",[{str[3]}]" + '}');
                }
            }
            return Out.ToArray();
        }

        /// <summary>
        /// Конвертиция строкового массива в форматированный массив
        /// </summary>
        /// <param name="_data">Массив для конвертации</param>
        /// <returns>Массив строк, определенного формата</returns>
        public static string[] CreateTEGS(string[] _data)
        {
            List<string> Out = new List<string>();
            string[] buf;
            foreach (string str in _data)
            {
                buf = str.Split(',');
                Out.Add('{' + buf[0] + ',' + buf[2] + $",[{buf[3]}]" + '}');
            }

            return Out.ToArray();
        }

        /// <summary>
        /// Формирует дирекорию для записи в файл
        /// </summary>
        /// <param name="_Tegs">Директория, содержащаяя нужную информацию</param>
        /// <returns>сформированый массив данных</returns>
        public static string[] CreateTEGS(Dictionary<string, string[]> _Tegs)
        {
            string[] _out = new string[_Tegs.Count];
            string[] _keys = _Tegs.Keys.ToArray();
            for (int ind = 0; ind < _keys.Length; ind++)
            {
                _out[ind] = "{" + string.Format("{0},{1},[{2}]", _keys[ind], _Tegs[_keys[ind]][0], _Tegs[_keys[ind]][1]) + "}";
            }
            return _out;
        }

        /// <summary>
        /// Конвертация CSV файла в строковый массив
        /// </summary>
        /// <param name="_path">Путь к CSV файлу</param>
        /// <param name="_sort">Список критериев для сортировки</param>
        /// <returns>Массив строк, определенного формата</returns>
        public static string[] CreateTEGS(string _path, string[] _sort)
        {
            List<string> Out = new List<string>();
            using (TextFieldParser tfp = new TextFieldParser(_path, Encoding.UTF8))
            {
                tfp.TextFieldType = FieldType.Delimited;
                tfp.SetDelimiters(",");
                while (!tfp.EndOfData)
                {
                    string[] str = tfp.ReadFields();
                    if (_sort.Contains(str[0].Split(':')[0]))
                        Out.Add('{' + str[0] + ',' + str[2] + $",[{str[3]}]" + '}');
                }
            }
            return Out.ToArray();
        }

        /// <summary>
        /// Конвертиция строкового массива в форматированный массив
        /// </summary>
        /// <param name="_data">Массив для конвертации</param>
        /// <param name="_sort">Список критериев для сортировки</param>
        /// <returns>Массив строк, определенного формата</returns>
        public static string[] CreateTEGS(string[] _data, string[] _sort)
        {
            List<string> Out = new List<string>();
            string[] buf;
            foreach (string str in _data)
            {
                buf = str.Split(',');
                if (_sort.Contains(buf[0].Split(':')[0]))
                    Out.Add('{' + buf[0] + ',' + buf[2] + $",[{buf[3]}]" + '}');
            }

            return Out.ToArray();
        }

        /// <summary>
        /// Выпуск или перезапись итогового файла
        /// </summary>
        /// <param name="_path">Место размещение файла</param>
        /// <param name="_data">Данные для вывода</param>
        public static void ToFile(string _path, string _data)
        {
            using (FileStream fs = new FileStream(_path, FileMode.Create))
            {
                byte[] b = Encoding.UTF8.GetBytes(_data);
                fs.Write(b, 0, b.Length);
            }
            Console.WriteLine("Saved");
        }

        /// <summary>
        /// Выпуск или перезапись итогового файла
        /// </summary>
        /// <param name="_path">Место размещение файла</param>
        /// <param name="_Rangs">Массив, состоящий из строк ранга</param>
        /// <param name="_Data">Массив, состоящий из форматированных строк данных</param>
        public static void ToFile(string _path, string[] _Rangs, string[] _Data)
        {
            string _data = Create(_Rangs, _Data);
            using (FileStream fs = new FileStream(_path, FileMode.Create))
            {
                byte[] b = Encoding.UTF8.GetBytes(_data);
                fs.Write(b, 0, b.Length);
            }
            Console.WriteLine("Saved");
        }

        /// <summary>
        /// Выпуск или перезапись итогового файла
        /// </summary>
        /// <param name="_path">Место размещение файла</param>
        /// <param name="_Rangs">Массив, состоящий из строк ранга</param>
        /// <param name="_Data">Массив, состоящий из форматированных строк данных</param>
        /// <param name="_Tegs">Массив, состоящий из форматированных строк тегов</param>
        public static void ToFile(string _path, string[] _Rangs, string[] _Data, string[] _Tegs)
        {
            string _data = Create(_Rangs, _Data, _Tegs);
            using (FileStream fs = new FileStream(_path, FileMode.Create))
            {
                byte[] b = Encoding.UTF8.GetBytes(_data);
                fs.Write(b, 0, b.Length);
            }
            Console.WriteLine("Saved");
        }

        /// <summary>
        /// Полуение значений и и имен адресов
        /// </summary>
        /// <param name="path">Строковый массив данный, полученные из файла</param>
        /// <returns>Дирекктория значений [string Name]->[ushort[]]</returns>
        public static Dictionary<string, ushort[]> GetData(string[] _data)
        {
            string[] _buf;
            string name;
            ushort[] value;
            Dictionary<string, ushort[]> _out = new Dictionary<string, ushort[]>();
            foreach (string str in _data)
            {
                _buf = str.Split(':');
                name = new Regex(@">\[\d*\]").Replace(_buf[0], "").Replace("<", "");
                value = _buf[1].Replace("{", "").Replace("}", "").Split(',').Select(x => ushort.Parse(x)).ToArray();
                _out.Add(name, value);
            }
            return _out;
        }

        /// <summary>
        /// Полуение значений и имен адресов
        /// </summary>
        /// <param name="path">путь к файлу</param>
        /// <returns>Дирекктория значений [string Name]->[ushort[]]</returns>
        public static Dictionary<string, ushort[]> GetData(string path)
        {
            string[] _data = Load(path, Type.DATA);
            string[] _buf;
            string name;
            ushort[] value;
            Dictionary<string, ushort[]> _out = new Dictionary<string, ushort[]>();
            foreach (string str in _data)
            {
                _buf = str.Split(':');
                name = new Regex(@">\[\d*\]").Replace(_buf[0], "").Replace("<", "");
                value = _buf[1].Replace("{", "").Replace("}", "").Split(',').Select(x => ushort.Parse(x)).ToArray();
                _out.Add(name, value);
            }
            return _out;
        }

        /// <summary>
        /// Получение значений адреса и значений
        /// </summary>
        /// <param name="path">Путь к файлу</param>
        /// <returns>Директория значений [string Adres] ->[string[2]{ Teg, Coment }]</returns>
        public static Dictionary<string, string[]> GetTegs(string path)
        {
            string[] _data = Load(path, Type.TEGS);
            string[] _buf;
            string name;
            string[] value;
            Dictionary<string, string[]> _out = new Dictionary<string, string[]>();
            foreach (string str in _data)
            {
                _buf = str.Replace("{", "").Replace("}", "").Split(',');
                name = _buf[0];
                value = new string[] { _buf[1], _buf[2].Replace("[", "").Replace("]", "") };
                _out.Add(name, value);
            }
            return _out;
        }

        /// <summary>
        /// Получение значений адреса и значений 
        /// </summary>
        /// <param name="_data">Счианный массив строк</param>
        /// <returns>Директория значений [string Adres] ->[string[2]{ Teg, Coment }]</returns>
        public static Dictionary<string, string[]> GetTegs(string[] _data)
        {
            string[] _buf;
            string name;
            string[] value;
            Dictionary<string, string[]> _out = new Dictionary<string, string[]>();
            foreach (string str in _data)
            {
                _buf = str.Replace("{", "").Replace("}", "").Split(',');
                name = _buf[0];
                value = new string[] { _buf[1], _buf[2].Replace("[", "").Replace("]", "") };
                _out.Add(name, value);
            }
            return _out;
        }
    }
}
