using System;
using System.Collections.Generic;
using System.Text;

using System.Drawing;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

using HersheyLib;
using HersheyData;

using CadCommon;

namespace OpenGLUtils
{
   
    public class MeshBuffers
    {
        public static bool GlReady = false;
        // capabilities
        public static bool HaveVBOs = false;

        public bool ShowAxes;
        public bool ShowGrid;
        public Vector3 AxisMin;
        public Vector3 AxisMax;

        public UnitsSpecification GridUnits = new UnitsSpecification();

        public Vector3 DisplayScale;


        //public Vector3 Rotation;

        //

        private Vector3[] VertexData;
        private Vector3[] NormalData;
        private int[] ColorData;
        private uint[] IndicesData;

        private int VertexBufferID;
        private int NormalBufferID;
        private int ColorBufferID;
        private int IndicesBufferID;

        private bool BuffersLoaded = false;

        private BeginMode DrawMode;

        private HersheyFont Font;

        public UnitsSpecification DisplayUnits; // readonly

        //private Matrix4 rotationMatrix;

        Color m_axisColor = Color.Black;

        public MeshBuffers()
        {
            VertexData = null;
            NormalData = null;
            ColorData = null;
            IndicesData = null;

            ShowAxes = true;
            ShowGrid = true;
            AxisMin = new Vector3(0, 0, 0);
            AxisMax = new Vector3(10f, 10f, 10f);

            DisplayScale = new Vector3(1, 1, 1);

            Font = new HersheyFont();
            Font.CreateFont(HersheyData.MyFont.Data);

            DisplayUnits = new UnitsSpecification(Units.millimeters, 1);

            GridUnits.Scale = 0.1;
            GridUnits.Units = Units.inch;
            //GridUnits.Scale = 1;
            //GridUnits.Units = Units.millimeters;
        }

        public static bool CheckCapabilities()
        {
            string version = GL.GetString(StringName.Version);
            int major = (int)version[0];
            int minor = (int)version[2];

            if (major <= 1 && minor < 5)
            {
                HaveVBOs = false;
            }
            else
            {
                // OPenGl v 1.5 or later
                HaveVBOs = true;
            }

            // check other miniumum requirements and maybe return false

            GlReady = true;

            return GlReady;
        }

        // list of ColorVertex[3]
        // list of Triangle (ColorVertex[3])
        // list of ColorVertex, Triangle (vector3i)
        // list of Vertex, ColorTriangle (vector3i, color)

        // indexed, color per Vertex
        private void AssignMesh (List<Vector3Ext> Vertices, List<TriangleIndex> Triangles)
        {
            DrawMode = BeginMode.Triangles;

            // convert lists to arrays
            VertexData = new Vector3[Vertices.Count];
            ColorData = new int[Vertices.Count];
            IndicesData = new uint [Triangles.Count * 3];

            int index = 0;
            foreach (Vector3Ext vertex in Vertices)
            {
                VertexData[index] = vertex.Position;
                ColorData[index] = ColorToRgba32(vertex.Attributes.Color);
                index++;
            }

            index = 0;
            foreach (TriangleIndex vector in Triangles)
            {
                IndicesData[index] = (uint)vector.v1;
                IndicesData[index+1] = (uint)vector.v2;
                IndicesData[index+2] = (uint)vector.v3;
                index += 3;
            }

            GenerateBuffers();
        }

        // non-indexed
        private void AssignMesh(List<TriangleExt> Triangles, Color Color)
        {
        }

