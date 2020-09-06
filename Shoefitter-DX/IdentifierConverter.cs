using SAGESharp.SLB;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace ShoefitterDX
{
    [ValueConversion(typeof(string), typeof(Identifier))]
    public class IdentifierConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Identifier id && targetType == typeof(string))
            {
                return id.ToString();
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue && targetType == typeof(Identifier))
            {
                try
                {
                    return Identifier.From(stringValue);
                }
                catch (Exception ex)
                {
                    return new ValidationResult("Invalid identifier. (" + ex.Message + ")");
                }
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
    }
}
