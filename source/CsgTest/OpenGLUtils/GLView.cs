using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Drawing;
using System.Windows.Forms;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace OpenGLUtils
{
    public class GLView
    {
        public bool m_init = false;

        double g_Draw3d_dx;
        double g_Draw3d_dy;
        double m_Beginx;
        double m_Beginy;          /* position of mouse */
        double[] m_Quat = new double[4] { 0, 0, 0, 1 };    /* orientation of object */
        double[] m_Rot = new double[4] { 0, 0, 0, 0 };     /* man rotation of object */
        double m_Zoom = 0.8;                      /* field of view in degrees? */
        Color m_BgColor = Color.White;
        Color m_axisColor = Color.Black;

        float ZBottom = 1.0f;
        float ZTop = 10.0f;
        bool m_Dragging = false;

        GLControl glControl;
        MeshBuffers MeshBuffers;


        public GLView (GLControl GlControl)
        {
            this.glControl = GlControl;
        }


        bool ModeIsOrtho()
        {
            return false;
        }

        public void InitGL(int width, int height)
        {
            //wxSize size = GetClientSize();

            if (!m_init)
            {
                m_init = true;
                //m_Zoom = 0.8;
                ZBottom = 1.0f;
                //ZTop = 10.0f;
                ZTop = 40.0f;

                GL.Disable(EnableCap.CullFace);
                GL.Enable(EnableCap.DepthTest);

                GL.Enable(EnableCap.LineSmooth);
                GL.Enable(EnableCap.ColorMaterial);
                GL.ColorMaterial(MaterialFace.FrontAndBack, ColorMaterialParameter.AmbientAndDiffuse);

                //GL.Material(MaterialFace.FrontAndBack, MaterialParameter.Specular, new Color4(1, 1, 1, 0));
                //GL.Material(MaterialFace.FrontAndBack, MaterialParameter.Emission, new Color4(0, 0, 0, 1));

                GL.Enable(EnableCap.PolygonSmooth); // ***

                /* speedups */
                GL.Enable(EnableCap.Dither);
                GL.ShadeModel(ShadingModel.Flat);
                GL.Hint(HintTarget.PerspectiveCorrectionHint, HintMode.Nicest); // ***
                GL.Hint(HintTarget.PolygonSmoothHint, HintMode.Nicest); // ***

                /* blend */
                GL.Enable(EnableCap.Blend);
                GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);  // ***
                //GL.BlendFunc(BlendingFactorSrc.SrcAlphaSaturate, BlendingFactorDest.One);
            }

            // set viewing projection

            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();

            float MAX_VIEW_ANGLE = 160.0f / 45.0f;

            if (m_Zoom > MAX_VIEW_ANGLE)
                m_Zoom = MAX_VIEW_ANGLE;

            if (ModeIsOrtho())
            {
                // OrthoReductionFactor is chosen so as to provide roughly the same size as
                // Perspective View
                double orthoReductionFactor = 40f / (double)m_Zoom;

                // Initialize Projection Matrix for Ortographic View
                GL.Ortho(-width / orthoReductionFactor, width / orthoReductionFactor,
                         -height / orthoReductionFactor, height / orthoReductionFactor,
                         1, 100);
            }
            else
            {
                // Ratio width / height of the window display
                double ratio_HV = (double)width / height;

                // Initialize Projection Matrix for Perspective View
                //gluPerspective( 45.0 * g_Parm_3D_Visu.m_Zoom, ratio_HV, 1, 10 );

                Matrix4 perspective = Matrix4.CreatePerspectiveFieldOfView((float)Math.PI / 180.0f * 45.0f * (float)m_Zoom, (float)ratio_HV, 1, 100);
                //Matrix4 perspective = Matrix4.CreatePerspectiveFieldOfView((float)Math.PI / 4f, (float)ratio_HV, 1, 10);

                GL.LoadMatrix(ref perspective);
            }

            // position viewer
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();
            GL.Translate(0.0F, 0.0F, -(ZBottom + ZTop) / 2);

            // clear color and depth buffers
            GL.ClearColor(m_BgColor);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            // Setup light sources:
            SetLights();

            //CheckGLError();
        }

        Color4 MakeLight(float intensity, float alpha)
        {
            return new Color4(intensity, intensity, intensity, alpha);
        }

        float[] MakeLight4f(float intensity, float alpha)
        {
            return new float[] { intensity, intensity, intensity, alpha };
        }

        void SetLights()
        {
            //            float  light;
            //            Color4 light_color;
            float[] Z_axis_pos = new float[] { 0.0f, 0.0f, 10.0f, 1f };
            float[] lowZ_axis_pos = new float[] { 0.0f, 0.0f, -10.0f, 1f };
            float[] light0_pos = new float[] { 1.0f, 1.0f, 0.0f, 1.0f };

            // set up Light 0
            //            GL.Light (LightName.Light0, LightParameter.Position, light0_pos);

            //            GL.Light(LightName.Light0, LightParameter.Ambient, MakeLight(0.0f, 1));
            //            GL.Light(LightName.Light0, LightParameter.Diffuse, MakeLight(1f, 1));
            //            GL.Light(LightName.Light0, LightParameter.Specular, MakeLight(1f, 1));

            // set up Light 1
            GL.Light(LightName.Light1, LightParameter.Position, lowZ_axis_pos);
            GL.Light(LightName.Light1, LightParameter.Diffuse, MakeLight(0.3f, 1));

            //
            GL.LightModel(LightModelParameter.LightModelAmbient, MakeLight4f(0.2f, 1.0f));
            //GL.ShadeModel(ShadingModel.Smooth);

            GL.Enable(EnableCap.Lighting);
            GL.Enable(EnableCap.Light0);
            //            GL.Enable (EnableCap.Light1);
        }

        public void RenderMeshBuffers(MeshBuffers MeshBuffers)
        {
            if (!MeshBuffers.GlReady || (MeshBuffers == null))
                return;

            this.MeshBuffers = MeshBuffers;

            GL.Viewport(0, 0, glControl.Width, glControl.Height);

            InitGL(glControl.Width, glControl.Height);

            GL.MatrixMode(MatrixMode.Modelview);

            // adjust view
            Matrix4 mat = new Matrix4(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

            GL.Translate(g_Draw3d_dx, g_Draw3d_dy, 0);

            Trackball.build_rotmatrix(ref mat, m_Quat);
            GL.MultMatrix(ref mat);

            GL.Rotate(m_Rot[0], 1, 0, 0);
            GL.Rotate(m_Rot[1], 0, 1, 0);
            GL.Rotate(m_Rot[2], 0, 0, 1);

            MeshBuffers.DrawBuffers();

            glControl.SwapBuffers();
        }


        // gui events
        public void MouseWheel(object sender, MouseEventArgs e)
        {
            float factor = 1.04f;

            if (e.Button == System.Windows.Forms.MouseButtons.None)
            {
                if (e.Delta > 0)
                {
                    m_Zoom /= factor;

                    if (m_Zoom <= 0.01)
                        m_Zoom = 0.01;
                }
                else
                    m_Zoom *= factor;

                RenderMeshBuffers(MeshBuffers);
            }

            m_Beginx = e.X;
            m_Beginy = e.Y;
        }

        public void MouseMove(object sender, MouseEventArgs e)
        {
            // wxSize size( GetClientSize() );
            Size size = glControl.Size;
            double[] spin_quat = new double[4];

            Vector2d last = new Vector2d((2.0 * m_Beginx - size.Width) / size.Width, (size.Height - 2.0 * m_Beginy) / size.Height);
            Vector2d current = new Vector2d((2.0 * e.X - size.Width) / size.Width, (size.Height - 2.0 * e.Y) / size.Height);

            //toolStripStatusLabel1.Text = string.Format("({0:g3},{1:g3}) - ({2:g3},{3:g3})", last.X, last.Y, current.X, current.Y);

            if (m_Dragging)
            {
                if (e.Button == System.Windows.Forms.MouseButtons.Left)
                {
                    /* drag in progress, simulate trackball */

                    Trackball.trackball(ref spin_quat, last.X, last.Y, current.X, current.Y);
                    Trackball.add_quats(spin_quat, m_Quat, ref m_Quat);
                }
                else if (e.Button == System.Windows.Forms.MouseButtons.Middle)
                {
                    /* middle button drag -> pan */

                    /* Current zoom and an additional factor are taken into account
                    * for the amount of panning. */

                    double PAN_FACTOR = 8.0 * m_Zoom;
                    g_Draw3d_dx -= PAN_FACTOR * (m_Beginx - e.X) / size.Width;
                    g_Draw3d_dy -= PAN_FACTOR * (e.Y - m_Beginy) / size.Height;
                }

                /* orientation has changed, redraw mesh */
                RenderMeshBuffers(MeshBuffers);
            }

            m_Beginx = e.X;
            m_Beginy = e.Y;

        }

        public void MouseDown(object sender, MouseEventArgs e)
        {
            if ((e.Button == System.Windows.Forms.MouseButtons.Left) ||
                (e.Button == System.Windows.Forms.MouseButtons.Middle))
            {
                m_Dragging = true;
                m_Beginx = e.X;
                m_Beginy = e.Y;
            }
            else
                m_Dragging = false;
        }

        public void MouseUp(object sender, MouseEventArgs e)
        {
            m_Dragging = false;
        }



    }
}
