/*
 *
 * Copyright (c) 2022-2023 Carbon Community 
 * All rights reserved.
 *
 */

using Carbon.Client.Packets;
using Network;

namespace Carbon.Client.Assets;

public class AddonManager : IDisposable
{
	public static AddonManager Instance { get; internal set; } = new();

	public List<Addon> Installed { get; } = new();

	public List<GameObject> PrefabCache { get; } = new();
	public List<byte[]> CurrentChunk { get; } = new();

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

	public void ProcessChunk(AddonDownload download)
	{
		switch (download.Format)
		{
			case AddonDownload.Formats.First:
				Console.WriteLine($"Initial chunk");
				CurrentChunk.Clear();
				break;
		}

		CurrentChunk.Add(download.BufferChunk);

		switch (download.Format)
		{
			case AddonDownload.Formats.Last:
				Console.WriteLine($"Finalized chunk");
				Installed.Add(Addon.ImportFromBuffer(CurrentChunk.SelectMany(x => x).ToArray()));
				CurrentChunk.Clear();
				break;
		}

		download.Dispose();
	}

	public async void Deliver(CarbonClient client, List<Addon> addons)
	{
		client.Send("addonrequest", new AddonRequest
		{
			AddonCount = addons.Count,
			BufferSize = addons.Sum(x => x.Buffer.Length),
		});

		Logger.Log($"Sent download request to {client.Connection} with {addons.Count:n0} addons...");

		foreach (var addon in addons)
		{
			var store = addon.Store();
			var chunks = store.Chunkify(4000000);

			Logger.Warn($" Processing {chunks.Length} chunks (total of {store.Length} or {ByteEx.Format(store.Length)})");

			for (int i = 0; i < chunks.Length; i++)
			{
				client.Send("addondownload", new AddonDownload
				{
					BufferChunk = chunks[i],
					Format = i == 0 ? AddonDownload.Formats.First : i == chunks.Length - 1 ? AddonDownload.Formats.Last : AddonDownload.Formats.Content
				});

				await AsyncEx.WaitForSeconds(0.1f);
			}
		}

		client.Send("addonfinalized");
	}

	public void Install(List<Addon> addons)
	{
		foreach(var addon in addons)
		{
			foreach(var asset in addon.Assets)
			{
				asset.Value.UnpackBundle();
			}

			Installed.Add(addon);
		}
	}
	public void Uninstall()
	{
		foreach(var prefab in PrefabCache)
		{
			try
			{
				UnityEngine.Object.Destroy(prefab);
			}
			catch(Exception ex)
			{
				Console.WriteLine($"Failed disposing a prefab ({ex.Message})\n{ex.StackTrace}");
			}
		}

		PrefabCache.Clear();

		foreach (var addon in Installed)
		{
			foreach(var asset in addon.Assets)
			{
				try
				{
					asset.Value.Dispose();
				}
				catch(Exception ex)
				{
					Console.WriteLine($"Failed disposing asset '{asset.Key}' of addon {addon.Name} ({ex.Message})\n{ex.StackTrace}");
				}
			}
		}

		Installed.Clear();
	}
}
