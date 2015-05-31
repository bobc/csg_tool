using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.IO;

using CadCommon;
//using OpenTK;
using FileImportExport;
using FileImportExport.VRML;
using FileImportExport.AMF;
using FileImportExport.STL;
using ConstructiveSolidGeometry;
using OpenGLUtils;
using RMC;
using CSGImport;

namespace TestCsg
{
    public partial class Form1 : Form
    {
        const string Company = "RMC";
        const string AppTitle = "CSGTest";

        const string AppDescription = "CSG Test Program";
        const string AppVersion = "0.1";
        const string AppCaption = "CSGTest";

        //string BasePath = @"C:\git_bobc\csg_tool\samples";
        string BasePath = @"C:\temp\csg";

        MeshBuffers MeshBuffers;
        GLView glView;

        ProjectSettings Project;
        AppSettings AppSettings;

        // document
        string Filename;
        CSG CurrentCsg;

        public Form1()
        {
            InitializeComponent();

            glView = new GLView(glControl1);

            this.splitContainer1.Panel2.MouseWheel += Panel2_MouseWheel;
        }

        void Panel2_MouseWheel(object sender, MouseEventArgs e)
        {
            glView.MouseWheel(sender, e);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            string AppDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/" + Company;
            string Filename = Path.Combine(AppDataFolder, AppTitle + ".config.xml");

            LoadAppSettings(Filename);

            if (!MeshBuffers.CheckCapabilities())
            {
                MessageBox.Show("OpenGL Problem" + Environment.NewLine +
                    Environment.NewLine +
                    "Minimum OpenGL requirements not supported (requires OpenGL 1.5 or later)",
                    AppTitle,
                    MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }

            // new doc
//            NewProject();

            Project = new ProjectSettings();
            //Document.FileName = "Untitled";
            //Project.ProjectFileName = Document.FileName;
            Project.ProjectFileName = "Untitled";

        }

        public void LoadAppSettings(string filename)
        {
            AppSettingsBase.Filename = filename;

            AppSettings = (AppSettings)AppSettings.LoadFromXmlFile(filename);

            if (AppSettings != null)
            {
                AppSettings.MainForm = this;
                AppSettings.OnLoad();
            }
            else
            {
                AppSettings = new AppSettings(this);
            }

            //toolStripMenuItemShowGrid.Checked = AppSettings.ShowGrid;
        }

        private void SaveAppSettings()
        {
            AppSettings.OnSaving();
            AppSettings.SaveToXmlFile(AppSettingsBase.Filename);
        }

        private void SetCaption ()
        {
            if (string.IsNullOrEmpty(Filename))
                this.Text = AppDescription;
            else
                this.Text = Path.GetFileNameWithoutExtension(Filename) + " - " + AppDescription;
        }


        private void GenDefs()
        {
            string [] data = File.ReadAllLines(@"c:\temp\materials.txt");

            List<string> outp = new List<string>();

            foreach (string s in data)
            {
                string[] fields = s.Split('\t');

                string name = fields[0].Trim().Replace (' ','_');

                outp.Add (string.Format("public static MaterialProperties {0}()", name));
                outp.Add ("{");
                outp.Add ("MaterialProperties result = new MaterialProperties();");
                outp.Add(string.Format("//result.Name = \"{0}\";", name));
                outp.Add (string.Format("result.diffuseColor = new ColorF({0}, {1}, {2}, 0);", fields[4], fields[5], fields[6]));
                outp.Add(string.Format("result.emissiveColor = new ColorF({0}, {1}, {2}, 0);", fields[1], fields[2], fields[3]));
                outp.Add(string.Format("result.specularColor = new ColorF({0}, {1}, {2}, 0);", fields[7], fields[8], fields[9]));
                outp.Add (string.Format("result.ambientIntensity = {0};", "CalcIntensity (result)"));
                outp.Add (string.Format("result.transparency = {0};", 0));
                outp.Add (string.Format("result.shininess = {0};", fields[10]));
                outp.Add ("return result;");
                outp.Add ("}");

            }

            File.WriteAllLines(@"c:\temp\materials.cs", outp.ToArray());
        }


        private void Test()
        {
            Volume red_cube = CSGShapes.cube(new Vector3(0, 0, 0), new Vector3(1, 1, 1), ColorF.FromColor(Color.Red));
            Volume blue_sphere = CSGShapes.sphere(new Vector3(1, 1, 1), 1f, 16, 8, ColorF.FromColor(Color.Blue));
            Volume cube2 = CSGShapes.cube(new Vector3(0, 0, 0), new Vector3(1.5f, 1, 1), ColorF.FromColor(Color.Red));

            Volume a = red_cube;
            Volume b = blue_sphere;
            Volume result;

            // union
            //result = a.union(b);
            //result = a.subtract2(b,0);
            result = a.intersect(b,0);

            //result = a.intersect2(b,0);
            

            Display(new CSG(result));

            WriteVrml(Path.Combine(BasePath, "result"), result);
        }

        private void GenTest()
        {
            //GenDefs();

            //
            Volume red_cube = CSGShapes.cube(new Vector3(-1, -1, -1), new Vector3(1, 1, 1), ColorF.FromColor (Color.Red));
            Volume blue_sphere = CSGShapes.sphere(new Vector3(2, 0, 0), 1f, 16, 8, ColorF.FromColor(Color.Blue));

            Volume cube = CSGShapes.cube(new Vector3(-1, -1, -1), new Vector3(1, 1, 1));
            Volume sphere = CSGShapes.sphere(new Vector3(0, 0, 0), 1f, 16, 8);

            Volume cube1 = CSGShapes.cube(new Vector3(0, 0, 0), new Vector3(1, 1, 1), ColorF.FromColor(Color.Red));
            Volume cube2 = CSGShapes.cube(new Vector3(1, 1, 0), new Vector3(1, 1, 1), ColorF.FromColor(Color.Blue));

            //WritePolygonsToStl("cube", cube.polygons);
            //WritePolygonsToStl("sphere", sphere.polygons);


            //List<Polygon> polygons = cube.subtract(sphere).toPolygons();
            //Mesh mesh = cube.toMesh();
            //            WriteMesh(mesh);
            //            WritePolygons("result_union", polygons);

            Volume result;

            WriteVrml("cube", red_cube);
            WriteVrml("sphere", blue_sphere);

            WriteAmf("cube", red_cube);
            WriteAmf("sphere", blue_sphere);

            if (true)
            {
                // Standard CSG operations
                Volume a = red_cube;
                Volume b = blue_sphere;

                // union
                result = a.union(b);
                WriteVrml("std_union", result);
                WriteAmf("std_union", result);

                //// difference
                //result = a.subtract2(b,0);
                //WriteVrml("std_difference", result);
                //WriteAmf("std_difference", result);

                //// intersect
                //result = a.intersect2(b,0);
                //WriteVrml("std_intersect", result);
                //WriteAmf("std_intersect", result);
            }

#if xxx
            if (true)
            {
                // alternative (surface)

                CSG a = red_cube;
                CSG b = sphere;

                // union : same

                GenAlt("nn", cube, sphere);
                GenAlt("nc", cube, blue_sphere);
                GenAlt("cn", red_cube, sphere);
                GenAlt("cc", red_cube, blue_sphere);
            }

            //
            //difference() {
            //  union() {
            //    translate([0,0,1]) color("green") cube([1,1,1]);
            //    color("red") cube([1,1,1]);
            //  };

            //  translate([-.5, -.5, -.5]) color("pink") cube([2,1,3]);
            //}

            red_cube = CSGShapes.cube(new Vector3(0, 0, 0), new Vector3(1, 1, 1), ColorF.FromColor(Color.Red));
            CSG green_cube = CSGShapes.cube(new Vector3(0,0,2), new Vector3(1, 1, 1), ColorF.FromColor(Color.Green));
            CSG pink_cube = CSGShapes.cube(new Vector3(0,1f,1), new Vector3(2, 1, 4), ColorF.FromColor(Color.Pink));

            //List<ConstructiveSolidGeometry.Polygon> res = green_cube.union(red_cube).subtract2(pink_cube,1).toPolygons();

            List<ConstructiveSolidGeometry.Polygon> res = green_cube.union(red_cube).toPolygons();

            WriteVrml("ex", res);
#endif
        }

        void GenAlt (string suffix, CSG a, CSG b)
        {
            List<ConstructiveSolidGeometry.Polygon> polygons;

#if xxx
            // "cut"
            polygons = a.subtract2(b, 0).toPolygons();
            WriteVrml("alt_difference_a0"+suffix, polygons);
            WriteAmf("alt_difference_a0" + suffix, polygons);

            polygons = a.subtract2(b, 1).toPolygons();
            WriteVrml("alt_difference_a1" + suffix, polygons);
            WriteAmf("alt_difference_a1" + suffix, polygons);

            polygons = a.subtract2(b, 0.5f).toPolygons();
            WriteVrml("alt_difference_a05" + suffix, polygons);
            WriteAmf("alt_difference_a05" + suffix, polygons);

            // intersect
            polygons = a.intersect2(b, 0).toPolygons();
            WriteVrml("alt_intersect_a0" + suffix, polygons);
            WriteAmf("alt_intersect_a0" + suffix, polygons);

            polygons = a.intersect2(b, 1).toPolygons();
            WriteVrml("alt_intersect_a1" + suffix, polygons);
            WriteAmf("alt_intersect_a1" + suffix, polygons);

            polygons = a.intersect2(b, 0.5f).toPolygons();
            WriteVrml("alt_intersect_a05" + suffix, polygons);
            WriteAmf("alt_intersect_a05" + suffix, polygons);
#endif
        }


        public void WriteMeshToStl(Mesh mesh)
        {
            List<string> data = new List<string>();
            data.Add("solid Model");
            for (int j = 0; j < mesh.triangles.Length / 3; j++ )
            {
                data.Add (string.Format (" facet normal {0} {1} {2}", mesh.normals[j].x, mesh.normals[j].y, mesh.normals[j].z));
                //data.Add (string.Format (" facet normal 0 0 0"));

                int k = j * 3;
                int a = mesh.triangles[k];
                int b = mesh.triangles[k+1];
                int c = mesh.triangles[k+2];

                data.Add("  outer loop");
                data.Add(string.Format("   vertex {0:g} {1:g} {2:g}", mesh.vertices[a].x, mesh.vertices[a].y, mesh.vertices[a].z));
                data.Add(string.Format("   vertex {0:g} {1:g} {2:g}", mesh.vertices[b].x, mesh.vertices[b].y, mesh.vertices[b].z));
                data.Add(string.Format("   vertex {0:g} {1:g} {2:g}", mesh.vertices[c].x, mesh.vertices[c].y, mesh.vertices[c].z));
                data.Add("  endloop");
                data.Add(" endfacet");
            }
            data.Add("endsolid Model");

            File.WriteAllLines(BasePath, data.ToArray());
        }

        public void WritePolygonsToStl(string name, List<ConstructiveSolidGeometry.Polygon> polygons)
        {
            List<string> data = new List<string>();

            data.Add("solid Model");

            foreach (ConstructiveSolidGeometry.Polygon poly in polygons)
            {

                //data.Add(string.Format("# color {0} ", props.Color.ToString()));
                for (int j=0; j <= poly.vertices.Length-3; j++)
                {
                    int a = 0;
                    int b = (j + 1) % poly.vertices.Length;
                    int c = (j + 2) % poly.vertices.Length;

                    //Debug.Log(String.Format("{0} {1} {2}", a, b, c));

                    data.Add(string.Format(" facet normal {0} {1} {2}", poly.plane.normal.x, poly.plane.normal.y, poly.plane.normal.z));
                    //data.Add (string.Format (" facet normal 0 0 0"));
                    data.Add("  outer loop");
                    data.Add(string.Format("   vertex {0:g} {1:g} {2:g}", poly.vertices[a].pos.x, poly.vertices[a].pos.y, poly.vertices[a].pos.z));
                    data.Add(string.Format("   vertex {0:g} {1:g} {2:g}", poly.vertices[b].pos.x, poly.vertices[b].pos.y, poly.vertices[b].pos.z));
                    data.Add(string.Format("   vertex {0:g} {1:g} {2:g}", poly.vertices[c].pos.x, poly.vertices[c].pos.y, poly.vertices[c].pos.z));
                    data.Add("  endloop");
                    data.Add(" endfacet");
                }
            }
            data.Add("endsolid Model");

            File.WriteAllLines(Path.Combine(BasePath, name + ".stl"), data.ToArray());
        }

        public void WriteVrml(string name, Volume volume)
        {
            WriteVrml(name, new CSG(volume));
        }

        // output separate object for each color (for kicad)
        public void WriteVrml(string name, CSG csg)
        {
            VrmlFile vrml = new VrmlFile();
            CadCommon.MeshIndexed mesh = new CadCommon.MeshIndexed();
            CadCommon.MaterialProperties mat_body = Materials.BlackPlastic();

            List<ColorF> colors = new List<ColorF>();

            //
            CadCommon.MeshIndexed shape;

            //TODO: scan for set of unique colors;
            foreach (Volume volume in csg.volumes)
            foreach (ConstructiveSolidGeometry.Polygon poly in volume.polygons)
            {
                if ( (poly.properties != null) && (poly.properties.Color != null) )
                {
                    bool found = false;
                    foreach (ColorF color in colors)
                        if (color.IsEqual( poly.properties.Color))
                        { 
                            found = true;
                            break;
                        }
                    if (!found)
                        colors.Add(poly.properties.Color);
                }
            }

            //MaterialProperties mat_red = Materials.ModifyColor(Materials.BlackPlastic(), ColorF.FromColor(Color.Red));
            //MaterialProperties mat_blue = Materials.ModifyColor(Materials.BlackPlastic(), ColorF.FromColor(Color.Blue));

            foreach (Volume volume in csg.volumes)
            {
                // no color
                shape = GetShape(volume, true, null);
                vrml.AddShape(shape, null);

                foreach (ColorF color in colors)
                {
                    MaterialProperties mat = Materials.ModifyColor(Materials.BlackPlastic(), color);

                    shape = GetShape(volume, true, color);
                    vrml.AddShape(shape, mat);
                }
            }

            //
            //vrml.SaveToFile(Path.Combine(BasePath, name + ".wrl"));
            vrml.SaveToFile(Path.ChangeExtension(name,".wrl"));
        }

        // output as single volume with triangle colors
        public void WriteAmf(string name, Volume volume)
        {
            WriteAmf(name, new CSG(volume));
        }

        public void WriteAmf (string name, CSG csg)
        {
            AmfFile amf = new AmfFile();
            amf.unit = "millimeter";

            CadCommon.MeshIndexed mesh = new CadCommon.MeshIndexed();
            CadCommon.MaterialProperties mat_body = Materials.BlackPlastic();

            //
            CadCommon.MeshIndexed shape;

            MaterialProperties mat_red = Materials.ModifyColor(Materials.BlackPlastic(), ColorF.FromColor(Color.Red));
            MaterialProperties mat_blue = Materials.ModifyColor(Materials.BlackPlastic(), ColorF.FromColor(Color.Blue));


            AmfObject obj = new AmfObject();
            obj.id = "0";
            obj.Metadata.Add(new AmfMetadata("name", name));
            amf.Objects.Add(obj);

            foreach (Volume volume in csg.volumes)
            {
                shape = GetShape(volume, false, volume.Color);
                //amf.AddShape(shape, mat_red);
                obj.AddVolume(shape, null);
            }

            //
            amf.SaveToXmlFile (Path.ChangeExtension(name,".amf"));
        }

        private ColorF GetColor (Volume volume, ConstructiveSolidGeometry.Polygon poly)
        {
            if  ( (poly.properties!=null) && (poly.properties.Color != null) )
                return poly.properties.Color;
            else if (volume.Color != null)
                return volume.Color;
            else
                return ColorF.FromColor(Color.Silver);

        }

        public CadCommon.MeshIndexed GetShape(Volume volume, bool SelectColor, ColorF color)
        {
            List<ConstructiveSolidGeometry.Polygon> polygons = volume.polygons;
            CadCommon.MeshIndexed mesh = new CadCommon.MeshIndexed();
            bool pick;

            foreach (ConstructiveSolidGeometry.Polygon poly in polygons)
            {
                // data.Add(string.Format("# color {0} ", props.Color.ToString()));

                ColorF this_color = GetColor (volume, poly);

                pick = false;
                if (!SelectColor)
                    pick = true;
                else if (poly.properties != null)
                {
                    if (color == null)
                        pick = poly.properties.Color == null;
                    else if (poly.properties.Color != null)
                        pick = poly.properties.Color.IsEqual(color);
                }
                else if ((color == null) && (poly.properties == null))
                    pick = true;

                if (pick)
                {
                    for (int j = 0; j <= poly.vertices.Length - 3; j++)
                    {
                        int a = 0;
                        int b = (j + 1) % poly.vertices.Length;
                        int c = (j + 2) % poly.vertices.Length;
                        
                        if (this_color != null)
                            mesh.AddTriangle(new TriangleExt(
                                            new OpenTK.Vector3(poly.vertices[a].pos.x, poly.vertices[a].pos.y, poly.vertices[a].pos.z),
                                            new OpenTK.Vector3(poly.vertices[b].pos.x, poly.vertices[b].pos.y, poly.vertices[b].pos.z),
                                            new OpenTK.Vector3(poly.vertices[c].pos.x, poly.vertices[c].pos.y, poly.vertices[c].pos.z),
                                            this_color.ToRGBColor() )
                                         );
                        else
                            mesh.AddTriangle(new TriangleExt(
                                            new OpenTK.Vector3(poly.vertices[a].pos.x, poly.vertices[a].pos.y, poly.vertices[a].pos.z),
                                            new OpenTK.Vector3(poly.vertices[b].pos.x, poly.vertices[b].pos.y, poly.vertices[b].pos.z),
                                            new OpenTK.Vector3(poly.vertices[c].pos.x, poly.vertices[c].pos.y, poly.vertices[c].pos.z))
                                         );
                    }
                }
            }

            return mesh;
        }

        // Display

        private void Display (CSG csg)
        {
            MeshBuffers = new MeshBuffers();
            MeshIndexed mesh = new MeshIndexed();

            MeshBuffers.ShowGrid = false;
            MeshBuffers.GridUnits = new UnitsSpecification();

            MeshBuffers.AxisMax = new OpenTK.Vector3(10f, 10f, 10f);


            foreach (Volume volume in csg.volumes)
            {
                Color color = Color.Gold;

                if (volume.Color != null)
                    color = volume.Color.ToRGBColor();

                mesh.AddMesh(volume.GetMesh(), color);
            }
            //
            MeshBuffers.AssignMesh(mesh);

            //
            glView.m_init = false;
            glView.RenderMeshBuffers(MeshBuffers);
        }


        private void ShowMesh()
        {
            MeshBuffers = new MeshBuffers();
            MeshIndexed mesh = new MeshIndexed();

            MeshBuffers.ShowGrid = false;
            MeshBuffers.GridUnits = new UnitsSpecification();

            MeshBuffers.AxisMax = new OpenTK.Vector3(1f, 1f, 1f);

            int fileIndex;
            for (fileIndex = 0; fileIndex < Project.Files.Count; fileIndex++)
            {
                FileBase file = Project.Files[fileIndex];

                if (file is StlFile)
                {
                    StlFile StlFile = file as StlFile;

                    mesh.Rotation = Project.fileList[fileIndex].Rotation;
                    mesh.DisplayScale = MeshBuffers.CalcScale(file.Units, MeshBuffers.DisplayUnits);
                    mesh.AddMesh(StlFile.Mesh, Project.fileList[fileIndex].Material.GetColor());
                }
                else if (file is AmfFile)
                {
                    AmfFile AmfFile = file as AmfFile;
                    AmfObject obj = AmfFile.Objects[0]; //todo

                    // get the id of the material from the volume
                    // get name from material metadata
                    // get Material properties from Materials or list (or default)
                    int volNum;
                    for (volNum = 0; volNum < obj.Mesh.Volumes.Count; volNum++)
                    {
                        string materialName = AmfFile.GetMaterialNameFromId(obj.Mesh.Volumes[volNum].MaterialId);
                        MaterialProperties material = Project.GetMaterialByName(materialName);

                        // color from : vol.material_id < vol.color < triangle.color < vertex.color

                        //!    vrmlFile.AddShape(obj.Mesh.GetIndexedFaceSetForVolume(volNum), material);
                        mesh.Rotation = Project.fileList[fileIndex].Rotation;

                        // TODO: get triangle color
                        mesh.AddMesh(obj.Mesh.GetMeshForVolume(volNum), material.GetColor());
                    }
                }
                else if (file is VrmlFile)
                {
                    VrmlConversionContext context = new VrmlConversionContext();

                    // traverse the tree looking for Shape,IndexedFaceSet
                    // keep transfrom
                    // discard group? switch
                    // add shapes to mesh

                    context.vrmlFile = file as VrmlFile;
                    context.transform = new FileImportExport.VRML.Transform();
                    context.material = new MaterialProperties();
                    context.mesh = mesh;

                    mesh.DisplayScale = MeshBuffers.CalcScale(Project.fileList[fileIndex].Units, MeshBuffers.DisplayUnits);
                    mesh.Rotation = Project.fileList[fileIndex].Rotation;

                    foreach (NodeStatement statement in context.vrmlFile.Scene.Statements)
                        FindShapes(context, statement);
                }

                //else error
            }

            // mesh.CalculateScale();
            MeshBuffers.AssignMesh(mesh);

            //
            glView.m_init = false;
            glView.RenderMeshBuffers(MeshBuffers);
        }

        private void FindShapes(VrmlConversionContext context, NodeStatement statement)
        {
            // "USE" has no Node
            if (statement.Type == NodeType.Use)
            {
                NodeStatement useTarget = context.vrmlFile.FindName(statement.NameId);
                if (useTarget != null)
                    FindShapes(context, useTarget);
            }
            else
            {
                if (statement.Node.TypeId == "IndexedFaceSet")
                {
                    {
                        //
                        //Trace("adding shape");

                        IndexedFaceSet faces = VrmlIndexedFaceSet.GetIndexedFaceSet(statement.Node);
                        faces.Scale(context.transform.Scale);
                        faces.Translate(context.transform.Translation);

                        context.mesh.AddToMesh(faces, context.material);
                    }
                }
                else if (statement.Node.TypeId == "Transform")
                {
                    context.transform.Fill(statement); // copy context?
                }
                else if (statement.Node.TypeId == "Material")
                {
                    //VrmlConversionContext newContext = new VrmlConversionContext(context);
                    //newContext.transform = context.transform;
                    //context = newContext;
                    FileImportExport.VRML.Material mat = new FileImportExport.VRML.Material();
                    mat.Fill(statement);
                    context.material = mat.GetMaterialProperties();
                }

                // recurse down
                foreach (NodeBodyElement bodyElem in statement.Node.nodeBody)
                {
                    if (bodyElem.Field.FieldValue is smfNodeValue)
                    {
                        smfNodeValue smfNode = bodyElem.Field.FieldValue as smfNodeValue;
                        foreach (NodeStatement substatement in smfNode.Values)
                        {
                            FindShapes(context, substatement);
                        }
                    }
                }
            }
        }

        private void ReadCsg (string Filename)
        {
            CSGImport.Node node = CSGImport.Parser.ParseFile(Filename);

            if (node != null)
            {
                Console.WriteLine(node.Unparse());
                CSG csg = EvaluateTree(node);
                if (csg != null)
                {
                    Display(csg);

                    WriteVrml(Filename, csg.volumes[0]);
                    WriteAmf(Filename, csg.volumes[0]);
                }
            }
        }

        private void CompileCsg (MemoryStream stream)
        {
            CSGImport.Node csgTree = CSGImport.Parser.ParseStream(stream);

            if (csgTree != null)
            {
                // Console.WriteLine(csgTree.Unparse());

                CurrentCsg = EvaluateTree(csgTree);
                if (CurrentCsg != null)
                {
                    Display(CurrentCsg);
                }
            }
        }

        
        // evaluate child nodes that are chained in a block list
        private List<CSG> EvaluateBlockList (Binary list)
        {
            //if ((list == null) || (list.Tag != NodeTag.blocklist) )
            //    return null;

            List<CSG> csg = new List<CSG>();
            while (list != null)
            {
                if (list.Tag == NodeTag.blocklist)
                {
                    csg.Add(EvaluateTree(list.lhs));
                    list = (list.rhs as Binary);
                }
                else
                {
                    csg.Add(EvaluateTree(list));
                    list = null;
                }
            }
            csg.Reverse();
            return csg;
        }

        private List<Binary> GetArgList (Binary list)
        {
            if ((list == null) || (list.Tag != NodeTag.arglist) )
                return null;

            List<Binary> args = new List<Binary>();
            while (list != null)
            {
                if (list.Tag == NodeTag.arglist)
                {
                    args.Add(list.lhs as Binary);
                    list = (list.rhs as Binary);
                }
                else //??
                {
                    args.Add(list as Binary);
                    list = null;
                }
            }
            args.Reverse();
            return args;
        }

        private Leaf FindArgument (List<Binary> Args, string Name)
        {
            if (Args != null)
                foreach (Binary node in Args)
                {
                    Leaf leaf = node.lhs as Leaf;
                    if (leaf.Name == Name)
                        return node.rhs as Leaf;
                }
            return null;
        }

        private double[] GetArgVector(List<Binary> Args, string Name)
        {
            Leaf leaf = FindArgument(Args, Name);

            if (leaf != null)
                return leaf.Vector;
            else
                return null;
        }

        private double GetArgNumber(List<Binary> Args, string Name, double _default = 0.0)
        {
            Leaf leaf = FindArgument(Args, Name);

            if (leaf != null)
                return leaf.Value;
            else
                return _default;
        }

        private bool GetArgBool(List<Binary> Args, string Name, bool _default = false)
        {
            Leaf leaf = FindArgument(Args, Name);

            if (leaf != null)
                return leaf.Name == "true";
            else
                return _default;
        }

        private CSG EvaluateUnion (Binary list, float alpha, float mix)
        {
            List<CSG> csg = EvaluateBlockList(list);

            if ((csg != null) && (csg.Count > 0))
            {
                CSG result = csg[0];

                for (int j = 1; j < csg.Count; j++)
                    result = result.union(csg[j], alpha, mix);

                return result;
            }
            else
                return null;
        }

        private CSG EvaluateTree(CSGImport.Node node)
        {
            if (node == null)
                return null;
            else if (node is Leaf)
            {
                //??
                return null;
            }
            else if (node is Binary)
            {
                Binary binary = node as Binary;

                switch (node.Tag)
                {
                    case NodeTag.cube:
                        {
                            List<Binary> args = GetArgList(binary.lhs as Binary);
                            double[] size = GetArgVector(args, "size");
                            bool center = GetArgBool(args, "center", false);

                            if (size == null)
                                size = new double[] {1,1,1};
                            Vector3 size_vec = new Vector3(0, 0, 0);
                            size_vec.x = (float)size[0] / 2;
                            size_vec.y = (float)size[1] / 2;
                            size_vec.z = (float)size[2] / 2;

                            Vector3 center_vec = new Vector3(0, 0, 0);
                            if (!center)
                                center_vec = size_vec;

                            return new CSG(CSGShapes.cube(center_vec, size_vec, null));
                        }

                    case NodeTag.sphere:
                        {
                            List<Binary> args = GetArgList(binary.lhs as Binary);
                            double radius = GetArgNumber(args, "r");

                            return new CSG(CSGShapes.sphere(new Vector3(0, 0, 0), (float)radius));
                        }

                    case NodeTag.cylinder:
                        {
                            List<Binary> args = GetArgList(binary.lhs as Binary);
                            double radius1 = GetArgNumber(args, "r1");
                            double radius2 = GetArgNumber(args, "r2");
                            double height = GetArgNumber(args, "h");
                            bool center = GetArgBool(args, "center", false);

                            return new CSG(CSGShapes.cylinder(height, radius1, radius2, center));
                        }

                    case NodeTag.color:
                        {
                            CSG result = EvaluateTree(binary.rhs);

                            Leaf leaf = binary.lhs as Leaf;

                            ColorF color = new ColorF(leaf.Vector[0], leaf.Vector[1], leaf.Vector[2], leaf.Vector[3]);

                            result.SetColor (color);

                            return result;
                        }

                    case NodeTag.multmatrix:
                        {
                            CSG result = EvaluateUnion(binary.rhs as Binary, 0, 0);

                            Leaf leaf = binary.lhs as Leaf;
                            double[][] matrix = leaf.Matrix;

                            //todo : rotations

                            result = result.translate(matrix[0][3], matrix[1][3], matrix[2][3]);

                            return result;
                        }


                    case NodeTag.group:
                    case NodeTag.union:
                        {
                            List<Binary> args = GetArgList(binary.lhs as Binary);

                            float mix = (float)GetArgNumber(args, "mix", 0);
                            float alpha = (float)GetArgNumber(args, "alpha", 0);

                            CSG result = EvaluateUnion(binary.rhs as Binary, alpha, mix);

                            return result;
                        }

                    case NodeTag.difference:
                        {
                            List<Binary> args = GetArgList(binary.lhs as Binary);
                            Binary list = binary.rhs as Binary;

                            float alpha = (float)GetArgNumber(args, "alpha", 0);

                            List<CSG> csg = EvaluateBlockList(list);

                            if ((csg != null) && (csg.Count > 0))
                            {
                                CSG result = csg[0];
                                for (int j = 1; j < csg.Count; j++)
                                    result = result.subtract(csg[j], alpha);
                                return result;
                            }
                            else
                                return null;
                        }

                    case NodeTag.intersect:
                        {
                            List<Binary> args = GetArgList(binary.lhs as Binary);
                            Binary list = binary.rhs as Binary;

                            float alpha = (float)GetArgNumber(args, "alpha", 0);
                            float mix = (float)GetArgNumber(args, "mix", -1);

                            List<CSG> csg= EvaluateBlockList(list);

                            if ((csg != null) && (csg.Count > 0))
                            {
                                CSG result = csg[0];
                                for (int j = 1; j < csg.Count; j++)
                                    result = result.intersect(csg[j], alpha, mix);
                                return result;
                            }
                            else
                                return null;
                        }
                }
            }

            return null;
        }

        private void testToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //GenTest();
            //return;

            //ReadCsg(@"c:\scad_projects\csg\booleans.csg");
            //ReadCsg(@"c:\scad_projects\csg\test2.csg");
            ReadCsg(@"c:\scad_projects\csg\bool_simple.csg");
            return;

            FileBase file = ImportFile(@"c:\temp\csg\std_difference.wrl");

            if (file != null)
            {
                FileProperties fileProperties = new FileProperties();
                fileProperties.FileName = file.FileName;
                // use default properties
                fileProperties.Material = new MaterialProperties();

                Project.fileList.Add(fileProperties);

                //
                ShowMesh();
            }

        }

