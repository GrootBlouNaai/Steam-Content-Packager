using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using SteamContentPackager.Tasks;

namespace SteamContentPackager.Utils.Converters;

public class TaskStateToVisibilityConverter2 : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value == null)
		{
			return null;
		}
		if ((TaskState)value == TaskState.Completed)
		{
			return Visibility.Visible;
		}
		return Visibility.Collapsed;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
