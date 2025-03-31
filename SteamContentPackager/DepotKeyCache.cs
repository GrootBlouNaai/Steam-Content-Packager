using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using SteamKit2;

namespace SteamContentPackager;

public class DepotKeyCache
{
	private static DepotKeyCache _instance;

	private readonly ConcurrentDictionary<uint, byte[]> _depotKeysDictionary = new ConcurrentDictionary<uint, byte[]>();

	private static object _writeLock = new object();

	private static string Path => $"{AppDomain.CurrentDomain.BaseDirectory}\\UserData\\depotKeys.bin";

	protected static DepotKeyCache Instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = new DepotKeyCache();
			}
			return _instance;
		}
	}

	public static ConcurrentDictionary<uint, byte[]> Keys => Instance._depotKeysDictionary;

	private DepotKeyCache()
	{
		Load();
	}

	public static void Save()
	{
		lock (_writeLock)
		{
			using FileStream fileStream = File.OpenWrite(Path);
			foreach (KeyValuePair<uint, byte[]> item in Instance._depotKeysDictionary)
			{
				fileStream.WriteUint32(item.Key);
				fileStream.Write(item.Value, 0, 32);
			}
		}
	}

	private void Load()
	{
		if (!File.Exists(Path))
		{
			return;
		}
		lock (_writeLock)
		{
			using FileStream fileStream = File.OpenRead(Path);
			while (fileStream.Position != fileStream.Length)
			{
				_depotKeysDictionary.TryAdd(fileStream.ReadUInt32(), fileStream.ReadBytes(32));
			}
		}
	}
}