        // non-indexed
        private void AssignMesh(List<TriangleExt> Triangles)
        {
            DrawMode = BeginMode.Triangles;
            // convert lists to arrays
            VertexData = new Vector3[Triangles.Count*3];
            ColorData = new int[Triangles.Count * 3];
            IndicesData = new uint[Triangles.Count * 3];

            int vertexNum = 0;
            int indexNum = 0;

            foreach (TriangleExt triangle in Triangles)
            {
                VertexData[vertexNum] = triangle.vertex1.Position;
                VertexData[vertexNum + 1] = triangle.vertex2.Position;
                VertexData[vertexNum + 2] = triangle.vertex3.Position;

                ColorData[vertexNum] = ColorToRgba32(triangle.Attributes.Color);
                ColorData[vertexNum+1] = ColorToRgba32(triangle.Attributes.Color);
                ColorData[vertexNum+2] = ColorToRgba32(triangle.Attributes.Color);

                IndicesData[indexNum] = (uint)vertexNum;
                IndicesData[indexNum + 1] = (uint)vertexNum+1;
                IndicesData[indexNum + 2] = (uint)vertexNum+2;

                vertexNum += 3;
                indexNum += 3;
            }

            GenerateBuffers();
        }

        public void AssignMesh(MeshIndexed mesh)
        {
            if ((mesh.Polygons.Count == 0) || (mesh.Vertices.Count == 0))
                return;

            int NumVertex = mesh.Polygons[0].VertexCount;
            if (NumVertex == 3)
                DrawMode = BeginMode.Triangles;
            else
                DrawMode = BeginMode.Quads;

            // convert lists to arrays
            VertexData = new Vector3[mesh.Polygons.Count * NumVertex];
            NormalData = new Vector3[mesh.Polygons.Count  * NumVertex]; // per vertex or face??
            ColorData = new int[mesh.Polygons.Count * NumVertex];
            IndicesData = new uint[mesh.Polygons.Count * NumVertex];

            int vectorNum = 0;
            int indexNum = 0;
            int normalNum = 0;

            Color defaultColor = Color.Silver;
            if (mesh.Attributes.HasColor)
                defaultColor = mesh.Attributes.Color;

            float screenScale = CalculateScale(mesh);
            this.DisplayScale = new Vector3(screenScale, screenScale, screenScale);

            screenScale /= mesh.DisplayScale;
            Vector3 MeshScale = new Vector3(screenScale, screenScale, screenScale);

            foreach (GlPolyIndex poly in mesh.Polygons)
            {
                Vector3 PolyNormal = Normals.CalcNormal(new Vector3[] { 
                    mesh.Vertices[poly.PointIndex[0]].Position, 
                    mesh.Vertices[poly.PointIndex[1]].Position, 
                    mesh.Vertices[poly.PointIndex[2]].Position,
                });


                for (int j = 0; j < NumVertex; j++)
                {
                    Vector3Ext point = mesh.Vertices[poly.PointIndex[j]];
                    VertexData[vectorNum + j] = Vector3.Multiply (point.Position, MeshScale);

                    NormalData[normalNum++] = PolyNormal;

                    // apply color in priority: vertex > polygon > mesh > default
                    if (point.Attributes.HasColor)
                        ColorData[vectorNum + j] = ColorToRgba32(point.Attributes.Color);
                    else if (poly.Attributes.HasColor)
                        ColorData[vectorNum + j] = ColorToRgba32(poly.Attributes.Color);
                    else
                        ColorData[vectorNum + j] = ColorToRgba32(defaultColor);

                    IndicesData[indexNum + j] = (uint)poly.PointIndex[j];
                }

                vectorNum += NumVertex;
                indexNum += NumVertex;
            }

            GenerateBuffers();
        }

        public float CalculateScale(MeshIndexed Mesh)
        {
            Vector3 min;
            Vector3 max;
            Vector3 scale;
            float result;

            Mesh.CalculateExtent(out min, out max);

            scale.X = (max.X - min.X) * Mesh.DisplayScale;
            scale.Y = (max.Y - min.Y) * Mesh.DisplayScale;
            scale.Z = (max.Z - min.Z) * Mesh.DisplayScale;

            // convert into OpenGL space
            scale.X /= 2.0f;
            scale.Y /= 2.0f;
            scale.Z /= 2.0f;

            result = Math.Max(scale.X, scale.Y);
            result = Math.Max(result, scale.Z);
            result = 1.0f / result;

            return result;
        }

