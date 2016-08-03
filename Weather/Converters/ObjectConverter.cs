using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Weather.Converters
{
    public class ObjectConverter:IValueConverter
    {
        /// <summary>
        /// 通用转换方法
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string[] param = parameter.ToString().ToLower().Split(':');
            if (value == null)
            {
                return param[1];
            }

            string valueStr = value.ToString().ToLower();
            if (param[0].Contains("|"))
            {
                string[] conpareArray = param[0].Split('|');
                return conpareArray.Contains(valueStr) ? param[1] : param[2];
            }
            else
            {
                return valueStr.Equals(param[0]) ? param[1] : param[2];
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            return !System.Convert.ToBoolean(value);
        }
    }//End public class ObjectConverter
}//End namespace
