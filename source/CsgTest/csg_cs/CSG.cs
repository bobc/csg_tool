using System.Collections;
using System.Collections.Generic;

using System.Drawing;

using CadCommon;


namespace ConstructiveSolidGeometry
{

    public class CSG
    {
        public List<Volume> volumes;

        public List<Material> materials;

        public CSG()
        {
            this.volumes = new List<Volume>();
            materials = new List<Material>();
        }

        public CSG(Volume volume)
        {
            this.volumes = new List<Volume>();
            volumes.Add(volume);

            materials = new List<Material>();
        }

        public CSG clone()
        {
            CSG csg = new CSG();
            foreach (Volume volume in this.volumes)
            {
                csg.volumes.Add(volume.clone());
            }

            foreach (Material mat in this.materials)
                csg.materials.Add(mat.clone());

            return csg;
        }

        public void SetColor(ColorF color)
        {
            foreach (Volume volume in volumes)
            {
                volume.SetColor (color);
            }
        }


        // no needed??
        private CSG inverse()
        {
            CSG csg = new CSG();

            foreach (Volume volume in this.volumes)
            {
                csg.volumes.Add(volume.inverse());
            }
            return csg;
        }

        public CSG translate (double x, double y, double z)
        {
            CSG csg = new CSG();

            foreach (Volume volume in this.volumes)
                csg.volumes.Add (volume.translate(x, y, z));

            return csg;
        }

#if MESH
        public Mesh toMesh()
        {
            List<Polygon> trisFromPolygons = new List<Polygon>();
            // triangulate polygons
            for (int i = this.polygons.Count - 1; i >= 0; i--)
            {
                if (this.polygons[i].vertices.Length > 3)
                {
                    //Debug.Log("!!! Poly to Tri (order): " + this.polygons[i].vertices.Length);
                    for (int vi = 1; vi < this.polygons[i].vertices.Length - 1; vi++)
                    {
                        IVertex[] tri = new IVertex[] {
                            this.polygons[i].vertices[0], 
                            this.polygons[i].vertices[vi], 
                            this.polygons[i].vertices[vi+1]
                        };
                        trisFromPolygons.Add(new Polygon(tri));
                    }
                    // the original polygon is replaced by a set of triangles
                    this.polygons.RemoveAt(i);
                }
            }
            this.polygons.AddRange(trisFromPolygons);

            // TODO: Simplify mesh - the boolean CSG algorithm leaves lots of coplanar
            //       polygons that share an edge that could be simplified.

            // At this point, we have a soup of triangles without regard for shared vertices.
            // We index these to combine any vertices with identical positions & normals 
            // (and maybe later UVs & vertex colors)

            List<Vertex> vertices = new List<Vertex>();
            int[] tris = new int[this.polygons.Count * 3];
            for (int pi = 0; pi < this.polygons.Count; pi++)
            {
                Polygon tri = this.polygons[pi];
                
                if (tri.vertices.Length > 3) Debug.LogError("Polygon should be a triangle, but isn't !!");

                for (int vi = 0; vi < 3; vi++)
                {
                    Vertex vertex = tri.vertices[vi] as Vertex;
                    bool equivalentVertexAlreadyInList = false;
                    for (int i = 0; i < vertices.Count; i++)
                    {
                        if (vertices[i].pos.ApproximatelyEqual(vertex.pos) && 
                            vertices[i].normal.ApproximatelyEqual(vertex.normal))
                        {
                            equivalentVertexAlreadyInList = true;
                            vertex.index = vertices[i].index;
                        }
                    }
                    if (!equivalentVertexAlreadyInList)
                    {
                        vertices.Add(vertex);
                        vertex.index = vertices.Count - 1;
                    }
                    tris[(pi * 3) + vi] = vertex.index;
                }
                //Debug.Log(string.Format("Added tri {0},{1},{2}: {3},{4},{5}", pi, pi+1, pi+2, tris[pi], tris[pi+1], tris[pi+2]));
            }

            Vector3[] verts = new Vector3[this.polygons.Count * 3];
            Vector3[] normals = new Vector3[this.polygons.Count * 3];
            Mesh m = new Mesh();
            for (int i = 0; i < vertices.Count; i++)
            {
                verts[i] = vertices[i].pos;
                normals[i] = vertices[i].normal;
            }
            m.vertices = verts;
            m.normals = normals;
            m.triangles = tris;
            //m.RecalculateBounds();
            //m.RecalculateNormals();
            //m.Optimize();

            //Debug.Log("toMesh verts, normals, tris: " + m.vertices.Length + ", " +m.normals.Length+", "+m.triangles.Length);

            return m;
        }

        public static CSG fromMesh(Mesh m, Transform tf)
        {
            List<Polygon> triangles = new List<Polygon>();
            int[] tris = m.triangles;
            Debug.Log("tris " + tris.Length);
            for (int t = 0; t < tris.Length; t += 3)
            {
                Vertex[] vs = new Vertex[3];
                vs[0] = TranslateVertex(m, tf, tris[t]);
                vs[1] = TranslateVertex(m, tf, tris[t + 1]);
                vs[2] = TranslateVertex(m, tf, tris[t + 2]);
                //Debug.Log("Tri index: " + (t+i).ToString() + ", Vertex: " + vs[i].pos);
                triangles.Add(new Polygon(vs));
            }
            Debug.Log("Poly " + triangles.Count);
            return CSG.fromPolygons(triangles);
        }

