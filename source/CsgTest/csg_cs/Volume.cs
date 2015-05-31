using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CadCommon;

// Constructive Solid Geometry (CSG) is a modeling technique that uses Boolean
// operations like union and intersection to combine 3D solids. This library
// implements CSG operations on meshes elegantly and concisely using BSP trees,
// and is meant to serve as an easily understandable implementation of the
// algorithm. All edge cases involving overlapping coplanar polygons in both
// solids are correctly handled.
// 
// Example usage:
// 
//     var cube = CSG.cube();
//     var sphere = CSG.sphere({ radius: 1.3 });
//     var polygons = cube.subtract(sphere).toPolygons();
// 
// ## Implementation Details
// 
// All CSG operations are implemented in terms of two functions, `clipTo()` and
// `invert()`, which remove parts of a BSP tree inside another BSP tree and swap
// solid and empty space, respectively. To find the union of `a` and `b`, we
// want to remove everything in `a` inside `b` and everything in `b` inside `a`,
// then combine polygons from `a` and `b` into one solid:
// 
//     a.clipTo(b);
//     b.clipTo(a);
//     a.build(b.allPolygons());
// 
// The only tricky part is handling overlapping coplanar polygons in both trees.
// The code above keeps both copies, but we need to keep them in one tree and
// remove them in the other tree. To remove them from `b` we can clip the
// inverse of `b` against `a`. The code for union now looks like this:
// 
//     a.clipTo(b);
//     b.clipTo(a);
//     b.invert();
//     b.clipTo(a);
//     b.invert();
//     a.build(b.allPolygons());
// 
// Subtraction and intersection naturally follow from set operations. If
// union is `A | B`, subtraction is `A - B = ~(~A | B)` and intersection is
// `A & B = ~(~A | ~B)` where `~` is the complement operator.
// 
// ## License
// 
// Original ActionScript version copyright (c) 2011 Evan Wallace (http://madebyevan.com/), under the MIT license.
// Ported to C# / Unity by Andrew Perry, 2013.

namespace ConstructiveSolidGeometry
{
    /// <summary>
    /// Two volumes can be combined using the `union()`, `subtract()`, and `intersect()` methods.
    /// </summary>
    public class Volume 
    {
        public List<Polygon> polygons;

        public ColorF Color;

        public int MaterialId;  //??

        //public Bounds cachedBoundingBox

        public Volume()
        {
            this.polygons = new List<Polygon>();
        }

        public Volume (List<Polygon> polygons)
        {
            this.polygons = new List<Polygon>();
            foreach (Polygon p in polygons)
            {
                this.polygons.Add(p.clone());
            }
        }

        public Volume (Polygon[] polygons)
        {
            this.polygons = new List<Polygon>();
            //TODO: Optimize polygons to share vertices?
            this.polygons.AddRange(polygons);
        }

        public void SetColor (ColorF color, bool setSurfaceColors = true)
        {
            Color = color;

            if (setSurfaceColors)
            {
                foreach (Polygon p in this.polygons)
                {
                    if (p.properties == null)
                        p.properties = new PolygonProperties(color);
                    else
                        p.properties.Color = color;
                }
            }
        }

        public Bounds getBounds ()
        {
			var minpoint = new Vector3(0, 0, 0);
			var maxpoint = new Vector3(0, 0, 0);
			var polygons = this.polygons;
			var numpolygons = polygons.Count;
			for(var i = 0; i < numpolygons; i++) 
            {
				var polygon = polygons[i];
				var bounds = polygon.boundingBox();
				if (i == 0) 
                {
					minpoint = bounds.Min;
					maxpoint = bounds.Max;
				} else {
					minpoint = minpoint.min(bounds.Min);
					maxpoint = maxpoint.max(bounds.Max);
				}
			}
			//this.cachedBoundingBox = [minpoint, maxpoint];
		
		    return new Bounds (minpoint, maxpoint);
	    }        
        
