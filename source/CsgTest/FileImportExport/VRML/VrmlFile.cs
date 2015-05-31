using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

using System.Drawing;
using OpenTK;

using RMC;
using Lexing;
using CadCommon;

namespace FileImportExport.VRML
{
    public class VrmlFile : FileBase
    {
        public Point3DF OutputScale;
        
        public List<Material> Materials = new List<Material>();
        
        public Scene Scene = new Scene();  // input

        public List<VrmlObjectForKicad> Volumes = new List<VrmlObjectForKicad>();  // output

        [XmlIgnore]
        public int VolumeIndex;

        [XmlIgnore]
        public int MaterialIndex;

        [XmlIgnore]
        public string LastError;

        // private stuff
        private Material defaultMaterial;
        private List<Material> DefinedMaterials = new List<Material>(); // record which materials have been DEF'ed


        private List<NameEntry> NameIndex = new List<NameEntry>();

        public VrmlFile()
        {
            LastError = "";

            defaultMaterial = new Material();
            defaultMaterial.Name = "mat_def";

            VolumeIndex = 1;
            MaterialIndex = 1;

            // mm to 0.1 inch
            float scale = 1 / 2.54f;
            OutputScale = new Point3DF(scale, scale, scale);
        }

        public static ColorF ParseColor(VrmlLexer Parser)
        {
            ColorF result = new ColorF();

            result.R = Parser.CurToken.RealValue;
            Parser.GetNextToken();

            result.G = Parser.CurToken.RealValue;
            Parser.GetNextToken();

            result.B = Parser.CurToken.RealValue;
            Parser.GetNextToken();

            return result;
        }

        public bool LoadMaterialsFile(string FileName)
        {
            bool result = false;
            VrmlFile MaterialsFile = new VrmlFile();

            if (MaterialsFile.LoadFromFile(FileName))
            {
                // get Material nodes
                foreach (NodeStatement statement in MaterialsFile.Scene.Statements)
                    if (statement.Node.TypeId == "Material")
                    {
                        Material material = new Material();
                        material.Fill(statement);
                        Materials.Add(material);
                    }

                result = true;
            }

            return result;
        }


        //    Parser.TextParser Parser = new Parser.TextParser();

        //    try
        //    {
        //        if (Parser.Initialise(FileName))
        //        {
        //            while (result && (Parser.CurToken.Value == "material") )
        //            {
        //                Parser.GetNextToken();

        //                if (Parser.CurToken.Value == "DEF")
        //                {
        //                    Material material = new Material() ;

        //                    Parser.GetNextToken(); //
        //                    material.Name= Parser.CurToken.Value;

        //                    Parser.GetNextToken();
        //                    if (Parser.CurToken.Value == "Material")
        //                    {
        //                        if (material.Parse(Parser))
        //                            Materials.Add(material);
        //                    }
        //                    else
        //                    {
        //                        result = false;
        //                        LastError = "Expected Material at " + Parser.CurToken.Location.ToString();
        //                    }

        //                }
        //                else
        //                {
        //                    result = false;
        //                    LastError = "Expected DEF at " + Parser.CurToken.Location.ToString();
        //                }

        //            }
        //        }
        //        else
        //        {
        //            LastError = Parser.LastError;
        //            result = false;
        //        }
        //    }
        //    finally
        //    {
        //        Parser.Finish();
        //    }

        //    return result;
        //}

        // this returns a new instance (copy) 
        public MaterialProperties GetMaterialByName(string Name)
        {

            foreach (Material material in Materials)
                if (string.Compare(Name, material.Name, true) == 0)
                    return material.GetMaterialProperties();

            return defaultMaterial.GetMaterialProperties();
        }
        
        public NodeStatement FindName (string Name)
        {
            foreach (NameEntry entry in NameIndex)
                if (Name == entry.Name)
                    return entry.NodeStatement;
            return null;
        }

        private void AddShape(IndexedFaceSet IndexedFaceSet, MaterialProperties material)
        {
        }

        /// <summary>
        /// Add shape (volume)
        /// </summary>
        /// <param name="IndexedFaceSet">Points in mm</param>
        /// <param name="material"></param>
        public void AddShape (MeshIndexed mesh, MaterialProperties material)
        {
            if (mesh.Polygons.Count > 0)
            {
                VrmlObjectForKicad volume = new VrmlObjectForKicad();

                if (!string.IsNullOrEmpty(mesh.Name))
                    volume.Name = mesh.Name;
                else
                    volume.Name = "object" + VolumeIndex++;

                MaterialProperties exportMaterial = new MaterialProperties(material);
                if (string.IsNullOrEmpty(exportMaterial.Name))
                {
                    exportMaterial.Name = "mat" + MaterialIndex++;
                }

                volume.AddShape(exportMaterial, mesh, OutputScale.X);

                Volumes.Add(volume);

                Scene.Statements.Add(volume.GetElement());
            }
        }


        bool Expect(VrmlLexer Parser, Lexing.TokenType token)
        {
            if ((Parser.CurToken.Type == token))
                return true;
            else
                return false;
        }

        bool Expect(VrmlLexer Parser, Lexing.TokenType token, string value)
        {
            if ((Parser.CurToken.Type == token) && (Parser.CurToken.Value == value))
                return true;
            else
                return false;
        }

        bool MatchKeyword(Lexing.Token token, string[] Keywords)
        {
            foreach (string s in Keywords)
                if (token.Value == s)
                    return true;
            return false;
        }

