/*
 *
 * Copyright (c) 2022-2023 Carbon Community 
 * All rights reserved.
 *
 */

using Newtonsoft.Json;
using ProtoBuf;

namespace Carbon.Client.Assets;

public partial class Asset : IDisposable
{
	public IEnumerator UnpackBundleAsync()
	{
		if (IsUnpacked)
		{
			Logger.Log($" Already unpacked '{Name}'");
			yield break;
		}

		var request = (AssetBundleCreateRequest)null;
		using var stream = new MemoryStream(Data);
		yield return request = AssetBundle.LoadFromStreamAsync(stream);

		CachedBundle = request.assetBundle;
		Logger.Log($" Unpacked bundle '{Name}'");
	}
	public void UnpackBundle()
	{
		if(IsUnpacked)
		{
			Logger.Log($" Already unpacked '{Name}'");
			return;
		}

		using var stream = new MemoryStream(Data);
		CachedBundle = AssetBundle.LoadFromStream(stream);
		Logger.Log($" Unpacked bundle '{Name}'");
	}

	public T LoadPrefab<T>(string path) where T : UnityEngine.Object
	{
		if (!IsUnpacked)
		{
			UnpackBundle();
		}

		return CachedBundle.LoadAsset<T>(path);
	}
	public T[] LoadAllPrefabs<T>() where T : UnityEngine.Object
	{
		if (!IsUnpacked)
		{
			UnpackBundle();
		}

		return CachedBundle.LoadAllAssets<T>();
	}
}
