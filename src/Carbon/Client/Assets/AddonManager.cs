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

	public FacepunchBehaviour Persistence => Community.Runtime.CorePlugin.persistence;

	public List<Addon> Installed { get; } = new();
	public Dictionary<string, UnityEngine.GameObject> InstalledCache { get; } = new();

	public List<GameObject> PrefabInstances { get; } = new();
	public List<byte[]> CurrentChunk { get; } = new();

	internal void FixName(GameObject gameObject)
	{
		gameObject.name = gameObject.name.Replace("(Clone)", string.Empty);
	}

	public GameObject CreateFromAsset(string path, Asset asset)
	{
		if (asset == null)
		{
			Logger.Warn($"Couldn't find '{path}' as the asset provided is null. (CreateFromAsset)");
			return null;
		}

		if (string.IsNullOrEmpty(path))
		{
			Logger.Warn($"Couldn't find prefab from asset '{asset.Name}' as it's an empty string. (CreateFromAsset)");
			return null;
		}

		var prefab = asset.LoadPrefab<GameObject>(path);

		if (prefab != null)
		{
			return CreateBasedOnImpl(prefab);
		}
		else
		{
			Logger.Warn($"Couldn't find '{path}' in any addons or assets. (CreateFromAsset)");
		}

		return null;
	}
	public GameObject CreateFromCache(string path)
	{
		if(InstalledCache.TryGetValue(path, out var prefab))
		{
			return CreateBasedOnImpl(prefab);
		}

		return null;
	}

	public void CreateFromCacheAsync(string path, Action<GameObject> callback = null)
	{
		if (string.IsNullOrEmpty(path))
		{
			Logger.Warn($"Couldn't find '{path}' as it's an empty string. (CreateFromCacheAsync)");
			return;
		}

		if (InstalledCache.TryGetValue(path, out var prefab))
		{
			Persistence.StartCoroutine(CreateBasedOnAsyncImpl(prefab, callback));
		}
		else
		{
			Logger.Warn($"Couldn't find '{path}' as it hasn't been cached yet. Use 'CreateFromAssetAsync'? (CreateFromCacheAsync)");
		}
	}
	public void CreateFromAssetAsync(string path, Asset asset, Action<GameObject> callback = null)
	{
		if (asset == null)
		{
			Logger.Warn($"Couldn't find '{path}' as the asset provided is null. (CreateFromAssetAsync)");
			return;
		}

		if (string.IsNullOrEmpty(path))
		{
			Logger.Warn($"Couldn't find '{path}' as it's an empty string. (CreateFromAssetAsync)");
			return;
		}

		var prefab = asset.LoadPrefab<GameObject>(path);

		if (prefab != null)
		{
			Persistence.StartCoroutine(CreateBasedOnAsyncImpl(prefab, callback));
		}
		else
		{
			Logger.Warn($"Couldn't find '{path}' in any addons or assets. (CreateFromAssetAsync)");
		}
	}

	#region Helpers

	internal GameObject CreateBasedOnImpl(GameObject source)
	{
		if (source == null)
		{
			return null;
		}

		var result = UnityEngine.Object.Instantiate(source);
		PrefabInstances.Add(result);

		FixName(result);

		return result;
	}
	internal IEnumerator CreateBasedOnAsyncImpl(GameObject gameObject, Action<GameObject> callback = null)
	{
		var result = (GameObject)null;

		yield return result = UnityEngine.Object.Instantiate(gameObject);
		PrefabInstances.Add(result);

		FixName(result);

		callback?.Invoke(result);
	}

	#endregion

	public void ProcessPrefab(GameObject prefab)
	{
		if (prefab == null)
		{
			return;
		}

		var rustComponent = prefab.GetComponent<RustComponent>();
		rustComponent.ApplyComponent(prefab);
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

	public async void Deliver(CarbonClient client, bool uninstallAll, List<Addon> addons)
	{
		client.Send("addonrequest", new AddonRequest
		{
			AddonCount = addons.Count,
			BufferSize = addons.Sum(x => x.Buffer.Length),
		});

		Logger.Log($"Sent download request to {client.Connection} with {addons.Count:n0} addons...");

		var sentMain = true;
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
					Format = chunks.Length == 1 ? AddonDownload.Formats.Whole : i == 0 ? AddonDownload.Formats.First : i == chunks.Length - 1 ? AddonDownload.Formats.Last : AddonDownload.Formats.Content,
					UninstallAll = sentMain
				});

				if (sentMain)
				{
					sentMain = false;
				}

				await AsyncEx.WaitForSeconds(0.1f);
			}

			await AsyncEx.WaitForSeconds(0.75f);
		}

		client.Send("addonfinalized");
	}
	public void Deliver(CarbonClient client, bool uninstallAll, params string[] urls)
	{
		client.Send("addonrequest", new AddonRequest
		{
			AddonCount = urls.Length,
			IsUrlDownload = true
		});

		Logger.Log($"Sent download request to {client.Connection} with {urls.Length:n0} addon URLs...");

		client.Send("addondownloadurl", new AddonDownloadUrl
		{
			Urls = urls,
			UninstallAll = uninstallAll
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
