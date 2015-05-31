#region license_info
/*
 * (c) Copyright 1993, 1994, Silicon Graphics, Inc.
 * ALL RIGHTS RESERVED
 * Permission to use, copy, modify, and distribute this software for
 * any purpose and without fee is hereby granted, provided that the above
 * copyright notice appear in all copies and that both the copyright notice
 * and this permission notice appear in supporting documentation, and that
 * the name of Silicon Graphics, Inc. not be used in advertising
 * or publicity pertaining to distribution of the software without specific,
 * written prior permission.
 *
 * THE MATERIAL EMBODIED ON THIS SOFTWARE IS PROVIDED TO YOU "AS-IS"
 * AND WITHOUT WARRANTY OF ANY KIND, EXPRESS, IMPLIED OR OTHERWISE,
 * INCLUDING WITHOUT LIMITATION, ANY WARRANTY OF MERCHANTABILITY OR
 * FITNESS FOR A PARTICULAR PURPOSE.  IN NO EVENT SHALL SILICON
 * GRAPHICS, INC.  BE LIABLE TO YOU OR ANYONE ELSE FOR ANY DIRECT,
 * SPECIAL, INCIDENTAL, INDIRECT OR CONSEQUENTIAL DAMAGES OF ANY
 * KIND, OR ANY DAMAGES WHATSOEVER, INCLUDING WITHOUT LIMITATION,
 * LOSS OF PROFIT, LOSS OF USE, SAVINGS OR REVENUE, OR THE CLAIMS OF
 * THIRD PARTIES, WHETHER OR NOT SILICON GRAPHICS, INC.  HAS BEEN
 * ADVISED OF THE POSSIBILITY OF SUCH LOSS, HOWEVER CAUSED AND ON
 * ANY THEORY OF LIABILITY, ARISING OUT OF OR IN PAD_CONNECTION WITH THE
 * POSSESSION, USE OR PERFORMANCE OF THIS SOFTWARE.
 *
 * US Government Users Restricted Rights
 * Use, duplication, or disclosure by the Government is subject to
 * restrictions set forth in FAR 52.227.19(c)(2) or subparagraph
 * (c)(1)(ii) of the Rights in Technical Data and Computer Software
 * clause at DFARS 252.227-7013 and/or in similar or successor
 * clauses in the FAR or the DOD or NASA FAR Supplement.
 * Unpublished-- rights reserved under the copyright laws of the
 * United States.  Contractor/manufacturer is Silicon Graphics,
 * Inc., 2011 N.  Shoreline Blvd., Mountain View, CA 94039-7311.
 *
 * OpenGL(TM) is a trademark of Silicon Graphics, Inc.
 */
/*
 * Trackball code:
 *
 * Implementation of a virtual trackball.
 * Implemented by Gavin Bell, lots of ideas from Thant Tessman and
 *   the August '88 issue of Siggraph's "Computer Graphics," pp. 121-129.
 *
 * Vector manip code:
 *
 * Original code from:
 * David M. Ciemiewicz, Mark Grossman, Henry Moreton, and Paul Haeberli
 *
 * Much mucking with by:
 * Gavin Bell
 */

/* Port to C#
 * Bob Cousins 2012
 */
#endregion

using System;
using OpenTK;  // Matrix4

namespace OpenGLUtils
{
    public static class Trackball
    {
        /*
         * This size should really be based on the distance from the center of
         * rotation to the point on the object underneath the mouse.  That
         * point would then track the mouse as closely as possible.  This is a
         * simple example, though, so that is left as an Exercise for the
         * Programmer.
         */
        const float TRACKBALLSIZE = 0.8f;

        public static void vzero(ref double [] v)
        {
            v[0] = 0.0;
            v[1] = 0.0;
            v[2] = 0.0;
        }

        public static void vset(ref double[] v, double x, double y, double z)
        {
            v[0] = x;
            v[1] = y;
            v[2] = z;
        }

        public static void vsub(double[] src1, double[] src2, ref double[] dst)
        {
            dst[0] = src1[0] - src2[0];
            dst[1] = src1[1] - src2[1];
            dst[2] = src1[2] - src2[2];
        }

        public static void vcopy(double[] v1, ref double[] v2)
        {
            int i;
            for (i = 0 ; i < 3 ; i++)
                v2[i] = v1[i];
        }

