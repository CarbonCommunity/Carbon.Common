/*
 *
 * Copyright (c) 2022-2024 Carbon Community
 * All rights reserved.
 *
 */

using Newtonsoft.Json;
using UnityEngine.Serialization;

namespace Carbon.Client;

[Serializable]
public class ClientConfig
{
	public bool Enabled = false;
	public EnvironmentOptions Environment = new();
	public GameplayOptions Gameplay = new();
	public List<AddonEntry> Addons = new();

	[JsonIgnore, NonSerialized] public string[] NetworkableAddons;

	public void RefreshNetworkables()
	{
		if (NetworkableAddons != null)
		{
			Array.Clear(NetworkableAddons, 0, NetworkableAddons.Length);
		}

		NetworkableAddons = Community.Runtime.ClientConfig.Addons.Where(x => x.Enabled).Select(x => x.Url).ToArray();
	}

	[Serializable]
	public class AddonEntry
	{
		public string Url;
		public bool Enabled = true;

		public bool IsEnabled() => Enabled;
	}

	[Serializable]
	public class EnvironmentOptions
	{
		public bool NoMap = false;
	}

	[Serializable]
	public class GameplayOptions
	{
		public bool UseOldRecoil = false;
	}
}
