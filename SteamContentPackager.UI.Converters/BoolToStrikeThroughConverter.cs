using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SteamContentPackager.UI.Converters;

internal class BoolToStrikeThroughConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value == null)
		{
			return null;
		}
		if ((bool)value)
		{
			return null;
		}
		return TextDecorations.Strikethrough;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
