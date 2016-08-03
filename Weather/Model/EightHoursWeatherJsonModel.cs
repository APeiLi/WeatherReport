using System.Collections.Generic;
namespace Weather.Model
{
    public class EightHoursWeatherJsonModel
    {
        public IList<string> Oneday { get; set; }
        public IList<IList<string>> TwoThreeDay { get; set; }
        public IList<IList<string>> SevenDay { get; set; }
    }//End public class EightHoursWeatherJsonModel
}
