/*
 * Since Google discontinued their geocaching and weather API, I didn't want to remove all the code to it.
 * This class is a place-holder until another service can be found.
 * So it will be designed for universal use.
 */

using System;
using System.Collections.Generic;

namespace GeolocationWeatherAPI
{
    public struct GeolocationResults
    {
        public bool success;
        public double lat;
        public double lng;
    } //Results Class

    public static class ConditionTypes
    {
        public const string MostlySunny = "mostly_sunny";
        public const string PartlyCloudy = "partly_cloudy";
        public const string MostlyCloudy = "mostly_cloudy";
        public const string Cloudy = "cloudy";

        public const string ChanceOfRain = "chance_of_rain";
        public const string ChanceOfStorm = "chance_of_storm";
        public const string ChanceOfTStorm = "chance_of_tstorm";
        public const string Mist = "mist";
        public const string Rain = "rain";
        public const string Showers = "showers";
        public const string Storm = "storm";
        public const string Thunderstorm = "thunderstorm";

        public const string ChanceOfSnow = "chance_of_snow";
        public const string Snow = "snow";
        public const string Sleet = "sleet";
        public const string RainSnow = "rain_snow";
        public const string Icy = "icy";

        public const string Dust = "dust";
        public const string Fog = "fog";
        public const string Smoke = "smoke";
        public const string Haze = "haze";
    }

    public class ConditionsForecast
    {
        public string City = "No Data";
        public string DayOfWeek = DateTime.Now.DayOfWeek.ToString();
        public string Condition = "No Data";
        public string TempF = "No Data";
        public string TempC = "No Data";
        public string Humidity = "No Data";
        public string Wind = "No Data";
        public string High = "No Data";
        public string Low = "No Data";
        public string Icon = "No Data";
    } //Conditions Class

    public interface WeatherInterface
    {
        GeolocationResults GetGeolocation(string location);
        ConditionsForecast GetCurrentConditions(string location);
        List<ConditionsForecast> GetForecast(string location);
    }
}