        bool IsNodeTypeId(Lexing.Token token)
        {
            return (token.Type == Lexing.TokenType.Name) && (Char.IsUpper(token.Value[0]));
        }

        
        public override bool LoadFromFile(string FileName)
        {
            bool result = true;
            VrmlLexer Parser = new VrmlLexer();

            // top level : Vrml Scene
            Scene = new Scene();

            NameIndex = new List<NameEntry>();

            try
            {
                if (Parser.Initialise(FileName))
                {
                    while (result && Parser.CurToken.Type != Lexing.TokenType.EOF)
                    {
                        if  ((Parser.CurToken.Value == "DEF") || (Parser.CurToken.Value == "USE") || 
                            IsNodeTypeId (Parser.CurToken) )
                        {
                            Scene.Statements.Add(ParseStatement(Parser));
                        }
                        else
                        {
                            //error: expected DEF, USE, NodeTypeId
                            Parser.GetNextToken();
                        }
                    }
                }
            }
            catch
            {
                result = false;
            }

            return result;
        }
        
        public bool SaveToFileExt(string FileName)
        {
            bool result = true;
            OutputContext context = new OutputContext();
            context.writer = File.CreateText (FileName);
            context.DefinedMaterials = new List<string>();
            int indent = 0;

            Scene.WriteFile(context, indent);

            context.writer.Close();

            return result;
        }

        Statement ParseStatement(VrmlLexer Parser)
        {
            Statement result = null;
            if ((Parser.CurToken.Value == "DEF") || (Parser.CurToken.Value == "USE") || IsNodeTypeId(Parser.CurToken))
            {
                result = ParseNodeStatement(Parser);
            }
            return result;
        }

        NodeStatement ParseNodeStatement(VrmlLexer Parser)
        {
            NodeStatement result = new NodeStatement();
            
            if (Parser.CurToken.Value == "DEF")
            {
                Parser.GetNextToken();
                //get name
                result.NameId = Parser.CurToken.Value;
                result.Type = NodeType.Define;
                Parser.GetNextToken();

                result.Node = ParseNode(Parser);

                NameEntry nameEntry = new NameEntry();
                nameEntry.Name = result.NameId;
                nameEntry.NodeStatement = result;
                NameIndex.Add(nameEntry);
            }
            else if (Parser.CurToken.Value == "USE")
            {
                Parser.GetNextToken();
                //get name
                result.NameId = Parser.CurToken.Value;
                result.Type = NodeType.Use;

                Parser.GetNextToken();
            }
            else if (IsNodeTypeId (Parser.CurToken))
            {
                result.Type = NodeType.Anonymous;
                result.Node = ParseNode(Parser);
            }

            return result;
        }

        Node ParseNode(VrmlLexer Parser)
        {
            Node result = new Node();

            if (Parser.CurToken.Type == Lexing.TokenType.Name)
            {
                result.TypeId = Parser.CurToken.Value;

                Parser.GetNextToken();
                if (Expect(Parser, Lexing.TokenType.Symbol, "{"))
                {
                    Parser.GetNextToken();

                    result.nodeBody = ParseNodeBody(Parser);

                    if (Expect(Parser, Lexing.TokenType.Symbol, "}"))
                        Parser.GetNextToken();
                }
                //else error: expecting {
            }
            else
            {
                // error: expected ident
            }
            return result;
        }

        List<NodeBodyElement> ParseNodeBody(VrmlLexer Parser)
        {
            List<NodeBodyElement> result = new List<NodeBodyElement>();

            // Field Id
            while (Parser.CurToken.Type == Lexing.TokenType.Name)
            {
                result.Add (ParseNodeBodyElement(Parser));
            }
            return result;
        }

        NodeBodyElement ParseNodeBodyElement(VrmlLexer Parser)
        {
            NodeBodyElement result = new NodeBodyElement();

            // Field Id
            if (Parser.CurToken.Type == Lexing.TokenType.Name)
            {

                result.Field.FieldId = Parser.CurToken.Value;

                Parser.GetNextToken();

                if (Parser.CurToken.Value == "IS")
                {
                    // Only for prototypes?
                    Parser.GetNextToken();
                    // field Id
                    Parser.GetNextToken();
                }
                else if ( (Parser.CurToken.Type == Lexing.TokenType.IntegerVal) ||
                          (Parser.CurToken.Type == Lexing.TokenType.FloatVal) ||
                          (Parser.CurToken.Type == Lexing.TokenType.StringLiteral) ||
                          (Parser.CurToken.Type == Lexing.TokenType.Name) ||
                          (Parser.CurToken.Value == "[") )
                {
                    //if (sym in [ DEF_sy, FALSE_sy, NULL_sy, TRUE_sy, USE_sy, 
                    //    IDENT_sy, DOUBLE_sy, FLOAT_sy, INT32_sy, STRING_sy, 
                    //    LSQUARE_sy] )then
   
                    result.Field.FieldValue = ParseFieldValue(Parser);
                }
            }

            return result;
        }

        FieldValue ParseFieldValue (VrmlLexer Parser)
        {
            FieldValue result = null;

            if ((Parser.CurToken.Value == "FALSE") || (Parser.CurToken.Value == "TRUE"))
            {
                result = ParseSfBoolValue(Parser);
            }
            else if ( (Parser.CurToken.Type == Lexing.TokenType.IntegerVal) ||
                      (Parser.CurToken.Type == Lexing.TokenType.FloatVal) ||
                      (Parser.CurToken.Type == Lexing.TokenType.StringLiteral) ||
                      IsNodeTypeId(Parser.CurToken) ||
                      (Parser.CurToken.Value == "["))
            {
                result = ParseMfValue(Parser);
            }
            return result;
        }

        FieldValue ParseSfBoolValue(VrmlLexer Parser)
        {
            sfBoolValue result = new sfBoolValue();

            if (Parser.CurToken.Value == "FALSE") 
            {
                result.Value = false;
                Parser.GetNextToken();
            }
            else if (Parser.CurToken.Value == "TRUE")
            {
                result.Value = true;
                Parser.GetNextToken();
            }
            // else error
            return result;
        }