        // import and add to project files
        private FileBase ImportFile(string FileName)
        {
            FileBase result = null;
            string ErrorMessage;
            //bool LoadedOk = false;
            //string FileExt = Path.GetExtension(FileName);

            result = Importer.ImportFile(FileName, AppSettings.ImportDefaultUnits, out ErrorMessage);

            //if (LoadedOk)
            //{
            //    // *** test
            //    vrmlFile.SaveToFileExt(@"c:\temp\test_out.wrl");
            //}
            //    stepFile.SaveToFile(@"c:\temp\test_out.stp");

            if (result != null)
            {
                Project.Files.Add(result);

//                Document.Modified = true;
//                AddInputMeshNode(result);
                textBox1.AppendText(string.Format("File {0} loaded ok", FileName) + Environment.NewLine);
            }
            else
                textBox1.AppendText(string.Format("Error reading file {0}: {1}", FileName, ErrorMessage) + Environment.NewLine);

            return result;
        }

        private void glControl1_MouseDown(object sender, MouseEventArgs e)
        {
            glView.MouseDown(sender, e);
        }

        private void glControl1_MouseMove(object sender, MouseEventArgs e)
        {
            glView.MouseMove(sender, e);
        }

        private void glControl1_MouseUp(object sender, MouseEventArgs e)
        {
            glView.MouseUp(sender, e);
        }

        //
        private void buttonCompile_Click(object sender, EventArgs e)
        {
            //Test();
            //ReadCsg(@"c:\scad_projects\csg\bool_simple.csg");

            MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(textBox1.Text));

            CompileCsg(ms);
        }

        private void splitContainer1_Panel2_Resize(object sender, EventArgs e)
        {
            if ((glView!=null) && glView.m_init)
                glView.RenderMeshBuffers(MeshBuffers);
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Filename = openFileDialog1.FileName;

                string[] data = File.ReadAllLines(Filename);

                textBox1.Clear();
                textBox1.Lines = data;
            }
        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
        }

        private void selectAllToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            textBox1.SelectAll();
        }

        private void cutToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            textBox1.Cut();
        }

        private void copyToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            textBox1.Copy();
        }

        private void pasteToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            textBox1.Paste();
        }

        private void buttonExport_Click(object sender, EventArgs e)
        {
            if (CurrentCsg != null)
            {
                if (string.IsNullOrEmpty(Filename))
                    Filename = "Untitled.csg";

                // export files
                WriteVrml(Filename, CurrentCsg);
                WriteAmf(Filename, CurrentCsg);
            }
        }

        private void compileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            buttonCompile_Click(sender, e);
        }


        //end

    }
}
