using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

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
        /// <summary>
        /// Чтение файла
        /// </summary>
        /// <param name="path"></param>
        public static string[] Load(string path, Type type_ret)
        {
            if (!path.Contains(".ldf")) return null;
            string[] readFile = File.ReadAllLines(path);

            int index = 1;

            string[] rangs; // Считыване рангов
            int count = int.Parse(readFile[0].Replace("[RANGS]:", ""));
            rangs = new string[count];
            for (int i = 0; i < count; i++)
            {
                rangs[i] = readFile[index];
                index++;
            }

            //Console.WriteLine(string.Join("\n", rangs));
            //Console.WriteLine();
            string[] tegs = null;
            try
            {
                count = int.Parse(readFile[index].Replace("[TEGS]:", "")); // Считывание тегов
                if (count != 0)
                {
                    tegs = new string[count];
                    index++;
                    for (int i = 0; i < count; i++)
                    {
                        tegs[i] = readFile[index];
                        index++;
                    }
                    // пока ничего
                    // index += count+1;
                    //Console.WriteLine(string.Join("\n", tegs));
                    //Console.WriteLine();
                }
                else index++;
            }
            catch{}

            count = int.Parse(readFile[index].Replace("[DATA]:", "")); // Считывание данных
            index++;
            string[] data = new string[count];
            for (int i = 0; i < count; i++)
            {
                data[i] = readFile[index];
                index++;
            }
            //Console.WriteLine(string.Join("\n", data));
            switch (type_ret)
            {
                case Type.RANG: return rangs;
                case Type.TEGS: return tegs;
                case Type.DATA: return data;
                    default: return null;
            }
        }

        /// <summary>
        /// Создание файла
        /// </summary>
        /// <param name="_Rangs">Текст рангов</param>
        /// <param name="_Data">Сфомированные данные</param>
        /// <returns>Строка, с готовым форматом для записи</returns>
        public static string Create(string[] _Rangs, string[] _Data)
        {
            string Out = $"[RANGS]:{_Rangs.Length}\n";
            Out += string.Join("\n", _Rangs);
            Out += $"\n[TEGS]:0\n[DATA]:{_Data.Length}\n";
            Out += string.Join("\n", _Data);
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
            string Out = $"[RANGS]:{_Rangs.Length}\n";
            Out += string.Join("\n", _Rangs);
            Out += $"\n[DATA]:{_Data.Length}\n";
            Out += string.Join("\n", _Data);
            return Out;
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
            string Out = $"[RANGS]:{_Rangs.Length}\n";
            Out += string.Join("\n", _Rangs);
            Out += $"\n[TEGS]:{_Tegs.Length}\n";
            Out += string.Join("\n", _Tegs);
            Out += $"\n[DATA]:{_Data.Length}\n";
            Out += string.Join("\n", _Data);
            return Out;
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
    }
}
