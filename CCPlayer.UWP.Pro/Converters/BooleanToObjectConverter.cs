﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media.Imaging;

namespace CCPlayer.UWP.Converters
{
    public class BooleanToObjectConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return (bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return (bool)value;
        }
    }
}
