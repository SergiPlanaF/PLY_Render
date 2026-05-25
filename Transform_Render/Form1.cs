using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;
using System.IO;
using System.Globalization;

namespace Transform_Render
{
    public partial class Form1 : Form
    {
        public enum ErrorCode
        {
            No_error = 0,           
            // ... other errors
        }

        struct point
        {
            public float x;
            public float y;
            public float z;
            public int R;
            public int G;
            public int B;
            public int A;
        }

        static point[] PointCloud = null;

        #region Global variables
        static long UpDatePOV = 0;
      
        static int PixelsSizeView = 640 * 480 * 4;//ARGB
        static byte[] PixelsView = new byte[PixelsSizeView];
        static object LockerPixelsView = new object();
        static Bitmap PreVisualitzacio = new Bitmap(640, 480, PixelFormat.Format32bppArgb);
        static BitmapData PreVisualitzacioData;
        static Rectangle Rectangle_PreVisualitzacioData = new Rectangle(0, 0, 640, 480);

        static long Gap_x = 0;
        static long Gap_y = 0;
        static long Gap_z = 0;
        static long Gap_yaw = 0;
        static long Gap_pitch = 0;
        static long Gap_roll = 0;
        static long zoom = 1;
        #endregion

        [DllImport("winmm.dll")]
        internal static extern uint timeBeginPeriod(uint period);

        public Form1()
        {
            InitializeComponent();

            #region Load Values
            Interlocked.Exchange(ref Gap_x, trackBarX.Value);
            Interlocked.Exchange(ref Gap_y, trackBarY.Value);
            Interlocked.Exchange(ref Gap_z, trackBarZ.Value);
            Interlocked.Exchange(ref Gap_yaw, trackBarYaw.Value);
            Interlocked.Exchange(ref Gap_pitch, trackBarPitch.Value);
            Interlocked.Exchange(ref Gap_roll, trackBarRoll.Value);
            Interlocked.Exchange(ref zoom, trackBarZoom.Value);

            labelX.Text = "x: "+ trackBarX.Value.ToString("F2");
            labelY.Text = "y: " + trackBarY.Value.ToString("F2");
            labelZ.Text = "z: " + trackBarZ.Value.ToString("F2");
            float Aux = (float)((trackBarYaw.Value / 10.0) * (Math.PI / 180.0));
            labelYaw.Text = "yaw: " + Aux.ToString("F2");
            Aux = (float)((trackBarPitch.Value / 10.0) * (Math.PI / 180.0));
            labelPitch.Text = "pitch: " + Aux.ToString("F2");
            Aux = (float)((trackBarRoll.Value / 10.0) * (Math.PI / 180.0));
            labelRoll.Text = "roll: " + Aux.ToString("F2");
            labelzoom.Text = "zoom: " + trackBarZoom.Value.ToString("F2");
            #endregion

            timeBeginPeriod(1);
         
            Thread ThreadRender = new Thread(Render);
            ThreadRender.Start();
           
        }

        delegate void Render_to_pictureBox();

        void func_Render_to_pictureBox()
        {
            lock (LockerPixelsView)
            {
                PreVisualitzacioData = PreVisualitzacio.LockBits(Rectangle_PreVisualitzacioData, ImageLockMode.WriteOnly, PreVisualitzacio.PixelFormat);                      
                Marshal.Copy(PixelsView, 0, PreVisualitzacioData.Scan0, PixelsSizeView);
                PreVisualitzacio.UnlockBits(PreVisualitzacioData);

                pictureBox_Image.Image = PreVisualitzacio;
            }            
        }

