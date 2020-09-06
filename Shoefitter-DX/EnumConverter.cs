using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace ShoefitterDX
{
    [ValueConversion(typeof(string), typeof(Enum))]
    public class EnumConverter : IValueConverter
    {
        public Type EnumType { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value.GetType() == EnumType)
            {
                return value.ToString();
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue && targetType == EnumType)
            {
                if (Enum.TryParse(EnumType, stringValue, out object result))
                {
                    return result;
                }
                else
                {
                    return new ValidationResult($"Value {stringValue} is not a member of enum {EnumType}.");
                }
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
    }
}
