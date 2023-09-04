/*
 *
 * Copyright (c) 2022-2023 Carbon Community 
 * All rights reserved.
 *
 */

// #define CLIENT

using Carbon.Client.Packets;

namespace Carbon.Client.Assets;

public class AddonManager : IDisposable
{
	public static AddonManager Instance { get; internal set; } = new();

	public List<Addon> Installed { get; } = new();

	public List<GameObject> PrefabCache { get; } = new();

	public GameObject CreateCacheBasedOn(GameObject source)
	{
		if (source == null)
		{
			return null;
		}

		var result = UnityEngine.Object.Instantiate(source);
		PrefabCache.Add(result);

		return result;
	}
	public GameObject CreatePrefab(string path, Asset asset)
	{
		if (string.IsNullOrEmpty(path))
		{
			return null;
		}

		return CreateCacheBasedOn(asset.LoadPrefab<GameObject>(path));
	}
	public GameObject CreatePrefab(string path, string assetName, Addon addon)
	{
		if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(assetName))
		{
			return null;
		}

		if(addon.Assets.TryGetValue(assetName, out var asset))
		{
			return CreateCacheBasedOn(asset.LoadPrefab<GameObject>(path));
		}

		return null;
	}
	public void Dispose()
	{
		foreach(var prefab in PrefabCache)
		{
			try
			{
				if (prefab == null) continue;

				UnityEngine.Object.Destroy(prefab);
			}
			catch(Exception ex)
			{
				Logger.Warn($"[AddonManager] Failed destroying cached prefab ({ex.Message})\n{ex.StackTrace}");
			}
		}

		foreach(var addon in Installed)
		{
			foreach (var asset in addon.Assets)
			{
				try
				{
					asset.Value.Dispose();
				}
				catch (Exception ex)
				{
					Logger.Warn($"[AddonManager] Failed disposing of asset '{asset.Key}' (of addon {addon.Name}) ({ex.Message})\n{ex.StackTrace}");
				}
			}
		}
	}

#if !CLIENT
	public void Deliver(CarbonClient client, List<Addon> addons)
	{
		client.Send("addonrequest", new AddonRequest
		{
			AddonCount = addons.Count,
		});

		Community.Runtime.CorePlugin.timer.In(1f, () =>
		{
			client.Send("addondownload", new AddonDownload
			{
				Addons = addons
			});
		});
	}
#else

#endif
}
