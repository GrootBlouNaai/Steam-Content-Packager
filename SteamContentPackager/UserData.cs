using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace SteamContentPackager;

public class UserData
{
	public class User
	{
		public byte[] SentryHash;

		public string LoginKey;

		[XmlIgnore]
		public string AuthCode;

		[XmlIgnore]
		public string TwoFactorCode;

		public string Username { get; set; }

		[XmlIgnore]
		public string Password { get; set; }
	}

	public List<User> users = new List<User>();

	private static UserData _instance;

	private static string Path => $"{AppDomain.CurrentDomain.BaseDirectory}\\UserData\\users.xml";

	public static List<User> Users => Instance.users;

	public static UserData Instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = Load();
			}
			return _instance;
		}
	}

	public static User GetUser(string username)
	{
		User user = Users.Find((User x) => x.Username == username);
		if (user == null)
		{
			user = new User
			{
				Username = username
			};
			Users.Add(user);
		}
		return user;
	}

	private static UserData Load()
	{
		if (File.Exists(Path))
		{
			using (FileStream stream = File.OpenRead(Path))
			{
				XmlSerializer xmlSerializer = new XmlSerializer(typeof(UserData));
				return (UserData)xmlSerializer.Deserialize(stream);
			}
		}
		return new UserData();
	}

	public static void Save()
	{
		using FileStream stream = File.Create(Path);
		XmlSerializer xmlSerializer = new XmlSerializer(typeof(UserData));
		xmlSerializer.Serialize(stream, Instance);
	}
}
