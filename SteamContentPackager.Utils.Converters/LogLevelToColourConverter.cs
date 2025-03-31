using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using SteamContentPackager.UI.Controls;

namespace SteamContentPackager.Utils.Converters;

public class LogLevelToColourConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value == null)
		{
			return null;
		}
		return (LogLevel)value switch
		{
			LogLevel.Info => Brushes.Gray, 
			LogLevel.Debug => Brushes.DimGray, 
			LogLevel.Warning => Brushes.Purple, 
			LogLevel.Error => Brushes.Red, 
			LogLevel.Success => Brushes.Green, 
			_ => Brushes.Silver, 
		};
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
