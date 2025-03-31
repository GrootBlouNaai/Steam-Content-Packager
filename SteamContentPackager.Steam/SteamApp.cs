using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SteamContentPackager.UI;
using SteamContentPackager.Utils;
using SteamKit2;
using SteamKit2.Types;

namespace SteamContentPackager.Steam;

public class SteamApp
{
	public class Depot : BindableBase
	{
		private bool _subscribed;

		public string Language;

		public string Architecture;

		public List<string> OSList = new List<string>();

		public List<string> Branches = new List<string>();

		public bool IsDlc;

		public uint DlcAppId;

		private bool _isChecked;

		public uint Id { get; }

		public uint ParentAppId { get; }

		public string Name { get; }

		public ulong MaxSize { get; }

		public ulong ManifestId { get; set; }

		public string EncryptedManifestId { get; set; }

		public bool IsChecked
		{
			get
			{
				return _isChecked;
			}
			set
			{
				if (!Subscribed && !IsChecked)
				{
					Log.Write("Cannot include unsubscribed depots!", LogLevel.Warning);
					return;
				}
				if (ManifestId == 0L && !string.IsNullOrEmpty(EncryptedManifestId))
				{
					Log.Write("Branch password required to decrypt manifestID!", LogLevel.Warning);
					return;
				}
				_isChecked = value;
				OnPropertyChanged("IsChecked");
			}
		}

		public bool Subscribed
		{
			get
			{
				return _subscribed;
			}
			set
			{
				_subscribed = value;
				OnPropertyChanged("Subscribed");
			}
		}

		public Depot(KeyValue keyValue, SteamApp parentApp, AppBranch branch)
		{
			Id = Convert.ToUInt32(keyValue.Name);
			ParentAppId = parentApp.Appid;
			Name = keyValue["name"].Value;
			IsDlc = keyValue["dlcappid"] != KeyValue.Invalid;
			if (IsDlc)
			{
				DlcAppId = keyValue["dlcappid"].AsUnsignedInteger();
			}
			Subscribed = GetIsSubscribed();
			if (Subscribed && IsDlc && parentApp.IsShared && parentApp.ExcludeFromFamilySharing)
			{
				Subscribed = false;
			}
			int num = keyValue["sharedinstall"].AsInteger();
			if (num == 1 || num == 2)
			{
				ParentAppId = keyValue["depotfromApp"].AsUnsignedInteger();
				if (!SteamSession.AppInfo.Items.ContainsKey(ParentAppId))
				{
					return;
				}
				keyValue = SteamSession.AppInfo.Items[ParentAppId].KeyValues["depots"][keyValue.Name];
			}
			Branches.AddRange(keyValue["manifests"].Children.Select((KeyValue x) => x.Name));
			Branches.AddRange(keyValue["encryptedmanifests"].Children.Select((KeyValue x) => x.Name));
			MaxSize = keyValue["maxsize"].AsUnsignedLong(0uL);
			KeyValue keyValue2 = keyValue["config"];
			string value = keyValue2["oslist"].Value;
			if (!string.IsNullOrEmpty(value))
			{
				OSList = value.Split(',').ToList();
			}
			Language = keyValue2["language"].Value;
			if (Id == 1 || Id == 3)
			{
				Language = "english";
			}
			Architecture = keyValue2["osarch"].Value;
			GetManifestId(keyValue, branch);
			_isChecked = Subscribed && ManifestId != 0;
		}

		private void GetManifestId(KeyValue depotKeyValue, AppBranch branch)
		{
			KeyValue keyValue = depotKeyValue["manifests"];
			KeyValue keyValue2 = depotKeyValue["encryptedmanifests"];
			if (branch.RequiresPass)
			{
				KeyValue keyValue3 = keyValue2[branch.Name];
				if (keyValue3 == KeyValue.Invalid && keyValue["public"] != KeyValue.Invalid)
				{
					ManifestId = keyValue["public"].AsUnsignedLong(0uL);
					return;
				}
				KeyValue keyValue4 = keyValue3["encrypted_gid"];
				KeyValue keyValue5 = keyValue3["encrypted_gid_2"];
				if (keyValue4 != KeyValue.Invalid)
				{
					EncryptedManifestId = keyValue4.Value;
					byte[] input = SteamKit2.Utils.DecodeHexString(keyValue4.Value);
					byte[] array = CryptoHelper.VerifyAndDecryptPassword(input, branch.Password);
					if (array == null)
					{
						Console.WriteLine("Password was invalid for branch {0}", branch);
						return;
					}
					ManifestId = BitConverter.ToUInt64(array, 0);
					EncryptedManifestId = "";
				}
				else if (keyValue5 != KeyValue.Invalid)
				{
					EncryptedManifestId = keyValue5.Value;
					byte[] input2 = SteamKit2.Utils.DecodeHexString(keyValue5.Value);
					if (branch.BetaPasswords.ContainsKey(branch.Name))
					{
						byte[] value = CryptoHelper.SymmetricDecryptECB(input2, branch.BetaPasswords[branch.Name]);
						ManifestId = BitConverter.ToUInt64(value, 0);
						EncryptedManifestId = null;
					}
				}
			}
			else
			{
				KeyValue keyValue6 = keyValue[branch.Name];
				if (keyValue6 != KeyValue.Invalid)
				{
					ManifestId = keyValue6.AsUnsignedLong(0uL);
				}
				else
				{
					ManifestId = keyValue["public"].AsUnsignedLong(0uL);
				}
			}
		}

		private bool GetIsSubscribed()
		{
			return PICSUpdater.OwnedDepots.Contains(Id) || PICSUpdater.OwnedApps.Contains(Id);
		}
	}

	public uint Appid { get; }

	public string Name { get; }

	public uint OwnerId { get; }

	public bool Installed { get; set; }

	public bool IsShared { get; }

	public bool ExcludeFromFamilySharing { get; set; }

	public SteamApp(uint appid, string name, uint ownerId, bool isInstalled = false)
	{
		Appid = appid;
		Name = name;
		OwnerId = ownerId;
		Installed = isInstalled;
		IsShared = ownerId != SteamSession.SteamClient.SteamID.AccountID;
	}

	public List<Depot> LoadDepots(AppBranch branch)
	{
		KeyValue keyValues = SteamSession.AppInfo.Items[Appid].KeyValues;
		ExcludeFromFamilySharing = keyValues["common"]["exfgls"].AsUnsignedInteger() != 0;
		List<Depot> list = new List<Depot>();
		Stopwatch stopwatch = new Stopwatch();
		stopwatch.Start();
		AppInfoCache.Item item = SteamSession.AppInfo.Items[Appid];
		List<KeyValue> children = item.KeyValues["depots"].Children;
		foreach (KeyValue item2 in children)
		{
			if (uint.TryParse(item2.Name, out var _))
			{
				list.Add(new Depot(item2, this, branch));
			}
		}
		stopwatch.Stop();
		Log.Write($"Loaded {list.Count} depots for {Name} in {stopwatch.Elapsed:g}");
		return list;
	}

	public override string ToString()
	{
		return $"{Name} ({Appid})";
	}
}