        // returns FieldValue
        FieldValue ParseMfValue(VrmlLexer Parser)
        {
            FieldValue result = null;

            if (Parser.CurToken.Value == "[")
            {
                Parser.GetNextToken();

                if ((Parser.CurToken.Value == "DEF") || (Parser.CurToken.Value == "USE") || IsNodeTypeId(Parser.CurToken))
                {
                    smfNodeValue val = new smfNodeValue();
                    val.Multi = true;

                    while ((Parser.CurToken.Type != Lexing.TokenType.EOF) && (Parser.CurToken.Value != "]"))
                    {
                        if ((Parser.CurToken.Value == "DEF") || (Parser.CurToken.Value == "USE") || IsNodeTypeId(Parser.CurToken))
                        {
                            val.Values.Add (ParseNodeStatement(Parser));
                        }

                        if (Parser.CurToken.Value == ",")
                            Parser.GetNextToken();
                    }
                    result = val;
                }
                else
                {
                    switch (Parser.CurToken.Type)
                    {
                        case Lexing.TokenType.StringLiteral:
                            {
                                smfStringValue val = new smfStringValue();
                                val.Multi = true;
                                while ((Parser.CurToken.Type != Lexing.TokenType.EOF) && (Parser.CurToken.Value != "]"))
                                {
                                    val.Values.Add(Parser.CurToken.Value);
                                    Parser.GetNextToken();
                                    if (Parser.CurToken.Value == ",")
                                        Parser.GetNextToken();

                                }
                                result = val;
                            }
                            break;

                        //case Lexing.TokenType.IntegerVal:
                        //    {
                        //        smfIntValue val = new smfIntValue();
                        //        val.Multi = true;
                        //        while ((Parser.CurToken.Type != Lexing.TokenType.EOF) && (Parser.CurToken.Value != "]"))
                        //        {
                        //            val.Values.Add(Parser.CurToken.GetValueAsInt());
                        //            Parser.GetNextToken();
                        //            if (Parser.CurToken.Value == ",")
                        //                Parser.GetNextToken();

                        //        }
                        //        result = val;
                        //    }
                        //    break;

                        case Lexing.TokenType.FloatVal:
                        case Lexing.TokenType.IntegerVal:
                            {
                                smfFloatValue val = new smfFloatValue();
                                val.Multi = true;
                                while ((Parser.CurToken.Type != Lexing.TokenType.EOF) && (Parser.CurToken.Value != "]"))
                                {
                                    if (Parser.CurToken.Type == Lexing.TokenType.FloatVal)
                                        val.Values.Add(Parser.CurToken.RealValue);
                                    else
                                        val.Values.Add(Parser.CurToken.IntValue);
                                    Parser.GetNextToken();
                                    if (Parser.CurToken.Value == ",")
                                        Parser.GetNextToken();

                                }
                                result = val;
                            }
                            break;

                        default:
                            if (Parser.CurToken.Value != "]")
                            {
                                // 
                                Parser.ErrorAtCurToken("expected ]");
                            }
                            else
                            {
                                MultiFieldValue val = new MultiFieldValue();
                                val.Multi = true;
                                result = val;
                            }
                            break;
                    }
                }
               
                if (Expect (Parser, Lexing.TokenType.Symbol, "]"))
                    Parser.GetNextToken();
            }
            else
            {
                if (Parser.CurToken.Value == "NULL")
                {
                    smfNodeValue val = new smfNodeValue();
                    result = val;

                    Parser.GetNextToken();
                }
                else if ((Parser.CurToken.Value == "DEF") || (Parser.CurToken.Value == "USE") || IsNodeTypeId(Parser.CurToken))
                {
                    smfNodeValue val = new smfNodeValue();
                    val.Values.Add(ParseNodeStatement(Parser));
                    result = val;
                }
                else if (Parser.CurToken.Type == Lexing.TokenType.StringLiteral)
                {
                    smfStringValue val = new smfStringValue();
                    val.Values.Add(Parser.CurToken.Value);
                    result = val;

                    Parser.GetNextToken();
                }
                //else if (Parser.CurToken.Type == Lexing.TokenType.FloatVal)
                else if (Parser.CurToken.IsANumber())
                {
                    smfFloatValue val = new smfFloatValue();

                    //while (Parser.CurToken.Type == Lexing.TokenType.FloatVal)
                    while (Parser.CurToken.IsANumber())
                    {
                        val.Values.Add(Parser.CurToken.GetValueAsDouble());

                        Parser.GetNextToken();
                        if (Parser.CurToken.Value == ",")
                            Parser.GetNextToken();
                    }
                    result = val;
                }
            //    else if (Parser.CurToken.Type == Lexing.TokenType.IntegerVal)
            //    {
            //        smfFloatValue val = new smfFloatValue();

            //        while (Parser.CurToken.Type == Lexing.TokenType.IntegerVal)
            //        {
            //            val.Values.Add(Parser.CurToken.IntValue);
            //            Parser.GetNextToken();
            //            if (Parser.CurToken.Value == ",")
            //                Parser.GetNextToken();
            //        }
            //        result = val;
            //    }
                 // else error
            }

            return result;
        }
        
