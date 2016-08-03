using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace Weather.Helper
{
    public class VisualHelper
    {
        public static T FindParentOfType<T>(DependencyObject obj) where T : FrameworkElement
        {
            DependencyObject parent = VisualTreeHelper.GetParent(obj);
            while (parent != null)
            {
                if (parent is T)
                {
                    return (T) parent;
                }
                parent = VisualTreeHelper.GetParent(parent);
            }
            return null;
        }
    }//End public class VisulHelper
}//End namespace
