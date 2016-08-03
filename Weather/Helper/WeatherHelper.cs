using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Weather.Model;

namespace Weather.Helper
{
    /// <summary>
    /// WeatherHelper类只有两个公开的接口：
    /// 第一个：GetCityInfos 用于查询城市信息
    /// 第二个：GetWeatherModel 用于获取该城市全部的天气信息
    /// </summary>
    public static class WeatherHelper
    {
        #region Webpage分析，天气信息获取

        /// <summary>
        /// 获取全部天气信息
        /// </summary>
        /// <param name="cityCode">城市代码</param>
        /// <returns></returns>
        public static WeatherModel GetWeatherModel(string cityCode, AlarmCityMenuModel alarmCityMenuModel)
        {
            try
            {
                if (string.IsNullOrEmpty(cityCode))
                {
                    throw new Exception("CityCode为空，无法获取天气信息！");
                }

                List<string> webpageStrList = CreateWebpageSource(CombineWeatherUrl(cityCode));
                WeatherModel weatherModel = new WeatherModel()
                {
                    CityCode = cityCode,
                    AlarmStrList = GetAlarmString(cityCode, alarmCityMenuModel),
                    //WeatherNow = GetCityNowWeather(webpageStrList[1]),
                    Weather7Days = GetCity7DayWeathers(webpageStrList[0]),
                    Weather8Hours = GetCity8HourWeathers(webpageStrList[0])
                };

                foreach (var str in weatherModel.AlarmStrList)
                {
                    if (!string.IsNullOrEmpty(str))
                    {
                        weatherModel.AlarmStr += str + "\n\n";
                    }
                }

                //去掉末尾的 \n
                if (!string.IsNullOrEmpty(weatherModel.AlarmStr))
                {
                    weatherModel.AlarmStr = weatherModel.AlarmStr.Remove(weatherModel.AlarmStr.Length - 2);
                }

                return weatherModel;
            }
            catch (Exception ex)
            {
                throw new Exception("天气类生成过程中发生一个错误！\n" + ex.Message);
            }
        }

        /// <summary>
        /// 生成新的AlarmCityMenuModel 城市预警信息菜单
        /// </summary>
        /// <returns></returns>
        public static AlarmCityMenuModel NewAlarmCityMenuModel()
        {
            try
            {
                HttpWebRequest request =
                    (HttpWebRequest)
                        WebRequest.Create(string.Format("http://product.weather.com.cn/alarm/grepalarm_cn.php?_={0}",
                            GetTimeStamp()));
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                if (response.GetResponseStream() != Stream.Null)
                {
                    StreamReader sr = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
                    string cityMenuStr = sr.ReadToEnd();
                    cityMenuStr = cityMenuStr.Substring(14, cityMenuStr.Length - 15);
                    return JsonConvert.DeserializeObject<AlarmCityMenuModel>(cityMenuStr);
                }
                else
                {
                    throw new Exception("生成预警信息导航模型失败！\n城市预警信息列表为空！");
                }
            }
            catch (Exception ex)
            {
                throw new Exception("生成预警信息导航模型失败！\n" + ex.Message);
            }
        }

