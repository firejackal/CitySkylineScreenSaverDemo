//Using the code from https://github.com/bp2008/DahuaSunriseSunset/blob/master/DahuaSunriseSunset/CalcSunTimes.cs
//which is also the same code from https://www.codeproject.com/Articles/29306/C-Class-for-Calculating-Sunrise-and-Sunset-Times

//////////////////////////////////////////////////////////////////////////////////////////////////////
//  
//  C# Singleton class and thread-safe class for calculating Sunrise and Sunset times.
//
// The algorithm was adapted from the JavaScript sample provided here:
//      http://home.att.net/~srschmitt/script_sun_rise_set.html
//
//  NOTICE: this code is provided "as-is", without any warrenty, obligations or liability for it.
//          You may use this code freely for any use.
// 
//  Zacky Pickholz (zacky.pickholz@gmail.com)
//
/////////////////////////////////////////////////////////////////////////////////////////////////////
using System;

public class SunRiseTimes
{
#region Private Data Members
    private const double mDR = System.Math.PI / 180;
    private const double mK1 = 15 * mDR * 1.0027379;

    private int[]  mRiseTimeArr = new int[2] { 0, 0 };
    private int[]  mSetTimeArr  = new int[2] { 0, 0 };
    private double mRizeAzimuth = 0.0;
    private double mSetAzimuth  = 0.0;

    private double[] mRightAscentionArr   = new double[3] { 0.0, 0.0, 0.0 };
    private double[] mDecensionArr        = new double[3] { 0.0, 0.0, 0.0 };
    private double[] mVHzArr              = new double[3] { 0.0, 0.0, 0.0 };

    private bool mIsSunrise = false;
    private bool mIsSunset  = false;
#endregion Private Data Members

    public abstract class Coords
    {
        protected int mDegrees = 0;
        protected int mMinutes = 0;
        protected int mSeconds = 0;

        public double ToDouble()
        {
            return this.Sign() * (this.mDegrees + ((double)this.mMinutes / 60.0) + ((double)this.mSeconds / 3600.0));
        } //ToDouble Function

        protected abstract int Sign();
    } //Coords Class

    public class LatitudeCoords : Coords
    {
        public enum Direction { North, South }

        private Direction mDirection = Direction.North;

        public LatitudeCoords(int degrees, int minutes, int seconds, Direction direction)
        {
            this.mDegrees   = degrees;
            this.mMinutes   = minutes;
            this.mSeconds   = seconds;
            this.mDirection = direction;
        } //Constructor

        protected override int Sign() { return (this.mDirection == Direction.North ? 1 : -1); }
    } //LatitudeCoords Class

    public class LongitudeCoords : Coords
    {
        public enum Direction { East, West }

        private Direction mDirection = Direction.East;

        public LongitudeCoords(int degrees, int minutes, int seconds, Direction direction)
        {
            this.mDegrees   = degrees;
            this.mMinutes   = minutes;
            this.mSeconds   = seconds;
            this.mDirection = direction;
        } //Constructor

        protected override int Sign() { return (this.mDirection == Direction.East ? 1 : -1); }
    } //LongitudeCoords Class

    /// <summary>
    /// Calculate sunrise and sunset times. Returns false if time zone and longitude are incompatible.
    /// </summary>
    /// <param name="lat">Latitude coordinates.</param>
    /// <param name="lon">Longitude coordinates.</param>
    /// <param name="date">Date for which to calculate.</param>
    /// <param name="riseTime">Sunrise time (output)</param>
    /// <param name="setTime">Sunset time (output)</param>
    /// <param name="isSunrise">Whether or not the sun rises at that day</param>
    /// <param name="isSunset">Whether or not the sun sets at that day</param>
    public bool CalculateSunRiseSetTimes(LatitudeCoords lat, LongitudeCoords lon, DateTime date,
                                            out DateTime riseTime, out DateTime setTime,
                                            out bool isSunrise, out bool isSunset)
    {
        return this.CalculateSunRiseSetTimes(lat.ToDouble(), lon.ToDouble(), date, out riseTime, out setTime, out isSunrise, out isSunset);
    } //CalculateSunRiseSetTimes Function