        private void GenerateBuffers ()
        {
            // we can't do any operations until GL is flagged as ready!
            if (!GlReady)
                return;
            //
            if (VertexData != null)
            {
                GL.GenBuffers(1, out VertexBufferID);
                GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferID);
                GL.BufferData<Vector3>(BufferTarget.ArrayBuffer,
                                       new IntPtr(VertexData.Length * Vector3.SizeInBytes),
                                       VertexData, BufferUsageHint.StaticDraw);
            }

            if (NormalData != null)
            {
                GL.GenBuffers(1, out NormalBufferID);
                GL.BindBuffer(BufferTarget.ArrayBuffer, NormalBufferID);
                GL.BufferData<Vector3>(BufferTarget.ArrayBuffer,
                                       new IntPtr(NormalData.Length * Vector3.SizeInBytes),
                                       NormalData, BufferUsageHint.StaticDraw);
            }

            if (ColorData != null)
            {
                GL.GenBuffers(1, out ColorBufferID);
                GL.BindBuffer(BufferTarget.ArrayBuffer, ColorBufferID);
                GL.BufferData<int>(BufferTarget.ArrayBuffer,
                                   new IntPtr(ColorData.Length * sizeof(int)),
                                   ColorData, BufferUsageHint.StaticDraw);
            }