        public Volume clone()
        {
            Volume volume = new Volume();

            if (Color != null)
                volume.Color = new ColorF(Color);
            volume.MaterialId = MaterialId;

            foreach (Polygon p in this.polygons)
            {
                volume.polygons.Add(p.clone());
            }
            return volume;
        }

        public Volume inverse()
        {
            Volume volume = this.clone();
            foreach (Polygon p in volume.polygons)
            {
                p.flip();
            }
            return volume;
        }

        public List<Polygon> toPolygons()
        {
            return this.polygons;
        }

        public Volume translate(double x, double y, double z)
        {
            Volume volume = this.clone();

            foreach (Polygon p in volume.polygons)
            {
                foreach (Vertex v in p.vertices)
                {
                    v.pos.x += (float)x;
                    v.pos.y += (float)y;
                    v.pos.z += (float)z;
                }
            }
            return volume;
        }

        /// <summary>
        /// Return a new volume representing space in either this solid or in the
        /// solid `volume`. Neither this solid nor the solid `volume` are modified.
        /// </summary>
        /// <remarks>
        ///    A.union(B)
        ///
        ///    +-------+            +-------+
        ///    |       |            |       |
        ///    |   A   |            |       |
        ///    |    +--+----+   =   |       +----+
        ///    +----+--+    |       +----+       |
        ///         |   B   |            |       |
        ///         |       |            |       |
        ///         +-------+            +-------+
        /// </remarks>
        /// <param name="volume"></param>
        /// <returns></returns>
        public Volume union(Volume volume)
        {
            Node a = new Node(this.polygons);
            Node b = new Node(volume.polygons);

            a.clipTo(b);

            b.clipTo(a);

            b.invert(); //??
            b.clipTo(a);
            b.invert();

            a.build(b.allPolygons());

            Volume result = new Volume(a.allPolygons());
            result.Color = this.Color;
            result.MaterialId = this.MaterialId;

            return result;
        }

        private Volume unionNonOverlapped(Volume volume)
        {
            Node a = new Node(this.polygons);
            Node b = new Node(volume.polygons);

#if xxx
            a.clipTo(b);

            b.clipTo(a);

            b.invert(); //??
            b.clipTo(a);
            b.invert();

            //a.build(b.allPolygons());
#endif
            List<Polygon> polys = a.allPolygons();
            polys.AddRange(b.allPolygons());

            Volume result = new Volume(polys);
            result.Color = this.Color;
            result.MaterialId = this.MaterialId;

            return result;
        }

        public bool mayOverlap(Volume volume)
        {
            if ((this.polygons.Count == 0) || (volume.polygons.Count == 0))
            {
                return false;
            }
            else
            {
                Bounds mybounds = this.getBounds();
                Bounds otherbounds = volume.getBounds();
                // [0].x/y  
                //    +-----+
                //    |     |
                //    |     |
                //    +-----+ 
                //          [1].x/y
                //return false;
                //echo(mybounds,"=",otherbounds);
                if (mybounds.Max.x < otherbounds.Min.x) return false;
                if (mybounds.Min.x > otherbounds.Max.x) return false;
                if (mybounds.Max.y < otherbounds.Min.y) return false;
                if (mybounds.Min.y > otherbounds.Max.y) return false;
                if (mybounds.Max.z < otherbounds.Min.z) return false;
                if (mybounds.Min.z > otherbounds.Max.z) return false;
                return true;
            }
        }

