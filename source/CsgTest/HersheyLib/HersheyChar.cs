using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
//using System.Drawing.Drawing2D;

using RMC;

namespace HersheyLib
{
    public class HersheyChar
    {
        public List<VectorPath> Paths;

        public Point MinExtent;
        public Point MaxExtent;

        public HersheyChar()
        {
            Paths = new List<VectorPath>();
            MinExtent = new Point(0,0);
            MaxExtent = new Point(0,0);
        }

        public int Width
        {
            get { 
                //CalculateExtent(); 
                return MaxExtent.X - MinExtent.X; 
            }
        }

        public void CalculateExtent()
        {
            foreach (VectorPath path in Paths)
            {
                foreach (PointF p in path.Points)
                {
                    if (p.X < MinExtent.X)
                        MinExtent.X = (int)p.X;
                    if (p.Y < MinExtent.Y)
                        MinExtent.Y = (int)p.Y;

                    if (p.X > MaxExtent.X)
                        MaxExtent.X = (int)p.X;
                    if (p.Y > MaxExtent.Y)
                        MaxExtent.Y = (int)p.Y;
                }
            }

        }

        public void ParseCsvCharacterData(string CharData)
        {
            int NumPoints;
            string[] tokens;
            VectorPath path = new VectorPath();

            tokens = StringUtils.SplitDsvText(CharData, ",");

            NumPoints = Convert.ToInt32(tokens[0]);
            int index;

            MinExtent.X = Convert.ToInt32(tokens[1]);
            MaxExtent.X = Convert.ToInt32(tokens[2]);

            Point p;
            Point lastP = new Point(0, 0);
            bool first = true;

            for (index = 3; index < NumPoints; index += 2)
            {
                int x = Convert.ToInt32(tokens[index]);
                int y = Convert.ToInt32(tokens[index + 1]);

                y = -y;  // to read old files
                p = new Point(x, y);

                if (x == -64)
                {
                    Paths.Add (path);
                    path = new VectorPath();

                    first = true;
                }
                else
                {
                    if (!first)
                    {
                    //    path.AddLine(lastP, p);

                    //    Paths.Add (path);
                    //    path = new VectorPath();
                    }

                    path.Points.Add(p);

                    first = false;
                    lastP = p;
                }
            }

        }

        public void SaveAsCs(List<string> lines, int CharNum)
        {
            string line;

            lines.Add(string.Format ("      {0}, {1}, {2}, {3},", CharNum, Paths.Count, MinExtent.X, MaxExtent.X));

            foreach (VectorPath path in Paths)
            {
                line = path.Points.Count.ToString() + ", ";

                foreach (PointF p in path.Points)
                {
                    line += p.X + ", " + p.Y + ", ";
                }

                lines.Add("        "+line);
            }

        }

        public override string ToString()
        {
            string line;
            int count=0;

            line = String.Format ("{0},{1}", 
                //(Paths.Count*3+1)*2,
                MinExtent.X, MaxExtent.X);
            count = 2;
            for (int pathNum = 0; pathNum < Paths.Count; pathNum++)
            {
                foreach (PointF p in Paths[pathNum].Points)
                {
                    line = line + string.Format (",{0},{1}", p.X, p.Y);
                    count += 2;
                }

                line = line + string.Format (",{0},{1}", -64,0);
                count += 2;
            }
            line = count.ToString() + "," + line;

            return line;
        }
    }
    
}