            if (IndicesData != null)
            {
                GL.GenBuffers(1, out IndicesBufferID);
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, IndicesBufferID);
                GL.BufferData<uint>(BufferTarget.ElementArrayBuffer,
                                    new IntPtr(IndicesData.Length * sizeof(uint)),
                                    IndicesData, BufferUsageHint.StaticDraw);
            }

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);

            BuffersLoaded = false;
        }

        public static float CalcScale(UnitsSpecification SourceUnits, UnitsSpecification DestUnits)
        {
            float scale = 1.0f;

            switch (SourceUnits.Units)
            {
                case Units.meters:
                    scale = 1000f;
                    break;
                case Units.millimeters:
                    scale = 1f;
                    break;
                case Units.inch:
                    scale = 25.4f;
                    break;
                case Units.feet:
                    scale = 25.4f * 12f;
                    break;
            }
            scale *= (float)SourceUnits.Scale;

            switch (DestUnits.Units)
            {
                case Units.meters:
                    scale /= 1000;
                    break;
                case Units.millimeters:
                    break;
                case Units.inch:
                    scale /= 25.4f;
                    break;
                case Units.feet:
                    scale /= 25.4f * 12f;
                    break;
            }

            scale /= (float)DestUnits.Scale;

            return scale;
        }

        public void ScaledVertex3(float x, float y, float z)
        {
            // display >> 1mm
            // mm >> gl
            //Vector3 UnitsScale = new Vector3();

            float scale = 1.0f;
            switch (DisplayUnits.Units)
            {
                case Units.millimeters:
                    scale = 1.0f;
                    break;
                case Units.inch:
                    scale = (float)(25.4f * DisplayUnits.Scale);
                    break;
            }

            GL.Vertex3(x * scale * DisplayScale.X, y * scale * DisplayScale.Y, z * scale * DisplayScale.Z);
        }

        public void TextOutXY(float x, float y, float z, string s)
        {
            float scale = 1f/20f;
            float offset = 0;

            foreach (Char c in s)
            {
                HersheyChar ch = Font.CharData[(byte)c];

                offset += -ch.MinExtent.X;

                if (ch.Paths.Count != 0)
                {
                    foreach (VectorPath path in ch.Paths)
                    {
                        int p1;
                        for (p1 = 0; p1 < path.Points.Count - 1; p1++)
                        {
                            ScaledVertex3(x + offset*scale + path.Points[p1].X * scale, y + path.Points[p1].Y * scale, z);
                            ScaledVertex3(x + offset*scale + path.Points[p1 + 1].X * scale, y + path.Points[p1 + 1].Y * scale, z);
                        }
                    }
                }

                offset += ch.MaxExtent.X;
            }
        }

        public void XArrow(float x, float y, float z)
        {
            ScaledVertex3(x-0.5f, y-0.5f, z);
            ScaledVertex3(x, y, z);

            ScaledVertex3(x, y, z);
            ScaledVertex3(x - 0.5f, y + 0.5f, z);

            ScaledVertex3(x - 0.5f, y + 0.5f, z);
            ScaledVertex3(x - 0.5f, y - 0.5f, z);
        }

        public void YArrow(float x, float y, float z)
        {
            ScaledVertex3(x - 0.5f, y - 0.5f, z);
            ScaledVertex3(x, y, z);

            ScaledVertex3(x, y, z);
            ScaledVertex3(x + 0.5f, y - 0.5f, z);

            ScaledVertex3(x + 0.5f, y - 0.5f, z);
            ScaledVertex3(x - 0.5f, y - 0.5f, z);
        }

        public void ZArrow(float x, float y, float z)
        {
            ScaledVertex3(x-0.5f, y, z-0.5f);
            ScaledVertex3(x, y, z);

            ScaledVertex3(x, y, z);
            ScaledVertex3(x + 0.5f, y, z - 0.5f);

            ScaledVertex3(x + 0.5f, y, z - 0.5f);
            ScaledVertex3(x - 0.5f, y, z - 0.5f);
        }

        public void DrawBuffers()
        {
            if (VertexBufferID != 0)
            {
                GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferID);
                GL.VertexPointer(3, VertexPointerType.Float, Vector3.SizeInBytes, IntPtr.Zero);
                GL.EnableClientState(ArrayCap.VertexArray);
            }

            if (NormalBufferID != 0)
            {
                GL.BindBuffer(BufferTarget.ArrayBuffer, NormalBufferID);
                GL.NormalPointer(NormalPointerType.Float, 0, IntPtr.Zero);
                GL.EnableClientState(ArrayCap.NormalArray);
            }

            if (ColorBufferID != 0)
            {
                GL.BindBuffer(BufferTarget.ArrayBuffer, ColorBufferID);
                GL.ColorPointer(4, ColorPointerType.UnsignedByte, sizeof(int), IntPtr.Zero);
                GL.EnableClientState(ArrayCap.ColorArray);
            }

            if (IndicesBufferID != 0)
            {
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, IndicesBufferID);
                GL.DrawElements(DrawMode, IndicesData.Length, DrawElementsType.UnsignedInt, IntPtr.Zero);
            }

            // draw axes
            GL.Enable(EnableCap.ColorMaterial);
            GL.Color3(m_axisColor);
            GL.Begin(BeginMode.Lines);
            
            GL.Normal3(0.0f, 0.0f, 1.0f);     // Normal is Z axis

            //
            DisplayUnits = GridUnits;
            // X axis
            GL.Vertex3(0.0f, 0.0f, 0.0f);
            ScaledVertex3(11.0f, 0.0f, 0.0f);
            XArrow(11.0f, 0.0f, 0.0f);

            TextOutXY(12.0f, 0.0f, 0.0f, "X");

            // Y axis - why is pcbnew Y axis drawn negative?

            GL.Vertex3(0.0f, 0.0f, 0.0f);
            ScaledVertex3(0.0f, 11.0f, 0.0f);
            YArrow(0.0f, 11.0f, 0.0f);

            TextOutXY(0.0f, 12.0f, 0.0f, "Y");

            if (ShowGrid)
            {
                // X-Y grid
                for (float x = -10.0f; x <= 10f; x += 1.0f)
                {
                    ScaledVertex3(x, -10f, 0.0f);
                    ScaledVertex3(x, 10f, 0.0f);

                    ScaledVertex3(-10f, x, 0.0f);
                    ScaledVertex3(10f, x, 0.0f);
                }
            }

            // Z axis
            GL.Normal3(0f, -1.0f, 0.0f);

            GL.Vertex3(0.0f, 0.0f, 0.0f);
            ScaledVertex3(0.0f, 0.0f, 11f);

            ZArrow(0.0f, 0.0f, 11.0f);

            //GL.Rotate(-90, 0.8,0,0);

            TextOutXY(0, 0, 12, "Z");

            DisplayUnits = new UnitsSpecification();

            GL.End();
            //

        }


        public static int ColorToRgba32(Color c)
        {
            return (int)((c.A << 24) | (c.B << 16) | (c.G << 8) | c.R);
        }	

    }


}
