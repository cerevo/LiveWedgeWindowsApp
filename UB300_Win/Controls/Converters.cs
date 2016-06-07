using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Cerevo.UB300_Win.Controls {
    public sealed class BooleanNegationConverter : IValueConverter {
        #region IValueConverter Members
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            return !(value is bool && (bool)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            return !(value is bool && (bool)value);
        }
        #endregion
    }

    public class ImageRectConverter : IValueConverter {
        #region IValueConverter Members
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            var source = (ImageSource)value;
            return new Rect(0, 0, source.Width, source.Height);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            throw new NotImplementedException();
        }
        #endregion
    }
}