    /// <summary>
    /// Calculate sunrise and sunset times. Returns false if time zone and longitude are incompatible.
    /// </summary>
    /// <param name="lat">Latitude in decimal notation.</param>
    /// <param name="lon">Longitude in decimal notation.</param>
    /// <param name="date">Date for which to calculate.</param>
    /// <param name="riseTime">Sunrise time (output)</param>
    /// <param name="setTime">Sunset time (output)</param>
    /// <param name="isSunrise">Whether or not the sun rises at that day</param>
    /// <param name="isSunset">Whether or not the sun sets at that day</param>
    public bool CalculateSunRiseSetTimes(double lat, double lon, DateTime date,
                                            out DateTime riseTime, out DateTime setTime,
                                            out bool isSunrise, out bool isSunset)
    {
        riseTime = DateTime.Now;
        setTime = DateTime.Now;
        isSunrise = false;
        isSunset = false;

        double zone = -Convert.ToInt32(Math.Round(TimeZone.CurrentTimeZone.GetUtcOffset(date).TotalSeconds / 3600));
        double jd   = GetJulianDay(date) - 2451545.0; //Julian day relative to Jan 1.5, 2000

        if((Sign(zone) == Sign(lon)) && (zone != 0)) {
            Console.WriteLine("WARNING: time zone and longitude are incompatible!");
            return false;
        }

        lon /= 360.0;
        double tz = zone / 24.0;
        double ct = jd / 36525.0 + 1.0; //centuries since 1900.0
        double t0 = LocalSiderealTimeForTimeZone(lon, jd, tz); //local sidereal time

        // get sun position at start of day
        jd += tz;
        double ra0, dec0; CalculateSunPosition(jd, ct, out ra0, out dec0);

        // get sun position at end of day
        jd += 1;
        double ra1, dec1; CalculateSunPosition(jd, ct, out ra1, out dec1);
        
        // make continuous 
        if(ra1 < ra0) ra1 += 2 * Math.PI;

        // initialize
        this.mIsSunrise = false;
        this.mIsSunset = false;
        this.mRightAscentionArr[0] = ra0;
        this.mDecensionArr[0] = dec0;

        // check each hour of this day
        for(int hour = 0; hour < 24; hour++) {
            this.mRightAscentionArr[2] = ra0 + (hour + 1) * (ra1 - ra0) / 24;
            this.mDecensionArr[2] = dec0 + (hour + 1) * (dec1 - dec0) / 24;
            this.mVHzArr[2] = TestHour(hour, zone, t0, lat);

            // advance to next hour
            this.mRightAscentionArr[0] = mRightAscentionArr[2];
            this.mDecensionArr[0] = mDecensionArr[2];
            this.mVHzArr[0] = this.mVHzArr[2];
        } //hour

        riseTime = new DateTime(date.Year, date.Month, date.Day, this.mRiseTimeArr[0], this.mRiseTimeArr[1], 0);
        setTime  = new DateTime(date.Year, date.Month, date.Day, this.mSetTimeArr[0],  this.mSetTimeArr[1],  0);

        isSunset  = true;
        isSunrise = true;

        // neither sunrise nor sunset
        if(!this.mIsSunrise && !this.mIsSunset) {
            isSunrise = (this.mVHzArr[2] < 0 ? false : true); // Sun down all day
            isSunset = (this.mVHzArr[2] >= 0 ? false : true);  // Sun up all day
            // sunrise or sunset
        } else {
            isSunrise = this.mIsSunrise; // No sunrise this date
            isSunset = this.mIsSunset; // No sunset this date
        }

        return true;
    } //CalculateSunRiseSetTimes Function

#region Private Methods
    private static int Sign(double value)
    {
        if(value > 0.0)
            return 1;
        else if(value < 0.0)
            return -1;
        else
            return 0;
    } //Sign Function

    ///<summary>Local Sidereal Time for zone</summary>
    private static double LocalSiderealTimeForTimeZone(double lon, double jd, double z)
    {
        //8640184.812999999
        double s = 24110.5 + 8640184.813 * jd / 36525 + 86636.6 * z + 86400 * lon;
        s /= 86400;
        s -= Math.Floor(s);
        return s *360 * mDR;
    } //LocalSiderealTimeForTimeZone Function

    /// <summary>Determine Julian day from a calendar date/</summary>
    /// <remarks>(Jean Meeus, "Astronomical Algorithms", Willmann-Bell, 1991)</remarks>
    private static double GetJulianDay(DateTime date)
    {
        int month = date.Month;
        int day   = date.Day;
        int year  = date.Year;

        bool gregorian = (year < 1583 ? false : true);

        if(month == 1 || month == 2) {
            year -= 1;
            month += 12;
        }

        double a = Math.Floor(System.Convert.ToDouble(year) / 100.0);
        double b = 0;

        if(gregorian)
            b = 2.0 - a + System.Math.Floor(a / 4.0);
        else
            b = 0.0;

        return Math.Floor(365.25 * (year + 4716))
             + Math.Floor(30.6001 * (month + 1))
             + day + b - 1524.5;
    } //GetJulianDay Function

