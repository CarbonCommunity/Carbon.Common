/*
 *
 * Copyright (c) 2022-2023 Carbon Community 
 * All rights reserved.
 *
 */

using ProtoBuf;

namespace Carbon.Client.Packets;

[ProtoContract(InferTagFromName = true)]
public class ItemDefinitionUpdate : BasePacket
{
	public string Shortname { get; set; }
	public string DisplayName { get; set; }
	public string DisplayDescription { get; set; }

	public override void Dispose()
	{

	}
}
