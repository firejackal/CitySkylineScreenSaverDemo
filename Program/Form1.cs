using System;
using System.Drawing;
using System.Windows.Forms;

namespace CitySkylineDemo.CS
{
    public partial class Form1 : Form
    {
        private const int MouseMovementDelta = 5;
        private int mOldMouseX, mOldMouseY;
        private WindowsLib.GDIPlusHelper Graphics;

        //private float mDelta;

        public Form1() { InitializeComponent(); }

        public Form1(Rectangle Bounds) { InitializeComponent(); this.Bounds = Bounds; }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.Text = "Screen Saver"; //My.Application.Info.Title;
            this.SetStyle(ControlStyles.Opaque | ControlStyles.UserPaint, true);
            this.Graphics = new WindowsLib.GDIPlusHelper(this, 0, 0);
            this.Graphics.Display.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            if (!Demo.IsPreview) {
                //SystemParametersInfo(SPI_SCREENSAVERRUNNING, 1&, 0&, 0&);
                Cursor.Hide();
                TopMost = true;
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if(!Demo.IsPreview) {
                Cursor.Show();
                //SystemParametersInfo(SPI_SCREENSAVERRUNNING, 0&, 0&, 0&);
            }

            Demo.StopLoop();
            //Demo.StopScreenSaver();
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if(!Demo.IsPreview) {
                if (e.KeyCode == Keys.Space) {
                    Demo.GenerateScene();
                } else if(e.KeyCode == Keys.N) {
                    Scene.Weather.UpdateWeather("");
                } else if(e.KeyCode == Keys.P) {
                    //Graphics.SaveScreenshot(System.IO.Path.Combine(My.Application.Info.DirectoryPath, "Screenshot.png"));
                } else if(e.KeyCode == Keys.R) {
                    Scene.Weather.UpdateWeather("rain");
                } else if(e.KeyCode == Keys.S) {
                    Scene.Weather.UpdateWeather("snow");
                } else {
                    this.Close();
                }
            }
        }

        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            if(!Demo.IsPreview) {
                // Determines whether the mouse was moved and whether the movement was large.
                // If so, the screen saver is ended.
                if((this.mOldMouseX > 0 && this.mOldMouseY > 0) && (Math.Abs(e.X - this.mOldMouseX) > MouseMovementDelta || Math.Abs(e.Y - this.mOldMouseY) > MouseMovementDelta)) {
                    this.Close();
                }

                // Assigns the current X and Y locations to OldX and OldY.
                this.mOldMouseX = e.X;
                this.mOldMouseY = e.Y;
            }
        }

        private void Form1_Paint(Object sender, PaintEventArgs e)
        {
            float gtDelta;
            gtDelta = Demo.FrameTools.CalcDelta();

            Scene.Update(gtDelta);
            Scene.DrawTo(this.Graphics.Display);

            this.Graphics.FlipToWindow();

            //force refresh
            //this.Invalidate();
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            // Update the scene size.
            Scene.ResizeScreen(this.ClientSize.Width, this.ClientSize.Height);
            if(this.Graphics != null) this.Graphics.ResizeDisplay(0, 0);
        }
    }
}