        // output functions
        //void OutputMaterial (List<string> lines, Material material)
        //{
        //    lines.Add(string.Format("        material DEF {0} Material {{", material.Name));
        //    lines.Add(string.Format("          diffuseColor {0:g6} {1:g6} {2:g6}", material.diffuseColor.R, material.diffuseColor.G, material.diffuseColor.B));
        //    lines.Add(string.Format("          emissiveColor {0:g6} {1:g6} {2:g6}", material.emissiveColor.R, material.emissiveColor.G, material.emissiveColor.B));
        //    lines.Add(string.Format("          specularColor {0:g6} {1:g6} {2:g6}", material.specularColor.R, material.specularColor.G, material.specularColor.B));
        //    lines.Add(string.Format("          ambientIntensity {0:g6}", material.ambientIntensity));
        //    lines.Add(string.Format("          transparency {0:g6}", material.transparency));
        //    lines.Add(string.Format("          shininess {0:g6}", material.shininess));
        //    lines.Add(string.Format("        }}"));
        //}

        //void OutputShape(List<string> lines, Shape shape)
        //{
        //    lines.Add(string.Format(
        //              "DEF {0} Transform {{", shape.Name));
        //    lines.Add("  children [");
        //    lines.Add("    Shape {");
        //    lines.Add("      appearance Appearance {");
        //    OutputMaterial(lines, shape.appearance.material);
        //    lines.Add("      }");

        //    lines.Add("      geometry IndexedFaceSet {");
        //    lines.Add("        coord Coordinate");
        //    lines.Add("        {");
        //    lines.Add("          point");
        //    lines.Add("          [");

        //    foreach (Point3DF p in shape.geometry.IndexedFaceSet.Points)
        //        lines.Add("            " + p.Scale (OutputScale).ToString() + ",");

        //    lines.Add("          ]");
        //    lines.Add("        }");
        //    lines.Add("        coordIndex [");

        //    foreach (Triplet triplet in shape.geometry.IndexedFaceSet.Triangles)
        //        lines.Add("            " + triplet.ToString() + " -1,");

        //    lines.Add("");

        //    lines.Add("        ]");
        //    lines.Add("      }");
        //    lines.Add("    }");
        //    lines.Add("  ]");
        //    lines.Add("}");
        //}

        public bool SaveToFile (string FileName)
        {
            return SaveToFileExt(FileName);

            //List<string> lines = new List<string>();

            //lines.Add("#VRML V2.0 utf8");
            //lines.Add("# Generated by STL_to_VRML");
            //lines.Add("");

            //foreach (Shape shape in Shapes)
            //    OutputShape(lines, shape);

            //try {
            //    FileUtils.SaveListToFile(lines, FileName);

            //    return true;
            //}
            //catch (Exception ex)
            //{
            //    LastError = ex.Message;
            //    return false;
            //}
        }

    }

    public class OutputContext
    {
        public TextWriter writer;
        public List<string> DefinedMaterials;
    }
    //
    // Classes for VRML syntax tree
    //

    /// <summary>
    /// base for all VRML syntax elements
    /// </summary>
    public abstract class Element
    {
        public Element()
        { }

        public abstract void WriteFile(OutputContext context, int indent);

        public static string Indent(int indent)
        {
            return new string(' ', indent);
        }
    }

    public class Scene : Element
    {
        public List<Statement> Statements;

        public Scene()
        {
            Statements = new List<Statement>();
        }

        public override void WriteFile(OutputContext context, int indent)
        {
            context.writer.WriteLine ("#VRML V2.0 utf8");
            context.writer.WriteLine("# Generated by FileConverter");
            context.writer.WriteLine("");

            foreach (Statement statement in Statements)
            {
                statement.WriteFile(context, indent);
                context.writer.WriteLine();

                context.writer.WriteLine();
            }
        }
    }

    /// <summary>
    /// base for Statement node
    /// </summary>
    public abstract class Statement : Element 
    {
        public Statement()
        { }
    }

    public enum NodeType { Anonymous, Define, Use };

    public class NodeStatement : Statement
    {
        public NodeType Type;
        public string NameId;
        public Node Node;

        public NodeStatement()
        { }

        public override void WriteFile(OutputContext context, int indent)
        {
            if (Type == NodeType.Use)
            {
                context.writer.WriteLine("USE " + NameId);
            }
            else
            {
                if (Node == null)
                    context.writer.WriteLine("NULL");
                else
                {
                    if (Type == NodeType.Define)
                    {
                        //TODO: Material only
                        if (StringUtils.FindString(context.DefinedMaterials, NameId) == -1)
                        {
                            context.writer.Write("DEF " + NameId + " ");
                            context.DefinedMaterials.Add(NameId);
                            Node.WriteFile(context, indent + 2);
                        }
                        else
                            context.writer.Write("USE " + NameId+" ");
                    }
                    else // anonymous
                        Node.WriteFile(context, indent + 2);
                }
            }
        }
    }

    // Nodes
    public class Node : Element
    {
        public string TypeId;
        public List<NodeBodyElement> nodeBody;

        public Node() 
        {
            nodeBody = new List<NodeBodyElement>();
        }

        public override void WriteFile(OutputContext context, int indent)
        {
            context.writer.WriteLine(TypeId + " {");
            context.writer.Write(Indent(indent));

            int index = 0;
            foreach (NodeBodyElement element in nodeBody)
            {
                element.WriteFile(context, indent);

                index++;
                context.writer.WriteLine();
                if (index != nodeBody.Count)
                {
                    // not last
                    context.writer.Write(Indent(indent));
                }
            }

            context.writer.Write(Indent(indent-2)); context.writer.Write("} ");

        }
    }

    public class NodeBodyElement : Element
    {
        public Field Field;

        public NodeBodyElement()
        {
            Field = new Field();
        }

        public NodeBodyElement(string fieldId, FieldValue fieldValue)
        {
            Field = new Field();
            Field.FieldId = fieldId;
            Field.FieldValue = fieldValue;
        }

        public override void WriteFile(OutputContext context, int indent)
        {
            Field.WriteFile(context, indent);
        }
    }

    
    public class Field : Element
    {
        public string FieldId;

