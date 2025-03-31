using System;

namespace SteamContentPackager.Utils;

[Serializable]
public class OutputSettings
{
	public string OutputDirectory = string.Empty;

	public string LastOwner = "1234567890";

	public string HosterName = "{HOSTERNAME}";

	public string UploaderName = "{UPLOADER}";

	public bool DownloadContent;

	public bool GenerateBBCode;

	public bool WriteDepotInfo;
}
