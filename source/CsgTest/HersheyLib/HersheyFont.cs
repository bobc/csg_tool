using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using System.Drawing;
using System.Drawing.Drawing2D;

using RMC;

namespace HersheyLib
{
    public class HersheyFont
    {
        public HersheyChar[] CharData;

        public int[] Translation;
        public bool NewStyle = false;
        public List<string> fileData;
            
        /// <summary>
        /// 
        /// </summary>
        public HersheyFont()
        {
            Translation = new int[256];
            CharData = new HersheyChar[256];
        }

        public HersheyFont LoadFromCsv(string Filename)
        {
            HersheyFont result = new HersheyFont();

            fileData = ReadFileStrings(Filename);

            for (int index = 0; index < 256; index++)
            {
                CharData[index] = new HersheyChar();

                if (NewStyle)
                {
                    CharData[index].ParseCsvCharacterData(fileData[index]);
                }
                else if (index < 128)
                {
                    int HersheyCharNum = Translation[index];

                    if (HersheyCharNum != -1)
                        CharData[index].ParseCsvCharacterData(fileData[HersheyCharNum]);
                }
            }

            return result;
        }

        public HersheyFont SaveToCsv(string Filename)
        {
            HersheyFont result = new HersheyFont();

            List<string> data = new List<string>() ;

            for (int index = 0; index < 256; index++)
            {
                string line = CharData[index].ToString();
                data.Add(line);
            }
            WriteFileStrings(Filename, data);

            return result;
        }

        public HersheyFont LoadTranslationCsv(string Filename, int FontNumber)
        {
            HersheyFont result = new HersheyFont();

            fileData = ReadFileStrings(Filename);

            bool first = true;
            foreach (string line in fileData)
            {
                if (!first)
                {
                    int AsciiCharNum = Convert.ToInt32(StringUtils.GetDsvField(line, ",", 0));
                    int HersheyCharNum = Convert.ToInt32(StringUtils.GetDsvField(line, ",", 2+FontNumber));
                    Translation[AsciiCharNum] = HersheyCharNum;
                }

                first = false;
            }

            return result;
        }


        public void SaveToCs (string Filename)
        {
            List<string> lines = new List<string>();

            lines.Add("namespace HersheyData");
            lines.Add("// This file is auto-generated");
            lines.Add("{");
            lines.Add("  public class MyFont");
            lines.Add("  {");
            lines.Add("    public static int [] Data = new int [] {");
            for (int index = 0; index < 256; index++)
            {
                if (CharData[index].Paths.Count != 0)
                    CharData[index].SaveAsCs(lines, index);
            }
            lines.Add("      -1, 0");
            lines.Add("    };");
            lines.Add("  }");
            lines.Add("}");

            WriteFileStrings(Filename, lines);
        }

        public void CreateFont(int[] Data)
        {
            int index = 0;
            while (index < Data.Length)
            {
                HersheyChar ch = new HersheyChar();

                int CharNum = Data[index++];
                int NumPaths = Data[index++];

                if (CharNum < 0)
                    return;

                ch.MinExtent.X = Data[index++];
                ch.MaxExtent.X = Data[index++];
                
                for (int pathnum = 0; pathnum < NumPaths; pathnum++)
                {
                    int NumPoints = Data[index++];
                    VectorPath path = new VectorPath();
                    for (int PointNum = 0; PointNum < NumPoints; PointNum++)
                    {
                        PointF p = new PointF();
                        p.X = Data[index++];
                        p.Y = Data[index++];
                        path.Points.Add(p);
                    }
                    ch.Paths.Add(path);
                }

                CharData[CharNum] = ch;
            }
        }

        public static List<string> ReadFileStrings(string Filename)
        {
            // create reader & open file
            TextReader tr = new StreamReader(Filename);
            List<string> result = new List<string>();

            string line = tr.ReadLine();

            while (line != null)
            {
                result.Add(line);
                line = tr.ReadLine();
            }

            // close the stream
            tr.Close();

            return result;
        }

        public static bool WriteFileStrings(string Filename, List<string> Data)
        {
            // create reader & open file
            TextWriter tr = new StreamWriter(Filename);

            foreach (string line in Data)
            {
                tr.WriteLine(line);
            }

            // close the stream
            tr.Close();

            return true;
        }

    }
}
