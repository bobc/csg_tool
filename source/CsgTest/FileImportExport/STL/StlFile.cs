using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

using System.Drawing;

using RMC;
using Lexing;

using CadCommon;

namespace FileImportExport.STL
{
    public class StlFile : FileBase
    {
        public string Name;

        //public List<Facet> Facets;
        public MaterialProperties Material;

        public MeshIndexed Mesh;

        public StlFile()
        {
            //Facets = new List<Facet>();
            Mesh = new MeshIndexed();
            Material = new MaterialProperties();
        }

        //public IndexedFaceSet GetIndexedFaceSet()
        //{
        //    return new IndexedFaceSet(Facets);
        //}

        public override string GetCsv()
        {
            //
            return FileName + "," + Material.Name + "," + Units.Units + "," + Units.Scale.ToString("g6");
        }

        public bool SaveToXmlFile(string FileName)
        {
            bool result = false;
            XmlSerializer serializer = new XmlSerializer(typeof(StlFile));
            TextWriter Writer = null;

            FileUtils.CreateDirectory(FileName);
            try
            {
                Writer = new StreamWriter(FileName, false, Encoding.UTF8);

                serializer.Serialize(Writer, this);
                result = true;
            }
            finally
            {
                if (Writer != null)
                {
                    Writer.Close();
                }
            }
            return result;
        }

        public static StlFile LoadFromXmlFile(string FileName)
        {
            StlFile result = null;
            XmlSerializer serializer = new XmlSerializer(typeof(StlFile));
            FileStream fs = new FileStream(FileName, FileMode.Open);

            try
            {
                result = (StlFile)serializer.Deserialize(fs);
            }
            finally
            {
                if (fs != null)
                {
                    fs.Close();
                }
            }

            return result;
        }

        public override bool LoadFromFile(string FileName)
        {
            bool result = true;
            //StlFile result = new StlFile();
            GeneralLexer Parser = new GeneralLexer();

            Mesh.Attributes.Color = Color.Silver;

            try
            {
                if (Parser.Initialise(FileName))
                {
                    if (Parser.CurToken.Value == "solid")
                    {
                        Parser.GetNextToken();
                        this.Name = Parser.CurToken.Value;

                        Parser.GetNextToken();

                        while (Parser.CurToken.Value == "facet")
                        {
                            // skip normal

                            Parser.SkipTo("outer");
                            Parser.GetNextToken(); // outer
                            Parser.GetNextToken(); // loop

                            Facet facet;
                            Point3DF[] points = new Point3DF[3];
                            int index = 0;

                            while (Parser.CurToken.Value == "vertex")
                            {
                                Parser.GetNextToken();
                                points[index] = new Point3DF();
                                points[index].X = (float)Parser.CurToken.RealValue;
                                Parser.GetNextToken();
                                points[index].Y = (float)Parser.CurToken.RealValue;
                                Parser.GetNextToken();
                                points[index].Z = (float)Parser.CurToken.RealValue;
                                Parser.GetNextToken();

                                index++;
                            }

                            facet = new Facet(points);

                            //this.Facets.Add(facet);
                            this.Mesh.AddFacet(facet, Mesh.Attributes.Color);

                            if (Parser.CurToken.Value == "endloop")
                                Parser.GetNextToken();

                            if (Parser.CurToken.Value == "endfacet")
                                Parser.GetNextToken();
                        }
                    }
                    else
                    {
                        result = false;
                        LastError = "Expected <solid> at " + Parser.CurToken.Location.ToString();
                    }
                }
                else
                {
                    LastError = Parser.LastError;
                    result = false;
                }
            }
            finally
            {
                Parser.Finish();
            }

            return result;
        }
    }

}
