/*
 *
 * Copyright (c) 2022-2023 Carbon Community 
 * All rights reserved.
 *
 */

using ProtoBuf;

namespace Carbon.Client.Assets;

[ProtoContract(InferTagFromName = true)]
public class Addon : IStore<Addon, Asset>
{
	public const string EXTENSION = ".cca";

	public string Name { get; set; }
	public List<Asset> Assets { get; set; } = new();
	public long CreationTime { get; set; } = DateTime.Now.Ticks;

	public Manifest GetManifest()
	{
		return new Manifest
		{
			Name = Name,
			Assets = Assets.Select(x => x.GetManifest()).ToArray(),
			CreationTime = CreationTime
		};
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

	public class Manifest
	{
		public string Name { get; set; }
		public Asset.Manifest[] Assets { get; set; }
		public long CreationTime { get; set; }
		public string CreationTimeReadable => new DateTime(CreationTime).ToString();
	}
}
