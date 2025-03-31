using System;
using System.Globalization;
using System.Windows.Data;

namespace SteamContentPackager.UI.Converters;

public class BoolToOpacityConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value == null)
		{
			return 1f;
		}
		return ((bool)value) ? 1f : 0.5f;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotSupportedException();
	}
}
