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
	public List<AssetBundle> BundleCache { get; } = new();

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

		foreach(var bundle in BundleCache)
		{
			try
			{
				bundle.Unload(true);
			}
			catch (Exception ex)
			{
				Logger.Warn($"[AddonManager] Failed unloading AssetBundle ({ex.Message})\n{ex.StackTrace}");
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
