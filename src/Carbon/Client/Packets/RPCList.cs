/*
 *
 * Copyright (c) 2022-2023 Carbon Community 
 * All rights reserved.
 *
 */

using ProtoBuf;

namespace Carbon.Client.Packets;

[ProtoContract(InferTagFromName = true)]
public class RPCList : BasePacket
{
	public string[] RpcNames;
	public uint[] RpcIds;

	public static RPCList Get()
	{
		return new RPCList()
		{
			RpcIds = RPC.rpcList.Select(x => x.Id).ToArray(),
			RpcNames = RPC.rpcList.Select(x => x.Name).ToArray()
		};
	}

	public void Sync()
	{
		foreach (var item in RpcNames)
		{
			RPC.Get(item);
		}
		foreach (var item in RpcIds)
		{
			RPC.Get(item);
		}

		foreach(var item in RPC.rpcList)
		{
			Console.WriteLine($"Registered RPC: {item.Name}[{item.Id}]");
		}
	}

	public override void Dispose()
	{
		Array.Clear(RpcNames, 0, RpcNames.Length);
		Array.Clear(RpcIds, 0, RpcIds.Length);
	}
}