        public static void vcross(double[] v1, double[] v2, ref double[] cross)
        {
            double [] temp = new double[3];

            temp[0] = (v1[1] * v2[2]) - (v1[2] * v2[1]);
            temp[1] = (v1[2] * v2[0]) - (v1[0] * v2[2]);
            temp[2] = (v1[0] * v2[1]) - (v1[1] * v2[0]);

            vcopy(temp, ref cross);
        }

        public static double vlength(double[] v)
        {
            return (double) Math.Sqrt (v[0] * v[0] + v[1] * v[1] + v[2] * v[2]);
        }

        public static void vscale(ref double[] v, double div)
        {
            v[0] *= div;
            v[1] *= div;
            v[2] *= div;
        }

        public static void vnormal(ref double[] v)
        {
            vscale(ref v, 1.0f/vlength(v));
        }

        public static double vdot(double[] v1, double[] v2)
        {
            return v1[0]*v2[0] + v1[1]*v2[1] + v1[2]*v2[2];
        }

        public static void vadd(double[] src1, double[] src2, ref double[] dst)
        {
            dst[0] = src1[0] + src2[0];
            dst[1] = src1[1] + src2[1];
            dst[2] = src1[2] + src2[2];
        }

        /*
         * Pass the x and y coordinates of the last and current positions of
         * the mouse, scaled so they are from (-1.0 ... 1.0).
         *
         * The resulting rotation is returned as a quaternion rotation in the
         * first paramater.
         */

        /*
         * Ok, simulate a track-ball.  Project the points onto the virtual
         * trackball, then figure out the axis of rotation, which is the cross
         * product of P1 P2 and O P1 (O is the center of the ball, 0,0,0)
         * Note:  This is a deformed trackball-- is a trackball in the center,
         * but is deformed into a hyperbolic sheet of rotation away from the
         * center.  This particular function was chosen after trying out
         * several variations.
         *
         * It is assumed that the arguments to this routine are in the range
         * (-1.0 ... 1.0)
         */
        // q[4]
        public static void trackball(ref double[] q, double p1x, double p1y, double p2x, double p2y)
        {
            double [] a = new double [3]; /* Axis of rotation */
            double phi;  /* how much to rotate about axis */
            double [] p1 = new double [3];
            double [] p2 = new double [3]; 
            double [] d = new double [3];
            double t;

            if ( (p1x == p2x) && (p1y == p2y) )
            {
                /* Zero rotation */
                vzero(ref q);
                q[3] = 1.0;
                return;
            }

            /*
             * First, figure out z-coordinates for projection of P1 and P2 to
             * deformed sphere
             */
            vset (ref p1, p1x, p1y, tb_project_to_sphere(TRACKBALLSIZE, p1x, p1y));
            vset (ref p2, p2x, p2y, tb_project_to_sphere(TRACKBALLSIZE, p2x, p2y));

            /*
             *  Now, we want the cross product of P1 and P2
             */
            vcross(p2,p1,ref a);

            /*
             *  Figure out how much to rotate around that axis.
             */
            vsub(p1, p2, ref d);
            t = vlength(d) / (2.0f*TRACKBALLSIZE);

            /*
             * Avoid problems with out-of-control values...
             */
            if (t > 1.0) t = 1.0;
            if (t < -1.0) t = -1.0;
            phi = 2.0f * (double) Math.Asin(t); //TODO: deg?

            axis_to_quat(a, phi, ref q);
        }

        /*
         * This function computes a quaternion based on an axis (defined by
         * the given vector) and an angle about which to rotate.  The angle is
         * expressed in radians.  The result is put into the third argument.
         */
        /*
        *  Given an axis and angle, compute quaternion.
        */
        // a[3], q[4]
        public static void axis_to_quat(double[] a, double phi, ref double[] q)
        {
            vnormal (ref a);
            vcopy (a, ref q);
            vscale(ref q, (double) Math.Sin(phi/2.0));  //TODO: check deg
            q[3] = (double) Math.Cos(phi/2.0);  //TODO: check def
        }

