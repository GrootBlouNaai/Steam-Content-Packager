using System;

namespace SteamContentPackager.Utils;

[Serializable]
public class SteamSettings
{
	public string Username = string.Empty;

	public string Password = string.Empty;

	public string LoginKey = string.Empty;

	public string SentryHash = string.Empty;

	public string ServerAddress = string.Empty;

	public string SteamPath = string.Empty;

	public uint ConnectionTimeout = 2000u;

	public uint CellId;
}
