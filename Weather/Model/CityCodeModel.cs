using System.Collections.Generic;

namespace Weather.Model
{
    public class CityCodeModel
    {
        public IList<ProvinceInfo> ProvinceList { get; set; }
    }//End public class CityCodeModel

    public class ProvinceInfo
    {
        public string Province { get; set; }
        public IList<CityInfo> City { get; set; } 
    }//End public class ProvinceInfo

    public class CityInfo
    {
        public string CityName { get; set; }
        public string CityCode { get; set; }
        public bool SelectStatus { get; set; }
    }//End public class ProvinceInfo
}
