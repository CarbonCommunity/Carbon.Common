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
public class Addon : IStore<Addon, Asset>
{
	public const string EXTENSION = ".cca";

	public string Name { get; set; }
	public string Author { get; set; }
	public string Description { get; set; }
	public string Version { get; set; }
	public string Checksum { get; set; }
	public Dictionary<string, Asset> Assets { get; set; } = new();
	public long CreationTime { get; set; } = DateTime.Now.Ticks;

	public Manifest GetManifest()
	{
		return new Manifest
		{
			Name = Name,
			Assets = Assets.Select(x => x.Value.GetManifest()).ToArray(),
			CreationTime = CreationTime
		};
	}

	public static Addon Create(AddonInfo info, params Asset[] assets) 
	{
		var addon = new Addon
		{
			Name = info.Name,
			Author = info.Author,
			Description = info.Description,
			Version = info.Version,
			Checksum = info.Checksum,
		};

		foreach(var asset in assets)
		{
			addon.Assets.Add(asset.Name, asset);
		}

		return addon;
	}
	public static Addon ImportFromBuffer(byte[] buffer)
	{
		return Serializer.Deserialize<Addon>(new ReadOnlySpan<byte>(buffer, 0, buffer.Length));

	}
	public static Addon ImportFromFile(string path)
	{
		var data = OsEx.File.ReadBytes(path);
		var result = ImportFromBuffer(data);
		Array.Clear(data, 0, data.Length);
		data = null;
		return result;
	}
	public byte[] Store()
	{
		using var stream = new MemoryStream();
		Serializer.Serialize(stream, this);
		return stream.ToArray();
	}
	public void StoreToFile(string path)
	{
		path += EXTENSION;

		OsEx.File.Create(path, Store());
	}

	public override string ToString()
	{
		return JsonConvert.SerializeObject(GetManifest(), Formatting.Indented);
	}

	public class Manifest
	{
		public string Name { get; set; }
		public Asset.Manifest[] Assets { get; set; }
		public long CreationTime { get; set; }
		public string CreationTimeReadable => new DateTime(CreationTime).ToString();
	}

	public struct AddonInfo
	{
		public string Name;
		public string Author;
		public string Description;
		public string Version;
		public string Checksum;
	}
}
