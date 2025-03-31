using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using SteamContentPackager.Tasks;

namespace SteamContentPackager.Utils;

public class StateToColourConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value == null)
		{
			return null;
		}
		TaskState taskState = (TaskState)value;
		if (taskState == TaskState.Cancelled || taskState == TaskState.Failed || taskState == TaskState.WaitingForLogin)
		{
			return new SolidColorBrush(Color.FromArgb(byte.MaxValue, byte.MaxValue, 51, 51));
		}
		return new SolidColorBrush(Color.FromArgb(byte.MaxValue, 172, 172, 172));
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return TaskState.Idle;
	}
}
