using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SteamContentPackager.Utils.Converters;

public class RectConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		double width = (double)values[0];
		double height = (double)values[1];
		return new Rect(0.0, 0.0, width, height);
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
