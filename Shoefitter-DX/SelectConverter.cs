using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Windows.Data;
using System.Windows.Markup;

namespace ShoefitterDX
{
    [ValueConversion(typeof(int), typeof(object))]
    [ContentProperty(nameof(Choices))]
    public class SelectConverter : IValueConverter
    {
        public System.Collections.IList Choices { get; set; } = new List<object>();

        public SelectConverter()
        {
            
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int valueIndex;
            if (value.GetType() == typeof(bool))
            {
                valueIndex = (bool)value ? 1 : 0;
            }
            else
            {
                valueIndex = (int)value;
            }
            return this.Choices[valueIndex];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
