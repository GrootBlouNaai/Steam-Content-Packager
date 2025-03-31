using System;
using System.Collections.Generic;

namespace SteamContentPackager.Utils;

public static class Log
{
	public interface ILogHandler
	{
		void OnMessageReceived(Message message);
	}

	public abstract class Handler : ILogHandler
	{
		public abstract void OnMessageReceived(Message message);

		protected virtual string FormatMessage(Message message)
		{
			return $"{message.Time:HH:mm:ss.fff} : [{message.LogLevel.ToString().PadLeft(7)}] =>\t{message.Value}";
		}
	}

	public class Message
	{
		public DateTime Time { get; }

		public string Value { get; }

		public LogLevel LogLevel { get; }

		public Message(string value, LogLevel logLevel)
		{
			Time = DateTime.Now;
			Value = value;
			LogLevel = logLevel;
		}
	}

	private static List<Handler> _handlers = new List<Handler>();

	public static T GetHandler<T>() where T : Handler
	{
		return (T)_handlers.Find((Handler x) => x.GetType() == typeof(T));
	}

	public static void AddHandler<T>(object args = null) where T : Handler
	{
		Handler handler = null;
		handler = ((args != null) ? ((Handler)Activator.CreateInstance(typeof(T), args)) : Activator.CreateInstance<T>());
		_handlers.Add(handler);
	}

	public static void WriteException(Exception exception, LogLevel logLevel = LogLevel.Error)
	{
		Write(exception.Message, logLevel, logToUI: false);
		string[] array = exception.StackTrace.Split('\n');
		foreach (string text in array)
		{
			Write(text.Trim(), logLevel, logToUI: false);
		}
	}

	public static void Write(object message, LogLevel logLevel = LogLevel.Info, bool logToUI = true)
	{
		if (message == null)
		{
			return;
		}
		Message message2 = new Message(message.ToString(), logLevel);
		try
		{
			foreach (Handler handler in _handlers)
			{
				if (logToUI || !(handler.GetType() == typeof(ListBoxLogHandler)))
				{
					handler.OnMessageReceived(message2);
				}
			}
		}
		catch (Exception)
		{
		}
	}
}
