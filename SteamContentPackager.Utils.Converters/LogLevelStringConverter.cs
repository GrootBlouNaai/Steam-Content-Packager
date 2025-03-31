using System;
using System.Globalization;
using System.Windows.Data;
using SteamContentPackager.UI.Controls;

namespace SteamContentPackager.Utils.Converters;

public class LogLevelStringConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value == null)
		{
			return null;
		}
		return $"[{((LogLevel)value/*cast due to .constrained prefix*/).ToString().ToUpper()}]";
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
