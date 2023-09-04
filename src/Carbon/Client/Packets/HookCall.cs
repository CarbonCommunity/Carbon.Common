/*
 *
 * Copyright (c) 2022-2023 Carbon Community 
 * All rights reserved.
 *
 */

using ProtoBuf;

namespace Carbon.Client.Packets;

[ProtoContract(InferTagFromName = true)]
public class HookCall : BasePacket
{
	public string Hook { get; set; }

	public override void Dispose()
	{

	}
}
