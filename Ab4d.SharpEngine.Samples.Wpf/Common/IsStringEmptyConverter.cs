using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security;
using System.Windows;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Xml;

namespace Ab4d.SharpEngine.Samples.Wpf.Common
{
    // From: Kevin Moore's Bag-o-Tricks (http://j832.com/bagotricks)
    public class IsStringEmptyConverter : IValueConverter
    {
        public object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            if (value is XmlElement xmlElement)
            {
                var descriptionAttribute = xmlElement.Attributes["Description"];
                if (descriptionAttribute != null)
                {
                    if (!string.IsNullOrEmpty(descriptionAttribute.Value))
                    {
                        if (targetType == typeof(Visibility))
                            return Visibility.Visible;
                         
                        return true;
                    }
                }
            }

            if (targetType == typeof(Visibility))
                return Visibility.Collapsed;
             
            return false;
        }

        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}