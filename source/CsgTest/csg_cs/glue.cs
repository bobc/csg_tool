using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConstructiveSolidGeometry
{
    public class Vector3
    {
        public float x,y,z;

        public Vector3 (float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public Vector3 clone()
        {
            return new Vector3 (x,y,z);
        }

        public Vector3 negated()
        {
            return new Vector3 (-x,-y,-z);
        }

        public Vector3 plus(Vector3 a)
        {
            return new Vector3(this.x + a.x, this.y + a.y, this.z + a.z);
        }

        public static Vector3 operator + (Vector3 a, Vector3 b)
        {
            return new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
        }

        public static Vector3 operator -(Vector3 a, Vector3 b)
        {
            return new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
        }

        public static Vector3 operator *(Vector3 a, float b)
        {
            return new Vector3(a.x * b, a.y * b, a.z * b);
        }

        public Vector3 minus(Vector3 a)
        {
            return new Vector3(this.x - a.x, this.y - a.y, this.z - a.z);
        }
        public Vector3 times(float a)
        {
            return new Vector3(this.x * a, this.y * a, this.z * a);
        }
        public Vector3 dividedBy(float a)
        {
            return new Vector3(this.x / a, this.y / a, this.z / a);
        }

        public float Dot(Vector3 a)
        {
            return (this.x * a.x + this.y * a.y + this.z * a.z);
        }

        public static float Dot(Vector3 a, Vector3 b)
        {
            return (b.x * a.x + b.y * a.y + b.z * a.z);
        }

        public Vector3 lerp(Vector3 a, float t)
        {
            return plus(a.minus(this).times(t));
        }

        public float length ()
        {
            return (float)Math.Sqrt(this.Dot(this));
        }

        public Vector3 unit()
        {
            return dividedBy (length());
        }

        public Vector3 cross (Vector3 a)
        {
            return new Vector3(
              this.y * a.z - this.z * a.y,
              this.z * a.x - this.x * a.z,
              this.x * a.y - this.y * a.x
              );
        }

        public static Vector3 Cross(Vector3 a, Vector3 b)
        {
            return new Vector3(
              a.y * b.z - a.z * b.y,
              a.z * b.x - a.x * b.z,
              a.x * b.y - a.y * b.x
              );
        }

        public bool ApproximatelyEqual(Vector3 a)
        {
            float epsilon = 0.0001f;
            return (Math.Abs(x - a.x) < epsilon) && (Math.Abs(y - a.y) < epsilon) && (Math.Abs(z - a.z) < epsilon);
        }

        public void Normalize ()
        {
            float len = length();
            x = x / len;
            y = y / len;
            z = z / len;
        }

        public Vector3 min (Vector3 p)
        {
            return new Vector3( Math.Min(this.x, p.x), Math.Min(this.y, p.y), Math.Min(this.z, p.z));
        }

        public Vector3 max(Vector3 p)
        {
            return new Vector3( Math.Max(this.x, p.x), Math.Max(this.y, p.y), Math.Max(this.z, p.z));
        }
    }


    public class Bounds
    {
        public Vector3 Min;
        public Vector3 Max;

        public Bounds (Vector3 Min, Vector3 Max)
        {
            this.Max = Max.clone();
            this.Min = Min.clone();
        }
    }

    public class Debug
    {
        public static void Log (string s)
        {
            Console.WriteLine(s);
        }

        public static void LogError(string s)
        {
            Console.WriteLine("Error: "+ s);
        }

        public static void LogWarning(string s)
        {
            Console.WriteLine("Warning: " + s);
        }

    }


    public class Mesh
    {
        public Vector3[] vertices;
        public Vector3[] normals;

        public List<Polygon> polygons;

        public int[] triangles;
    }

    public class Transform
    {
        public Vector3 TransformPoint (Vector3 a)
        {
            return new Vector3(a.x, a.y, a.z);
        }

        public Vector3 TransformDirection(Vector3 a)
        {
            return new Vector3(a.x, a.y, a.z);
        }
    }


    public class Mathf
    {
        public const float PI = 3.141592653589793238462643383279502884f;

        public static float Cos (float a)
        {
            return (float)Math.Cos (a);
        }

        public static float Sin(float a)
        {
            return (float)Math.Sin(a);
        }
    }
}
