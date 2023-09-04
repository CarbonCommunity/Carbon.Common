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
	public string Name { get; set; }
	public byte[] Data { get; set; }

	internal AssetBundle _cachedBundle { get; set; }

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

	public bool IsUnpacked => _cachedBundle != null;

	public void UnpackBundle()
	{
		if(IsUnpacked)
		{
			return;
		}

		using var stream = new MemoryStream(Data);
		_cachedBundle = AssetBundle.LoadFromStream(stream);
	}

	public T LoadPrefab<T>(string path) where T : UnityEngine.Object
	{
		if (!IsUnpacked)
		{
			UnpackBundle();
		}

		return _cachedBundle.LoadAsset<T>(path);
	}
	public T[] LoadAllPrefabs<T>() where T : UnityEngine.Object
	{
		if (!IsUnpacked)
		{
			UnpackBundle();
		}

		return _cachedBundle.LoadAllAssets<T>();
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

		_cachedBundle.Unload(true);
	}

	public class Manifest
	{
		public string Name { get; set; }
		public int BufferLength { get; set; }
	}
}
