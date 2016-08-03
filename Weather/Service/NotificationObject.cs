using System.ComponentModel;

namespace Weather.Service
{
    public class NotificationObject:INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged.Invoke(this,new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
