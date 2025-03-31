using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using SteamContentPackager.Utils;
using SteamKit2;
using SteamKit2.Types;

namespace SteamContentPackager.Steam;

internal class Utils
{
	public static string InstallPath => Registry.CurrentUser.OpenSubKey("Software\\Valve\\Steam")?.GetValue("SteamPath").ToString().Replace("/", "\\");

	public static bool IsInstalled => Registry.CurrentUser.OpenSubKey("Software\\Valve\\Steam") != null;

	public static List<DirectoryInfo> LibraryFolders
	{
		get
		{
			KeyValue keyValue = KeyValue.LoadAsText($"{InstallPath}\\steamapps\\libraryfolders.vdf");
			return (from x in keyValue.Children
				where Directory.Exists(x.Value)
				select x into y
				select new DirectoryInfo(y.Value)).ToList();
		}
	}

	public static List<uint> InstalledApps
	{
		get
		{
			List<uint> list = new List<uint>();
			if (string.IsNullOrEmpty(InstallPath))
			{
				return list;
			}
			if (!File.Exists($"{InstallPath}\\steam.exe"))
			{
				Log.Write($"Invalid SteamDirectory: {InstallPath}");
				return list;
			}
			Log.Write($"SteamPath: {InstallPath}");
			KeyValue keyValue = KeyValue.LoadAsText($"{InstallPath}\\steamapps\\libraryfolders.vdf");
			List<string> list2 = new List<string> { $"{InstallPath}\\steamapps\\" };
			list2.AddRange(from x in keyValue.Children
				where Directory.Exists(x.Value)
				select $"{x.Value}\\steamapps\\");
			foreach (string item2 in list2)
			{
				string[] files = Directory.GetFiles(item2, "*.acf", SearchOption.TopDirectoryOnly);
				string[] array = files;
				foreach (string text in array)
				{
					try
					{
						KeyValue keyValue2 = KeyValue.LoadAsText(text);
						if (SteamSession.AppInfo.Items.ContainsKey(keyValue2["appid"].AsUnsignedInteger()))
						{
							AppInfoCache.Item item = SteamSession.AppInfo.Items[keyValue2["appid"].AsUnsignedInteger()];
							list.Add(item.AppId);
						}
					}
					catch (Exception exception)
					{
						Log.Write($"Failed to load installed app from '{text}'", LogLevel.Warning);
						Log.WriteException(exception, LogLevel.Warning);
					}
				}
			}
			return list;
		}
	}

	public static string GetACFByAppid(uint appid)
	{
		KeyValue keyValue = KeyValue.LoadAsText($"{InstallPath}\\steamapps\\libraryfolders.vdf");
		List<string> list = new List<string> { $"{InstallPath}\\steamapps\\" };
		list.AddRange(from x in keyValue.Children
			where Directory.Exists(x.Value)
			select $"{x.Value}\\steamapps\\");
		foreach (string item in list.SelectMany((string x) => Directory.GetFiles(x, "*.acf", SearchOption.TopDirectoryOnly)))
		{
			Console.WriteLine(item);
			if (item.Contains($"{appid}"))
			{
				return item;
			}
		}
		return null;
	}

	public static List<uint> GetInstalledAppIds()
	{
		KeyValue keyValue = KeyValue.LoadAsText($"{InstallPath}\\steamapps\\libraryfolders.vdf");
		List<string> list = new List<string> { $"{InstallPath}\\steamapps\\" };
		list.AddRange(from x in keyValue.Children
			where Directory.Exists(x.Value)
			select $"{x.Value}\\steamapps\\");
		List<uint> list2 = new List<uint>();
		foreach (string item in list.SelectMany((string x) => Directory.GetFiles(x, "*.acf", SearchOption.TopDirectoryOnly)))
		{
			string s = new FileInfo(item).Name.Split('.')[0].Remove(0, 12);
			if (uint.TryParse(s, out var result))
			{
				list2.Add(result);
			}
		}
		return list2;
	}
}
