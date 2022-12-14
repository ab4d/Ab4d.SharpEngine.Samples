#nullable disable

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
using System.Windows.Controls;
using System.Windows.Documents;

namespace Ab4d.SharpEngine.Samples.Wpf.Common
{
    // Replaces all "\\n" to Environment.NewLine
    public class LineBreakableStringConverter : IValueConverter
    {
        public object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            if (value is string valueString)
                return valueString.Replace("\\n", Environment.NewLine);

            return null;
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