/*
 *
 * Copyright (c) 2022-2023 Carbon Community 
 * All rights reserved.
 *
 */

using ProtoBuf;

namespace Carbon.Client.Assets;

[ProtoContract(InferTagFromName = true)]
public class Asset
{
	public string Name { get; set; }
	public byte[] Data { get; set; }

	public Manifest GetManifest()
	{
		return new Manifest
		{
			Name = Name,
			BufferLength = Data.Length,
		};
	}

	public class Manifest
	{
		public string Name { get; set; }
		public int BufferLength { get; set; }
	}
}
