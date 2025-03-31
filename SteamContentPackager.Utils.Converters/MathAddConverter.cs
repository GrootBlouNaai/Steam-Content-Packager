using System;
using System.Globalization;
using System.Windows.Data;

namespace SteamContentPackager.Utils.Converters;

public class MathAddConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		double num = (double)values[0];
		double num2 = (double)values[1];
		return num + num2;
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
