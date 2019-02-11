using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Drawing;
using StrikeSoft.Libraries;

namespace CitySkylineDemo.CS
{
    public static class Demo
    {
        public static Form1[] MainWindow;
        public static ObjectDataCollection ObjectDatas = new ObjectDataCollection();
        public static FrameTools FrameTools = new FrameTools();
        public static bool IsPreview;
        public static bool mEnd;
        private static Random mRand = new Random();

        public const int LayersCount = 3;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
            System.Windows.Forms.Application.EnableVisualStyles();

            Scene.Options.ReadSettings();
            CheckArgs(System.Environment.GetCommandLineArgs());

            //System.Windows.Forms.Application.Run(new Form1());
        }

        private static void CheckArgs(string[] args)
        {
            int previewWindowHandle;
            ScreenSaverHelper.StartupModes startType = ScreenSaverHelper.ParseStartupMode(args, out previewWindowHandle);

            // Determine whether the screen saver should show user definable options.
            if(startType == ScreenSaverHelper.StartupModes.Configure) {
                frmConfig usercnfg = new frmConfig();
                usercnfg.ShowDialog();
                // Determine whether the screen saver should just execute.
            } else if(startType == ScreenSaverHelper.StartupModes.Start) {
                //Check for previous instance.
                if(!ScreenSaverHelper.HasPreviousInstance()) {
                    // Create a Screen Saver form, and then display the form.
                    IsPreview = false;
                    StartScreenSaver();
                }
            } else if(startType == ScreenSaverHelper.StartupModes.Preview) {
                //Check for previous instance.
                if(!ScreenSaverHelper.HasPreviousInstance()) {
                    // Create a Screen Saver form, and then display the form.
                    if(args.Length > 1) {
                        IsPreview = true;
                        StartScreenSaver(previewWindowHandle);
                    }
                }
            }
        } //CheckArgs

        private static void StartScreenSaver(int targetWindow = 0)
        {
            if (targetWindow != 0) {
                MainWindow = new Form1[1];
                MainWindow[0].Show();
                ScreenSaverHelper.SetPreviewWindow(MainWindow[0], (IntPtr)targetWindow);
            } else {
                MainWindow = new Form1[Screen.AllScreens.Length];
                for(int i = 0; i < Screen.AllScreens.Length; i++)
                {
                    MainWindow[i] = new Form1(Screen.AllScreens[i].Bounds);
                    MainWindow[i].Show();
                }
            }
            
            // Create each building type.
            ObjectDatas.Add(new PolygonObjectData("BUILDING", new PointF[] { new PointF(0.0f, 0.0f), new PointF(1.0f,  0.0f), new PointF(1.0f, 1.0f),  new PointF(0.0f, 1.0f) }, Color.Black));
            ObjectDatas.Add(new PolygonObjectData("BUILDING", new PointF[] { new PointF(0.0f, 0.5f), new PointF(0.5f,  0.0f), new PointF(1.0f, 0.5f),  new PointF(1.0f, 1.0f),   new PointF(0.0f, 1.0f) },     Color.Black));
            ObjectDatas.Add(new PolygonObjectData("BUILDING", new PointF[] { new PointF(0.0f, 0.5f), new PointF(0.15f, 0.5f), new PointF(0.15f, 0.0f), new PointF(0.85f, 0.0f),  new PointF(0.85f, 0.5f), new PointF(1.0f, 0.5f), new PointF(1.0f, 1.0f), new PointF(0.0f, 1.0f) }, Color.Black));
            
            // resize scene for only the first window
            Scene.Width = MainWindow[0].DisplayRectangle.Width;
            Scene.ResizeScreen(MainWindow[0].ClientSize.Width, MainWindow[0].ClientSize.Height);
            GenerateScene();

            // Get the weather information.
            Scene.Weather.UpdateLocation(Scene.Options.DefaultLocation);

            try {
                mEnd = false;
                while (!mEnd)
                {
                    for (int i = 0; i < MainWindow.Length; i++)
                    {
                        MainWindow[i].Invalidate(); //force redraw
                    }
                    System.Windows.Forms.Application.DoEvents();
                }
            } catch (Exception ex) {
                //allow to close the application on error.
                MessageBox.Show("An error happened, so sad. This is what happened:" + System.Environment.NewLine + ex.Message);
            }

            Scene.Dispose();
            //for (int i = 0; i < MainWindow.Length; i++)
            //{
            //    MainWindow[i].Dispose();
            //    MainWindow = null;
            //}
            //System.Windows.Forms.Application.Exit();
        } //StartScreenSaver

        public static void StopLoop() { mEnd = true; }

        public static void GenerateScene()
        {
            // Clear all existing layers.
            Scene.Layers.Clear();
            
            // Enumerate the building types.
            List<BaseObjectData> objects = ObjectDatas.EnumByTypeID("BUILDING");
            // If no building types are found then exit.
            if (objects == null || objects.Count == 0) return;

            // Generate each building line for the layer.
            for (int layer = 1; layer <= LayersCount; layer++) {
                GenerateBuildingLine(Scene.Layers.Add(), layer, objects);
            } //layer
        } //AddLayer

        public static void GenerateBuildingLine(LayerItem layer, int layerIndex, List<BaseObjectData> objects)
        {
            float minWidth = 0.01F;
            float maxWidth = 0.07F;
            float minHeight = 0.1F;
            float maxHeight = 0.3F;
            float maxSpacing = 0.02F;

            float lastLeft = 0.0f;
            while (lastLeft < 2.0F) { //Scene.Width
                float randWidth = minWidth + ((float)mRand.NextDouble() * (maxWidth - minWidth));
                float randHeight = minHeight + ((float)mRand.NextDouble() * (maxHeight - minHeight));
                int randObject = mRand.Next(0, objects.Count);
                layer.Objects.Add(new BuildingObjectItem(new ObjectPosition(layerIndex, lastLeft, 1.0F - randHeight, randWidth, randHeight, Alignments.Bottom), ObjectDatas[randObject], "LAYER_" + layerIndex));
                lastLeft += (randWidth + ((float)mRand.NextDouble() * maxSpacing));
            }
        } //GenerateBuildingLine
    }
}
