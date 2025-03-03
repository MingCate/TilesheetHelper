using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TilesheetHelper.Converters
{
    public class DebugConverter : IValueConverter
    {
        public static DebugConverter Instance = new DebugConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Debug.WriteLine($"DebugConverter: {value.GetType().Name}: {(value as IEnumerable<BorderedImageButton>)?.Count() ?? 0}");
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
        public DebugConverter ProvideValue()
        {
            return this;
        }

    }
}
