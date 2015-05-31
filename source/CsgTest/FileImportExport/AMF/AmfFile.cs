using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

using System.Drawing;

using RMC;

using CadCommon;

// AMF is an XML format
// read file XML
// write file XML

namespace FileImportExport.AMF
{
    [XmlRootAttribute("amf")]
    public class AmfFile : FileBase
    {
        [XmlAttribute("unit")]
        public string unit;

        [XmlElement("metadata")]
        public List<AmfMetadata> Metadata;

        [XmlElement("object")]
        public List<AmfObject> Objects;

        [XmlElement("material")]
        public List<AmfMaterial> Materials;

        public AmfFile() 
        {
            Metadata = new List<AmfMetadata>();
            Objects = new List<AmfObject>();
            Materials = new List<AmfMaterial>();
        }


        // todo : use volume[0] material?
        public override string GetCsv()
        {
            string mat = "";

            if (Materials.Count > 0)
            {
                mat = AmfMetadata.GetMetaValue(Materials[0].Metadata, "Name");
                if (mat == null)
                    mat = Materials[0].MateriaId;
            }
            else
                mat = "0";

            return FileName + "," + mat + "," + Units.Units + "," + Units.Scale.ToString("g6");
        }

        public string GetMaterialNameFromId(string id)
        {
            if (id=="0")
                return null;
            else
            {
                foreach (AmfMaterial material in Materials)
                    if (id == material.MateriaId)
                    {
                        string s = AmfMetadata.GetMetaValue(material.Metadata, "name");

                        return s;
                    }
            }
            return null;
        }