        public FieldValue FieldValue; // can be null (NULL keyword)

        public override void WriteFile(OutputContext context, int indent)
        {
            context.writer.Write (FieldId + " ");
            if (FieldValue == null)
            {
                context.writer.Write("NULL");
            }
            else
            {
                FieldValue.WriteFile(context, indent);
            }
        }
    }

    //
    public class FieldValue : Element
    {
        public override void WriteFile(OutputContext context, int indent)
        {
            context.writer.Write("<>");
        }
    }

    public class sfBoolValue : FieldValue
    {
        public bool Value;

        public override void WriteFile(OutputContext context, int indent)
        {
            if (Value)
                context.writer.Write("TRUE");
            else
                context.writer.Write("FALSE");
        }
    }

    public class MultiFieldValue : FieldValue
    {
        public bool Multi;

        public int GroupSize = 3;

        public virtual int GetCount { get { return 0; } }

        public virtual void WriteValue(OutputContext context, int index) { }

        public override void WriteFile(OutputContext context, int indent)
        {
            if (GetCount == 0)
            {
                if (Multi)
                {
                    context.writer.WriteLine("[ ] ");
                }
            }
            else
            {
                int lineCount;
                int count = 0;

                lineCount = GroupSize;

                if (Multi)
                {
                    context.writer.WriteLine("[ ");
                    context.writer.Write(Indent(indent + 2));
                }

                for (int j = 0; j < GetCount; j++)
                {
                    //writer.Write(val.ToString("g6"));
                    WriteValue(context, j);

                    count++;
                    if (Multi && (count == lineCount))
                    {
                        count = 0;
                        context.writer.WriteLine(",");
                        context.writer.Write(Indent(indent + 2));
                    }
                    else
                        context.writer.Write(" ");

                }
                if (Multi)
                    context.writer.Write("]");
            }
        }
    }

    public class smfFloatValue : MultiFieldValue
    {
        public List<double> Values;

        public smfFloatValue()
        {
            Values = new List<double>();
        }

        public smfFloatValue(bool multi, double [] values)
        {
            this.Multi = multi;
            Values = new List<double>();
            foreach (double d in values)
                Values.Add(d);
        }

        public smfFloatValue(bool multi, List<Vector3Ext> values, double outputScale)
        {
            this.Multi = multi;
            Values = new List<double>();
            foreach (Vector3Ext point in values)
            {
                Values.Add(point.Position.X * outputScale);
                Values.Add(point.Position.Y * outputScale);
                Values.Add(point.Position.Z * outputScale);
            }
        }

        public override int GetCount
        {
            get { return Values.Count; }
        }

        public override void WriteValue(OutputContext context, int index)
        {
            context.writer.Write(Values[index].ToString("g6"));
            //writer.Write(Values[index].ToString("g"));
        }
    }

    public class smfIntValue : MultiFieldValue
    {
        public List<int> Values;

        public smfIntValue()
        {
            Values = new List<int>();
        }

        public smfIntValue(bool multi, MeshIndexed mesh, int groupSize)
        {
            this.Multi = multi;
            Values = new List<int>();

            this.GroupSize = groupSize;
            if (mesh.Polygons.Count > 0)
                this.GroupSize = mesh.Polygons[0].VertexCount+1;

            foreach (GlPolyIndex poly in mesh.Polygons)
            {
                foreach (int p in poly.PointIndex)
                    Values.Add(p);
                Values.Add(-1);
            }
        }

        public override int GetCount
        {
            get { return Values.Count; }
        }

        public override void WriteValue(OutputContext context, int index)
        {
            context.writer.Write(Values[index].ToString());
        }

    }

    public class smfStringValue : MultiFieldValue
    {
        public List<string> Values;

        public smfStringValue ()
        {
            Values = new List<string>();
        }

        public override int GetCount
        {
            get { return Values.Count; }
        }

        public override void WriteValue(OutputContext context, int index)
        {
            context.writer.Write("\""+Values[index]+"\"");
        }
    }

    public class smfNodeValue : MultiFieldValue
    {
        public List<NodeStatement> Values;

        public smfNodeValue()
        {
            Values = new List<NodeStatement>();
        }

        public smfNodeValue(bool multi, NodeStatement [] values)
        {
            this.Multi = multi;
            Values = new List<NodeStatement>();
            foreach (NodeStatement statement in values)
                Values.Add(statement);
        }

        public override int GetCount
        {
            get { return Values.Count; }
        }

        public override void WriteValue(OutputContext context, int index)
        {
            
        }

        public override void WriteFile(OutputContext context, int indent)
        {
            if (GetCount == 0)
            {
                if (Multi)
                {
                    context.writer.WriteLine("[ ] ");
                }
                else
                    context.writer.Write("NULL");
            }
            else
            {
                if (Multi)
                {
                    context.writer.WriteLine("[ ");
                    context.writer.Write(Indent(indent+2));
                }
                foreach (NodeStatement statement in Values)
                {
                    if (statement == null)
                        context.writer.Write("NULL");
                    else
                        statement.WriteFile(context, indent);
                }
                if (Multi)
                {
                    context.writer.WriteLine("");
                    context.writer.Write("]");
                }
                else
                {
                    //    context.writer.WriteLine();
                }
            }
        }

    }



    //
    // Group, Transform, Switch
    //public class Group : Element
    //{

    //}

    public class VrmlObjectForKicad
    {
        public string Name;

        public Transform volume;

        public VrmlObjectForKicad()
        {
            volume = new Transform();
            //volume.Name
        }

        //public void AddShape (MaterialProperties material, IndexedFaceSet indexedFaceSet, double OutputScale)

