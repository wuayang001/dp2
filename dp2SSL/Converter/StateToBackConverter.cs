﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

using DigitalPlatform.Text;

namespace dp2SSL
{
    public class StateToBackConverter : IValueConverter
    {
        public Color OpenColor { get; set; }
        public Color CloseColor { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((string)value == "open")
                return new SolidColorBrush(OpenColor);

            return new SolidColorBrush(CloseColor);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class DoorToBackConverter : IMultiValueConverter
    {
        public Color OpenColor { get; set; }
        public Color CloseColor { get; set; }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            DoorItem door = (values[0] as DoorItem);
            string state = values[1] as string;
            /*
            if (state == "open")
                return new SolidColorBrush(OpenColor);

            return new SolidColorBrush(CloseColor);
            */
            /*
            if (state == "open")
                return new SolidColorBrush(door.OpenBrush);

            return new SolidColorBrush(door.CloseBrush);
            */
            Brush brush = null;
            if (state == "open")
                brush = door.OpenBrush;
            else
                brush = door.CloseBrush;
            /*
            if (brush is ImageBrush)
            {
                var imageBrush = brush as ImageBrush;
                return new ImageBrush(imageBrush.ImageSource.Clone());
            }
            */

            return brush;
            /*
            BrushConverter convertor = new BrushConverter();

            Brush brush = null;
            if (state == "open")
                brush = (Brush)convertor.ConvertFromString(door.OpenColor);
            else
                brush = (Brush)convertor.ConvertFromString(door.CloseColor);
            brush.Freeze();
            return brush;
            */
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class EntityStateToBackConverter : IValueConverter
    {
        public Color OverflowColor { get; set; }
        public Color OverdueColor { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var list = StringUtil.SplitList((string)value);
            foreach (string s in list)
            {
                if (s == "overflow")
                    return new SolidColorBrush(OverflowColor);
                if (s == "overdue")
                    return new SolidColorBrush(OverdueColor);
            }

            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

#if REMOVED
    public class EntityUiiConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Entity entity = (Entity)value;

            string pii = entity.PII;
            string oi = entity.OI;
            string aoi = entity.AOI;
            if (string.IsNullOrEmpty(oi) == false)
                return oi + "." + pii;
            if (string.IsNullOrEmpty(aoi) == false)
                return aoi + "." + pii;
            return pii;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
#endif
}