        public static AmfFile LoadFromXmlFile(string FileName)
        {
            AmfFile result = null;
            XmlSerializer serializer = new XmlSerializer(typeof(AmfFile));
            FileStream fs = new FileStream(FileName, FileMode.Open);

            try
            {
                result = (AmfFile)serializer.Deserialize(fs);
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
            bool result = false;
            AmfFile newFile = null;

            try
            {
                newFile = LoadFromXmlFile(FileName);
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
            }

            if (newFile != null)
            {
                this.unit = newFile.unit;
                this.Materials = newFile.Materials;
                this.Metadata = newFile.Metadata;
                this.Objects = newFile.Objects;
                result = true;
            }
            else
            {
                //LastError = "Error reading file";
            }

            return result;
        }


        public bool SaveToXmlFile(string FileName)
        {
            bool result = false;
            XmlSerializer serializer = new XmlSerializer(typeof(AmfFile));
            TextWriter Writer = null;
            XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
            ns.Add("", "");

            FileUtils.CreateDirectory(FileName);
            try
            {
                Writer = new StreamWriter(FileName, false, Encoding.Default);

                serializer.Serialize(Writer, this, ns);
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


    }


    public class AmfObject
    {
        [XmlAttribute("id")]
        public string id;

        [XmlElement("metadata")]
        public List<AmfMetadata> Metadata;

        [XmlElement("mesh")]
        public AmfMesh Mesh;

        // material

        [XmlElement("color")]
        public AmfColor Color;

        public AmfObject() {
            Metadata = new List<AmfMetadata>();
            Mesh = new AmfMesh();
        }

        // id:name
        public string GetName()
        {
            string result = id;
            
            string name = AmfMetadata.GetMetaValue(Metadata, "name");

            if (name != null)
            {
                result += ":" + name;
            }

            return result;
        }

        public void AddVolume(MeshIndexed mesh, AmfColor color)
        {
            if ((mesh.Polygons != null) && (mesh.Polygons.Count > 0))
            {
                int baseIndex = Mesh.Vertices.Vertices.Count;

                foreach (Vector3Ext vector in mesh.Vertices)
                {
                    if (vector.Attributes.HasColor)
                        Mesh.Vertices.Vertices.Add(new AmfVertex(new Point3DF(vector.Position.X, vector.Position.Y, vector.Position.Z),
                                                                 new AmfColor(vector.Attributes.Color.R, vector.Attributes.Color.G, vector.Attributes.Color.B, vector.Attributes.Color.A)));
                    else
                        Mesh.Vertices.Vertices.Add(new AmfVertex(new Point3DF(vector.Position.X, vector.Position.Y, vector.Position.Z)));
                }

                AmfVolume volume = new AmfVolume();

                volume.Color = color;

                foreach (GlPolyIndex poly in mesh.Polygons)
                {
                    AmfTriangle triangle = new AmfTriangle(baseIndex + poly.PointIndex[0], baseIndex + poly.PointIndex[1], baseIndex + poly.PointIndex[2]);

                    if (poly.Attributes.HasColor)
                        triangle.Color = new AmfColor(ColorF.FromColor(poly.Attributes.Color));
                    volume.Triangles.Add(triangle);
                }

                Mesh.Volumes.Add(volume);
            }
        }

    }

    public class AmfMetadata
    {
        [XmlAttribute("type")]
        public string type;

        [XmlText]
        public string value;

        public AmfMetadata() { }

        public AmfMetadata(string type, string value) 
        {
            this.type = type;
            this.value = value;
        }

        public static string GetMetaValue(List<AmfMetadata> Metadata, string Type)
        {
            foreach (AmfMetadata item in Metadata)
            {
                if (string.Compare (item.type, Type, true) == 0)
                    return item.value;
            }
            return null;
        }

    }


    // Vector3[Color]
    // TriangleIndex[Color] *

    public class AmfMesh
    {
        [XmlElement("vertices")]
        public AmfVertices Vertices;

        [XmlElement("volume")]
        public List<AmfVolume> Volumes;

        public AmfMesh() {
            Vertices = new AmfVertices();
            Volumes = new List<AmfVolume>();
        }

        public MeshIndexed GetMeshForVolume(int volume)
        {
            MeshIndexed result = new MeshIndexed();

            if (volume < Volumes.Count)
            {
                foreach (AmfTriangle triangle in Volumes[volume].Triangles)
                {
                    Facet facet = new Facet(Vertices.Vertices[triangle.v1].Coordinates,
                        Vertices.Vertices[triangle.v2].Coordinates,
                        Vertices.Vertices[triangle.v3].Coordinates);

                    result.AddFacet(facet, Volumes[volume].GetColor());
                }
            }

            return result;
        }
    }

    public class AmfVertices
    {
        [XmlElement("vertex")]
        public List<AmfVertex> Vertices;

        public AmfVertices() {
            Vertices = new List<AmfVertex>();
        }
    }

    public class AmfVolume
    {
        [XmlAttribute("materialid")]
        public string MaterialId;

        [XmlElement("metadata")]
        public List<AmfMetadata> Metadata;

        [XmlElement("color")]
        public AmfColor Color;

        [XmlElement("triangle")]
        public List<AmfTriangle> Triangles;

        public AmfVolume() {
            Metadata = new List<AmfMetadata>();
            Triangles = new List<AmfTriangle>();
        }

        // Volume may have name
        public string GetName()
        {
            string result = AmfMetadata.GetMetaValue(Metadata, "name");

            if (result == null)
            {
                result = "none";
            }

            return result;
        }

        public Color GetColor()
        {
            if (Color != null)
                return Color.GetSystemColor();
            else
                return System.Drawing.Color.Silver;
        }


    }

    // similar to OpenGL Vector3Color
    public class AmfVertex
    {
        [XmlElement("coordinates")]
        public Point3DF Coordinates;

        [XmlElement("color")]
        public AmfColor Color;

        public AmfVertex() { }

        public AmfVertex(Point3DF point)
        {
            this.Coordinates = point;
            this.Color = null;
        }

        public AmfVertex(Point3DF point, AmfColor color) 
        {
            this.Coordinates = point;
            this.Color = color;
        }
    }

    //public class AmfCoordinates
    //{
    //    public double x;
    //    public double y;
    //    public double z;

    //    public AmfCoordinates() { }

    //    public AmfCoordinates(double x, double y, double z)
    //    {
    //        this.x = x;
    //        this.y = y;
    //        this.z = z;
    //    }
    //}

    // similar to Triplet but with extra properties: color, normals
    // similar to OpenGL TriangleIndexColor
    public class AmfTriangle
    {
        [XmlElement("color")]
        public AmfColor Color;

        public int v1;
        public int v2;
        public int v3;

        public AmfTriangle() { }

        public AmfTriangle(int v1, int v2, int v3) 
        {
            this.v1 = v1;
            this.v2 = v2;
            this.v3 = v3;
        }
    }

    public class AmfColor
    {
        public double r;
        public double g;
        public double b;
        public double a;  // NB Alpha is transparency

        public AmfColor() { }

        public AmfColor(double r, double g, double b, double a) 
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }

        public AmfColor(ColorF color)
        {
            r = color.R;
            g = color.G;
            b = color.B;
            a = color.A;
        }


        // convert to Color
        public Color GetSystemColor()
        {
            int r, g, b;

            r = ColorF.ByteRange(this.r);
            g = ColorF.ByteRange(this.g);
            b = ColorF.ByteRange(this.b);

            return Color.FromArgb(r, g, b);
        }
    }


    public class AmfMaterial
    {
        [XmlAttribute("id")]
        public string MateriaId;

        [XmlElement("metadata")]
        public List<AmfMetadata> Metadata;

        //composite
        public AmfMaterial()
        {
            Metadata = new List<AmfMetadata>();
        }
    }

}
