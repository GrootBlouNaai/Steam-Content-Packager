using System;
using System.Globalization;
using System.Threading;
using System.Windows.Data;

namespace SteamContentPackager.Utils.Converters;

public class UpdateThresholdConverter : IValueConverter
{
	private int _updateThreshold;

	private DateTime _lastUpdate = DateTime.MinValue;

	public int UpdateThreshold
	{
		get
		{
			return _updateThreshold;
		}
		set
		{
			if (value < 0)
			{
				throw new ArgumentOutOfRangeException("UpdateThreshold", value, "Cannot be less than zero.");
			}
			_updateThreshold = value;
		}
	}

	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		Thread.Sleep(50);
		DateTime now = DateTime.Now;
		TimeSpan timeSpan = now - _lastUpdate;
		if ((double)UpdateThreshold <= timeSpan.TotalMilliseconds)
		{
			_lastUpdate = now;
			return value;
		}
		return Binding.DoNothing;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return value;
	}
}
