using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SteamContentPackager.Annotations;
using SteamContentPackager.Utils;
using SteamKit2;

namespace SteamContentPackager.Steam;

public class SteamApp : INotifyPropertyChanged
{
	public uint Appid;

	public string InstallDir;

	public string AcfPath;

	public uint BuildId;

	public List<Depot> Depots = new List<Depot>();

	private ImageSource _image;

	public string Name { get; set; }

	public string SizeOnDisk { get; set; }

	public ImageSource Image
	{
		get
		{
			return _image;
		}
		set
		{
			_image = value;
			OnPropertyChanged("Image");
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;

	public SteamApp()
	{
	}

	public void FetchImage()
	{
		string filename = $"{Environment.CurrentDirectory}\\Images\\{Appid}.jpg";
		if (File.Exists(filename))
		{
			Application.Current.Dispatcher.Invoke(delegate
			{
				Image = LoadImage(filename);
			});
			return;
		}
		try
		{
			WebClient webClient = new WebClient();
			string address = $"http://cdn.akamai.steamstatic.com/steam/apps/{Appid}/header.jpg";
			if (!Directory.Exists($"{Environment.CurrentDirectory}\\Images"))
			{
				Directory.CreateDirectory($"{Environment.CurrentDirectory}\\Images");
			}
			webClient.DownloadFile(address, filename);
			Application.Current.Dispatcher.Invoke(delegate
			{
				Image = LoadImage(filename);
			});
		}
		catch (Exception)
		{
			if (File.Exists(filename))
			{
				File.Delete(filename);
			}
		}
	}

	private ImageSource LoadImage(string path)
	{
		BitmapImage bitmapImage = new BitmapImage();
		bitmapImage.BeginInit();
		bitmapImage.UriSource = new Uri(path, UriKind.Absolute);
		bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
		bitmapImage.EndInit();
		return bitmapImage;
	}

	public SteamApp(string acfPath)
	{
		KeyValue val = KeyValue.LoadAsText(acfPath);
		AcfPath = acfPath;
		Appid = val["appid"].AsUnsignedInteger(0u);
		InstallDir = val["installdir"].Value;
		SizeOnDisk = string.Format("({0})", FileSizeSuffix.FromUlong(val["sizeondisk"].AsUnsignedLong(0uL), 2));
		Name = ((val["name"] != null) ? val["name"].Value : val["userconfig"]["name"].Value);
		BuildId = val["buildid"].AsUnsignedInteger(0u);
		foreach (KeyValue child in val["installeddepots"].Children)
		{
			Depot depot = new Depot(child);
			if (depot.ManifestId != 0L)
			{
				Depots.Add(depot);
			}
		}
	}

	public override string ToString()
	{
		return Name;
	}

	[NotifyPropertyChangedInvocator]
	protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}
