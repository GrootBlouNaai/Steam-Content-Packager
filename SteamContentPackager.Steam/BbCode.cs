using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Script.Serialization;
using SteamContentPackager.Utils;

namespace SteamContentPackager.Steam;

internal class BbCode
{
	public class SteamStoreInfo
	{
		private readonly Dictionary<string, object> _dictionary;

		public bool Success;

		public T GetValue<T>(string name)
		{
			return (T)_dictionary[name];
		}

		public SteamStoreInfo(uint appid, string val)
		{
			JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
			_dictionary = (Dictionary<string, object>)javaScriptSerializer.DeserializeObject(val);
			_dictionary = (Dictionary<string, object>)_dictionary[appid.ToString()];
			Success = GetValue<bool>("success");
			if (Success)
			{
				_dictionary = (Dictionary<string, object>)_dictionary["data"];
			}
		}
	}

	public static async Task<string> Generate(SteamApp steamApp)
	{
		try
		{
			WebClient webClient = new WebClient();
			SteamStoreInfo storeInfo = new SteamStoreInfo(val: await webClient.DownloadStringTaskAsync(new Uri($"http://store.steampowered.com/api/appdetails/?appids={steamApp.Appid}")), appid: steamApp.Appid);
			if (!storeInfo.Success)
			{
				Log.Write($"Steam store info not found for {steamApp.Name}", LogLevel.Warning);
				return null;
			}
			string description = ProcessHtml(storeInfo.GetValue<string>("detailed_description"));
			string bbcode = File.ReadAllText($"{AppDomain.CurrentDomain.BaseDirectory}\\Config\\BBCodeTemplate.txt");
			bbcode = bbcode.Replace("{Appid}", $"{steamApp.Appid}");
			bbcode = bbcode.Replace("{AppName}", $"{steamApp.Name}");
			bbcode = bbcode.Replace("{Description}", $"{description}");
			bbcode = bbcode.Replace("{HosterName}", $"{Config.HosterName}");
			bbcode = bbcode.Replace("{Uploader}", $"{Config.UploaderName}");
			return bbcode.Replace("{Date}", $"{DateTime.UtcNow:dd.MM.yyyy}");
		}
		catch (Exception ex)
		{
			Exception e = ex;
			Log.Write("Failed to generate BBCode", LogLevel.Error);
			Log.WriteException(e);
			return null;
		}
	}

	private static string ProcessHtml(string description)
	{
		description = HttpUtility.HtmlDecode(description);
		description = description.Replace("<h1>Steam Greenlight</h1><p><img src=\"http://storefront.steampowered.com/v/gfx/apps/223220/extras/banner.png\"></p>", "");
		description = description.Replace("<br>", Environment.NewLine).Replace("<li>", "[*]").Replace("</li>", "");
		description = description.Replace("<br />", "");
		description = description.Replace("</ul>", "[/list]").Replace("<ul class=\"bb_ul\">", "[list]");
		description = description.Replace("<strong>", "[b]").Replace("</strong>", "[/b]");
		description = description.Replace("<h2 class=\"bb_tag\">", "\n\n[color=#FF0000][b]").Replace("</h2>", "[/b][/color]\n[img]http://cdn.store.steampowered.com/public/images/v5/maincol_gradient_rule.png[/img]\n");
		description = description.Replace("<u>", "[u]").Replace("</u>", "[/u]");
		description = description.Replace("<i>", "[i]").Replace("</i>", "[/i]");
		description = description.Replace("<h1>", "").Replace("</h1>", "");
		description = description.Replace("<p>", Environment.NewLine).Replace("</p>", Environment.NewLine);
		string pattern = "<img src=(.*)>";
		foreach (Match item in Regex.Matches(description, pattern, RegexOptions.IgnoreCase))
		{
			string arg = item.Groups[1].Value.Replace("\"", "");
			description = description.Replace(item.Value, $"[img]{arg}[/img]");
		}
		description = description.Replace("<img src=\"(.*)\">", "[img]$1[/img]");
		return description;
	}
}
