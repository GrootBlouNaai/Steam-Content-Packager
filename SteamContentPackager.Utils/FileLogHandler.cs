using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SteamContentPackager.Utils;

internal class FileLogHandler : Log.Handler
{
	private readonly StreamWriter _writer;

	public FileLogHandler()
	{
		FileInfo fileInfo = new FileInfo($"{AppDomain.CurrentDomain.BaseDirectory}\\Logs\\{DateTime.Now:MM.dd.yy_HH.mm}.log");
		fileInfo.Directory?.Create();
		List<FileInfo> list = (from x in fileInfo.Directory?.GetFiles("*.log")
			orderby x.CreationTime
			select x).ToList();
		while (list != null && list.Count > 19)
		{
			list[0].Delete();
			list.RemoveAt(0);
		}
		_writer = File.AppendText(fileInfo.FullName);
		_writer.AutoFlush = true;
	}

	public override void OnMessageReceived(Log.Message message)
	{
		_writer.WriteLine(FormatMessage(message));
	}
}
