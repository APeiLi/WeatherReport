using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows.Input;
using mshtml;
using Weather.Model;

namespace Weather.ViewModel
{
    public partial class MainViewModel
    {
        #region Properties

        private string _inputCityNameStr;
        /// <summary>
        /// 城市名称，用于记录用户输入值
        /// </summary>
        public string InputCityNameStr
        {
            get { return _inputCityNameStr; }
            set { _inputCityNameStr = value; base.RaisePropertyChanged(); }
        }

        private List<WeatherModel> AllWeahterModelList { get; set; }

        private List<WeatherModel> RecordWeatherModelList { get; set; }

        private WeatherModel _getWeatherModel;
        /// <summary>
        /// 用于实际显示的天气信息集合类
        /// </summary>
        public WeatherModel GetWeatherModel
        {
            get { return _getWeatherModel; }
            set { _getWeatherModel = value; base.RaisePropertyChanged(); }
        }

        private List<CityHourWeather> _getCityHourWeather;
        /// <summary>
        /// 用于实际显示的天气信息集合类
        /// </summary>
        public List<CityHourWeather> GetCityHourWeather
        {
            get { return _getCityHourWeather; }
            set { _getCityHourWeather = value; base.RaisePropertyChanged(); }
        }

        private CityInfo _dataGridselectItemCityInfo;
        /// <summary>
        /// Datagrid --已保存城市当前的选择项
        /// </summary>
        public CityInfo DataGridSelectItemCityInfo
        {
            get { return _dataGridselectItemCityInfo; }
            set { _dataGridselectItemCityInfo = value; base.RaisePropertyChanged(); }
        }

        private CityInfo _listSelectItemCityInfo;
        /// <summary>
        /// List --天气显示界面中，当前城市的选择项
        /// </summary>
        public CityInfo ListSelectItemCityInfo
        {
            get { return _listSelectItemCityInfo; }
            set
            {
                _listSelectItemCityInfo = value; base.RaisePropertyChanged();
                ShowCurrentWeatherModel();
            }
        }

        private CityDayWeather _listSelectItemCityDayWeather;
        /// <summary>
        /// List --7天天气显示中，“当前天”的选择项
        /// </summary>
        public CityDayWeather ListSelectItemCityDayWeather
        {
            get { return _listSelectItemCityDayWeather; }
            set
            {
                _listSelectItemCityDayWeather = value; base.RaisePropertyChanged();
                ShowCurrent8HourWeather();
            }
        }

        private WebpageSource _getWebpageSource = new WebpageSource();
        /// <summary>
        /// 获取从View端传回来的WebpageSource
        /// </summary>
        public WebpageSource GetWebpageSource
        {
            get { return _getWebpageSource; }
            set { _getWebpageSource = value; base.RaisePropertyChanged(); }
        }

        public class WebpageSource
        {
            public string CityCode { get; set; }
            public string WebpageSourceStr { get; set; }
        }

        public ObservableCollection<CityInfo> FindCityInfoList { get; set; }

        public ObservableCollection<CityInfo> SavedCityInfoList { get; set; }

        private string _appErrorInfoStr;
        /// <summary>
        /// 程序错误信息记录
        /// </summary>
        public string AppErrorInfoStr
        {
            get { return _appErrorInfoStr; }
            set { _appErrorInfoStr = value; base.RaisePropertyChanged(); }
        }

        /// <summary>
        /// 控制ShowErrorInfoStr方法。避免在显示时出现这种情况：上一个显示即将结束时，后一个已经显示了信息；而上一个在显示结束后将清空显示类容，却错误的导致把后一个刚刚才显示的信息也消掉了
        /// </summary>
        public CancellationTokenSource TokenSourceAppErrorInfoStr = new CancellationTokenSource();

        /// <summary>
        /// 控制天气信息的初始化。避免同时有两个Task在初始化天气信息。
        /// </summary>
        public CancellationTokenSource TokenSourceInitWeatherInfo = new CancellationTokenSource();

        /// <summary>
        /// 天气信息初始化完成标记
        /// </summary>
        public bool InitComplete = true;

        private bool _openWeiXinSent;
        /// <summary>
        /// 是否开启微信推送
        /// </summary>
        public bool OpenWeiXinSent
        {
            get { return _openWeiXinSent; }
            set { _openWeiXinSent = value; base.RaisePropertyChanged(); }
        }

        private int _timingInitInt = 900000;
        /// <summary>
        /// 天气信息刷新时间
        /// </summary>
        public int TimingInitInt
        {
            get { return _timingInitInt; }
            set { _timingInitInt = value; base.RaisePropertyChanged(); }
        }

        #region 微信推送时间属性

        private List<TimeAndNameModel> _twentyFourHourList;

        public List<TimeAndNameModel> TwentyFourHourList
        {
            get
            {
                if (_twentyFourHourList == null)
                {
                    _twentyFourHourList = new List<TimeAndNameModel>();
                    for (int i = 0; i < 24; i++)
                    {
                        _twentyFourHourList.Add(new TimeAndNameModel()
                        {
                            TimeChineseName = i + "点",
                            TimeInt = i
                        });
                    }
                }
                return _twentyFourHourList;
            }
            set { _twentyFourHourList = value; base.RaisePropertyChanged(); }
        }

        private int _startHourInt = 8;

        public int StartHourInt
        {
            get { return _startHourInt; }
            set { _startHourInt = value; base.RaisePropertyChanged(); }
        }

        private int _endHourInt = 18;

        public int EndHourInt
        {
            get { return _endHourInt; }
            set { _endHourInt = value; base.RaisePropertyChanged(); }
        }

        private List<TimeAndNameModel> _sixtyMinuteList;

        public List<TimeAndNameModel> SixtyMinuteList
        {
            get
            {
                if (_sixtyMinuteList == null)
                {
                    _sixtyMinuteList = new List<TimeAndNameModel>();
                    for (int i = 0; i < 60; i++)
                    {
                        _sixtyMinuteList.Add(new TimeAndNameModel()
                        {
                            TimeChineseName = i + "分",
                            TimeInt = i
                        });
                    }
                }
                return _sixtyMinuteList;
            }
            set { _sixtyMinuteList = value; base.RaisePropertyChanged(); }
        }

        private int _startMinuteInt = 30;

        public int StartMinuteInt
        {
            get { return _startMinuteInt; }
            set { _startMinuteInt = value; base.RaisePropertyChanged(); }
        }

        private int _endMinuteInt;

        public int EndMinuteInt
        {
            get { return _endMinuteInt; }
            set { _endMinuteInt = value; base.RaisePropertyChanged(); }
        }

        #endregion

        private readonly Stack<string> _logStack = new Stack<string>();
        private readonly Stack<string> _copyStack = new Stack<string>();
        #endregion


        #region ICommand

        public ICommand SearchCityCommand { get; set; }
        public ICommand AddCityInfoToXmlCommand { get; set; }
        public ICommand DeleteCityInfoXmlCommand { get; set; }
        public ICommand RefreshWeatherNow { get; set; }

        #endregion
    }//End public partial class
}//End namespace
