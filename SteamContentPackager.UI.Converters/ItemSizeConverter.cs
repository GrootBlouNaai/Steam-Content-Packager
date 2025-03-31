using System;
using System.Globalization;
using System.Windows.Data;

namespace SteamContentPackager.UI.Converters;

public class ItemSizeConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (parameter == null)
		{
			parameter = 0.0;
		}
		if (value == null)
		{
			return 0.0;
		}
		if (double.TryParse(parameter.ToString(), out var result))
		{
			double num = (double)value - result;
			return num;
		}
		return 0.0;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