        private static IList<string> GetAlarmString(string cityCode, AlarmCityMenuModel menuModel)
        {
            IList<string> alarmStringList = new List<string>();
            List<List<string>> cityAndMenu = new List<List<string>>();

            try
            {
                foreach (var menu in menuModel.Data)
                {
                    if (cityCode.Length > 5)
                    {
                        for (int i = 0; cityCode.Length - i >= 5; i += 2)
                        {
                            string patten = "^" + cityCode.Substring(0, cityCode.Length - i) + "-";
                            Regex rgx = new Regex(patten, RegexOptions.Singleline);

                            if (rgx.IsMatch(menu[1]))
                            {
                                cityAndMenu.Add(menu.ToList());
                            }
                        }
                    }
                    else
                    {
                        string patten = "^" + cityCode + "-";
                        Regex rgx = new Regex(patten, RegexOptions.Singleline);

                        if (rgx.IsMatch(menu[1]))
                        {
                            cityAndMenu.Add(menu.ToList());
                        }
                    }
                }

                foreach (var menu in cityAndMenu)
                {
                    HttpWebRequest request =
                        (HttpWebRequest)
                            WebRequest.Create(
                                string.Format(
                                    "http://product.weather.com.cn/alarm/webdata/{0}?_={1}", menu[1], GetTimeStamp()));
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                    if (response.GetResponseStream() != Stream.Null)
                    {
                        StreamReader sr = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
                        string alarmStr = sr.ReadToEnd().Substring(14);

                        var alarmInfoModel = JsonConvert.DeserializeObject<AlarmInformationModel>(alarmStr);
                        alarmStr = string.Format("{0}发布{1}{2}预警    {3}\n    {4}", menu[0], alarmInfoModel.SignalType,
                            alarmInfoModel.SignalLevel, alarmInfoModel.IssueTime, alarmInfoModel.IssueContent);

                        while (true)
                        {
                            if (alarmStr.EndsWith("\n"))
                            {
                                alarmStr = alarmStr.Remove(alarmStr.Length - 1);
                            }
                            else
                            {
                                break;
                            }
                        }

                        alarmStringList.Add(alarmStr);
                    }
                    else
                    {
                        throw new Exception("获取城市预警信息失败！\n未返回具体数据。");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("生成城市预警信息失败！\n" + ex.Message);
            }

            return alarmStringList;
        }

        /// <summary>
        /// 生成时间戳（毫秒级）
        /// </summary>
        /// <returns></returns>
        public static long GetTimeStamp()
        {
            DateTime dtStart = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
            DateTime dtNow = DateTime.Now;
            return (long)(dtNow - dtStart).TotalMilliseconds;
        }

        /// <summary>
        /// 获取城市当前天气信息
        /// </summary>
        /// <param name="webpage1D"></param>
        /// <returns></returns>
        private static CityNowWeather GetCityNowWeather(string webpage1D)
        {
            try
            {
                CityNowWeather nowWeather = new CityNowWeather();

                string patten = @"<div\sclass=\""crumbs\sfl\"">(.|\s)*?</div>";
                Regex rg = new Regex(patten, RegexOptions.Multiline);
                string dataDiv = rg.Match(webpage1D).Value;
                patten = @"<span>([^<]*)</span>";
                rg = new Regex(patten, RegexOptions.Singleline);
                var dataCityName = rg.Matches(dataDiv)[1].Groups[1].Value;
                nowWeather.CityName = dataCityName;//cityName

                nowWeather.GetTime = DateTime.Now.ToString("T");//GetTime

                patten = @"<ul\sclass=\""clearfix\"">(.|\s)*?</ul>";
                rg = new Regex(patten, RegexOptions.Multiline);
                string dataUl = rg.Match(webpage1D).Value;

                patten = @"<p\sclass=\""wea""\stitle=\"".*\"">([^<]*)</p>";
                rg = new Regex(patten, RegexOptions.Multiline);
                var dataWeatherText = rg.Matches(dataUl);
                nowWeather.DayWeatherText = dataWeatherText[0].Groups[1].Value;//DayWeatherText
                nowWeather.NightWeatherText = dataWeatherText[1].Groups[1].Value;//NightWeatherText

                patten = @"<big\sclass=\""(.+)\"">";
                rg = new Regex(patten, RegexOptions.Multiline);
                var dataWeatherCode = rg.Matches(dataUl);
                nowWeather.DayWeatherCode = dataWeatherCode[0].Groups[1].Value;//DayWeatherCode
                nowWeather.NightWeatherCode = dataWeatherCode[1].Groups[1].Value;//DayWeatherCode

                patten = @"<span>([^<]*)</span>";
                rg = new Regex(patten, RegexOptions.Multiline);
                var dataTemperature = rg.Matches(dataUl);
                nowWeather.DayTemperature = dataTemperature[0].Groups[1].Value + "℃";
                nowWeather.SunriseTime = dataTemperature[1].Groups[1].Value;
                nowWeather.NightTemperature = dataTemperature[2].Groups[1].Value + "℃";
                nowWeather.SunsetTime = dataTemperature[3].Groups[1].Value;

                patten = @"<span\sclass=\""\""\stitle=\""(.*)\"">([^<]*)</span>";
                rg = new Regex(patten, RegexOptions.Multiline);
                var dataWind = rg.Matches(dataUl);
                nowWeather.DayWindDirection = dataWind[0].Groups[1].Value;//DayWindDirection
                nowWeather.DayWindText = dataWind[0].Groups[2].Value;//DayWindtext
                nowWeather.NightWindDirection = dataWind[1].Groups[1].Value;//NightWindDirection
                nowWeather.NightWindText = dataWind[1].Groups[2].Value;//NightWindText

                return nowWeather;
            }
            catch (Exception ex)
            {
                throw new Exception("获取城市当前天气信息失败！\n" + ex.Message);
            }
        }

        /// <summary>
        /// 获取7天天气信息
        /// </summary>
        /// <param name="webpageStr"></param>
        /// <returns></returns>
        private static IList<CityDayWeather> GetCity7DayWeathers(string webpageStr)
        {
            try
            {
                IList<CityDayWeather> sevenDayWeathers = new List<CityDayWeather>();

                string patten = @"<ul\sclass=\""t\sclearfix\"">(.|\s)*?(<li\s*.*>(.|\s)*?</li>)(.|\s)*?</ul>";
                Regex rg = new Regex(patten, RegexOptions.Multiline);
                string text7DaysWeather = rg.Match(webpageStr).Value;

                patten = @"<li\s*.*>(.|\s)*?</li>";
                rg = new Regex(patten, RegexOptions.Multiline);
                var sevenDaysWeatherCollection = rg.Matches(text7DaysWeather);
                foreach (var value in sevenDaysWeatherCollection)
                {
                    CityDayWeather cityOneDayWeather = new CityDayWeather();

                    patten = @"<h1>([^<]*)</h1>";
                    rg = new Regex(patten, RegexOptions.Singleline);
                    var dataH1 = rg.Match(value.ToString()).Groups[1].Value;
                    cityOneDayWeather.Date = dataH1;//Date

                    patten = @"<big\sclass=\""((\w|\s)*?)\"">";
                    rg = new Regex(patten, RegexOptions.Singleline);
                    var dataBig = rg.Matches(value.ToString());
                    if (dataBig[0].Groups[1].Value.Length > 5)
                    {
                        cityOneDayWeather.DayWeatherCode = dataBig[0].Groups[1].Value.Substring(5,
                            dataBig[0].Groups[1].Value.Length - 5);
                    }
                    cityOneDayWeather.NightWeatherCode = dataBig[1].Groups[1].Value.Substring(5, dataBig[1].Groups[1].Value.Length - 5);//NightWeatherCode

                    //patten = @"<p\stitle=\""(.*)\""\sclass=\""wea\"">";
                    patten = @"<p(.|\s)*>([^\n]*)</p>";
                    rg = new Regex(patten, RegexOptions.Singleline);
                    var dataWeatherText = rg.Match(value.ToString()).Groups[2].Value;
                    cityOneDayWeather.DayWeatherText = dataWeatherText;//整天的天气信息

                    patten = @"<span>(.*\d+..)</span>";
                    rg = new Regex(patten, RegexOptions.Singleline);
                    var dataTemperatureHigh = rg.Match(value.ToString()).Groups[1].Value;
                    if (!string.IsNullOrEmpty(dataTemperatureHigh))
                    {
                        cityOneDayWeather.DayTemperature = dataTemperatureHigh;//最高温度（白天温度）
                    }

                    patten = @"<span>(.*\d+)</span>";
                    rg = new Regex(patten, RegexOptions.Singleline);
                    var dataTemperatureHigh1 = rg.Match(value.ToString()).Groups[1].Value;
                    if (!string.IsNullOrEmpty(dataTemperatureHigh1))
                    {
                        cityOneDayWeather.DayTemperature = dataTemperatureHigh1 + "℃";//最高温度（白天温度）
                    }

                    patten = @"<i>(.*\d+..)</i>";
                    rg = new Regex(patten, RegexOptions.Singleline);
                    var dataTemperatureLow = rg.Match(value.ToString()).Groups[1].Value;
                    cityOneDayWeather.NightTemperature = dataTemperatureLow;//最低温度（夜间温度）

                    patten = @"<span\stitle=\""(\w*)\""\sclass=";
                    rg = new Regex(patten, RegexOptions.Singleline);
                    var dataWind = rg.Matches(value.ToString());
                    if (dataWind.Count == 2)
                    {
                        cityOneDayWeather.DayWindDirection = dataWind[0].Groups[1].Value;//DayWindDirection
                        cityOneDayWeather.NightWindDiretion = dataWind[1].Groups[1].Value;//NightWindDirection
                    }
                    else
                    {
                        cityOneDayWeather.NightWindDiretion = dataWind[0].Groups[1].Value;
                    }


                    patten = @"<i>([\u4e00-\u9fa5]+)</i>";
                    rg = new Regex(patten, RegexOptions.Singleline);
                    var dataWindText = rg.Match(value.ToString()).Groups[1].Value;
                    cityOneDayWeather.WindText = dataWindText;//WindText

                    sevenDayWeathers.Add(cityOneDayWeather);
                }
                return sevenDayWeathers;
            }
            catch (Exception ex)
            {
                throw new Exception("获取城市7天天气信息失败！\n" + ex.Message);
            }
        }

        /// <summary>
        /// 获取7天8小时天气信息
        /// </summary>
        /// <param name="webpageStr"></param>
        /// <returns></returns>
        private static IList<IList<CityHourWeather>> GetCity8HourWeathers(string webpageStr)
        {
            try
            {
                IList<IList<CityHourWeather>> sevenDayEightHourWeathers = new List<IList<CityHourWeather>>();

                EightHoursWeatherJsonModel eightHoursWeatherJson = GetCity8HoursWeatherJson(webpageStr);
                foreach (var dayHourWeather in eightHoursWeatherJson.SevenDay)
                {
                    IList<CityHourWeather> eightHourWeathers = new List<CityHourWeather>();

                    foreach (var hourWeather in dayHourWeather)
                    {
                        CityHourWeather cityHourWeather = new CityHourWeather();
                        string[] weatherStr = hourWeather.Split(',');
                        for (int i = 0; i < weatherStr.Length; i++)
                        {
                            switch (i)
                            {
                                case 0:
                                    cityHourWeather.Hour = weatherStr[i];
                                    break;
                                case 1:
                                    cityHourWeather.WeatherCode = weatherStr[i];
                                    break;
                                case 2:
                                    cityHourWeather.WeatherText = weatherStr[i];
                                    break;
                                case 3:
                                    cityHourWeather.Temperature = weatherStr[i];
                                    break;
                                case 4:
                                    cityHourWeather.WindDirection = weatherStr[i];
                                    break;
                                case 5:
                                    cityHourWeather.WindText = weatherStr[i];
                                    break;
                                case 6:
                                    cityHourWeather.WindScale = weatherStr[i] + "级";
                                    break;
                            }
                        }
                        eightHourWeathers.Add(cityHourWeather);
                    }

                    if (eightHourWeathers.Count == 4)
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            CityHourWeather cityHourWeather = new CityHourWeather();
                            eightHourWeathers.Add(cityHourWeather);
                        }
                    }

                    sevenDayEightHourWeathers.Add(eightHourWeathers);
                }
                return sevenDayEightHourWeathers;
            }
            catch (Exception ex)
            {
                throw new Exception("获取城市7天8小时天气信息失败！\n" + ex.Message);
            }
        }