        //Show Image of loaded Point Cloud  
        void Render()
        {
            Render_to_pictureBox render_to_pictureBox = new Render_to_pictureBox(func_Render_to_pictureBox);

            #region Variables
            float x_t, y_t, z_t;
            float[] Min_z_PointCloud = new float[640 * 480];
            int[] Index_to_PointCloud = new int[640 * 480];
            int i;
            int x, y;
            int i_PointCloud;
            int i_PixelsView;

            float Local_Gap_x = 0;
            float Local_Gap_y = 0;
            float Local_Gap_z = 0;
            float Local_Gap_yaw = 0;
            float Local_Gap_pitch = 0;
            float Local_Gap_roll = 0;
            float Local_zoom = 1;

            float cos_yaw, sin_yaw;
            float cos_pitch, sin_pitch;
            float cos_roll, sin_roll;
            #endregion

            while (true)
            {
                if (Interlocked.Read(ref UpDatePOV) != 0)
                {
                    Interlocked.Exchange(ref UpDatePOV, 0);

                    #region Generate Local Gaps & zoom
                    //Apply Conversion and convert to float
                    Local_Gap_x = Interlocked.Read(ref Gap_x);
                    Local_Gap_y = Interlocked.Read(ref Gap_y);
                    Local_Gap_z = Interlocked.Read(ref Gap_z);
                    Local_Gap_yaw = Interlocked.Read(ref Gap_yaw);
                    Local_Gap_pitch = Interlocked.Read(ref Gap_pitch);
                    Local_Gap_roll = Interlocked.Read(ref Gap_roll);
                    Local_zoom =  Interlocked.Read(ref zoom);

                    Local_Gap_yaw = (float ) ((Local_Gap_yaw / 10.0) * (Math.PI / 180.0));
                    Local_Gap_pitch = (float)((Local_Gap_pitch / 10.0) * (Math.PI / 180.0));
                    Local_Gap_roll = (float)((Local_Gap_roll / 10.0) * (Math.PI / 180.0));                   

                    #endregion

                    #region Generate Precalculation sin / cos 
                    cos_yaw = (float)Math.Cos(Local_Gap_yaw);
                    sin_yaw = (float)Math.Sin(Local_Gap_yaw);
                    cos_pitch = (float)Math.Cos(Local_Gap_pitch);
                    sin_pitch = (float)Math.Sin(Local_Gap_pitch);
                    cos_roll = (float)Math.Cos(Local_Gap_roll);
                    sin_roll = (float)Math.Sin(Local_Gap_roll);
                    #endregion

                    #region Index_to_PointCloud & Min_z_PointCloud Inicialization 
                    //
                    // Index_to_PointCloud Value: -1 -> No Data, Other value: Index to PointCloud
                    //
                    // Min_z_PointCloud Value: Initial Value float.MaxValue
                    // 
                    for (i=0;i< Index_to_PointCloud.Length;i++)
                    {
                        Index_to_PointCloud[i] = -1;
                        Min_z_PointCloud[i] = float.MaxValue;
                    }
                    #endregion

                    #region Generate Index_to_PointCloud from All PointCloud (Min Value of "z"
                    for (i = 0; i < PointCloud.Length; i++)
                    {
                        //Apply rotate, translation
                        x_t = (PointCloud[i].x * (cos_yaw * cos_pitch)) +
                            (PointCloud[i].y * ((cos_yaw * sin_pitch * sin_roll) - (sin_yaw * cos_roll))) +
                            (PointCloud[i].z * ((cos_yaw * sin_pitch * cos_roll) + (sin_yaw * sin_roll)));
                        y_t = (PointCloud[i].x * (sin_yaw * cos_pitch)) +
                            (PointCloud[i].y * ((sin_yaw * sin_pitch * sin_roll) + (cos_yaw * cos_roll))) +
                            (PointCloud[i].z * ((sin_yaw * sin_pitch * cos_roll) - (cos_yaw * sin_roll)));
                        z_t = (PointCloud[i].x * (-sin_pitch)) +
                            (PointCloud[i].y * (cos_pitch * sin_roll)) +
                            (PointCloud[i].z * (cos_pitch * cos_roll));
                    
                        x_t = x_t * Local_zoom;
                        y_t = y_t * Local_zoom;
                        z_t = z_t * Local_zoom;

                        x_t = Local_Gap_x + x_t;
                        y_t = Local_Gap_y + y_t;
                        z_t = Local_Gap_z + z_t;

                        x = (int)x_t;                    
                        y = (int)y_t;

                        if ((x > 0) && (x < 640) &&
                            (y > 0) && (y < 480))
                        {
                            i_PointCloud = x + y * 640;
                            if (z_t < Min_z_PointCloud[i_PointCloud])
                            {
                                Min_z_PointCloud[i_PointCloud] = z_t;
                                Index_to_PointCloud[i_PointCloud] = i;
                            }
                        }
                    }


                    #endregion

                    #region Generate Imatge From Index_to_PointCloud
                    lock (LockerPixelsView)
                    {
                        for (x=0;x<640;x++)
                        {
                            for (y=0;y<480;y++)
                            {
                                i_PointCloud = x + y * 640;
                                i_PixelsView = (x + y * 640) * 4;
                                if (Index_to_PointCloud[i_PointCloud]!=-1)
                                {
                                    PixelsView[i_PixelsView] = (byte)PointCloud[Index_to_PointCloud[i_PointCloud]].B;
                                    PixelsView[i_PixelsView + 1] = (byte)PointCloud[Index_to_PointCloud[i_PointCloud]].G;
                                    PixelsView[i_PixelsView + 2] = (byte)PointCloud[Index_to_PointCloud[i_PointCloud]].R;
                                    PixelsView[i_PixelsView + 3] = 255; // Solid Color 
                                }
                                else
                                {
                                    PixelsView[i_PixelsView] = 0;
                                    PixelsView[i_PixelsView + 1] = 0;
                                    PixelsView[i_PixelsView + 2] = 0;
                                    PixelsView[i_PixelsView + 3] = 0; // Transparent Color 
                                    //Default Color
                                }
                            }
                        }
                    }
                    #endregion

                    this.Invoke(render_to_pictureBox);
                }
                Thread.Sleep(1);
            }
        }

