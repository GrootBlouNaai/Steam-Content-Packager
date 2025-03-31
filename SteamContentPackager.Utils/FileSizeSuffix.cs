using System;

namespace SteamContentPackager.Utils;

internal class FileSizeSuffix
{
	private static readonly string[] SizeSuffixes = new string[9] { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

	public static string FromUlong(ulong value, int decimalPlaces = 1)
	{
		if (value == 0L)
		{
			return "0.0 bytes";
		}
		int num = (int)Math.Log(value, 1024.0);
		decimal num2 = (decimal)value / (decimal)(1L << num * 10);
		if (Math.Round(num2, decimalPlaces) >= 1000m)
		{
			num++;
			num2 /= 1024m;
		}
		return string.Format("{0:n" + decimalPlaces + "} {1}", num2, SizeSuffixes[num]);
	}
}