        /*
         * Project an x,y pair onto a sphere of radius r OR a hyperbolic sheet
         * if we are away from the center of the sphere.
         */
        private static double tb_project_to_sphere(double r, double x, double y)
        {
            double d, t, z;

            d = (double) Math.Sqrt(x*x + y*y);
            if (d < r * 0.70710678118654752440) 
            {    
                /* Inside sphere */
                z = (double) Math.Sqrt(r*r - d*d);
            } else 
            {           
                /* On hyperbola */
                t = r / 1.41421356237309504880f;
                z = t*t / d;
            }
            return z;
        }

        /*
         * Given two rotations, e1 and e2, expressed as quaternion rotations,
         * figure out the equivalent single rotation and stuff it into dest.
         *
         * This routine also normalizes the result every RENORMCOUNT times it is
         * called, to keep error from creeping in.
         *
         * NOTE: This routine is written so that q1 or q2 may be the same
         * as dest (or each other).
         */

        private const int RENORMCOUNT = 97;

        private static int count = 0;

        /*
         * Given two quaternions, add them together to get a third quaternion.
         * Adding quaternions to get a compound rotation is analagous to adding
         * translations to get a compound translation.  When incrementally
         * adding rotations, the first argument here should be the new
         * rotation, the second and third the total rotation (which will be
         * over-written with the resulting new total rotation).
         */

        // q1 [4], q2[4], dest[4]
        public static void add_quats(double [] q1, double [] q2, ref double [] dest)
        {
            double [] t1 = new double[4];
            double [] t2 = new double[4];
            double [] t3 = new double[4];
            double [] tf = new double[4];

            vcopy(q1, ref t1);
            vscale(ref t1, q2[3]);

            vcopy(q2, ref t2);
            vscale(ref t2, q1[3]);

            vcross(q2, q1, ref t3);
            vadd(t1, t2, ref tf);
            vadd(t3, tf, ref tf);
            tf[3] = q1[3] * q2[3] - vdot(q1,q2);

            dest[0] = tf[0];
            dest[1] = tf[1];
            dest[2] = tf[2];
            dest[3] = tf[3];

            if (++count > RENORMCOUNT) 
            {
                count = 0;
                normalize_quat(ref dest);
            }
        }

        /*
         * Quaternions always obey:  a^2 + b^2 + c^2 + d^2 = 1.0
         * If they don't add up to 1.0, dividing by their magnitude will
         * renormalize them.
         *
         * Note: See the following for more information on quaternions:
         *
         * - Shoemake, K., Animating rotation with quaternion curves, Computer
         *   Graphics 19, No 3 (Proc. SIGGRAPH'85), 245-254, 1985.
         * - Pletinckx, D., Quaternion calculus as a basic tool in computer
         *   graphics, The Visual Computer 5, 2-13, 1989.
         */
        // q[4]
        private static void normalize_quat (ref double [] q)
        {
            int i;
            double mag;

            mag = (q[0]*q[0] + q[1]*q[1] + q[2]*q[2] + q[3]*q[3]);
            for (i = 0; i < 4; i++) 
                q[i] /= mag;
        }


        /*
        * Build a rotation matrix, given a quaternion rotation.
        *
        */
        public static void build_rotmatrix(ref Matrix4 m, double[] q)
        {
            m.M11 = (float)(1.0 - 2.0 * (q[1] * q[1] + q[2] * q[2]));
            m.M12 = (float)(2.0 * (q[0] * q[1] - q[2] * q[3]));
            m.M13 = (float)(2.0 * (q[2] * q[0] + q[1] * q[3]));
            m.M14 = 0.0f;

            m.M21 = (float)(2.0 * (q[0] * q[1] + q[2] * q[3]));
            m.M22 = (float)(1.0 - 2.0 * (q[2] * q[2] + q[0] * q[0]));
            m.M23 = (float)(2.0 * (q[1] * q[2] - q[0] * q[3]));
            m.M24 = 0.0f;

            m.M31 = (float)(2.0 * (q[2] * q[0] - q[1] * q[3]));
            m.M32 = (float)(2.0 * (q[1] * q[2] + q[0] * q[3]));
            m.M33 = (float)(1.0 - 2.0 * (q[1] * q[1] + q[0] * q[0]));
            m.M34 = 0.0f;

            m.M41 = 0.0f;
            m.M42 = 0.0f;
            m.M43 = 0.0f;
            m.M44 = 1.0f;
        }

    }
}
