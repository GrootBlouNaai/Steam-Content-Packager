using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SteamContentPackager.UI.DragAndDrop.Icons;

public static class IconFactory
{
	public static BitmapImage EffectNone { get; } = GetImage("EffectNone.png", 12);

	public static BitmapImage EffectCopy { get; } = GetImage("EffectCopy.png", 12);

	public static BitmapImage EffectMove { get; } = GetImage("EffectMove.png", 12);

	public static BitmapImage EffectLink { get; } = GetImage("EffectLink.png", 12);

	private static BitmapImage GetImage(string iconName, int size)
	{
		return new BitmapImage();
	}

	public static Cursor CreateCursor(double rx, double ry, SolidColorBrush brush, Pen pen)
	{
		DrawingVisual drawingVisual = new DrawingVisual();
		using (DrawingContext drawingContext = drawingVisual.RenderOpen())
		{
			drawingContext.DrawRectangle(brush, new Pen(Brushes.Black, 0.1), new Rect(0.0, 0.0, rx, ry));
			drawingContext.Close();
		}
		RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap(64, 64, 96.0, 96.0, PixelFormats.Pbgra32);
		renderTargetBitmap.Render(drawingVisual);
		using MemoryStream memoryStream = new MemoryStream();
		PngBitmapEncoder pngBitmapEncoder = new PngBitmapEncoder();
		pngBitmapEncoder.Frames.Add(BitmapFrame.Create(renderTargetBitmap));
		pngBitmapEncoder.Save(memoryStream);
		byte[] array = memoryStream.ToArray();
		int length = array.GetLength(0);
		using MemoryStream memoryStream2 = new MemoryStream();
		memoryStream2.Write(BitConverter.GetBytes((short)0), 0, 2);
		memoryStream2.Write(BitConverter.GetBytes((short)2), 0, 2);
		memoryStream2.Write(BitConverter.GetBytes((short)1), 0, 2);
		memoryStream2.WriteByte(32);
		memoryStream2.WriteByte(32);
		memoryStream2.WriteByte(0);
		memoryStream2.WriteByte(0);
		memoryStream2.Write(BitConverter.GetBytes((short)(rx / 2.0)), 0, 2);
		memoryStream2.Write(BitConverter.GetBytes((short)(ry / 2.0)), 0, 2);
		memoryStream2.Write(BitConverter.GetBytes(length), 0, 4);
		memoryStream2.Write(BitConverter.GetBytes(22), 0, 4);
		memoryStream2.Write(array, 0, length);
		memoryStream2.Seek(0L, SeekOrigin.Begin);
		return new Cursor(memoryStream2);
	}
}
