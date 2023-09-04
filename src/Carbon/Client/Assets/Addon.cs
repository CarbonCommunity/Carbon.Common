/*
 *
 * Copyright (c) 2022-2023 Carbon Community 
 * All rights reserved.
 *
 */

using ProtoBuf;

namespace Carbon.Client.Assets;

[ProtoContract(InferTagFromName = true)]
public class Addon
{
	public string Name { get; set; }
	public List<Asset> Assets { get; set; } = new();
}