        public void AddShape (MaterialProperties material, MeshIndexed mesh, double OutputScale)
        {
            if (mesh.Polygons.Count > 0)
            {
                Shape shape = new Shape();
                shape.appearance.material = new Material(material);

                //TODO:

                if (mesh.Polygons[0].VertexCount > 4)
                {
                    MeshIndexed newMesh = new MeshIndexed();
                    foreach (GlPolyIndex poly in mesh.Polygons)
                    {

                    }

                    shape.geometry = new VrmlIndexedFaceSet(newMesh);
                }
                else
                    shape.geometry = new VrmlIndexedFaceSet(mesh);

                (shape.geometry as VrmlIndexedFaceSet).OutputScale = OutputScale;

                volume.children.Values.Add(shape.GetElement());
            }
        }

        public NodeStatement GetElement()
        {
            NodeStatement statement = new NodeStatement();

            statement.Type = NodeType.Define;
            statement.NameId = Name;

            NodeStatement trans = volume.GetElement();
            statement.Node = trans.Node;
            //
            return statement;
        }
    }

    //
    // Node specific classes
    //

    public class Shape
    {
        public string Name; // NodeNameId
        
        public const string NodeTypeId = "Shape";

        public Appearance appearance; // nodeBodyElement
        public Geometry geometry;

        public Shape()
        {
            Name = "";
            appearance = new Appearance();
            geometry = new VrmlIndexedFaceSet();
        }

        public NodeStatement GetElement()
        {
            NodeStatement statement = new NodeStatement();
            statement.Type = NodeType.Anonymous;
            statement.NameId = Name;
            statement.Node = new Node();
            statement.Node.TypeId = NodeTypeId;
            statement.Node.nodeBody = new List<NodeBodyElement>();
            statement.Node.nodeBody.Add(new NodeBodyElement("appearance", new smfNodeValue(false, new NodeStatement[] { appearance.GetElement() })));
            statement.Node.nodeBody.Add(new NodeBodyElement("geometry", new smfNodeValue(false, new NodeStatement[] { geometry.GetElement() })));
            //

            return statement;
        }
    }

    // field value
    public class Rotation
    {
        // normalized rotation axis vector plus
        // rotation about axis in radians
        public float X;
        public float Y;
        public float Z;
        public float A;

        public Rotation() { }

        public Rotation(float x, float y, float z, float a) 
        {
            X = x;
            Y = y;
            Z = z;
            A = a;
        }
    }

    public class Transform
    {
        public string Name; // NodeNameId

        public const string NodeTypeId = "Transform";

        // nodeBodyElements
//        public smfFloatValue center;
        public smfNodeValue children; 
        public smfFloatValue rotation;
//        public smfFloatValue scale;
        public smfFloatValue scaleOrientation;
//        public smfFloatValue translation;

        public Point3DF Center;
        public Rotation Rotation
        {
            get { return new Rotation((float)rotation.Values[0], (float)rotation.Values[1], (float)rotation.Values[2], (float)rotation.Values[3]); }
        }
        public Point3DF Scale;
        public Rotation ScaleOrientation
        {
            get { return new Rotation((float)scaleOrientation.Values[0], (float)scaleOrientation.Values[1], (float)scaleOrientation.Values[2], (float)scaleOrientation.Values[3]); }
        }
        public Point3DF Translation ;

        public Transform()
        {
            Name = null;
            children = new smfNodeValue();
            children.Multi = true;

            //Center = new smfFloatValue(false, new double[] { 0, 0, 0});

            Center = new Point3DF (0,0,0);
            Scale = new Point3DF(1,1,1);
            Translation = new Point3DF(0, 0, 0);

            rotation = new smfFloatValue(false, new double[] { 0, 0, 1, 0 });
            scaleOrientation = new smfFloatValue(false, new double[] { 0, 0, 1, 0 });
        }

        public NodeStatement GetElement()
        {
            NodeStatement statement = new NodeStatement();
            statement.Type = NodeType.Anonymous;
            statement.NameId = Name;
            statement.Node = new Node();
            statement.Node.TypeId = NodeTypeId;
            statement.Node.nodeBody = new List<NodeBodyElement>();
            statement.Node.nodeBody.Add(new NodeBodyElement("children", children ));
            //

            return statement;
        }

        // Vec3f
        public static Point3DF FillVec3f(FieldValue value)
        {
            if (value is smfFloatValue)
            {
                smfFloatValue fval = value as smfFloatValue;
                if (fval.Values.Count == 3)
                    return new Point3DF((float)fval.Values[0], (float)fval.Values[1], (float)fval.Values[2]);
                else
                    return new Point3DF();
            }
            else
                return new Point3DF();
        }

        public void Fill(NodeStatement root)
        {
            Name = root.NameId;

            foreach (NodeBodyElement element in root.Node.nodeBody)
            {
                switch (element.Field.FieldId)
                {
                    case "center":
                        this.Center = FillVec3f(element.Field.FieldValue);
                        break;
                    case "scale":
                        this.Scale = FillVec3f(element.Field.FieldValue);
                        break;
                    case "translation":
                        this.Translation = FillVec3f(element.Field.FieldValue);
                        break;
                }
            }
        }

    }


    public class Appearance
    {
        public string Name;

        public const string NodeTypeId = "Appearance";

        public Material material;
        // public Texture;
        // public TextureTransform;

        public Appearance()
        {
            Name = "";
            material = new Material();
        }

