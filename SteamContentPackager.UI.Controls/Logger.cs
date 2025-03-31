using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace SteamContentPackager.UI.Controls;

public class Logger
{
	private static string _filename;

	private static int _lineNumber = 0;

	private static LogViewer _logViewer;

	private static readonly object Locker = new object();

	public static void Init(LogViewer logViewer)
	{
		_logViewer = logViewer;
		_logViewer.Entries = new ObservableCollection<LogEntry>();
	}

	public static void StartNewLog(string name)
	{
		_lineNumber = 0;
		DateTime now = DateTime.Now;
		_filename = string.Format($"{Environment.CurrentDirectory}\\Logs\\{name}_" + "{0:0000}{1:00}{2:00}-{3:00}{4:00}.log", now.Year, now.Month, now.Day, now.Hour, now.Minute);
		int num = 2;
		while (File.Exists(_filename))
		{
			_filename = string.Format($"{Environment.CurrentDirectory}\\Logs\\{name}_" + "{0:0000}{1:00}{2:00}-{3:00}{4:00}" + $"_{num++}.log", now.Year, now.Month, now.Day, now.Hour, now.Minute);
		}
		new FileInfo(_filename).Directory?.Create();
	}

	public static void UpdateEntry(LogEntry entry, string result)
	{
		lock (Locker)
		{
			try
			{
				string[] array = File.ReadAllLines(_filename);
				entry.Result = $" - {result}";
				array[entry.LineNumber] += $" - {result}";
				File.WriteAllLines(_filename, array);
			}
			catch (Exception value)
			{
				Console.WriteLine(value);
			}
		}
	}

	public static LogEntry WriteEntry(object message, LogLevel logLevel = LogLevel.Info, bool writeToFile = true)
	{
		lock (Locker)
		{
			LogEntry entry = new LogEntry(message.ToString(), logLevel);
			Application.Current.Dispatcher.Invoke(delegate
			{
				entry.LineNumber = _lineNumber;
				_lineNumber++;
				_logViewer.Entries.Add(entry);
				_logViewer.ListBox.ScrollIntoView(_logViewer.ListBox.Items[_logViewer.ListBox.Items.Count - 1]);
				if (!string.IsNullOrEmpty(_filename) && writeToFile)
				{
					using (StreamWriter streamWriter = File.AppendText(_filename))
					{
						streamWriter.Write($"[{entry.Time}] ");
						streamWriter.Write($"[{entry.LogLevel}]".PadLeft(9));
						streamWriter.WriteLine($" -> {entry.Message}");
					}
				}
			});
			return entry;
		}
	}
}