        /// <summary>
        /// 获取7天8小时的天气信息（返回EightHoursWeatherJsonModel，还需进一步分析获取）
        /// </summary>
        /// <param name="webpageStr"></param>
        /// <returns></returns>
        private static EightHoursWeatherJsonModel GetCity8HoursWeatherJson(string webpageStr)
        {
            try
            {
                string patten = @"<script>(.|\s)*?var\shour3data=(.*)(.|\s)*?</script>";
                Regex rg = new Regex(patten, RegexOptions.Multiline);
                string text8HoursWeather = rg.Match(webpageStr).Groups[2].Value;
                text8HoursWeather = text8HoursWeather.Replace("1d", "oneday");
                text8HoursWeather = text8HoursWeather.Replace("23d", "twoThreeday");
                text8HoursWeather = text8HoursWeather.Replace("7d", "sevenday");

                return JsonConvert.DeserializeObject<EightHoursWeatherJsonModel>(text8HoursWeather);
            }
            catch (Exception ex)
            {
                throw new Exception("城市8小时天气信息模型类获取失败！\n" + ex.Message);
            }
        }

        #endregion

        #region 获取WebPageSource

        /// <summary>
        /// 生成全部城市信息的信息类 _cityCodeModel
        /// </summary>
        /// <returns></returns>
        private static CityCodeModel GetCityCodeModel()
        {
            try
            {
                var textAllCityInfo = new StreamReader(@"CityCodeInfo.txt", Encoding.UTF8).ReadToEnd();

                return JsonConvert.DeserializeObject<CityCodeModel>(textAllCityInfo);
            }
            catch (Exception ex)
            {
                throw new Exception("生成城市信息模型类失败！\n" + ex.Message);
            }
        }

