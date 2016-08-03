using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using Weather.Helper;
using Weather.Model;
using Weather.ViewModel;
using Button = System.Windows.Controls.Button;
using DragDropEffects = System.Windows.DragDropEffects;
using DragEventArgs = System.Windows.DragEventArgs;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace Weather
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            Closing += (s, e) => ViewModelLocator.Cleanup();

            RbOpenWeiXinSentNo.IsChecked = true;

            SetIcon();
        }

        /// <summary>
        /// 目标数据源
        /// </summary>
        private CityInfo _currentMouseOverItem;

        private bool _isDragFlag = true;
        private void SavedCityInfoDataGrid_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point point = e.GetPosition(SavedCityInfoDataGrid);
            HitTestResult result = VisualTreeHelper.HitTest(SavedCityInfoDataGrid, point);
            if (result == null)
            {
                return;
            }
            Button button = VisualHelper.FindParentOfType<Button>(result.VisualHit);
            _isDragFlag = button == null ? true : false;
        }

        private void SavedCityInfoDataGrid_OnMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && _isDragFlag)
            {
                if (SavedCityInfoDataGrid.SelectedItem == null)
                {
                    return;
                }

                DragDrop.DoDragDrop(SavedCityInfoDataGrid, SavedCityInfoDataGrid.SelectedItem, DragDropEffects.Move);
            }
        }

        private void SavedCityInfoDataGrid_OnDragEnter(object sender, DragEventArgs e)
        {
            Point point = e.GetPosition(SavedCityInfoDataGrid);
            HitTestResult result = VisualTreeHelper.HitTest(SavedCityInfoDataGrid, point);
            if (result != null)
            {
                DataGridRow row = VisualHelper.FindParentOfType<DataGridRow>(result.VisualHit);
                if (row == null)
                {
                    return;
                }

                _currentMouseOverItem = row.DataContext as CityInfo;

                e.Effects = _currentMouseOverItem != null ? DragDropEffects.Move : DragDropEffects.None;
            }
        }

        private void SavedCityInfoDataGrid_OnDrop(object sender, DragEventArgs e)
        {
            (this.DataContext as MainViewModel).ChangeSavedCityListByDrop(SavedCityInfoDataGrid.SelectedItem as CityInfo, _currentMouseOverItem);
        }

        private void SavedCityInfoDataGrid_OnPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                (this.DataContext as MainViewModel).DoubleClickItemGoToTop();
            }
        }

        private NotifyIcon _notifyIcon;
        private void SetIcon()
        {
            _notifyIcon = new NotifyIcon();
            _notifyIcon.Icon = new System.Drawing.Icon("WeatherSmall.ico");
            _notifyIcon.Visible = false;

            //打开菜单项
            System.Windows.Forms.MenuItem open = new System.Windows.Forms.MenuItem("还原窗口");
            open.Click += new EventHandler(Show);
            //切换微信推送菜单项
            System.Windows.Forms.MenuItem weixinSentChange = new System.Windows.Forms.MenuItem("打开微信推送");
            weixinSentChange.Click += new EventHandler(WeixinSentChange);
            //退出菜单项
            System.Windows.Forms.MenuItem exit = new System.Windows.Forms.MenuItem("退出");
            exit.Click += new EventHandler(Close);

            //关联托盘控件
            System.Windows.Forms.MenuItem[] childenMenuItems = new System.Windows.Forms.MenuItem[] { open, weixinSentChange, exit };
            _notifyIcon.ContextMenu = new System.Windows.Forms.ContextMenu(childenMenuItems);

            _notifyIcon.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(Show);
        }

        private void Show(object sender, EventArgs e)
        {
            _notifyIcon.Visible = false;
            WindowState = WindowState.Normal;
            Visibility = Visibility.Visible;
            ShowInTaskbar = true;
            Activate();
        }

        private void WeixinSentChange(object sender, EventArgs e)
        {
            (this.DataContext as MainViewModel).OpenWeiXinSent = (this.DataContext as MainViewModel).OpenWeiXinSent
                ? false
                : true;

            _notifyIcon.ContextMenu.MenuItems[1].Text = (this.DataContext as MainViewModel).OpenWeiXinSent
                ? "关闭微信推送"
                : "打开微信推送";

            string openWeiXinSent = (this.DataContext as MainViewModel).OpenWeiXinSent ? "已开启" : "未开启";
            string text = string.Format("天气监测与推送程序\n微信推送：{0}\n后台运行中...", openWeiXinSent);
            _notifyIcon.Text = text;
        }

        private void Close(object sender, EventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        private void MainWindow_OnStateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                ShowInTaskbar = false;
                _notifyIcon.Visible = true;

                _notifyIcon.ShowBalloonTip(1000, "提示:", "程序将在后台继续运行。", ToolTipIcon.Info);

                string openWeiXinSent = (this.DataContext as MainViewModel).OpenWeiXinSent ? "已开启" : "未开启";
                string text = string.Format("天气监测与推送程序\n微信推送：{0}\n后台运行中...", openWeiXinSent);
                _notifyIcon.Text = text;

                _notifyIcon.ContextMenu.MenuItems[1].Text = (this.DataContext as MainViewModel).OpenWeiXinSent
                    ? "关闭微信推送"
                    : "打开微信推送";
            }
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            var mbr = System.Windows.MessageBox.Show("确定退出吗？", "系统提示", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (mbr != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                (this.DataContext as MainViewModel).WriteLog("退出程序");
            }
            catch (Exception ex)
            {
                (this.DataContext as MainViewModel).WriteLog("退出程序竟然都发生错误！*了狗！\n" + ex.Message);
            }
        }
    }//End public partial MainWindow
}