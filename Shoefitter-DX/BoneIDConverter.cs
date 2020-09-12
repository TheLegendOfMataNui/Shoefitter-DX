using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace ShoefitterDX
{
    [ValueConversion(typeof(int), typeof(string), ParameterType = typeof(bool))]
    public class BoneIDConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 2 && values[0] is uint i && values[1] is bool biped && targetType == typeof(string))
            {
                string[] names = biped ? SAGESharp.BHDFile.BipedBoneNames : SAGESharp.BHDFile.NonBipedBoneNames;
                return i < names.Length ? names[i] : "<Invalid ID>";
            }
            else
            {
                throw new ArgumentException();
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
