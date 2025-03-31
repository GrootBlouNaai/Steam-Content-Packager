using System.Collections.Generic;
using SteamContentPackager.UI;

namespace SteamContentPackager.Steam;

public class AppBranch : BindableBase
{
	private string _pass;

	public string Name;

	public uint BuildId;

	public Dictionary<string, byte[]> BetaPasswords = new Dictionary<string, byte[]>();

	public bool RequiresPass { get; }

	public string Password
	{
		get
		{
			return _pass;
		}
		set
		{
			_pass = value;
			OnPropertyChanged("Password");
		}
	}

	public AppBranch(string name, bool requiresPass, uint buildId)
	{
		Name = name;
		RequiresPass = requiresPass;
		BuildId = buildId;
	}

	public override string ToString()
	{
		return Name;
	}
}
