using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SteamContentPackager.UI.Converters;

public class BoolToColorConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		SolidColorBrush solidColorBrush = new SolidColorBrush(Color.FromArgb(byte.MaxValue, 145, 145, 145));
		SolidColorBrush solidColorBrush2 = new SolidColorBrush(Color.FromArgb(byte.MaxValue, 128, 128, 128));
		if (value == null)
		{
			return solidColorBrush2;
		}
		return ((bool)value) ? solidColorBrush : solidColorBrush2;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotSupportedException();
	}
}