        public NodeStatement GetElement()
        {
            //
            NodeStatement statement = new NodeStatement();
            statement.Type = NodeType.Anonymous;
            statement.Node = new Node();
            statement.Node.TypeId = NodeTypeId;
            statement.Node.nodeBody = new List<NodeBodyElement>();
            //
            statement.Node.nodeBody.Add(new NodeBodyElement("material", new smfNodeValue(false, new NodeStatement[] { material.GetElement() })));
//
            //
            //NodeBodyElement apElement = new NodeBodyElement();
            //apElement.Field.FieldId = "appearance";

            //smfNodeValue apFieldValue = new smfNodeValue();
            //apFieldValue.Multi = false;
            //apFieldValue.Values.Add(statement);
            //apElement.Field.FieldValue = apFieldValue;

            return statement;
        }

    }

    public class Material
    {
        public string Name;

        public const string NodeTypeId = "Material";

        public ColorF diffuseColor; 
        public double ambientIntensity;
        public ColorF emissiveColor;
        public ColorF specularColor;
        public double shininess;
        public double transparency; // 0 = opaque

        public Material()
        {
            Name = "default";
            diffuseColor = new ColorF(0.8, 0.8, 0.8, 0.0);
            ambientIntensity = 0.2;
            emissiveColor = new ColorF(0, 0, 0, 0);
            specularColor = new ColorF(0, 0, 0, 0);
            shininess = 0.2;
            transparency = 0.0;
        }

        public Material(Material source)
        {
            Name = source.Name;
            diffuseColor = new ColorF(source.diffuseColor);
            ambientIntensity = source.ambientIntensity;
            emissiveColor = new ColorF(source.emissiveColor);
            specularColor = new ColorF(source.specularColor);
            shininess = source.shininess;
            transparency = source.transparency;
        }

        public Material(MaterialProperties source)
        {
            Name = source.Name;
            diffuseColor = new ColorF(source.diffuseColor);
            ambientIntensity = source.ambientIntensity;
            emissiveColor = new ColorF(source.emissiveColor);
            specularColor = new ColorF(source.specularColor);
            shininess = source.shininess;
            transparency = source.transparency;
        }

        public MaterialProperties GetMaterialProperties()
        {
            MaterialProperties result = new MaterialProperties();
            result.Name = this.Name;
            result.diffuseColor = new ColorF(this.diffuseColor);
            result.ambientIntensity = this.ambientIntensity;
            result.emissiveColor = new ColorF(this.emissiveColor);
            result.specularColor = new ColorF(this.specularColor);
            result.shininess = this.shininess;
            result.transparency = this.transparency;
            return result;
        }

        // Get the diffuse color as ARGB
        public Color GetColor()
        {
            int r, g, b, a;

            r = ColorF.ByteRange(this.diffuseColor.R);
            g = ColorF.ByteRange(this.diffuseColor.G);
            b = ColorF.ByteRange(this.diffuseColor.B);
            a = 255-ColorF.ByteRange(this.transparency);  // 255 = opaque

            return Color.FromArgb (a,r,g,b);
        }

        public smfFloatValue GetColorValue (ColorF color)
        {
            return new smfFloatValue(false, new double [] { color.R, color.G, color.B} );
        }

        public smfFloatValue GetSFloatValue (double val)
        {
            return new smfFloatValue(false, new double [] { val } );
        }

        public static ColorF FillColor(FieldValue value)
        {
            if (value is smfFloatValue)
            {
                smfFloatValue fval = value as smfFloatValue;
                if (fval.Values.Count == 3)
                    return new ColorF(fval.Values[0], fval.Values[1], fval.Values[2], 1);  // alpha should be 0?
                else
                    return new ColorF();
            }
            else
                return new ColorF();
        }

        public void Fill (NodeStatement root)
        {
            Name = root.NameId;

            foreach (NodeBodyElement element in root.Node.nodeBody)
            {
                switch (element.Field.FieldId)
                {
                    case "diffuseColor":                        
                        this.diffuseColor = FillColor (element.Field.FieldValue);
                        break;
                    case "emissiveColor":
                        this.emissiveColor = FillColor (element.Field.FieldValue);
                        break;
                    case "specularColor":
                        this.specularColor = FillColor (element.Field.FieldValue);
                        break;
                    case "ambientIntensity":
                        this.ambientIntensity = (element.Field.FieldValue as smfFloatValue).Values[0];
                        break;
                    case "transparency":
                        this.transparency = (element.Field.FieldValue as smfFloatValue).Values[0];
                        break;
                    case "shininess":
                        this.shininess = (element.Field.FieldValue as smfFloatValue).Values[0];
                        break;
                }
            }
        }

        public NodeStatement GetElement()
        {
            NodeStatement statement = new NodeStatement();

            if (Name != null)
            {
                statement.Type = NodeType.Define;
                statement.NameId = Name;
            }
            else
                statement.Type = NodeType.Anonymous;

            statement.Node = new Node();

            statement.Node.TypeId = NodeTypeId;
            statement.Node.nodeBody = new List<NodeBodyElement>();

            statement.Node.nodeBody.Add(new NodeBodyElement("diffuseColor", GetColorValue(diffuseColor)));
            statement.Node.nodeBody.Add(new NodeBodyElement("emissiveColor", GetColorValue(emissiveColor)));
            statement.Node.nodeBody.Add(new NodeBodyElement("specularColor", GetColorValue(specularColor)));
            statement.Node.nodeBody.Add(new NodeBodyElement("ambientIntensity", GetSFloatValue(ambientIntensity)));
            statement.Node.nodeBody.Add(new NodeBodyElement("transparency", GetSFloatValue(transparency)));
            statement.Node.nodeBody.Add(new NodeBodyElement("shininess", GetSFloatValue(shininess)));

            return statement;
        }

