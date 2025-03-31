using System;
using System.Collections.Generic;
using SteamContentPackager.Packing;
using SteamKit2;

namespace SteamContentPackager.Steam;

public class ValidationResult : SubTask.Result
{
	public List<FileMapping> InvalidFiles;

	public TimeSpan TimeElapsed;
}
