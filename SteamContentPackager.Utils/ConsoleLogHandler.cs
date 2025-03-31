using System;

namespace SteamContentPackager.Utils;

internal class ConsoleLogHandler : Log.Handler
{
	public override void OnMessageReceived(Log.Message message)
	{
		Console.WriteLine(FormatMessage(message));
	}
}