        public bool Parse(VrmlLexer Parser)
        {
            string LastError;
            bool result = true;

            // assert Curtoken = "Material";

            Parser.GetNextToken(); //
            if (Parser.CurToken.Value == "{")
            {
                Parser.GetNextToken(); //

                // set defaults

                while (Parser.CurToken.Value != "}")
                {
                    switch (Parser.CurToken.Value)
                    {
                        case "diffuseColor":
                            Parser.GetNextToken();
                            this.diffuseColor = VrmlFile.ParseColor(Parser);
                            break;
                        case "emissiveColor":
                            Parser.GetNextToken();
                            this.emissiveColor = VrmlFile.ParseColor(Parser);
                            break;
                        case "specularColor":
                            Parser.GetNextToken();
                            this.specularColor = VrmlFile.ParseColor(Parser);
                            break;
                        case "ambientIntensity":
                            Parser.GetNextToken();
                            this.ambientIntensity = Parser.CurToken.RealValue;
                            Parser.GetNextToken();
                            break;
                        case "transparency":
                            Parser.GetNextToken();
                            this.transparency = Parser.CurToken.RealValue;
                            Parser.GetNextToken();
                            break;
                        case "shininess":
                            Parser.GetNextToken();
                            this.shininess = Parser.CurToken.RealValue;
                            Parser.GetNextToken();
                            break;
                        default:
                            result = false;
                            LastError = "Unexpected token " + Parser.CurToken.Value + " at " + Parser.CurToken.Location.ToString();
                            break;
                    }
                }

                if (Parser.CurToken.Value == "}")
                {
                    Parser.GetNextToken(); //
                }
            }
            else
            {
                result = false;
            }
        
            return result;
        }
    }


    public class Coordinate
    {
        public string Name;

        public const string NodeTypeId = "Coordinate";
        
        // Vector3f [] point;

        public Coordinate()
        {
        }

        public Coordinate(IndexedFaceSet faceSet)
        {
        }

        //TODO
        public static NodeStatement GetElement(MeshIndexed mesh, double OutputScale)
        {

            NodeStatement statement = new NodeStatement();
            statement.Type = NodeType.Anonymous;
            //if (Name != null)
            //    statement.NameId = Name;
            statement.Node = new Node();
            statement.Node.TypeId = NodeTypeId;
            statement.Node.nodeBody = new List<NodeBodyElement>();
            statement.Node.nodeBody.Add(new NodeBodyElement("point", new smfFloatValue(true, mesh.Vertices, OutputScale )));

            //
            return statement;
        }

        public static List<Point3DF> GetPoints(Node coordNode)
        {
            List<Point3DF> result = new List<Point3DF>();

            smfFloatValue pointField = (coordNode.nodeBody[0].Field.FieldValue as smfFloatValue);

            for (int j = 0; j < pointField.Values.Count; j+=3 )
            {
                Point3DF point = new Point3DF();
                point.X = (float)pointField.Values[j];
                point.Y = (float)pointField.Values[j + 1];
                point.Z = (float)pointField.Values[j + 2];

                result.Add(point);
            }

            return result;
        }
    }

    public abstract class Geometry
    {
        public abstract NodeStatement GetElement();
    }

    public class VrmlIndexedFaceSet : Geometry 
    {
        public string Name;

        public const string NodeTypeId = "IndexedFaceSet";

        // following contains coord (Points) : sfNode (Coordinate)
        // and coordIndex (Triangles) : mfInt32
        //public IndexedFaceSet IndexedFaceSet;
        public MeshIndexed Mesh;

        public double OutputScale;

        public VrmlIndexedFaceSet()
        {
            Mesh = new MeshIndexed();
        }

        public VrmlIndexedFaceSet( MeshIndexed mesh)
        {
            this.Mesh = mesh;
        }

        public override NodeStatement GetElement()
        {
            NodeStatement statement = new NodeStatement();
            statement.Type = NodeType.Anonymous;
            statement.NameId = Name;
            statement.Node = new Node();
            statement.Node.TypeId = NodeTypeId;
            statement.Node.nodeBody = new List<NodeBodyElement>();
            statement.Node.nodeBody.Add(new NodeBodyElement("coord",      new smfNodeValue(false, new NodeStatement[] { Coordinate.GetElement(Mesh, OutputScale) })));
            statement.Node.nodeBody.Add(new NodeBodyElement("coordIndex", new smfIntValue(true, Mesh, 4) ));


            //
            return statement;
        }

        public static List<PolyIndex> GetPolygons(smfFloatValue value)
        {
            List<PolyIndex> result = new List<PolyIndex>();

            int j = 0;
            while (j < value.Values.Count)
            {
                PolyIndex poly = new PolyIndex();
                while ( (j < value.Values.Count) && (value.Values[j] >=0) )
                {
                    poly.PointIndex.Add((int)value.Values[j]);
                    j++;
                }

                while ((j < value.Values.Count) && (value.Values[j] < 0))
                {
                    j++;
                }

                //TODO: check poly len
                result.Add(poly);
            }

            return result;

        }

        public static IndexedFaceSet GetIndexedFaceSet(Node geomNode)
        {
            IndexedFaceSet faces = new IndexedFaceSet();

            foreach (NodeBodyElement bodyElem in geomNode.nodeBody)
            {
                switch (bodyElem.Field.FieldId)
                {
                    case "coord":
                        {
                            smfNodeValue nodeValue = bodyElem.Field.FieldValue as smfNodeValue;
                            Node coordNode = nodeValue.Values[0].Node;
                            faces.Points = Coordinate.GetPoints(coordNode);
                        }
                        break;
                    case "coordIndex":
                        {
                            //TODO: should be ints
                            smfFloatValue value = (bodyElem.Field.FieldValue as smfFloatValue);
                            faces.Polygons = GetPolygons(value);
                        }
                        break;
                }
            }

            return faces;            
        }
    }

    public class NameEntry
    {
        public string Name;
        public NodeStatement NodeStatement;
    }
}
