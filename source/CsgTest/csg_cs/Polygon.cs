using System;
using System.Collections.Generic;

namespace ConstructiveSolidGeometry
{
    /// <summary>
    /// Represents a convex polygon. The vertices used to initialize a polygon must
    /// be coplanar and form a convex loop.
    /// 
    /// Each convex polygon has a `shared` property, which is shared between all
    /// polygons that are clones of each other or were split from the same polygon.
    /// This can be used to define per-polygon properties (such as surface color).
    /// </summary>
    public class Polygon
    {
        public IVertex[] vertices;
        //public System.Object shared; // TODO: maybe this should be a Dictionary ??
        public Plane plane;

        public PolygonProperties properties;

        public Polygon(IVertex[] vertices)
        {
            this.vertices = vertices;
            this.plane = Plane.fromPoints(vertices[0].pos, vertices[1].pos, vertices[2].pos);
        }

        public Polygon(IVertex[] vertices, PolygonProperties properties)
        {
            this.vertices = vertices;
            this.properties = properties;
            this.plane = Plane.fromPoints(vertices[0].pos, vertices[1].pos, vertices[2].pos);
        }

        public Polygon(List<IVertex> vertices)
        {
            this.vertices = vertices.ToArray();
            this.plane = Plane.fromPoints(vertices[0].pos, vertices[1].pos, vertices[2].pos);
        }


        public Polygon(List<IVertex> vertices, PolygonProperties properties)
        {
            this.vertices = vertices.ToArray();
            this.properties = properties;
            this.plane = Plane.fromPoints(vertices[0].pos, vertices[1].pos, vertices[2].pos);
        }

        public Polygon clone()
        {
            List<IVertex> vs = new List<IVertex>();
            foreach (IVertex v in this.vertices)
            {
                vs.Add(v.clone());
            }
            return new Polygon(vs.ToArray(), this.properties);
        }

        public void flip()
        {
            Array.Reverse(this.vertices, 0, this.vertices.Length);
            foreach (IVertex v in this.vertices)
            {
                v.flip();
            }
            this.plane.flip();
        }

        public Bounds boundingBox ()
        {
			Vector3 minpoint;
            Vector3 maxpoint;
			var vertices = this.vertices;
			var numvertices = vertices.Length;
			if(numvertices == 0) 
            {
				minpoint = new Vector3(0, 0, 0);
			} else {
				minpoint = vertices[0].pos;
			}

			maxpoint = minpoint;
			for(var i = 1; i < numvertices; i++) {
				var point = vertices[i].pos;
				minpoint = minpoint.min(point);
				maxpoint = maxpoint.max(point);
			}

			//this.cachedBoundingBox = [minpoint, maxpoint];

            return new Bounds(minpoint, maxpoint);
	    }
    }
}