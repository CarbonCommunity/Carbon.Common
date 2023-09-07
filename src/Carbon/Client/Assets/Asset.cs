/*
 *
 * Copyright (c) 2022-2023 Carbon Community 
 * All rights reserved.
 *
 */

using Newtonsoft.Json;
using ProtoBuf;

namespace Carbon.Client.Assets;

[ProtoContract(InferTagFromName = true)]
public class Asset : IDisposable
{
	[ProtoMember(1)]
	public string Name { get; set; }

	[ProtoMember(2)]
	public byte[] Data { get; set; }

	public AssetBundle CachedBundle { get; set; }

	public Manifest GetManifest()
	{
		return new Manifest
		{
			Name = Name,
			BufferLength = Data.Length,
		};
	}

	public static Asset CreateFrom(string name, byte[] data)
	{
		return new Asset
		{
			Name = name,
			Data = data
		};
	}
	public static Asset CreateFromFile(string path)
	{
		return CreateFrom(Path.GetFileNameWithoutExtension(path), OsEx.File.ReadBytes(path));
	}

	public bool IsUnpacked => CachedBundle != null;

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
	}
	public void UnpackBundle()
	{
		if(IsUnpacked)
		{
			return;
		}

		using var stream = new MemoryStream(Data);
		CachedBundle = AssetBundle.LoadFromStream(stream);
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

	public override string ToString()
	{
		return JsonConvert.SerializeObject(GetManifest(), Formatting.Indented);
	}

	public void Dispose()
	{
		if (!IsUnpacked)
		{
			return;
		}

		CachedBundle.Unload(true);

		if (Data != null)
		{
			Array.Clear(Data, 0, Data.Length);
			Data = null;
		}
	}

	public class Manifest
	{
		public string Name { get; set; }
		public int BufferLength { get; set; }
	}
}
