using System;
using System.IO;
using SteamContentPackager.Utils;
using SteamKit2;

namespace SteamContentPackager.Steam;

public class Depot
{
	public uint Id;

	public uint DlcAppid;

	public bool DlcDepot;

	public ulong ManifestId;

	public bool ManifestAvailable => File.Exists($"{Settings.SteamPath}\\depotcache\\{Id}_{ManifestId}.manifest");

	public Depot(KeyValue depotKeyValue)
	{
		Id = Convert.ToUInt32(depotKeyValue.Name);
		ManifestId = depotKeyValue["manifest"].AsUnsignedLong(0uL);
		KeyValue val = depotKeyValue["dlcappid"];
		if (val != KeyValue.Invalid)
		{
			DlcDepot = true;
			DlcAppid = val.AsUnsignedInteger(0u);
		}
	}
}
