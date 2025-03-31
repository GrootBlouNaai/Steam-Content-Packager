using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SteamContentPackager.UI.Converters;

internal class BoolToBoldConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value == null)
		{
			return FontWeights.Normal;
		}
		if ((bool)value)
		{
			return FontWeights.Bold;
		}
		return FontWeights.Normal;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