        private static Vertex TranslateVertex(Mesh m, Transform tf, int tri)
        {
            Vector3 pos = tf.TransformPoint(m.vertices[tri]);
            Vector3 norm = tf.TransformDirection(m.normals[tri]);

            return new Vertex(pos, norm);
        }
#endif
        /// <summary>
        /// Return a new CSG with all volumes unioned with volumes in 'csg'.
        /// Volumes in this CSG and in 'csg' are not modified.
        /// </summary>
        /// <remarks>
        /// The intersection of overlapping volumes has a composition depending on
        /// the 'mix' parameter.
        /// </remarks>
        /// <param name="csg"></param>
        /// <param name="mix">0 to 1</param>
        /// <returns></returns>
        public CSG union(CSG csg, float alpha=0, float mix=0)
        {
            CSG result = new CSG();

            // a = this
            // b = csg

            // todo: intersections

            // a.clipto(b)
            foreach (Volume a in this.volumes)
            {
                Volume new_vol = a.clone();

                if ( (mix > 0))
                    foreach (Volume b in csg.volumes)
                    {
                        new_vol = new_vol.subtract(b, 1-alpha);

                        if (mix < 1)
                        {
                            Volume clip = a.intersect(b, alpha, mix);
                            if (clip.polygons.Count > 0)
                                result.volumes.Add(clip);
                        }
                    }

                if (new_vol.polygons.Count > 0)
                    result.volumes.Add(new_vol);
            }

            // b.clipto(a)
            
            foreach (Volume b in csg.volumes)
            {
                Volume new_vol = b.clone();

                if (mix < 1)
                    foreach (Volume a in this.volumes)
                    {
                        new_vol = new_vol.subtract(a, 1-alpha);
                    }

                if (new_vol.polygons.Count > 0)
                    result.volumes.Add(new_vol);
            }


            return result;
        }

        // not used
        private static CSG union(CSG csg, Volume volume)
        {
            CSG result = new CSG ();

            foreach (Volume v in csg.volumes)
            {
                bool overlaps = false;

                if (v.mayOverlap(volume))
                {
                    Volume new_vol = v.intersect(volume);
                    if (new_vol.polygons.Count>0)
                    {
                        // real overlap
                        overlaps = true;

                        if ( (v.Color == null) || (volume.Color== null) || v.Color.IsEqual (volume.Color) ) 
                        {
                            // simple union
                            new_vol = v.union (volume);
                            result.volumes.Add(volume);
                        }
                        else
                        {

                        }
                    }
                }

                if (!overlaps)
                    result.volumes.Add(volume);
            }

            return result;
        }

        /// <summary>
        /// Return a new CSG representing volumes in this CSG excluded by volumes
        /// in `csg`. Neither this solid nor the solid `csg` are modified.
        /// </summary>
        /// <param name="csg"></param>
        /// <param name="alpha">if alpha = 1, (this) takes priority</param>
        /// <returns></returns>
        public CSG subtract(CSG csg, float alpha = 0)
        {
            CSG result = new CSG();

            foreach (Volume this_vol in volumes)
            {
                Volume my_vol = this_vol.clone();

                foreach (Volume other_vol in csg.volumes)
                    my_vol = my_vol.subtract(other_vol, alpha);

                result.volumes.Add(my_vol);
            }

            return result;
        }

        /// <summary>
        /// Return a new CSG solid representing space both this solid and in the
        /// solid `csg`. Neither this solid nor the solid `csg` are modified.
        /// </summary>
        /// <remarks>
        /// A.intersect(B)
        /// 
        ///    +-------+
        ///    |       |
        ///    |   A   |
        ///    |    +--+----+   =   +--+
        ///    +----+--+    |       +--+
        ///         |   B   |
        ///         |       |
        ///         +-------+
        /// </remarks>
        /// <param name="csg"></param>
        /// <returns>CSG of the intersection</returns>
        public CSG intersect(CSG csg, float alpha=0, float mix=-1)
        {
            CSG result = new CSG();

            foreach (Volume this_vol in volumes)
            {
                foreach (Volume other_vol in csg.volumes)
                {
                    Volume new_vol = this_vol.clone();
                    new_vol = new_vol.intersect(other_vol, alpha, mix);
                    if (new_vol.polygons.Count > 0)
                        result.volumes.Add(new_vol);
                }
            }

            return result;
        }

#if xxx
        /// <summary>
        /// Construct a CSG solid from a list of `Polygon` instances.
        /// The polygons are cloned
        /// </summary>
        /// <param name="polygons"></param>
        /// <returns></returns>
        public static CSG fromPolygons(List<Polygon> polygons)
        {
            //TODO: Optimize polygons to share vertices
            CSG csg = new CSG();
            foreach (Polygon p in polygons)
            {
                csg.polygons.Add(p.clone());
            }

            return csg;
        }

        /// <summary>
        /// Create CSG from array, does not clone the polygons
        /// </summary>
        /// <param name="polygon"></param>
        /// <returns></returns>
        public static CSG fromPolygons(Polygon[] polygons)
        {
            //TODO: Optimize polygons to share vertices
            CSG csg = new CSG();
            csg.polygons.AddRange(polygons);
            return csg;
        }
#endif
    }

}
