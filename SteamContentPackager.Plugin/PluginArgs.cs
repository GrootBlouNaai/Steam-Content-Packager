using System.Collections.Generic;
using SteamContentPackager.UI;

namespace SteamContentPackager.Plugin;

public class PluginArgs : BindableBase
{
	public string LibraryFolder;

	public List<string> FileList;
}
