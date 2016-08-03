using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using Weather.Helper;
using Weather.Model;

namespace Weather.ViewModel
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// See http://www.mvvmlight.net
    /// </para>
    /// </summary>
    public partial class MainViewModel : ViewModelBase
    {
        public MainViewModel()
        {
            FindCityInfoList = new ObservableCollection<CityInfo>();
            SavedCityInfoList = new ObservableCollection<CityInfo>();
            SearchCityCommand = new RelayCommand(SearchCityAndSaveCityInfo);
            DeleteCityInfoXmlCommand = new RelayCommand(DeleteSavedCity);
            RefreshWeatherNow = new RelayCommand(OrderInit);
            AllWeahterModelList = new List<WeatherModel>();
            RecordWeatherModelList = new List<WeatherModel>();

            WriteLog("开始程序");

            TaskWhileInit();
        }

        private void TaskWhileInit()
        {
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    WriteLog("\t自动更新开始");
                    #region 其他初始化天气信息的Task通通取消

                    if (TokenSourceInitWeatherInfo.Token.CanBeCanceled)
                    {
                        TokenSourceInitWeatherInfo.Cancel();
                    }

                    #endregion

                    while (!InitComplete) //当有其他的更新正在进行时，就等待其完成
                    {
                        WriteLog("\t发现有未完成任务，自动更新等待中...");
                        Thread.Sleep(1000);
                    }

                    InitAllWeatherInfo();

                    WriteLog("\t自动更新结束");
                    Thread.Sleep(TimingInitInt);
                }
            });
        }

        /// <summary>
        /// 排队初始化天气信息（等待（一定的时间，让）主线程中的初始化完成后再进行当前的Task）
        /// </summary>
        private void OrderInit()
        {
            if (TokenSourceInitWeatherInfo.Token.CanBeCanceled)
            {
                TokenSourceInitWeatherInfo.Cancel();
            }

            TokenSourceInitWeatherInfo = new CancellationTokenSource();
            CancellationToken token = TokenSourceInitWeatherInfo.Token;

            Task.Factory.StartNew(() =>
            {
                if (InitComplete)
                {
                    WriteLog("\t手动更新开始");
                    InitAllWeatherInfo();
                    WriteLog("\t手动刷新结束");
                }
                else
                {
                    ShowErrorInfoStr("手动刷新操作已拦截。检测到有初始化任务尚未完成，本次天气信息刷新将在一分钟之后进行。");

                    token.WaitHandle.WaitOne(600000);

                    if (InitComplete)
                    {
                        InitAllWeatherInfo();
                    }
                }
            }, token);
        }

        /// <summary>
        /// 初始化天气信息
        /// </summary>
        public void InitAllWeatherInfo()
        {
            InitComplete = false;

            WriteLog("\t更新天气");

            try
            {
                ShowErrorInfoStr("正在初始化城市信息...");
                GetSavedCityFromXml();
                ShowErrorInfoStr("城市信息初始化完毕！");

                ShowErrorInfoStr("正在抓取天气信息...", 100000);
                WeatherInit();
                ShowErrorInfoStr("天气信息初始化完毕！");

                ShowErrorInfoStr("正在分析数据，以决定是否通过微信推送...");
                WeiXinSent();
                ShowErrorInfoStr("微信推送过程结束！");
            }
            catch (Exception ex)
            {
                ShowErrorInfoStr("初始化天气信息中发生错误！\n" + ex.Message);
            }

            WriteLog("\t完成更新");

            InitComplete = true;
        }

        private void WeatherInit()
        {
            AlarmCityMenuModel alarmCityMenuModel = WeatherHelper.NewAlarmCityMenuModel();

            AllWeahterModelList.Clear();

            foreach (var city in SavedCityInfoList)
            {
                try
                {
                    WeatherModel getOneWeatherModel = WeatherHelper.GetWeatherModel(city.CityCode, alarmCityMenuModel);
                    AllWeahterModelList.Add(getOneWeatherModel);
                }
                catch (Exception ex)
                {
                    ShowErrorInfoStr(city.CityName + " 天气更新时发生一个错误！\n" + ex.Message);
                }
            }

            //默认显示第一个城市 和第一天的8小时天气
            if (AllWeahterModelList.Count > 0)
            {
                GetWeatherModel = AllWeahterModelList[0];
                if (GetWeatherModel.Weather8Hours.Count > 0)
                {
                    GetCityHourWeather = GetWeatherModel.Weather8Hours[0].ToList();
                }
            }
        }

        private void WeiXinSent()
        {
            if (AllWeahterModelList.Count == 0)
            {
                return;
            }

            //读出日志信息
            string logString = string.Empty;
            try
            {
                if (File.Exists(System.Windows.Forms.Application.StartupPath + @"\Log.txt"))
                {
                    StreamReader sr = new StreamReader(System.Windows.Forms.Application.StartupPath + @"\Log.txt", Encoding.UTF8);
                    logString = sr.ReadToEnd();
                    sr.Close();
                    sr.Dispose();
                }
            }
            catch (Exception ex)
            {
                ShowErrorInfoStr("微信推送时加载日志记录失败！\n" + ex.Message);
            }

            try
            {
                if (!OpenWeiXinSent)
                {
                    //更新RecordWeatherModelList
                    RecordWeatherModelList.Clear();
                    foreach (var model in AllWeahterModelList)
                    {
                        WeatherModel weather = new WeatherModel()
                        {
                            CityCode = model.CityCode,
                            AlarmStr = model.AlarmStr,
                            AlarmStrList = model.AlarmStrList
                        };

                        RecordWeatherModelList.Add(weather);
                    }

                    return;
                }

                //推送时间验证
                if (!(StartHourInt == EndHourInt && StartMinuteInt == EndMinuteInt)) //假如开始时间等于结束时间，那么跳过验证
                {
                    if (StartHourInt < EndHourInt || (StartHourInt == EndHourInt && StartMinuteInt < EndMinuteInt))
                    {
                        int hourNow = Convert.ToInt32(DateTime.Now.Hour.ToString());
                        int minuteNow = Convert.ToInt32(DateTime.Now.Minute.ToString());

                        if (!((hourNow > StartHourInt && hourNow < EndHourInt) || (hourNow == StartHourInt && minuteNow >= StartMinuteInt) || (hourNow == EndHourInt && minuteNow <= EndMinuteInt)))
                        {
                            RecordWeatherModelList.Clear();//清空记录，当到了预定时间之后，可以与日志比较。把没发过的一波推送
                            return;
                        }
                    }
                }

                if (RecordWeatherModelList.Count == 0)
                {
                    List<List<string>> alarmStrList = new List<List<string>>();

                    foreach (var model in AllWeahterModelList)
                    {
                        WeatherModel weather = new WeatherModel()
                        {
                            CityCode = model.CityCode,
                            AlarmStr = model.AlarmStr,
                            AlarmStrList = model.AlarmStrList
                        };

                        RecordWeatherModelList.Add(weather);

                        if (!string.IsNullOrEmpty(model.AlarmStr))
                        {
                            alarmStrList.Add(model.AlarmStrList.ToList());
                        }
                    }

                    //排除重复的预警信息
                    if (alarmStrList.Count > 0)
                    {
                        for (int i = 0; i < alarmStrList.Count; i++)
                        {
                            for (int j = 0; j < alarmStrList[i].Count; j++)
                            {
                                for (int k = i + 1; k < alarmStrList.Count && k > i; k++)
                                {
                                    for (int l = 0; l < alarmStrList[k].Count && l >= 0 && k > i; l++)
                                    {
                                        if (alarmStrList[i][j] == alarmStrList[k][l])
                                        {
                                            alarmStrList[k].RemoveAt(l);
                                            l--;

                                            if (alarmStrList[k].Count == 0)
                                            {
                                                alarmStrList.RemoveAt(k);
                                                k--;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    //用日志的信息继续过滤需要发送的信息，日志中出现过的就不再发送
                    List<string> readSentStrList = new List<string>();
                    int breakNumber = 0;
                    foreach (var strList in alarmStrList)
                    {
                        string readySentStr = string.Empty;

                        foreach (var s in strList)
                        {
                            //假如在日志中发现已经发送过该条信息，那么就跳过
                            if (logString.Contains(s))
                            {
                                breakNumber++;
                                break;
                            }
                            
                            readySentStr += s + "\n";
                        }

                        if (!string.IsNullOrEmpty(readySentStr))
                        {
                            readSentStrList.Add(readySentStr);
                        }
                    }

                    if (breakNumber > 0)
                    {
                        ShowErrorInfoStr(breakNumber + " 条数据已被过滤。");
                    }


                    if (readSentStrList.Count > 0)
                    {
                        WeiXinHelper.WeiXinSentMessage("********↓↓↓********");
                    }

                    foreach (var str in readSentStrList)
                    {
                        try
                        {
                            WeiXinHelper.WeiXinSentMessage(str);

                            WriteLog("\r\n\t***********************************************************\r\n\t微信推送：\r\n\r\n\t\t\t" + str);
                        }
                        catch (Exception ex)
                        {
                            try
                            {
                                WeiXinHelper.WeiXinSentMessage("信息过长，已被截取：\n    " + str.Substring(0, 980));
                                ShowErrorInfoStr("循环调用微信推送时发生一个错误！\n" + str + "\n" + ex.Message);
                            }
                            catch (Exception) { }
                        }
                    }

                    if (readSentStrList.Count > 0)
                    {
                        WeiXinHelper.WeiXinSentMessage("********↑↑↑********");
                        WriteLog("\r\n\t***********************************************************\r\n");
                    }

                }
                else
                {
                    List<List<string>> alarmStrList = new List<List<string>>();

                    foreach (var model in AllWeahterModelList)
                    {
                        bool findSameModel = false;

                        foreach (var recordModel in RecordWeatherModelList)
                        {
                            if (model.CityCode == recordModel.CityCode)
                            {
                                findSameModel = true;

                                if (!string.IsNullOrEmpty(model.AlarmStr) && !string.Equals(model.AlarmStr, recordModel.AlarmStr))
                                {
                                    if (string.IsNullOrEmpty(recordModel.AlarmStr))
                                    {
                                        alarmStrList.Add(model.AlarmStrList.ToList());
                                    }
                                    else
                                    {
                                        List<string> tempAlarmStrList = new List<string>();
                                        foreach (var modelStr in model.AlarmStrList)
                                        {
                                            bool canBeAdd = true;
                                            foreach (var recordModelStr in recordModel.AlarmStrList)
                                            {
                                                if (string.Equals(modelStr, recordModelStr))
                                                {
                                                    canBeAdd = false;
                                                }
                                            }

                                            if (canBeAdd)
                                            {
                                                tempAlarmStrList.Add(modelStr + "\n");
                                            }
                                        }

                                        if (tempAlarmStrList.Count > 0)
                                        {
                                            alarmStrList.Add(tempAlarmStrList);
                                        }
                                    }
                                }
                            }

                            break;//不会同时有两个相同的城市，所以只要找到了相同的城市名，那么就跳出
                        }

                        if (!findSameModel && model.AlarmStrList.Count > 0)
                        {
                            alarmStrList.Add(model.AlarmStrList.ToList());
                        }
                    }

                    //排除重复的预警信息
                    if (alarmStrList.Count > 0)
                    {
                        for (int i = 0; i < alarmStrList.Count; i++)
                        {
                            for (int j = 0; j < alarmStrList[i].Count; j++)
                            {
                                for (int k = i + 1; k < alarmStrList.Count && k > i; k++)
                                {
                                    for (int l = 0; l < alarmStrList[k].Count && l >= 0 && k > i; l++)
                                    {
                                        if (alarmStrList[i][j] == alarmStrList[k][l])
                                        {
                                            alarmStrList[k].RemoveAt(l);
                                            l--;

                                            if (alarmStrList[k].Count == 0)
                                            {
                                                alarmStrList.RemoveAt(k);
                                                k--;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }


                    //用日志的信息继续过滤需要发送的信息，日志中出现过的就不再发送
                    List<string> readSentStrList = new List<string>();
                    int breakNumber = 0;
                    foreach (var strList in alarmStrList)
                    {
                        string readySentStr = string.Empty;

                        foreach (var s in strList)
                        {
                            //假如在日志中发现已经发送过该条信息，那么就跳过
                            if (logString.Contains(s))
                            {
                                breakNumber++;
                                break;
                            }

                            readySentStr += s + "\n";
                        }

                        if (!string.IsNullOrEmpty(readySentStr))
                        {
                            readSentStrList.Add(readySentStr);
                        }
                    }

                    if (breakNumber > 0)
                    {
                        ShowErrorInfoStr(breakNumber + " 条数据已被过滤。");
                    }


                    if (readSentStrList.Count > 0)
                    {
                        WeiXinHelper.WeiXinSentMessage("********↓↓↓********");
                    }

                    foreach (var str in readSentStrList)
                    {
                        try
                        {
                            WeiXinHelper.WeiXinSentMessage(str);

                            WriteLog("\r\n\t***********************************************************\r\n\t微信推送：\r\n\r\n\t\t\t" + str);
                        }
                        catch (Exception ex)
                        {
                            try
                            {
                                WeiXinHelper.WeiXinSentMessage("信息过长，已被截取：\n" + str.Substring(0, 980));
                                ShowErrorInfoStr("循环调用微信推送时发生一个错误！\n" + str + "\n" + ex.Message);
                            }
                            catch (Exception) { }
                        }
                    }

                    if (readSentStrList.Count > 0)
                    {
                        WeiXinHelper.WeiXinSentMessage("********↑↑↑********");
                        WriteLog("\r\n\t***********************************************************\r\n");
                    }

                    //更新RecordWeatherModelList
                    RecordWeatherModelList.Clear();
                    foreach (var model in AllWeahterModelList)
                    {
                        WeatherModel weather = new WeatherModel
                        {
                            CityCode = model.CityCode,
                            AlarmStr = model.AlarmStr,
                            AlarmStrList = model.AlarmStrList
                        };

                        RecordWeatherModelList.Add(weather);
                    }
                }
            }
            catch (Exception ex)
            {
                ShowErrorInfoStr("调用微信推送时发生一个错误！\n" + ex.Message);
            }

        }

        private void ShowCurrentWeatherModel()
        {
            try
            {
                if (ListSelectItemCityInfo == null || AllWeahterModelList == null)
                {
                    GetWeatherModel = null;
                    return;
                }

                bool findModel = false;

                foreach (var model in AllWeahterModelList)
                {
                    if (model.CityCode == ListSelectItemCityInfo.CityCode)
                    {
                        GetWeatherModel = model;


                        if (model.Weather7Days.Count > 0)
                        {
                            ListSelectItemCityDayWeather = model.Weather7Days[0];
                            findModel = true;
                        }
                        break;
                    }
                }

                if (!findModel)
                {
                    GetWeatherModel = null;
                }
            }
            catch (Exception ex)
            {
                ShowErrorInfoStr("显示当前城市选项天气是发生错误！\n" + ex.Message);
            }
        }

        private void ShowCurrent8HourWeather()
        {
            if (ListSelectItemCityDayWeather != null && GetWeatherModel.Weather8Hours.Count > 0)
            {
                GetCityHourWeather =
                    GetWeatherModel.Weather8Hours[GetWeatherModel.Weather7Days.IndexOf(ListSelectItemCityDayWeather)]
                        .ToList();
            }
            else
            {
                GetCityHourWeather = null;
            }
        }

        private void DeleteSavedCity()
        {
            if (!InitComplete)
            {
                MessageBox.Show("天气信息正在排队初始化中，现在不能删除城市，请稍后再试！", "系统提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBoxResult mbr = MessageBox.Show("确定删除所有勾选项吗？", "系统提示", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (mbr != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                List<CityInfo> waitDeleteCityInfoList = (from cityInfo in SavedCityInfoList
                                                         where cityInfo.SelectStatus
                                                         select cityInfo).ToList();

                if (waitDeleteCityInfoList.Count == 0)
                {
                    WriteLog("\t删除城市：" + DataGridSelectItemCityInfo.CityName);

                    XmlHelper.DeleteFromXml(DataGridSelectItemCityInfo);
                    SavedCityInfoList.Remove(DataGridSelectItemCityInfo);
                }
                else
                {
                    foreach (var delectCity in waitDeleteCityInfoList)
                    {
                        XmlHelper.DeleteFromXml(delectCity);
                        SavedCityInfoList.Remove(delectCity);
                    }

                    string deleteCity = string.Empty;
                    foreach (var cityInfo in waitDeleteCityInfoList)
                    {
                        deleteCity += cityInfo.CityName + " ";
                    }
                    WriteLog("\t删除城市：" + deleteCity);
                }
            }
            catch (Exception ex)
            {
                ShowErrorInfoStr("城市删除时发生错误！\n" + ex.Message);
            }
        }

        public void GetSavedCityFromXml()
        {
            try
            {
                if (SavedCityInfoList.Count != 0)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        SavedCityInfoList.Clear();
                    });
                }

                foreach (var city in XmlHelper.ReadFromXml())
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        SavedCityInfoList.Add(city);
                    });
                }

                if (SavedCityInfoList.Count > 0)
                {
                    ListSelectItemCityInfo = SavedCityInfoList[0];
                }
            }
            catch (Exception ex)
            {
                ShowErrorInfoStr("从XML中取出已保存城市过程中发生错误！\n" + ex.Message);
            }
        }

        public void SearchCityAndSaveCityInfo()
        {
            if (!InitComplete)
            {
                MessageBox.Show("天气信息正在排队初始化中，现在不能新增城市，请稍后再试！", "系统提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (string.IsNullOrEmpty(InputCityNameStr))
                {
                    MessageBox.Show("输入城市名为空！", "系统提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                FindCityInfoList.Clear();

                foreach (var city in WeatherHelper.GetCityInfos(InputCityNameStr))
                {
                    FindCityInfoList.Add(city);
                }


                #region 加入保存列表、刷新显示

                if (FindCityInfoList.Count == 0)
                {
                    MessageBox.Show("未查询到该城市信息！\n\n仅能查询国内非港澳台地区！", "系统提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                foreach (var savedCity in SavedCityInfoList)
                {
                    foreach (var findCity in FindCityInfoList)
                    {
                        if (savedCity.CityName == findCity.CityName && savedCity.CityCode == findCity.CityCode)
                        {
                            MessageBox.Show("保存失败！保存过程已被拦截。\n" + savedCity.CityName + "已经存在！", "系统提示", MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                            return;
                        }
                    }
                }

                foreach (var city in FindCityInfoList)
                {
                    SavedCityInfoList.Add(city);
                }

                XmlHelper.WriteToXml(SavedCityInfoList.ToList());

                FindCityInfoList.Clear();

                //ShowErrorInfoStr("请添加完所有城市后手动刷新天气信息！");

                WriteLog("\t添加城市：" + InputCityNameStr);

                MessageBox.Show(InputCityNameStr + " 添加成功！它将排末尾。\n请添加完所有城市后手动刷新天气信息！", "系统提示", MessageBoxButton.OK, MessageBoxImage.Information);
                InputCityNameStr = string.Empty;

                #endregion
            }
            catch (Exception ex)
            {
                ShowErrorInfoStr("添加城市过程中发生一个错误！\n" + ex.Message);
            }
        }

        public void ChangeSavedCityListByDrop(CityInfo sourceItem, CityInfo targetItem)
        {
            if (!InitComplete)
            {
                MessageBox.Show("天气信息正在排队初始化中，现在不能改变城市顺序，请稍后再试！", "系统提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            InitComplete = false;

            try
            {
                SavedCityInfoList.Move(SavedCityInfoList.IndexOf(sourceItem), SavedCityInfoList.IndexOf(targetItem));

                Task.Factory.StartNew(() =>
                {
                    XmlHelper.WriteToXml(SavedCityInfoList.ToList());
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("已保存城市顺序调整失败！\n" + ex.Message, "系统提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            InitComplete = true;
        }

        public void DoubleClickItemGoToTop()
        {
            if (!InitComplete)
            {
                MessageBox.Show("天气信息正在排队初始化中，现在不能改变城市顺序，请稍后再试！", "系统提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            InitComplete = false;

            try
            {
                if (DataGridSelectItemCityInfo != null)
                {
                    SavedCityInfoList.Move(SavedCityInfoList.IndexOf(DataGridSelectItemCityInfo), 0);

                    Task.Factory.StartNew(() =>
                    {
                        XmlHelper.WriteToXml(SavedCityInfoList.ToList());
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("已保存城市顺序调整失败！\n" + ex.Message, "系统提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            InitComplete = true;
        }

        public void WriteLog(string logStr)
        {
            try
            {
                if (string.IsNullOrEmpty(logStr))
                {
                    return;
                }

                lock (_logStack)
                {
                    _logStack.Push(logStr);
                }

                Task.Factory.StartNew(() =>
                {
                    lock (_copyStack)
                    {
                        lock (_logStack)
                        {
                            while (_logStack.Count > 0)
                            {
                                _copyStack.Push(_logStack.Pop());
                            }
                        }

                        while (_copyStack.Count > 0)
                        {
                            LogHelper.SpecialWriteToLog(_copyStack.Pop());
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                WeiXinHelper.WeiXinSentMessage("程序已终止！" + "日志写入错误！\n" + ex.Message);//通过微信通知我，程序发生了错误！
                throw new Exception("日志写入错误！\n" + ex.Message);//终止程序
            }
        }

        public void ShowErrorInfoStr(string errorStr, int durationOfTime = 10000)
        {
            WriteLog("\t\t程序进程记录：" + errorStr);

            if (TokenSourceAppErrorInfoStr.Token.CanBeCanceled)
            {
                TokenSourceAppErrorInfoStr.Cancel();
            }

            TokenSourceAppErrorInfoStr = new CancellationTokenSource();
            CancellationToken token = TokenSourceAppErrorInfoStr.Token;

            Task.Factory.StartNew(() =>
            {
                AppErrorInfoStr = errorStr;

                token.WaitHandle.WaitOne(durationOfTime);

                if (!token.IsCancellationRequested)
                {
                    AppErrorInfoStr = string.Empty;
                }
            }, token);
        }

        /// <summary>
        /// 主线程中弹出messagebox，无返回值
        /// </summary>
        /// <param name="message"></param>
        public void ShowMessageInAppCurrent(string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(message, "系统提示", MessageBoxButton.OK, MessageBoxImage.Information);
            });
        }
    }//End public class
}//End namespace