        /// <summary>
        /// Return a new CSG solid representing space in this solid but not in the
        /// solid `csg`. Neither this solid nor the solid `csg` are modified.
        /// </summary>
        /// <remarks>
        /// A.subtract(B)
        ///    +-------+            +-------+
        ///    |       |            |       |
        ///    |   A   |            |       |
        ///    |    +--+----+   =   |    +--+
        ///    +----+--+    |       +----+
        ///         |   B   |
        ///         |       |
        ///         +-------+
        /// </remarks>
        /// <param name="volume"></param>
        /// <param name="alpha">if alpha = 1, (this) takes priority</param>
        /// <returns></returns>
        public Volume subtract(Volume volume, float alpha = 0)
        {
            Node a = new Node(this.polygons);
            Node b = new Node(volume.polygons);

            PolygonProperties props_a = a.polygons[0].properties;
            PolygonProperties props_b = b.polygons[0].properties;
            PolygonProperties blended_props = new PolygonProperties(ColorF.Blend(props_a.Color, props_b.Color, alpha));

            a.invert();
            a.clipTo(b);

            b.clipTo(a);

            b.invert();
            b.clipTo(a);
            b.invert();

            //a.setProperties(props_a);

            a.build(b.getAllWithProperties(blended_props));
            a.invert();

            Volume result = new Volume(a.allPolygons());
            result.Color = this.Color;
            result.MaterialId = this.MaterialId;

            return result;
        }


        /// <summary>
        /// Return a new voulme representing space both in this solid and in the
        /// solid `volume`. Neither this solid nor the solid `volume` are modified.
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
        /// <param name="volume"></param>
        /// <returns>volume of the intersection</returns>
        public Volume intersect(Volume volume, float alpha = 0, float mix = -1)
        {
            Node a = new Node(this.polygons);
            Node b = new Node(volume.polygons);

            PolygonProperties surface_props = new PolygonProperties(ColorF.Blend(this.Color, volume.Color, alpha));
            PolygonProperties interior_props = new PolygonProperties(ColorF.Blend(volume.Color, this.Color, mix));

            a.invert();

            b.clipTo(a);

            b.invert();
            a.clipTo(b);
            b.clipTo(a);
            //

            //!a.setProperties(props_a);
            //b.setProperties(blended_props);

            Node c = new Node();

            if (mix == -1)
                c.build(a.allPolygons());
            else
                c.build(a.getAllWithProperties(interior_props));

            //c.build(a.allPolygons());

            if (mix == -1)
                c.build(b.getAllWithProperties(surface_props));
            else
                c.build(b.getAllWithProperties(interior_props));
            //c.build(b.allPolygons());
            c.invert();

            Volume result = new Volume(c.allPolygons());

            if (mix == -1)
                result.Color = this.Color;
            else
                result.Color = interior_props.Color;

            result.MaterialId = this.MaterialId;

            return result;
        }


        public MeshIndexed GetMesh()
        {
            MeshIndexed result = new MeshIndexed();

            foreach (Polygon poly in this.polygons)
            {
                for (int j = 0; j <= poly.vertices.Length - 3; j++)
                {
                    int a = 0;
                    int b = (j + 1) % poly.vertices.Length;
                    int c = (j + 2) % poly.vertices.Length;

                    if ( (poly.properties!= null) && (poly.properties.Color != null) )
                        result.AddTriangle(new TriangleExt(
                                        new OpenTK.Vector3(poly.vertices[a].pos.x, poly.vertices[a].pos.y, poly.vertices[a].pos.z),
                                        new OpenTK.Vector3(poly.vertices[b].pos.x, poly.vertices[b].pos.y, poly.vertices[b].pos.z),
                                        new OpenTK.Vector3(poly.vertices[c].pos.x, poly.vertices[c].pos.y, poly.vertices[c].pos.z),
                                        poly.properties.Color.ToRGBColor())
                                     );
                    else
                        result.AddTriangle(new TriangleExt(
                                        new OpenTK.Vector3(poly.vertices[a].pos.x, poly.vertices[a].pos.y, poly.vertices[a].pos.z),
                                        new OpenTK.Vector3(poly.vertices[b].pos.x, poly.vertices[b].pos.y, poly.vertices[b].pos.z),
                                        new OpenTK.Vector3(poly.vertices[c].pos.x, poly.vertices[c].pos.y, poly.vertices[c].pos.z))
                                     );
                }
            }


            return result;
        }

    }
}
