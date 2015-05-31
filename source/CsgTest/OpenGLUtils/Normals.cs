using System;
using System.Collections.Generic;
using System.Text;


using OpenTK;

namespace OpenGLUtils
{
    public class Normals
    {

        // finds a normal vector and normalizes it
        public static Vector3 CalcNormal (Vector3 [] v)
        {
            Vector3 a, b;
            Vector3 normal;

            // calculate the vectors A and B
            // note that v[3] is defined with counterclockwise winding in mind
            // a
            a.X = (float)(v[0].X - v[1].X);
            a.Y = (float)(v[0].Y - v[1].Y);
            a.Z = (float)(v[0].Z - v[1].Z);
            // b
            b.X = (float)(v[1].X - v[2].X);
            b.Y = (float)(v[1].Y - v[2].Y);
            b.Z = (float)(v[1].Z - v[2].Z);

            // calculate the cross product
            normal.X = (a.Y * b.Z) - (a.Z * b.Y);
            normal.Y = (a.Z * b.X) - (a.X * b.Z);
            normal.Z = (a.X * b.Y) - (a.Y * b.X);

            // normalize
            return Normalize(normal);
        }

        public static Vector3 Normalize (Vector3 v)
        {
            // calculate the length of the vector
            float len = (float)(Math.Sqrt((v.X * v.X) + (v.Y * v.Y) + (v.Z * v.Z)));

            // avoid division by 0
            if (len == 0.0f)
                len = 1.0f;

            // reduce to unit size
            v.X /= len;
            v.Y /= len;
            v.Z /= len;

            return v;
        }



    }
}