        /// <summary>
        /// 供查询用的全部城市信息类
        /// </summary>
        private static CityCodeModel _cityCodeModel;

        /// <summary>
        /// 查询城市信息
        /// </summary>
        /// <param name="readStr"></param>
        /// <returns></returns>
        public static IList<CityInfo> GetCityInfos(string readStr)
        {
            try
            {
                if (_cityCodeModel == null)
                {
                    _cityCodeModel = GetCityCodeModel();
                }

                IList<CityInfo> cityInfos = new List<CityInfo>();

                foreach (var provinceValue in _cityCodeModel.ProvinceList)
                {
                    foreach (var cityValue in provinceValue.City)
                    {
                        if (cityValue.CityName == readStr || cityValue.CityCode == readStr)
                        {
                            CityInfo cityInfo = new CityInfo()
                            {
                                CityName = cityValue.CityName,
                                CityCode = cityValue.CityCode
                            };
                            cityInfos.Add(cityInfo);
                            //break; //这里决定不break，因为担心有同一个省下面可能有相同的城市名？
                        }
                    }
                }

                return cityInfos;
            }
            catch (Exception ex)
            {
                throw new Exception("获取与输入匹配城市时发生错误！\n" + ex.Message);
            }
        }

        /// <summary>
        /// 生成URl
        /// </summary>
        /// <param name="cityCode"></param>
        /// <returns></returns>
        private static List<string> CombineWeatherUrl(string cityCode)
        {
            try
            {
                if (string.IsNullOrEmpty(cityCode))
                {
                    throw new Exception("传入CityCode为空！");
                }
                List<string> urlList = new List<string>()
                {
                string.Format("http://www.weather.com.cn/weather/{0}.shtml", cityCode),
                string.Format("http://www.weather.com.cn/weather1d/{0}.shtml", cityCode)
                };

                return urlList;
            }
            catch (Exception ex)
            {
                throw new Exception("生成URl失败！\n" + ex.Message);
            }
        }

        /// <summary>
        /// 生成WebpageSource
        /// </summary>
        /// <param name="urlList"></param>
        /// <returns></returns>
        private static List<string> CreateWebpageSource(List<string> urlList)
        {
            try
            {
                List<string> webpageSourceList = new List<string>();

                foreach (var urlStr in urlList)
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(urlStr);
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                    if (response.GetResponseStream() != Stream.Null)
                    {
                        StreamReader sr = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
                        webpageSourceList.Add(sr.ReadToEnd());
                    }
                    else
                    {
                        throw new Exception("返回页面信息为空！");
                    }
                }

                return webpageSourceList;
            }
            catch (Exception ex)
            {
                throw new Exception("获取页面信息失败！\n" + ex.Message);
            }
        }

        #endregion

    }//End public static class WeatherHelper
}
