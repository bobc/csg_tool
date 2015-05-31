using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RMC
{
    public class FileUtils
    {
        public static void CreateDirectory(string Filename)
        {
            string path = Path.GetDirectoryName(Filename);

            if ((path != "") && !Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        public static void SaveListToFile(List<string> list, string filename)
        {
            string[] array = new string[list.Count];

            int index = 0;
            foreach (string s in list)
                array[index++] = s;

            File.WriteAllLines(filename, array);
        }

        public static void WriteFile(string Filename, List<string> Data)
        {
            StreamWriter sw;

            if (File.Exists(Filename))
            {
                sw = File.CreateText(Filename);
            }
            else
            {
                sw = File.CreateText(Filename);
            }

            foreach (string line in Data)
            {
                sw.WriteLine(line);
            }
            sw.Close();
        }

    }
}
