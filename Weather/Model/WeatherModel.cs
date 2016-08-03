using System.Collections.Generic;

namespace Weather.Model
{
    public class WeatherModel
    {
        public string CityCode { get; set; }
        
        public string AlarmStr { get; set; }

        public IList<string> AlarmStrList { get; set; } 

        public CityNowWeather WeatherNow { get; set; }
        public IList<CityDayWeather> Weather7Days { get; set; }
        public IList<IList<CityHourWeather>> Weather8Hours { get; set; }
    }//End public class WeatherModel

    public class CityNowWeather
    {
        public string CityName { get; set; }
        public string GetTime { get; set; }
        public string DayWeatherText { get; set; }
        public string DayWeatherCode { get; set; }
        public string DayTemperature { get; set; }
        public string DayWindText { get; set; }
        public string DayWindDirection { get; set; }
        public string NightWeatherText { get; set; }
        public string NightWeatherCode { get; set; }
        public string NightTemperature { get; set; }
        public string NightWindText { get; set; }
        public string NightWindDirection { get; set; }
        public string WindDirection { get; set; }
        public string WindScale { get; set; }
        public string RelativeHumidity { get; set; }
        public string AlarmText { get; set; }
        public string SunriseTime { get; set; }
        public string SunsetTime { get; set; }
    }//End public class CityNowWeather

    public class CityDayWeather
    {
        public string Date { get; set; }
        public string DayWeatherText { get; set; }
        public string DayWeatherCode { get; set; }
        public string DayTemperature { get; set; }
        public string DayWindDirection { get; set; }
        public string NightWeatherText { get; set; }
        public string NightWeatherCode { get; set; }
        public string NightTemperature { get; set; }
        public string NightWindDiretion { get; set; }
        public string WindText { get; set; }
    }//End public class City7DayWeather

    public class CityHourWeather
    {
        public string Hour { get; set; }
        public string WeatherText { get; set; }
        public string WeatherCode { get; set; }
        public string Temperature { get; set; }
        public string WindText { get; set; }
        public string WindDirection { get; set; }
        public string WindScale { get; set; }
    }//End public class City8HourWeather

}