        //LOAD PointCloud from ASCII PLY File
        ErrorCode ReadASCII_PLY(string File)
        {
            #region Variables
            ErrorCode status = ErrorCode.No_error;
            string Line;          
            string[] Split;
            int TotalPointCloud = 0;
            int i;
            point LocalPoint;
            #endregion

            StreamReader sr = new StreamReader(File);
            //Header PLY FILE
            //
            //ply
            //format ascii 1.0
            //element vertex Number of points
            //property float x
            //property float y
            //property float z
            //property uchar red
            //property uchar green
            //property uchar blue
            //end_header
            //

            Line = sr.ReadLine(); //ply
            Line = sr.ReadLine(); //format
            Line = sr.ReadLine(); //element vertex
            Split = Line.Split(' ');
            TotalPointCloud = int.Parse(Split[2]); //element vertex Number of points
            Line = sr.ReadLine(); //property float x
            Line = sr.ReadLine(); //property float y
            Line = sr.ReadLine(); //property float z
            Line = sr.ReadLine(); //property uchar red
            Line = sr.ReadLine(); //property uchar green
            Line = sr.ReadLine(); //property uchar blue
            Line = sr.ReadLine(); //end_header

            PointCloud = new point[TotalPointCloud];

            for (i=0;i< TotalPointCloud;i++)
            {
                Line = sr.ReadLine(); //data
                Split = Line.Split(' ');
                LocalPoint = new point();
                LocalPoint.x = float.Parse(Split[0], CultureInfo.InvariantCulture); 
                LocalPoint.y = float.Parse(Split[1], CultureInfo.InvariantCulture);
                LocalPoint.z = float.Parse(Split[2], CultureInfo.InvariantCulture);                
                LocalPoint.R = int.Parse(Split[3]);
                LocalPoint.G = int.Parse(Split[4]);
                LocalPoint.B = int.Parse(Split[5]);
                LocalPoint.A = 255; //Fixed Opacity
                PointCloud[i] = LocalPoint;
            }
            sr.Close();

            return status;
        }

        //Open new PLY FILE. In case PointCloud is Update without Error send signal to Render function
        private void openFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            openFileDialog.Filter ="PLY File (*.ply) | *.ply";

            // Avoid "ALL files"
            openFileDialog.FilterIndex = 1;
            
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                if (ReadASCII_PLY(openFileDialog.FileName)== ErrorCode.No_error)
                {
                    Interlocked.Exchange(ref UpDatePOV, 1);
                }                
            }         
        }
      
        #region Managa trackbars
        private void trackBarX_Scroll(object sender, EventArgs e)
        {
            Interlocked.Exchange(ref Gap_x, trackBarX.Value);
            Interlocked.Exchange(ref UpDatePOV, 1);

            labelX.Text = "x: " + trackBarX.Value.ToString("F2");
        }

        private void trackBarY_Scroll(object sender, EventArgs e)
        {
            Interlocked.Exchange(ref Gap_y, trackBarY.Value);
            Interlocked.Exchange(ref UpDatePOV, 1);

            labelY.Text = "y: " + trackBarY.Value.ToString("F2");
        }

        private void trackBarZ_Scroll(object sender, EventArgs e)
        {
            Interlocked.Exchange(ref Gap_z, trackBarZ.Value);
            Interlocked.Exchange(ref UpDatePOV, 1);

            labelZ.Text = "z: " + trackBarZ.Value.ToString("F2");
        }

        private void trackBarYaw_Scroll(object sender, EventArgs e)
        {
            Interlocked.Exchange(ref Gap_yaw, trackBarYaw.Value);
            Interlocked.Exchange(ref UpDatePOV, 1);

            float Aux = (float)((trackBarYaw.Value / 10.0) * (Math.PI / 180.0));
            labelYaw.Text = "yaw: " + Aux.ToString("F2");
        }

        private void trackBarPitch_Scroll(object sender, EventArgs e)
        {
            Interlocked.Exchange(ref Gap_pitch, trackBarPitch.Value);
            Interlocked.Exchange(ref UpDatePOV, 1);

            float Aux = (float)((trackBarPitch.Value / 10.0) * (Math.PI / 180.0));
            labelPitch.Text = "pitch: " + Aux.ToString("F2");
        }

        private void trackBarRoll_Scroll(object sender, EventArgs e)
        {
            Interlocked.Exchange(ref Gap_roll, trackBarRoll.Value);
            Interlocked.Exchange(ref UpDatePOV, 1);

            float Aux = (float)((trackBarRoll.Value / 10.0) * (Math.PI / 180.0));
            labelRoll.Text = "roll: " + Aux.ToString("F2");
        }

        private void trackBarZoom_Scroll(object sender, EventArgs e)
        {
            Interlocked.Exchange(ref zoom, trackBarZoom.Value);
            Interlocked.Exchange(ref UpDatePOV, 1);

            labelzoom.Text = "zoom: " + trackBarZoom.Value.ToString("F2");
        }
        #endregion
    }
}