    /// <summary>Returns the Sun's position using fundamental arguments.</summary>
    /// <remarks>(Van Flandern and Pulkkinen, 1979)</remarks>
    private static void CalculateSunPosition(double jd, double ct, out double outAscension, out double outDeclination)
    {
        double lo = (0.779072 + 0.00273790931 * jd);
        lo -= Math.Floor(lo);
        lo *= 2 * Math.PI;

        double g = 0.993126 + 0.0027377785 * jd;
        g -= Math.Floor(g);
        g *= 2 * Math.PI;

        double v = 0.39785 * Math.Sin(lo);
        v = v - 0.01 * Math.Sin(lo - g);
        v = v + 0.00333 * Math.Sin(lo + g);
        v = v - 0.00021 * ct * Math.Sin(lo);

        double u = 1 - 0.03349 * Math.Cos(g);
        u = u - 0.00014 * Math.Cos(2 * lo);
        u = u + 0.00008 * Math.Cos(lo);

        double w = -0.0001 - 0.04129 * Math.Sin(2 * lo);
        w = w + 0.03211 * Math.Sin(g);
        w = w + 0.00104 * Math.Sin(2 * lo - g);
        w = w - 0.00035 * Math.Sin(2 * lo + g);
        w = w - 0.00008 * ct * Math.Sin(g);

        // compute sun's right ascension
        double s = w / Math.Sqrt(u - v * v);
        outAscension = lo + Math.Atan(s / Math.Sqrt(1 - s * s));

        // ...and declination 
        s = v / Math.Sqrt(u);
        outDeclination = Math.Atan(s / Math.Sqrt(1 - s * s));
    } //CalculateSunPosition Function

    /// <summary>test an hour for an event</summary>
    private double TestHour(int k, double zone, double t0, double lat)
    {
        double a, b, c, d, e, s, z;
        double time;
        int hr, min;
        double az, dz, hz, nz;

        double[] ha = new double[3];
        ha[0] = t0 - mRightAscentionArr[0] + k * mK1;
        ha[2] = t0 - mRightAscentionArr[2] + k * mK1 + mK1;

        ha[1] = (ha[2] + ha[0]) / 2;   // hour angle at half hour
        mDecensionArr[1] = (mDecensionArr[2] + mDecensionArr[0]) / 2; // declination at half hour

        s = Math.Sin(lat * mDR);
        c = Math.Cos(lat * mDR);
        z = Math.Cos(90.833 * mDR);    // refraction + sun semidiameter at horizon

        if(k <= 0) mVHzArr[0] = s* Math.Sin(mDecensionArr[0]) + c* Math.Cos(mDecensionArr[0]) * Math.Cos(ha[0]) - z;

        mVHzArr[2] = s * Math.Sin(mDecensionArr[2]) + c * Math.Cos(mDecensionArr[2]) * Math.Cos(ha[2]) - z;

        if(Sign(mVHzArr[0]) == Sign(mVHzArr[2])) return mVHzArr[2]; // no event this hour

        mVHzArr[1] = s * Math.Sin(mDecensionArr[1]) + c * Math.Cos(mDecensionArr[1]) * Math.Cos(ha[1]) - z;

        a = 2 * mVHzArr[0] - 4 * mVHzArr[1] + 2 * mVHzArr[2];
        b = -3 * mVHzArr[0] + 4 * mVHzArr[1] - mVHzArr[2];
        d = b * b - 4 * a * mVHzArr[0];

        if(d < 0) return mVHzArr[2]; //no event this hour

        d = Math.Sqrt(d);
        e = (-b + d) / (2 * a);

        if(e > 1 || e < 0) e = (-b - d) / (2 * a);

        time = (double)k + e + 1.0 / 120.0; // time of an event

        hr = Convert.ToInt32(Math.Floor(time));
        min = Convert.ToInt32(Math.Floor((time - hr) * 60));

        hz = ha[0] + e * (ha[2] - ha[0]);                 // azimuth of the sun at the event
        nz = -Math.Cos(mDecensionArr[1]) * Math.Sin(hz);
        dz = c * Math.Sin(mDecensionArr[1]) - s * Math.Cos(mDecensionArr[1]) * Math.Cos(hz);
        az = Math.Atan2(nz, dz) / mDR;
        if(az < 0) az += 360;

        if(mVHzArr[0] < 0 && mVHzArr[2] > 0) {
            mRiseTimeArr[0] = hr;
            mRiseTimeArr[1] = min;
            mRizeAzimuth = az;
            mIsSunrise = true;
        }

        if(mVHzArr[0] > 0 && mVHzArr[2] < 0) {
            mSetTimeArr[0] = hr;
            mSetTimeArr[1] = min;
            mSetAzimuth = az;
            mIsSunset = true;
        }

        return mVHzArr[2];
    } //TestHour Function
#endregion Private Methods
} //SunRiseTimes Class
