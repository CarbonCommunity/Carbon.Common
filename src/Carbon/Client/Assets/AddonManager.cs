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
	public Dictionary<string, UnityEngine.GameObject> InstalledCache { get; } = new();

	public List<GameObject> PrefabInstances { get; } = new();
	public List<byte[]> CurrentChunk { get; } = new();

	public GameObject CreateCacheBasedOn(GameObject source)
	{
		if (source == null)
		{
			return null;
		}

		var result = UnityEngine.Object.Instantiate(source);
		PrefabInstances.Add(result);

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

		if (addon.Assets.TryGetValue(assetName, out var asset))
		{
			return CreateCacheBasedOn(asset.LoadPrefab<GameObject>(path));
		}

		return null;
	}
	public GameObject CreatePrefab(string path)
	{
		if(InstalledCache.TryGetValue(path, out var prefab))
		{
			return CreateCacheBasedOn(prefab);
		}

		return null;
	}
	public void ProcessPrefab(GameObject prefab)
	{
		if (prefab == null)
		{
			return;
		}

		var rustComponent = prefab.GetComponent<RustComponent>();
		rustComponent.ApplyComponent();
	}
	public void Dispose()
	{
		foreach (var prefab in PrefabInstances)
		{
			try
			{
				if (prefab == null) continue;

				UnityEngine.Object.Destroy(prefab);
			}
			catch (Exception ex)
			{
				Logger.Warn($"[AddonManager] Failed destroying cached prefab ({ex.Message})\n{ex.StackTrace}");
			}
		}

		foreach (var addon in Installed)
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
			case AddonDownload.Formats.Whole:
			case AddonDownload.Formats.First:
				Console.WriteLine($"Initial chunk");
				CurrentChunk.Clear();
				break;
		}

		CurrentChunk.Add(download.BufferChunk);

		switch (download.Format)
		{
			case AddonDownload.Formats.Whole:
			case AddonDownload.Formats.Last:
				Console.WriteLine($"Finalized chunk");

				var completeBuffer = CurrentChunk.SelectMany(x => x).ToArray();
				Installed.Add(Addon.ImportFromBuffer(completeBuffer));
				Array.Clear(completeBuffer, 0, completeBuffer.Length);
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
			var buffer = addon.Buffer;
			var chunks = buffer.Chunkify(4000000);

			Logger.Warn($" Processing {chunks.Length} chunks (total of {buffer.Length} or {ByteEx.Format(buffer.Length)})");

			for (int i = 0; i < chunks.Length; i++)
			{
				client.Send("addondownload", new AddonDownload
				{
					BufferChunk = chunks[i],
					Format = chunks.Length == 1 ? AddonDownload.Formats.Whole : i == 0 ? AddonDownload.Formats.First : i == chunks.Length - 1 ? AddonDownload.Formats.Last : AddonDownload.Formats.Content
				});

				await AsyncEx.WaitForSeconds(0.1f);
			}

			await AsyncEx.WaitForSeconds(0.75f);
		}

		client.Send("addonfinalized");
	}
	public void Deliver(CarbonClient client, params string[] urls)
	{
		client.Send("addonrequest", new AddonRequest
		{
			AddonCount = urls.Length,
			IsUrlDownload = true
		});

		Logger.Log($"Sent download request to {client.Connection} with {urls.Length:n0} addon URLs...");

		client.Send("addondownloadurl", new AddonDownloadUrl
		{
			Urls = urls
		});
	}

	public void Install(List<Addon> addons)
	{
		foreach (var addon in addons)
		{
			foreach (var asset in addon.Assets)
			{
				asset.Value.UnpackBundle();
			}

			Installed.Add(addon);
		}
	}
	public void Uninstall()
	{
		foreach (var prefab in InstalledCache)
		{
			try
			{
				UnityEngine.Object.Destroy(prefab.Value);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Failed disposing asset '{prefab.Key}' ({ex.Message})\n{ex.StackTrace}");
			}
		}

		InstalledCache.Clear();

		foreach (var prefab in PrefabInstances)
		{
			try
			{
				UnityEngine.Object.Destroy(prefab);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Failed disposing a prefab ({ex.Message})\n{ex.StackTrace}");
			}
		}

		PrefabInstances.Clear();

		foreach (var addon in Installed)
		{
			foreach (var asset in addon.Assets)
			{
				try
				{
					asset.Value.Dispose();
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Failed disposing asset '{asset.Key}' of addon {addon.Name} ({ex.Message})\n{ex.StackTrace}");
				}
			}
		}

		Installed.Clear();
	}
}
