using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;

namespace Scribe.Wpf.Converters
{
    [ContentProperty("Brushes")]
    public class ColorIndexToBrushConverter : IValueConverter
    {
        public ColorIndexToBrushConverter()
        {
            DefaultBrush = System.Windows.Media.Brushes.Black;
        }

        public Brush DefaultBrush { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public IList Brushes { get; set; } = new List<Brush>();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var index = (int)value;
            if (Brushes == null || index < 0 || index >= Brushes.Count)
                return DefaultBrush;
            return Brushes[index];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
