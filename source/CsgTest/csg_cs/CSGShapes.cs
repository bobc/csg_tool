using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CadCommon;

namespace ConstructiveSolidGeometry
{
    public static class CSGShapes
    {
        /// <summary>
        /// Cube function
        /// </summary>
        /// <param name="center">world space center of the cube</param>
        /// <param name="radius">size of the cube created at center</param>
        /// <returns></returns>
        public static Volume cube(Vector3 center, Vector3 radius, ColorF color = null)
        {
            //!Vector3 c = center.GetValueOrDefault(Vector3.zero);
            //!Vector3 r = radius.GetValueOrDefault(Vector3.one);

            Vector3 c = center;
            Vector3 r = radius;

            Polygon[] polygons = new Polygon[6];
            int[][][] data = new int[][][] {
                new int[][]{new int[]{0, 4, 6, 2}, new int[]{-1, 0, 0}},
                new int[][]{new int[]{1, 3, 7, 5}, new int[]{1, 0, 0}},
                new int[][]{new int[]{0, 1, 5, 4}, new int[]{0, -1, 0}},
                new int[][]{new int[]{2, 6, 7, 3}, new int[]{0, 1, 0}},
                new int[][]{new int[]{0, 2, 3, 1}, new int[]{0, 0, -1}},
                new int[][]{new int[]{4, 5, 7, 6}, new int[]{0, 0, 1}}
            };

            for (int x = 0; x < 6; x++)
            {
                int[][] v = data[x];


                Vector3 normal = new Vector3((float)v[1][0], (float)v[1][1], (float)v[1][2]);

                IVertex[] verts = new IVertex[4];
                for (int i = 0; i < 4; i++)
                {
                    verts[i] = new Vertex(
                        new Vector3(
                            c.x + (r.x * (2 * (((v[0][i] & 1) > 0) ? 1 : 0) - 1)),
                            c.y + (r.y * (2 * (((v[0][i] & 2) > 0) ? 1 : 0) - 1)),
                            c.z + (r.z * (2 * (((v[0][i] & 4) > 0) ? 1 : 0) - 1))),
                            normal
                        );
                }
                polygons[x] = new Polygon(verts, new PolygonProperties(color));
            }

            return new Volume (polygons);
        }

        private static void makeSphereVertex(ref List<IVertex> vxs, Vector3 center, float r, float theta, float phi)
        {
            theta *= Mathf.PI * 2;
            phi *= Mathf.PI;
            Vector3 dir = new Vector3(Mathf.Cos(theta) * Mathf.Sin(phi),
                                      Mathf.Cos(phi),
                                      Mathf.Sin(theta) * Mathf.Sin(phi)
            );
            Vector3 sdir = dir;
            sdir *= r;
            vxs.Add(new Vertex(center + sdir, dir));
        }

        public static Volume sphere(Vector3 center, float radius = 1, float slices = 16f, float stacks = 8f, ColorF color = null)
        {
            float r = radius;
            List<Polygon> polygons = new List<Polygon>();
            List<IVertex> vertices;

            for (int i = 0; i < slices; i++)
            {
                for (int j = 0; j < stacks; j++)
                {
                    vertices = new List<IVertex>();
                    makeSphereVertex(ref vertices, center, r, i / slices, j / stacks);
                    if (j > 0) makeSphereVertex(ref vertices, center, r, (i + 1) / slices, j / stacks);
                    if (j < stacks - 1) makeSphereVertex(ref vertices, center, r, (i + 1) / slices, (j + 1) / stacks);
                    makeSphereVertex(ref vertices, center, r, i / slices, (j + 1) / stacks);
                    polygons.Add(new Polygon(vertices, new PolygonProperties(color)));
                }
            }
            return new Volume (polygons);
        }


        private static Vertex point (Vector3 s, Vector3 ray, double r, Vector3 axisX, Vector3 axisY, Vector3 axisZ, int stack, double slice, int normalBlend) 
        {
            double angle = slice * Math.PI * 2;
            Vector3 outp = axisX.times((float)Math.Cos(angle)).plus(axisY.times((float)Math.Sin(angle)));

            Vector3 pos = s.plus(ray.times(stack)).plus(outp.times((float)r));
            Vector3 normal = outp.times(1 - Math.Abs(normalBlend)).plus(axisZ.times(normalBlend));

            return new Vertex(pos, normal);
        }

        // Construct a solid cylinder. 
        // 
        public static Volume cylinder (double height, double radius1, double radius2, bool center)
        {
            List<Polygon> polygons = new List<Polygon>();

            var s = new Vector3(0, 0, 0);
            var e = new Vector3(0, 0, (float)height);

            Vector3 ray = e.minus(s);
            double r = radius1;
            int slices = 30;
            Vector3 axisZ = ray.unit();
            bool isY = (Math.Abs(axisZ.y) > 0.5);
            Vector3 axisX = new Vector3(0, 1, 0).cross(axisZ).unit();
            Vector3 axisY = axisX.cross(axisZ).unit();
            Vertex start = new Vertex(s, axisZ.negated());
            Vertex end = new Vertex(e, axisZ.unit());

            for (var i = 0; i < slices; i++)
            {
                double t0 = (double)i / slices;
                double t1 = ((double)i + 1) / slices;

                polygons.Add(new Polygon(new Vertex[] {
                    start, 
                    point(s,ray,radius1, axisX, axisY, axisZ, 0, t0, -1), 
                    point(s,ray,radius1, axisX, axisY, axisZ, 0, t1, -1)},
                    new PolygonProperties(null)));

                polygons.Add(new Polygon(new Vertex[] {
                    point(s,ray,radius1, axisX, axisY, axisZ, 0, t1, 0), 
                    point(s,ray,radius1, axisX, axisY, axisZ, 0, t0, 0), 
                    point(s,ray,radius1, axisX, axisY, axisZ, 1, t0, 0), 
                    point(s,ray,radius1, axisX, axisY, axisZ, 1, t1, 0)},
                    new PolygonProperties(null)));

                polygons.Add(new Polygon(new Vertex[] {
                    end, 
                    point(s,ray,radius1, axisX, axisY, axisZ, 1, t1, 1), 
                    point(s,ray,radius1, axisX, axisY, axisZ, 1, t0, 1)},
                    new PolygonProperties(null)));
            }

            return new Volume(polygons);
        }

    }
}
