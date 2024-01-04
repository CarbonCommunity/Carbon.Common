/*
 *
 * Copyright (c) 2022-2024 Carbon Community
 * All rights reserved.
 *
 */

using Carbon.SDK.Client;
using Newtonsoft.Json;
using UnityEngine.Serialization;

namespace Carbon.Client;

[Serializable]
public class ClientConfig
{
	public bool Enabled = false;
	public EnvironmentOptions Environment = new();
	public ClientOptions Client = new();
	public List<AddonEntry> Addons = new();

	[JsonIgnore, NonSerialized] public string[] NetworkedAddonsCache;

	public void RefreshNetworkedAddons()
	{
		if (NetworkedAddonsCache != null)
		{
			Array.Clear(NetworkedAddonsCache, 0, NetworkedAddonsCache.Length);
		}

		NetworkedAddonsCache = Community.Runtime.ClientConfig.Addons.Where(x => x.Enabled).Select(x => x.Url).ToArray();
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
}
