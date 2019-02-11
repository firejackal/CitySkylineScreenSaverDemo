using System;
using System.Threading;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace CitySkylineDemo.CS
{
    public enum Alignments { Top, Bottom }

    /// <summary>Holds data about the scene.</summary>
    public static class Scene
    {
        public static OptionsManager Options = new OptionsManager();
        public static LayersCollection Layers = new LayersCollection();
        public static Weather Weather = new Weather(new DummyWeatherData());
        public static int Width;
        private static int mScreenWidth;
        private static int mScreenHeight;
        public static float ScreenRatio;
        public static Random Rand = new Random();

        public static void DrawTo(Graphics canvas)
        {
            // Clear the screen.
            canvas.Clear(GetBackgroundColor());

            // Draw each layer.
            if (Layers.Count > 0)
            {
                // Draw the weather effects.
                Weather.DrawTo(canvas, 0);

                for (int layerIndex = 0; layerIndex < Layers.Count; layerIndex++)
                {
                    // Draw the layer.
                    Layers[layerIndex].DrawTo(canvas);
                    // Draw the weather effects.
                    Weather.DrawTo(canvas, 1 + layerIndex);
                } //layerIndex
            }
            // Draw the weather effects.
            //Weather.DrawTo(canvas)
        } //DrawTo

        public static Color GetColor(string colorID)
        {
            if (colorID.Equals("SUN", StringComparison.CurrentCultureIgnoreCase))
            {
                return Weather.ComputeSunColor();
            } else if (colorID.StartsWith("LAYER_", StringComparison.CurrentCultureIgnoreCase))
            {
                if (Layers.Count > 0)
                {
                    for (int layer = 1; layer <= Layers.Count; layer++)
                    {
                        if (colorID.Equals("LAYER_" + layer, StringComparison.CurrentCultureIgnoreCase))
                        {
                            return GetLayerColor(layer, Layers.Count);
                        }
                    } //layer
                }
            }

            return GetLayerFrontColor();
        } //GetColor

        public static Color GetBackgroundColor() { return Weather.ComputeSceneColor(); }

        public static Color GetLayerFrontColor() { return Color.FromArgb(0, 0, 0); }

        public static Color GetLayerBackColor() { return Blend(GetLayerFrontColor(), GetBackgroundColor(), 0.8F); }

        public static Color GetLayerColor(int layerIndex, int layerCount)
        {
            return Blend(GetLayerBackColor(), GetLayerFrontColor(), ((float)layerIndex / (float)layerCount));
        } //GetLayerColor

        public static Color GetCloudColor(int layerIndex, int layerCount)
        {
            return Color.FromArgb(Options.CloudMinAlpha + ((Options.CloudMaxAlpha - Options.CloudMinAlpha) * ((layerCount - layerIndex) / layerCount)), Options.CloudColor.R, Options.CloudColor.G, Options.CloudColor.B);
            //return Blend(GetLayerBackColor(), Options.CloudColor, (layerIndex / layerCount));
        } //GetCloudColor

        public static float ComputeScale(int layer)
        {
            if (layer > 0)
                return Options.MinScale + ((layer / Layers.Count) * (Options.MaxScale - Options.MinScale));
            else
                return 1.0F;
        } //ComputeScale

        public static void TransformToScreenSpace(PointF position, SizeF size, int layer, Alignments alignment, out PointF outNewPosition, out SizeF outNewSize)
        {
            TransformToScreenSpace(position.X, position.Y, size.Width, size.Height, layer, alignment, out outNewPosition, out outNewSize);
        } //TransformToScreenSpace

        public static void TransformToScreenSpace(float left, float top, float width, float height, int layer, Alignments alignment, out PointF outNewPosition, out SizeF outNewSize)
        {
            // First step is to compute the scale.
            float scale = ComputeScale(layer);

            // return the adjusted size.
            //ScreenRatio
            outNewSize = new SizeF();
            outNewSize.Width = ((width * ScreenWidth) * scale);
            outNewSize.Height = ((height * ScreenHeight) * scale);

            // return the adjusted position.
            outNewPosition = new PointF();
            outNewPosition.X = ((left * scale)) * ScreenWidth;
            if (alignment == Alignments.Top)
                outNewPosition.Y = ((top * scale)) * ScreenHeight;
            else if (alignment == Alignments.Bottom)
            {
                outNewPosition.Y = ((top * scale) * ScreenHeight);
                outNewPosition.Y += ((top + height) * ScreenHeight) - (((top * scale) * ScreenHeight) + outNewSize.Height);
            }
        } //TransformToScreenSpace

        public static void TransformToWorldSpace(float left, float top, float width, float height, int layer, Alignments alignment, out PointF outNewPosition, out SizeF outNewSize)
        {
            // First step is to compute the scale.
            float scale = ComputeScale(layer);

            // return the adjusted size.
            //ScreenRatio
            outNewSize = new SizeF();
            outNewSize.Width = (width / scale) / (float)ScreenWidth;
            outNewSize.Height = (height / scale) / (float)ScreenHeight;

            // return the adjusted position.
            outNewPosition = new PointF();
            outNewPosition.X = (left / (float)ScreenWidth) / scale;
            if (alignment == Alignments.Top)
                outNewPosition.Y = (top / (float)ScreenHeight) / scale;
            else if (alignment == Alignments.Bottom)
            {
                outNewPosition.Y = 0 - (top / (float)ScreenHeight) / scale;
                //TODO: Fix:
                outNewPosition.Y += ((top - height) / (float)ScreenHeight) + (((top / scale) / (float)ScreenHeight) - outNewSize.Height);
            }
        } //TransformToScreenSpace

        public static int ScreenWidth { get { return mScreenWidth; } }

        public static int ScreenHeight { get { return mScreenHeight; } }

        public static void ResizeScreen(int width, int height)
        {
            mScreenWidth = width;
            mScreenHeight = height;
            ScreenRatio = (width == 0 ? 0.0F : (float)height / (float)width);

            Layers.OnSceneResized();
            Weather.OnSceneResized();
        } //Resize

        public static void Update(float deltaTime) { Weather.Update(deltaTime); }

        private static Color Blend(Color a, Color b, float percent)
        {
            if (percent <= 0.0f) return a;
            if (percent >= 1.0f) return b;

            return System.Drawing.Color.FromArgb(
             (int)((b.R * percent) + (a.R * (1.0f - percent))),
             (int)((b.G * percent) + (a.G * (1.0f - percent))),
             (int)((b.B * percent) + (a.B * (1.0f - percent))));
        } //Blend

        public static void Dispose() { Weather.Dispose(); }
    } //Scene

    public struct ObjectPosition
    {
        public int Layer;
        public Alignments Alignment;
        private PointF mPos;
        private SizeF mSize;
        private PointF mTransPos;
        private SizeF mTransSize;

        public ObjectPosition(int layer, float left, float top, float width, float height, Alignments alignment)
        {
            this.Layer = layer;
            this.Alignment = alignment;
            this.mPos = new PointF(left, top);
            this.mSize = new SizeF(width, height);
            this.mTransPos = PointF.Empty;
            this.mTransSize = SizeF.Empty;
            this.UpdateTransformedPosition();
        } //Constructor

        public PointF Position {
            get { return this.mPos; }
            set { this.mPos = value; this.UpdateTransformedPosition(); }
        } //Position

        public SizeF Size {
            get { return this.mSize; }
            set { this.mSize = value; this.UpdateTransformedPosition(); }
        } //Value

        public void UpdateTransformedPosition()
        {
            Scene.TransformToScreenSpace(this.mPos, this.mSize, this.Layer, this.Alignment, out this.mTransPos, out this.mTransSize);
        } //UpdateTransformedPosition

        public RectangleF ComputeTransformedPosition(float newX, float newY)
        {
            PointF pos; SizeF size;
            Scene.TransformToScreenSpace(newX, newY, this.mSize.Width, this.mSize.Height, this.Layer, this.Alignment, out pos, out size);
            return new RectangleF(pos, size);
        } //UpdateTransformedPosition

        public PointF GetTransformedPosition() { return this.mTransPos; }

        public SizeF GetTransformedSize() { return this.mTransSize; }
    } //UntransformedPosition

    public class LayersCollection : List<LayerItem>
    {
        public LayerItem Add()
        {
            base.Add(new LayerItem());
            return base[base.Count - 1];
        } //Add

        //public void DrawTo(Graphics canvas)
        //    if(base.Count > 0) {
        //        for(int index = 0; index < base.Count; index++) {
        //            this[index].DrawTo(canvas);
        //        } //index
        //    }
        //} //DrawTo

        public void OnSceneResized()
        {
            if (base.Count > 0)
            {
                for (int index = 0; index < base.Count; index++)
                {
                    this[index].OnSceneResized();
                } //index
            }
        } //OnSceneResized
    } //LayersCollection

    public class LayerItem
    {
        public ObjectsCollection Objects = new ObjectsCollection();

        public void DrawTo(Graphics canvas) { this.Objects.DrawTo(canvas); }

        public void OnSceneResized() { this.Objects.OnSceneResized(); }
    } //LayerItem

    public class ObjectsCollection : List<ObjectItem>
    {
        public ObjectItem Add(ObjectPosition position, BaseObjectData data)
        {
            this.Add(new ObjectItem(position, data));
            return base[base.Count - 1];
        } //Add

        public ObjectItem Add(ObjectPosition position, BaseObjectData data, Color color)
        {
            this.Add(new ObjectItem(position, data, color));
            return base[base.Count - 1];
        } //Add

        public ObjectItem Add(ObjectPosition position, BaseObjectData data, string colorID)
        {
            this.Add(new ObjectItem(position, data, colorID));
            return base[base.Count - 1];
        } //Add

        public void DrawTo(Graphics canvas)
        {
            if (base.Count > 0)
            {
                for (int index = 0; index < base.Count; index++)
                {
                    base[index].DrawTo(canvas);
                } //index
            }
        } //DrawTo

        public void DrawTo(Graphics canvas, int layer)
        {
            if (base.Count > 0)
            {
                for (int index = 0; index < base.Count; index++)
                {
                    if (base[index].Position.Layer == layer) base[index].DrawTo(canvas);
                } //index
            }
        } //DrawTo

        public void OnSceneResized()
        {
            if (base.Count > 0)
            {
                for (int index = 0; index < base.Count; index++)
                {
                    base[index].OnSceneResized();
                } //index
            }
        } //OnSceneResized
    } //ObjectsCollection

    public class ObjectItem
    {
        public ObjectPosition Position;
        public BaseObjectData Data;
        public Color Color;
        public string ColorID;

        public ObjectItem() { }

        public ObjectItem(ObjectPosition position, BaseObjectData data)
        {
            this.Position = position;
            this.Color = data.Color;
            this.Data = data;
        } //New

        public ObjectItem(ObjectPosition position, BaseObjectData data, Color color)
        {
            this.Position = position;
            this.Color = color;
            this.Data = data;
        } //New

        public ObjectItem(ObjectPosition position, BaseObjectData data, string colorID)
        {
            this.Position = position;
            this.ColorID = colorID;
            this.Data = data;
        } //New

        public virtual void DrawTo(Graphics canvas)
        {
            // First scale the position.
            PointF newPos = this.Position.GetTransformedPosition();
            SizeF newSize = this.Position.GetTransformedSize();

            // Setup the transformation.
            canvas.ResetTransform();
            canvas.TranslateTransform(newPos.X, newPos.Y);
            canvas.ScaleTransform(newSize.Width, newSize.Height);

            // if(there is a color ID then get the color for that ID.
            if (!string.IsNullOrEmpty(this.ColorID)) this.Color = Scene.GetColor(this.ColorID);

            // Draw the polygon.
            this.Data.DrawTo(canvas, this.Color);
        } //DrawTo

        public void OnSceneResized() { this.Position.UpdateTransformedPosition(); }
    } //ObjectItem

    public class BuildingObjectItem : ObjectItem
    {
        public float WindowLeftOffset = 0.05F;
        public float WindowRightOffset = 0.05F;
        public float WindowTopOffset = 0.05F;
        public float WindowBottomOffset = 0.05F;
        public int WindowColumns = 5;
        public int WindowRows = 10;

        public BuildingObjectItem() { }

        public BuildingObjectItem(ObjectPosition position, BaseObjectData data)
            : base(position, data) { }

        public BuildingObjectItem(ObjectPosition position, BaseObjectData data, Color color)
            : base(position, data, color) { }

        public BuildingObjectItem(ObjectPosition position, BaseObjectData data, string colorID)
            : base(position, data, colorID) { }

        public override void DrawTo(Graphics canvas)
        {
            base.DrawTo(canvas);

            // Calculate base rectangle.
            RectangleF baseRect = RectangleF.FromLTRB(0.0F + this.WindowLeftOffset, 0.0F + this.WindowTopOffset, 1.0F - this.WindowRightOffset, 1.0F - this.WindowBottomOffset);
            //baseRect.Location = this.Position.GetTransformedPosition();
            //baseRect.Size = this.Position.GetTransformedSize();

            // Determine window size.
            float windowWidth = (baseRect.Width / (float)WindowColumns);
            float windowHeight = (baseRect.Height / (float)WindowRows);

            // draw windows
            float wX1, wY1, wX2, wY2;
            for (int y = 1; y <= this.WindowRows; y++)
            {
                for (int x = 1; x <= this.WindowColumns; x++)
                {
                    wX1 = (windowWidth * (x - 1)) + (windowWidth * 0.25F);
                    wY1 = (windowHeight * (y - 1)) + (windowHeight * 0.25F);
                    wX2 = wX1 + (windowWidth * 0.5F);
                    wY2 = wY1 + (windowHeight * 0.5F);

                    //canvas.FillRectangle(Brushes.Yellow, wX1, wY1, (wX2 - wX1), (wY2 - wY1));
                } //x
            } //y
        } //DrawTo
    } //BuildingObjectItem

    public enum ObjectTypes { Polygon, Circle }

    public class ObjectDataCollection : List<BaseObjectData>
    {
        public int FindByTypeID(string typeID)
        {
            if (base.Count > 0)
            {
                for (int index = 0; index < base.Count; index++)
                {
                    if (string.Equals(base[index].TypeID, typeID, StringComparison.CurrentCultureIgnoreCase)) return index;
                } //index
            }

            return -1;
        } //FindByTypeID

        public List<BaseObjectData> EnumByTypeID(string typeID)
        {
            List<BaseObjectData> results = new List<BaseObjectData>();

            if (base.Count > 0)
            {
                for (int index = 0; index < base.Count; index++)
                {
                    if (string.Equals(base[index].TypeID, typeID, StringComparison.CurrentCultureIgnoreCase)) results.Add(base[index]);
                } //index
            }

            return (results.Count == 0 ? null : results);
        } //EnumByTypeID
    } //ObjectDataCollection

    public abstract class BaseObjectData
    {
        public String TypeID = "";
        public Color Color;

        public BaseObjectData() { }

        public BaseObjectData(string typeID, Color color)
        {
            this.TypeID = typeID;
            this.Color = color;
        } //New

        public virtual void DrawTo(Graphics canvas) { this.DrawTo(canvas, this.Color); }

        public abstract void DrawTo(Graphics canvas, Color overrideColor);
    } //BaseObjectData

    public class PolygonObjectData : BaseObjectData
    {
        public PointF[] PolygonData;

        public PolygonObjectData() : base() { }

        public PolygonObjectData(string typeID, PointF[] polygonData, Color color)
            : base(typeID, color)
        {
            this.PolygonData = polygonData;
        } //New

        public override void DrawTo(Graphics canvas, Color overrideColor)
        {
            canvas.FillPolygon(new SolidBrush(overrideColor), this.PolygonData);
        }
    } //PolygonObjectData

    public class CloudObjectData : BaseObjectData
    {
        public PointF[] Sections;

        public CloudObjectData()
            : base()
        {
            this.Generate();
        } //New

        public CloudObjectData(string typeID, Color color)
            : base(typeID, color)
        {
            this.Generate();
        } //New

        /// <summary>Generate a random cloud.</summary>
        public void Generate()
        {
            const float minDist = 0.4F;
            const float maxDist = 0.5F;
            const float angleChangeMin = (float)Math.PI * 0.2F;
            const float angleChangeMax = (float)Math.PI * 0.3F;
            List<PointF> points = new List<PointF>();

            float angle = 0.0f, dist, newX, newY;
            do
            {
                angle += angleChangeMin + ((float)Scene.Rand.NextDouble() * (angleChangeMax - angleChangeMin));

                dist = minDist + ((float)Scene.Rand.NextDouble() * (maxDist - minDist));
                RotatePoint(out newX, out newY, 0.5F, 0.5F, dist, angle - 0.2F);
                points.Add(new PointF(newX, newY));

                RotatePoint(out newX, out newY, 0.5F, 0.5F, minDist, angle);
                points.Add(new PointF(newX, newY));

                dist = minDist + ((float)Scene.Rand.NextDouble() * (maxDist - minDist));
                RotatePoint(out newX, out newY, 0.5F, 0.5F, dist, angle + 0.2F);
                points.Add(new PointF(newX, newY));

                //RotatePoint(newX, newY, 0.5F, 0.5F, maxDist, angle - 0.1F);
                //points.Add(new PointF(newX, newY));
                //RotatePoint(newX, newY, 0.5F, 0.5F, minDist, angle);
                //points.Add(new PointF(newX, newY));
                //RotatePoint(newX, newY, 0.5F, 0.5F, maxDist, angle + 0.1F);
                //points.Add(new PointF(newX, newY));

                //dist = minDist + (Scene.Rand.NextDouble() * (maxDist - minDist));
                //RotatePoint(newX, newY, 0.5F, 0.5F, dist, angle);
                //points.Add(new PointF(newX, newY));
            } while (angle <= (Math.PI * 2));

            this.Sections = new PointF[points.Count];
            for (int index = 0; index < points.Count; index++)
            {
                this.Sections[index] = points[index];
            } //index
        } //Generate

        private static void RotatePoint(out float outX, out float outY, float offsetX, float offsetY, float distance, float angleRads)
        {
            outX = (float)(offsetX - distance * Math.Cos(angleRads));
            outY = (float)(offsetY + distance * Math.Sin(angleRads));
        } //RotatePoint

        public override void DrawTo(Graphics canvas, Color overrideColor)
        {
            if (this.Sections != null && this.Sections.Length > 0)
            {
                canvas.FillClosedCurve(new SolidBrush(overrideColor), this.Sections, FillMode.Winding, 1.0F);
            }
        }
    } //CloudObjectData

    public class CircleObjectData : BaseObjectData
    {
        public float HorizontalRadius;
        public float VerticalRadius;

        public CircleObjectData() : base() { }

        public CircleObjectData(string typeID, float horizontalRadius, float verticalRadius, Color color)
            : base(typeID, color)
        {
            this.HorizontalRadius = horizontalRadius;
            this.VerticalRadius = verticalRadius;
        } //New

        public override void DrawTo(Graphics canvas, Color overrideColor)
        {
            canvas.FillEllipse(new SolidBrush(overrideColor), -this.HorizontalRadius, -this.VerticalRadius, this.HorizontalRadius * 2.0F, this.VerticalRadius * 2.0F);
        } //DrawTo
    } //CircleObjectData

    public class Weather
    {
        private string Location = "";
        private double Lat, Lng;
        private SunRiseTimes SunTimes = new SunRiseTimes();
        public DateTime SunRiseTime, SunSetTime, NextSunRiseTime, PrevSunSetTime;
        public bool IsSunRise, IsSunSet;
        public PrecipitationTypes PrecType = PrecipitationTypes.None;
        public bool IsLightning;
        public Precipitations Precipitation = Precipitations.None;
        public CloudinessTypes Cloudiness = CloudinessTypes.None;
        private ParticleSystem RainSystem = new ParticleSystem(); //new ParticleSystem(90);
        private ObjectItem Sun;
        private ObjectItem Moon;
        private ObjectsCollection Clouds = new ObjectsCollection();
        private Thread mLocationThread, mWeatherThread;
        private CloudinessTypes mLastCloudiness = CloudinessTypes.None;
        public bool DynamicWeather = true;
        public int WeatherUpdateDelay = 30 * 60 * 1000; //30 minutes?
        private GeolocationWeatherAPI.WeatherInterface mWeather;

        public enum CloudinessTypes { None, Light, Partly, Mostly, Full }

        public enum PrecipitationTypes { None, Raining, Snowing, Sleeting }

        public enum Precipitations { None, Light, Medium, Heavy }

        public Weather(GeolocationWeatherAPI.WeatherInterface weatherAPI)
        {
            this.mWeather = weatherAPI;
            this.mLocationThread = new Thread(this.UpdateLocationThread);
            this.mWeatherThread = new Thread(this.UpdateWeatherThread);
            this.SetDefaults();
        } //New

        public Weather(GeolocationWeatherAPI.WeatherInterface weatherAPI, string location)
        {
            this.mWeather = weatherAPI;
            this.mLocationThread = new Thread(this.UpdateLocationThread);
            this.mWeatherThread = new Thread(this.UpdateWeatherThread);
            this.UpdateLocation(location);
        } //New

        private void SetDefaults()
        {
            this.Location = Scene.Options.DefaultLocation;
            this.Lat = Scene.Options.DefaultLAT;
            this.Lng = Scene.Options.DefaultLNG;

            // Update the sun rise/set times.
            this.UpdateSunRiseSetTimes();

            // Clear weather
            this.PrecType = PrecipitationTypes.None;
            this.IsLightning = false;
            this.Cloudiness = CloudinessTypes.None;
            this.Precipitation = Precipitations.None;

            // Default weather. (causes some issues when enabled)
            //if(!string.IsNullOrEmpty(Scene.Options.OverrideWeatherCondition)) this.UpdateWeather(Scene.Options.OverrideWeatherCondition);
        } //SetDefaults

        #region Update Code
        public void UpdateLocation(string location, bool threaded = true)
        {
            this.SetDefaults();

            this.Location = location;

            // Get the geometry location.
            if (!threaded)
            {
                GeolocationWeatherAPI.GeolocationResults results = this.mWeather.GetGeolocation(location);
                if (results.success) UpdateLocation(results.lat, results.lng);
            } else
                this.mLocationThread.Start();
        } //UpdateLocation

        private void UpdateLocationThread()
        {
            // Update everything using the specified location.
            this.UpdateLocation(this.Location, false);
        } //UpdateLocationThread

        private void UpdateLocation(GeolocationWeatherAPI.GeolocationResults results)
        {
            if (!results.success) return;
            // Update the location with the LAT and LNG.
            this.UpdateLocation(results.lat, results.lng);
        } //UpdateLocation

        public void UpdateLocation(double lat, double lng)
        {
            // Update the location.
            this.Lat = lat;
            this.Lng = lng;

            // Update the sun rise/set times.
            this.UpdateSunRiseSetTimes();

            // Update the weather.
            this.UpdateWeather();
        } //UpdateLocation

        private void UpdateSunRiseSetTimes()
        {
            // Get the sun rise/set time.
            DateTime riseTime = DateTime.Now;
            bool sunrise = false, sunset = false;
            this.SunTimes.CalculateSunRiseSetTimes(this.Lat, this.Lng, DateTime.Now.AddDays(-1), out riseTime, out this.PrevSunSetTime, out sunrise, out sunset);

            // Get the sun rise/set time.
            this.SunTimes.CalculateSunRiseSetTimes(this.Lat, this.Lng, DateTime.Now, out SunRiseTime, out SunSetTime, out IsSunRise, out IsSunSet);

            // Get the next day rise time.
            DateTime setTime = DateTime.Now;
            this.SunTimes.CalculateSunRiseSetTimes(this.Lat, this.Lng, DateTime.Now.AddDays(1), out NextSunRiseTime, out setTime, out sunrise, out sunset);
        } //UpdateSunRiseSetTimes

        public void UpdateWeather(bool threaded = true)
        {
            if (String.IsNullOrEmpty(this.Location)) return;

            if (!threaded)
            {
                if (String.IsNullOrEmpty(Scene.Options.OverrideWeatherCondition))
                {
                    GeolocationWeatherAPI.ConditionsForecast condition = this.mWeather.GetCurrentConditions(this.Location);
                    if (condition != null) this.UpdateWeather(condition);
                } else
                {
                    this.UpdateWeather(Scene.Options.OverrideWeatherCondition);
                }
            } else
                this.mWeatherThread.Start();
        } //UpdateWeather

        public void UpdateWeather(GeolocationWeatherAPI.ConditionsForecast condition)
        {
            //this.IsPrecType = PrecipitationTypes.None;
            //this.Precipitation = Precipitations.None;
            //this.IsLightning = false;
            //this.Cloudiness = CloudinessTypes.None;

            if (condition != null) this.UpdateWeather(System.IO.Path.GetFileNameWithoutExtension(condition.Icon));
        } //UpdateWeather

        public void UpdateWeather(string condition)
        {
            this.PrecType = PrecipitationTypes.None;
            this.Precipitation = Precipitations.None;
            this.IsLightning = false;
            this.Cloudiness = CloudinessTypes.None;

            switch (condition.ToLower())
            {
                case (GeolocationWeatherAPI.ConditionTypes.MostlySunny): this.Cloudiness = CloudinessTypes.Light; break;
                case (GeolocationWeatherAPI.ConditionTypes.PartlyCloudy): this.Cloudiness = CloudinessTypes.Partly; break;
                case (GeolocationWeatherAPI.ConditionTypes.MostlyCloudy): this.Cloudiness = CloudinessTypes.Mostly; break;
                case (GeolocationWeatherAPI.ConditionTypes.Cloudy): this.Cloudiness = CloudinessTypes.Full; break;

                case (GeolocationWeatherAPI.ConditionTypes.Mist): this.PrecType = PrecipitationTypes.Raining; this.Cloudiness = CloudinessTypes.Full; this.Precipitation = Precipitations.Light; break;
                case (GeolocationWeatherAPI.ConditionTypes.ChanceOfRain):
                case (GeolocationWeatherAPI.ConditionTypes.Rain): this.PrecType = PrecipitationTypes.Raining; this.Cloudiness = CloudinessTypes.Full; this.Precipitation = Precipitations.Light; break;
                case (GeolocationWeatherAPI.ConditionTypes.Showers): this.PrecType = PrecipitationTypes.Raining; this.Cloudiness = CloudinessTypes.Full; this.Precipitation = Precipitations.Medium; break;
                case (GeolocationWeatherAPI.ConditionTypes.ChanceOfStorm):
                case (GeolocationWeatherAPI.ConditionTypes.Storm): this.PrecType = PrecipitationTypes.Raining; this.Cloudiness = CloudinessTypes.Full; this.Precipitation = Precipitations.Heavy; break;
                case (GeolocationWeatherAPI.ConditionTypes.ChanceOfTStorm):
                case (GeolocationWeatherAPI.ConditionTypes.Thunderstorm): this.PrecType = PrecipitationTypes.Raining; this.IsLightning = true; this.Cloudiness = CloudinessTypes.Full; this.Precipitation = Precipitations.Heavy; break;

                case (GeolocationWeatherAPI.ConditionTypes.ChanceOfSnow):
                case (GeolocationWeatherAPI.ConditionTypes.Snow): this.PrecType = PrecipitationTypes.Snowing; this.Cloudiness = CloudinessTypes.Full; this.Precipitation = Precipitations.Medium; break;
                //case(GeolocationWeatherAPI.ConditionTypes.Snow) : this.PrecType = PrecipitationTypes.Snowing; this.Cloudiness = CloudinessTypes.Full; this.Precipitation = Precipitations.Heavy; break;

                case (GeolocationWeatherAPI.ConditionTypes.Sleet):
                case (GeolocationWeatherAPI.ConditionTypes.RainSnow):
                case (GeolocationWeatherAPI.ConditionTypes.Icy): this.PrecType = PrecipitationTypes.Sleeting; this.Cloudiness = CloudinessTypes.Full; this.Precipitation = Precipitations.Medium; break;
            }

            if (this.Cloudiness != this.mLastCloudiness)
            {
                this.mLastCloudiness = this.Cloudiness;
                this.CreateClouds();
            }
        } //UpdateWeather

        private void UpdateWeatherThread()
        {
            if (this.DynamicWeather)
            {
                do
                {
                    //Console.Out.WriteLine("Weather is updated.");
                    this.UpdateWeather(false);
                    Thread.Sleep(this.WeatherUpdateDelay);
                } while (true);
            } else
            {
                //Console.Out.WriteLine("Weather is updated.");
                this.UpdateWeather(false);
            }
        } //UpdateLocationThread

        public void OnSceneResized()
        {
            if (this.Sun != null) this.Sun.OnSceneResized();
            if (this.Moon != null) this.Moon.OnSceneResized();
            if (this.Clouds != null) this.Clouds.OnSceneResized();
            if (this.PrecType == PrecipitationTypes.Raining) this.RainSystem.Initialized = false;
        } //OnSceneResized
        #endregion Update Code

        public Color ComputeSceneColor()
        {
            // Rise Time-1 = Night
            // Rise Time   = Dawn
            // Rise Time+1 = Sunny
            // Set Time-1  = Sunny
            // Set Time    = Dusk
            // Set Time+1  = Night

            DateTime sunSetBefore = this.SunSetTime.AddMinutes(-30);
            DateTime sunSetAfter = this.SunSetTime.AddMinutes(30);
            DateTime sunRiseBefore = this.SunRiseTime.AddMinutes(-30);
            DateTime sunRiseAfter = this.SunRiseTime.AddMinutes(30);

            Color results;
            if (DateTime.Now >= this.SunRiseTime && DateTime.Now < sunRiseAfter)
            {
                // blend with dawn -> day
                int diff = (DateTime.Now - this.SunRiseTime).Minutes;
                results = Blend(Scene.Options.SunriseColor, Scene.Options.DayColor, diff / 30);
            } else if (DateTime.Now >= sunSetBefore && DateTime.Now < this.SunSetTime)
            {
                // blend with day -> dusk
                int diff = (this.SunSetTime - DateTime.Now).Minutes;
                results = Blend(Scene.Options.DayColor, Scene.Options.SunsetColor, (30 - diff) / 30);
            } else if (DateTime.Now >= this.SunSetTime && DateTime.Now < sunSetAfter)
            {
                // blend with dusk -> night
                int diff = (DateTime.Now - this.SunSetTime).Minutes;
                results = Blend(Scene.Options.SunsetColor, Scene.Options.NightColor, diff / 30);
            } else if (DateTime.Now >= sunRiseBefore && DateTime.Now < this.SunRiseTime)
            {
                // blend with night -> dawn
                int diff = (this.SunRiseTime - DateTime.Now).Minutes;
                results = Blend(Scene.Options.NightColor, Scene.Options.SunriseColor, (30 - diff) / 30);
            } else if (DateTime.Now > sunSetAfter || DateTime.Now < sunRiseBefore)
                results = Scene.Options.NightColor;
            else //If Now > sunRiseAfter OrElse Now < sunSetBefore Then
                results = Scene.Options.DayColor;

            if (this.Cloudiness == CloudinessTypes.Full)
                return Blend(results, Scene.GetCloudColor(1, Scene.Layers.Count), 0.8F);
            else
                return results;
        } //ComputeSceneColor

        public Color ComputeSunColor()
        {
            Color sceneColor = this.ComputeSceneColor();
            if (this.Cloudiness == CloudinessTypes.Full)
                return Blend(sceneColor, Color.Yellow, 0.2F);
            else
                return Blend(sceneColor, Color.Yellow, 0.8F);
        } //ComputeSunColor

        public float ComputeDayPercentage()
        {
            int nowDiff = (DateTime.Now - this.SunRiseTime).Seconds;
            int totalDiff = (this.SunSetTime - this.SunRiseTime).Seconds;
            if (nowDiff < 0 || nowDiff > totalDiff)
                return -1.0f;
            else
                return ((float)nowDiff / (float)totalDiff);
        } //ComputeDayPercentage

        public float ComputeNightPercentage()
        {
            int nowDiff, totalDiff;
            if (this.SunSetTime > DateTime.Now)
            {
                nowDiff = (DateTime.Now - this.PrevSunSetTime).Seconds;
                totalDiff = (this.SunRiseTime - this.PrevSunSetTime).Seconds;
            } else
            {
                nowDiff = (DateTime.Now - this.SunSetTime).Seconds;
                totalDiff = (this.NextSunRiseTime - this.SunSetTime).Seconds;
            }
            if (nowDiff < 0 || nowDiff > totalDiff)
                return -1;
            else
                return (nowDiff / totalDiff);
        } //ComputeNightPercentage

        public void DrawTo(Graphics canvas, int layer)
        {
            if (layer == 0) this.DrawPlanets(canvas);
            if (this.Clouds.Count > 0) this.Clouds.DrawTo(canvas, layer);
            if (this.PrecType != PrecipitationTypes.None) this.RainSystem.DrawTo(canvas);
        } //DrawTo

        private void CreateSun()
        {
            CircleObjectData data = new CircleObjectData("SUN", 0.5F * (Scene.ScreenHeight / Scene.ScreenWidth), 0.5F, Color.Yellow);
            this.Sun = new ObjectItem(new ObjectPosition(0, 0.0F, 0.0F, 0.3F, 0.3F, Alignments.Top), data, "SUN");
        } //CreateSun

        private void CreateMoon()
        {
            CircleObjectData data = new CircleObjectData("MOON", 0.5F * Scene.ScreenRatio, 0.5F, Color.DarkGray);
            this.Moon = new ObjectItem(new ObjectPosition(0, 0.0F, 0.0F, 0.3F, 0.3F, Alignments.Top), data);
        } //CreateMoon

        private void CreateClouds()
        {
            this.Clouds.Clear();

            int cloudCount = Scene.Options.CloudLightCount;
            if (this.Cloudiness == CloudinessTypes.None)
                return;
            else if (this.Cloudiness == CloudinessTypes.Light)
                cloudCount = Scene.Options.CloudLightCount;
            else if (this.Cloudiness == CloudinessTypes.Partly)
                cloudCount = Scene.Options.CloudPartialCount;
            else if (this.Cloudiness == CloudinessTypes.Mostly)
                cloudCount = Scene.Options.CloudMostlyCount;
            else if (this.Cloudiness == CloudinessTypes.Full)
                cloudCount = Scene.Options.CloudFullCount;

            for (int layer = 1; layer <= Scene.Layers.Count; layer++)
            {
                for (int cloudIndex = 1; cloudIndex <= cloudCount; cloudIndex++)
                {
                    float left = -0.3f + ((float)Scene.Rand.NextDouble() * 2.3f);
                    float top = (float)Scene.Rand.NextDouble() * 0.8f; //only 80% of the screen
                    float width = Scene.Options.CloudMinWidth + ((float)Scene.Rand.NextDouble() * (Scene.Options.CloudMaxWidth - Scene.Options.CloudMinWidth));
                    float height = Scene.Options.CloudMinHeight + ((float)Scene.Rand.NextDouble() * (Scene.Options.CloudMaxHeight - Scene.Options.CloudMinHeight));

                    this.Clouds.Add(new ObjectPosition(layer, left, top, width, height, Alignments.Top), new CloudObjectData(), Scene.GetCloudColor(layer, Scene.Layers.Count));
                } //cloudIndex
            } //layer
        } //CreateClouds

        private void DrawPlanets(Graphics canvas)
        {
            this.DrawSun(canvas);
            this.DrawMoon(canvas);
        } //DrawPlanets

        private void DrawMoon(Graphics canvas)
        {
            if (this.Moon == null) this.CreateMoon();

            float phasePer = this.ComputeNightPercentage();
            if (phasePer < 0) return;

            // Adjust the moon position.
            this.Moon.Position.Position = GetPlanetPosition(phasePer);
            this.Moon.DrawTo(canvas);
        } //DrawSun

        private void DrawSun(Graphics canvas)
        {
            if (this.Cloudiness == CloudinessTypes.Full) return;

            if (this.Sun == null) this.CreateSun();

            float phasePer = this.ComputeDayPercentage();
            if (phasePer < 0) return;

            // Adjust the sun position.
            this.Sun.Position.Position = GetPlanetPosition(phasePer);
            this.Sun.DrawTo(canvas);
        } //DrawSun

        private void AnimateClouds(float deltaTime)
        {
            float newX, layerScale;
            PointF transPos; SizeF transSize;
            for (int index = 0; index < this.Clouds.Count; index++)
            {
                layerScale = Scene.ComputeScale(this.Clouds[index].Position.Layer);

                // Compute the new X position.
                newX = (this.Clouds[index].Position.Position.X - (0.05f * deltaTime));
                this.Clouds[index].Position.Position = new PointF(newX, this.Clouds[index].Position.Position.Y);

                // Get the actual (drawing coords)
                transPos = this.Clouds[index].Position.GetTransformedPosition();
                transSize = this.Clouds[index].Position.GetTransformedSize();
                if ((transPos.X + transSize.Width) < 0.0F)
                {
                    PointF scrRight;
                    SizeF newSize = SizeF.Empty; //not needed
                    Scene.TransformToWorldSpace(Scene.ScreenWidth, transPos.Y, transSize.Width, transSize.Height, this.Clouds[index].Position.Layer, this.Clouds[index].Position.Alignment, out scrRight, out newSize);
                    this.Clouds[index].Position.Position = new PointF(scrRight.X, this.Clouds[index].Position.Position.Y);
                }
            } //index
        } //AnimateClouds

        public void Update(float deltaTime)
        {
            // Update the clouds
            if (this.Clouds.Count > 0) this.AnimateClouds(deltaTime);

            // Update the precipitation.
            if (this.PrecType != PrecipitationTypes.None)
            {
                ParticleSystem.ParticleTypes partType = ParticleSystem.ParticleTypes.RainDroplet;
                if (this.PrecType == PrecipitationTypes.Raining)
                {
                    partType = ParticleSystem.ParticleTypes.RainDroplet;
                } else if (this.PrecType == PrecipitationTypes.Snowing)
                {
                    partType = ParticleSystem.ParticleTypes.Snow;
                } else if (this.PrecType == PrecipitationTypes.Sleeting)
                {
                    partType = ParticleSystem.ParticleTypes.Sleet;
                }
                this.RainSystem.Update(this.Precipitation, partType, deltaTime);
            }
        } //Update

        private PointF GetPlanetPosition(float phase)
        {
            double angle = Math.PI - ((phase * Math.PI) - Math.PI);

            float newX = 0.0f, newY = 0.0f;
            RotatePoint(out newX, out newY, 0.5F, 1.0F, 0.7F, (float)angle);
            //if(phasePer >= 0.0F && phasePer <= 0.5F)
            //    topPos = (((0.5F - phasePer) / 0.5F) * Scene.ScreenHeight);
            //else
            //    topPos = (((phasePer - 0.5F) / 0.5F) * Scene.ScreenHeight);

            return new PointF(newX, newY);
        } //GetPlanetPosition

        private static Color Blend(Color a, Color b, float percent)
        {
            if (percent <= 0.0f) return a;
            if (percent >= 1.0f) return b;

            return System.Drawing.Color.FromArgb(
             ((int)(b.R * percent) + (int)(a.R * (1.0f - percent))),
             ((int)(b.G * percent) + (int)(a.G * (1.0f - percent))),
             ((int)(b.B * percent) + (int)(a.B * (1.0f - percent))));
        } //Blend

        private static void RotatePoint(out float outX, out float outY, float offsetX, float offsetY, float distance, float angleRads)
        {
            outX = (float)(offsetX - distance * Math.Cos(angleRads));
            outY = (float)(offsetY + distance * Math.Sin(angleRads));
        } //RotatePoint

        public class ParticleSystem
        {
            public enum ParticleTypes { RainDroplet, Snow, Sleet }

            public int ParticlesMaxCount = 100;
            public float ParticleMinSpeed = 20.0F;
            public float ParticleMaxSpeed = 40.0F;
            public float ParticleMinSize = 3.0F;
            public float ParticleMaxSize = 8.0F;
            public bool Initialized;
            public ParticlesCollection Particles = new ParticlesCollection();
            public ParticleTypes ParticleType = ParticleTypes.RainDroplet;
            public static Random Random = new Random();
            private Precipitations mLastPrecipitation;

            public void Reset(Precipitations precipitation)
            {
                this.Initialized = true;
                this.mLastPrecipitation = precipitation;

                this.Particles.Clear();
                this.Spawn((int)(this.ParticlesMaxCount * ((float)precipitation / 3.0f)), Scene.ScreenWidth, Scene.ScreenHeight);
            } //Reset

            public void Spawn(int count, int sceneWidth, int sceneHeight)
            {
                for (int index = 1; index <= count; index++)
                {
                    int x = Random.Next(0, sceneWidth);
                    int y = Random.Next(0, sceneHeight);

                    float maxSpeed = (this.ParticleMaxSpeed - this.ParticleMinSpeed) * ((int)this.mLastPrecipitation / 3.0f);

                    ParticleItem drop = new ParticleItem(x, y, 0, (this.ParticleMinSpeed + (maxSpeed * (float)Random.NextDouble())), ParticleTypes.RainDroplet);
                    this.ResetParticle(drop);
                    drop.OnDeath += new ParticleItem.DeathEvent(this.OnDropletDeath);
                    this.Particles.Add(drop);
                } //index
            } //Count

            public void DrawTo(Graphics canvas) { this.Particles.DrawTo(canvas); }

            public void Update(Precipitations precipitation, ParticleTypes type, float deltaTime)
            {
                this.ParticleType = type;
                if (!this.Initialized || this.mLastPrecipitation != precipitation) this.Reset(precipitation);
                this.Particles.Update(Scene.ScreenWidth, Scene.ScreenHeight, deltaTime);
            } //DrawTo

            private void OnDropletDeath(ParticleItem Object)
            {
                // just reset the data, but from the top of the screen
                Object.X = Random.Next(0, Scene.ScreenWidth);
                Object.Y = 0.0F - Object.VelocityY;
                this.ResetParticle(Object);
            } //OnDropletDeath

            private void ResetParticle(ParticleItem Object)
            {
                Object.Alive = true;
                Object.Type = this.ParticleType;
                Object.SnowSize = (this.ParticleMinSize + (this.ParticleMaxSize - this.ParticleMinSize) * (float)Random.NextDouble());
                if (Object.Type == ParticleTypes.Sleet) Object.Type = (ParticleTypes)Random.Next(0, 2);
            } //ResetParticle

            public class ParticlesCollection : List<ParticleItem>
            {
                public ParticleItem Add(float x, float y, float velocityX, float velocityY, ParticleTypes particleType)
                {
                    base.Add(new ParticleItem(x, y, velocityX, velocityY, particleType));
                    return base[base.Count - 1];
                } //Add

                public void DrawTo(Graphics canvas)
                {
                    if (base.Count > 0)
                    {
                        for (int index = 0; index < base.Count; index++)
                        {
                            if (base[index].Alive) base[index].DrawTo(canvas);
                        } //index
                    }
                } //DrawTo

                public void Update(int sceneWidth, int sceneHeight, float deltaTime)
                {
                    if (base.Count > 0)
                    {
                        for (int index = 0; index < base.Count; index++)
                        {
                            if (base[index].Alive) base[index].Update(sceneWidth, sceneHeight, deltaTime);
                        } //index
                    }
                } //DrawTo
            } //ParticlesCollection

            public class ParticleItem
            {
                public bool Alive = true;
                public ParticleTypes Type;
                public float X, Y;
                public float VelocityX, VelocityY;
                public float SnowSize = 5.0F;

                public delegate void DeathEvent(ParticleItem Object);

                public event DeathEvent OnDeath;

                public ParticleItem() { }

                public ParticleItem(float x, float y, float velocityX, float velocityY, ParticleTypes type)
                {
                    this.Alive = true;
                    this.X = x;
                    this.Y = y;
                    this.VelocityX = velocityX;
                    this.VelocityY = velocityY;
                    this.Type = type;
                } //New

                public void DrawTo(Graphics canvas)
                {
                    if (!this.Alive) return;

                    canvas.ResetTransform();
                    //canvas.TranslateTransform(newPos.X, newPos.Y);
                    //canvas.ScaleTransform(newSize.Width, newSize.Height);

                    if (this.Type == ParticleTypes.RainDroplet)
                        canvas.DrawLine(Pens.White, this.X, this.Y, this.X - this.VelocityX, this.Y - this.VelocityY);
                    else if (this.Type == ParticleTypes.Snow)
                        canvas.FillEllipse(new SolidBrush(Color.White), this.X - (this.SnowSize * 0.5F), this.Y - this.SnowSize, this.SnowSize, this.SnowSize);
                } //DrawTo

                public void Update(int sceneWidth, int sceneHeight, float deltaTime)
                {
                    this.X += this.VelocityX;
                    this.Y += this.VelocityY;

                    if (this.Type == ParticleTypes.Snow)
                    {
                        // if(snow, then add some random wind?
                        this.VelocityX += -5.0F + (10.0F * (float)Random.NextDouble());
                    }

                    if (this.VelocityY > 0.0F && this.Y >= (sceneHeight + this.VelocityY))
                    {
                        this.Alive = false;
                        if (this.OnDeath != null) this.OnDeath(this);
                    } else if (this.VelocityX < 0.0F && (this.X + (this.SnowSize * 0.5F)) + this.VelocityX < 0)
                    {
                        this.Alive = false;
                        if (this.OnDeath != null) this.OnDeath(this);
                    } else if (this.VelocityX > 0.0F && (this.X + (this.SnowSize * 0.5F)) + this.VelocityX > sceneWidth)
                    {
                        this.Alive = false;
                        if (this.OnDeath != null) this.OnDeath(this);
                    }
                } //Update
            } //ParticleItem
        } //ParticleSystem

        public void Dispose()
        {
            this.mWeatherThread.Abort();
            this.mLocationThread.Abort();
        } //Dispose
    } //Weather
}
