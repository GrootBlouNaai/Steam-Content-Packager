using System;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using SteamContentPackager.Steam;
using SteamContentPackager.UI.Controls;

namespace SteamContentPackager.Utils;

internal class BBCode
{
	private static SteamApp _currentSteamApp;

	public static void WriteForumBbCode(SteamApp app)
	{
		if (!Settings.GenerateBBCode)
		{
			return;
		}
		_currentSteamApp = app;
		try
		{
			Logger.WriteEntry("Writing BBCode");
			string arg = app.Appid.ToString();
			WebClient webClient = new WebClient();
			webClient.DownloadStringCompleted += ClientOnDownloadStringCompleted;
			webClient.DownloadStringAsync(new Uri($"http://store.steampowered.com/api/appdetails/?appids={arg}"));
		}
		catch (Exception)
		{
			File.WriteAllText($"{Settings.OutputDirectory}\\{app.InstallDir}_BBCode.txt", "Failed to get steam store info");
		}
	}

	public static void WriteTestForumBbCode(uint appid)
	{
		if (!Settings.GenerateBBCode)
		{
			return;
		}
		_currentSteamApp = new SteamApp
		{
			Appid = appid,
			InstallDir = "TEST"
		};
		try
		{
			if (!Directory.Exists($"{Settings.OutputDirectory}"))
			{
				Directory.CreateDirectory($"{Settings.OutputDirectory}");
			}
			WebClient webClient = new WebClient();
			webClient.DownloadStringCompleted += ClientOnDownloadStringCompleted;
			webClient.DownloadStringAsync(new Uri($"http://store.steampowered.com/api/appdetails/?appids={appid}"));
		}
		catch (Exception)
		{
			File.WriteAllText($"{Settings.OutputDirectory}\\{_currentSteamApp.InstallDir}_BBCode.txt", "Failed to get steam store info");
		}
	}

	private static void ClientOnDownloadStringCompleted(object sender, DownloadStringCompletedEventArgs downloadStringCompletedEventArgs)
	{
		((WebClient)sender).DownloadStringCompleted -= ClientOnDownloadStringCompleted;
		string result = downloadStringCompletedEventArgs.Result;
		JObject val = JObject.Parse(result);
		string text = _currentSteamApp.Appid.ToString();
		if ((bool)val[text][(object)"success"])
		{
			string text2 = ((object)val[text][(object)"data"][(object)"detailed_description"]).ToString();
			string text3 = HeaderImage(text) + Title("About This Game");
			text2 = text2.Replace("<h1>Steam Greenlight</h1><p><img src=\"http://storefront.steampowered.com/v/gfx/apps/223220/extras/banner.png\"></p>", "");
			text2 = text2.Replace("<br>", Environment.NewLine).Replace("<li>", "[*]").Replace("</li>", "");
			text2 = text2.Replace("<br />", "");
			text2 = text2.Replace("</ul>", "[/list]").Replace("<ul class=\"bb_ul\">", "[list]");
			text2 = text2.Replace("<strong>", "[b]").Replace("</strong>", "[/b]");
			text2 = text2.Replace("<h2 class=\"bb_tag\">", "\n\n[color=#FF0000][b]").Replace("</h2>", "[/b][/color]\n[img]http://cdn.store.steampowered.com/public/images/v5/maincol_gradient_rule.png[/img]\n");
			text2 = text2.Replace("<u>", "[u]").Replace("</u>", "[/u]");
			text2 = text2.Replace("<i>", "[i]").Replace("</i>", "[/i]");
			text2 = text2.Replace("<h1>", "").Replace("</h1>", "");
			text2 = text2.Replace("<p>", Environment.NewLine).Replace("</p>", Environment.NewLine);
			string pattern = "<img src=(.*)>";
			foreach (Match item in Regex.Matches(text2, pattern, RegexOptions.IgnoreCase))
			{
				string value = item.Groups[1].Value;
				text2 = text2.Replace(item.Value, $"[img]{value}[/img]");
			}
			text2 = text2.Replace("<img src=\"(.*)\">", "[img]$1[/img]");
			text2 += Title("Official Site", includeGradient: false);
			text2 += StorePage(text);
			string text4 = ((object)val[text][(object)"data"][(object)"website"])?.ToString();
			if (!string.IsNullOrEmpty(text4))
			{
				text2 += $"[url]{text4}[/url]\n";
			}
			text2 += Title("Download Links", includeGradient: false);
			text2 += "[list][color=yellow][b]Mirror 1 (ACF)[/b][/color]\n";
			text2 = text2 + $"[url=http://PutYourUrlHere][color=cyan]{_currentSteamApp.Name}[/color] | [color=#FF8000]" + DateTime.Today.ToString("dd.MM.yy") + "[/color][/url]";
			string hosterName = Settings.HosterName;
			string uploaderName = Settings.UploaderName;
			text2 += $" ({hosterName}) [i] < uploaded by {uploaderName} / pw: cs.rin.ru >[/i][/list]";
			text3 += text2.Replace("&amp;", "&");
			File.WriteAllText($"{Settings.OutputDirectory}\\{_currentSteamApp.InstallDir}_BBCode.txt", text3);
		}
		else
		{
			File.WriteAllText($"{Settings.OutputDirectory}\\{_currentSteamApp.InstallDir}_BBCode.txt", result);
		}
	}

	public static string Title(string title, bool includeGradient = true)
	{
		string text = $"\n\n[color=#FF0000][b]{title}:[/b][/color]\n";
		if (includeGradient)
		{
			return text + "[img]http://cdn.store.steampowered.com/public/images/v5/maincol_gradient_rule.png[/img]\n";
		}
		return text + "\n";
	}

	public static string StorePage(string appid)
	{
		return $"[url]http://store.steampowered.com/app/{appid}/[/url]\n";
	}

	public static string HeaderImage(string appid)
	{
		return $"[img]https://steamdb.info/static/camo/apps/{appid}/header.jpg[/img]\n";
	}
}
