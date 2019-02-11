using System;
using System.Collections.Generic;
using GeolocationWeatherAPI;

namespace CitySkylineDemo.CS
{
    public class DummyWeatherData : WeatherInterface
    {
        public ConditionsForecast GetCurrentConditions(String location)
        {
            ConditionsForecast current = new ConditionsForecast();
            current.City = "Columbus, OH";
            current.Condition = ConditionTypes.Cloudy;
            current.DayOfWeek = DateTime.Now.DayOfWeek.ToString();
            current.Icon = ConditionTypes.Cloudy;
            current.Low = "32";
            current.High = "50";
            current.Humidity = "60%";
            current.TempF = "32";
            current.TempC = "0";
            current.Wind = "5MPH";
            return current;
        }

        public List<ConditionsForecast> GetForecast(String location)
        {
            // not using, just use the current conditions repeated 7 times.
            ConditionsForecast current = this.GetCurrentConditions(location);
            return new List<ConditionsForecast>() { current, current, current, current, current, current, current };
        }

        public GeolocationResults GetGeolocation(String location)
        {
            // ignore the location and return a geolocation for Columbus, Ohio.
            GeolocationResults results = new GeolocationResults() {
                success = true,
                lat = 39.961178,
                lng = -82.998795
            };

            return results;
        } //GetGeolocation Function
    } //DummyWeatherData Class
}
