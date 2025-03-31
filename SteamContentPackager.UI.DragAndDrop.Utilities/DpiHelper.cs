using System.Reflection;
using System.Windows;
using System.Windows.Media;

namespace SteamContentPackager.UI.DragAndDrop.Utilities;

public static class DpiHelper
{
	private static Matrix _transformToDevice;

	private static Matrix _transformToLogical;

	public static double DpiX;

	public static double DpiY;

	static DpiHelper()
	{
		DpiX = 0.0;
		DpiY = 0.0;
		PropertyInfo property = typeof(SystemParameters).GetProperty("DpiX", BindingFlags.Static | BindingFlags.NonPublic);
		PropertyInfo property2 = typeof(SystemParameters).GetProperty("Dpi", BindingFlags.Static | BindingFlags.NonPublic);
		int num = (int)property.GetValue(null, null);
		DpiX = num;
		int num2 = (int)property2.GetValue(null, null);
		DpiY = num2;
		_transformToLogical = Matrix.Identity;
		_transformToLogical.Scale(96.0 / (double)num, 96.0 / (double)num2);
		_transformToDevice = Matrix.Identity;
		_transformToDevice.Scale((double)num / 96.0, (double)num2 / 96.0);
	}

	public static Point LogicalPixelsToDevice(Point logicalPoint)
	{
		return _transformToDevice.Transform(logicalPoint);
	}

	public static Point DevicePixelsToLogical(Point devicePoint)
	{
		return _transformToLogical.Transform(devicePoint);
	}

	public static Rect LogicalRectToDevice(Rect logicalRectangle)
	{
		Point point = LogicalPixelsToDevice(new Point(logicalRectangle.Left, logicalRectangle.Top));
		Point point2 = LogicalPixelsToDevice(new Point(logicalRectangle.Right, logicalRectangle.Bottom));
		return new Rect(point, point2);
	}

	public static Rect DeviceRectToLogical(Rect deviceRectangle)
	{
		Point point = DevicePixelsToLogical(new Point(deviceRectangle.Left, deviceRectangle.Top));
		Point point2 = DevicePixelsToLogical(new Point(deviceRectangle.Right, deviceRectangle.Bottom));
		return new Rect(point, point2);
	}

	public static Size LogicalSizeToDevice(Size logicalSize)
	{
		Point point = LogicalPixelsToDevice(new Point(logicalSize.Width, logicalSize.Height));
		Size result = default(Size);
		result.Width = point.X;
		result.Height = point.Y;
		return result;
	}

	public static Size DeviceSizeToLogical(Size deviceSize)
	{
		Point point = DevicePixelsToLogical(new Point(deviceSize.Width, deviceSize.Height));
		return new Size(point.X, point.Y);
	}

	public static Thickness LogicalThicknessToDevice(Thickness logicalThickness)
	{
		Point point = LogicalPixelsToDevice(new Point(logicalThickness.Left, logicalThickness.Top));
		Point point2 = LogicalPixelsToDevice(new Point(logicalThickness.Right, logicalThickness.Bottom));
		return new Thickness(point.X, point.Y, point2.X, point2.Y);
	}
}
