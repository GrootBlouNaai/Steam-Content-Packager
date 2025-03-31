using System;
using System.Globalization;
using System.Windows.Data;

namespace SteamContentPackager.UI.Converters;

internal class SizeStringConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value == null)
		{
			return "0.00 B";
		}
		if (value is ulong)
		{
			return FormatBytes((ulong)value);
		}
		if (value is double)
		{
			return FormatBytes((double)value);
		}
		return "0.00 B";
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}

	public static string FormatBytes(ulong bytes)
	{
		string[] array = new string[5] { "B", "KB", "MB", "GB", "TB" };
		double num = bytes;
		int num2 = 0;
		while (num2 < array.Length && bytes >= 1024)
		{
			num = (double)bytes / 1024.0;
			num2++;
			bytes /= 1024;
		}
		return $"{num:0.##} {array[num2]}";
	}

	public static string FormatBytes(double bytes)
	{
		string[] array = new string[5] { "B", "KB", "MB", "GB", "TB" };
		double num = bytes;
		int num2 = 0;
		while (num2 < array.Length && bytes >= 1024.0)
		{
			num = bytes / 1024.0;
			num2++;
			bytes /= 1024.0;
		}
		return $"{num:0.##} {array[num2]}";
	}
}
