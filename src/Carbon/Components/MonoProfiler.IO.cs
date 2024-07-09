/*
 *
 * Copyright (c) 2024 Carbon Community
 * Copyright (c) 2024 Patrette
 * All rights reserved.
 *
 */

using System.IO.Compression;
using System.Runtime.InteropServices.WindowsRuntime;
using Carbon.Profiler;

namespace Carbon.Components;

public partial class MonoProfiler
{
	public static byte[] SerializeSample(Sample sample)
	{
		using var memoryStream = new MemoryStream();
		using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress))
		{
			using var writer = new BinaryWriter(gzipStream);

			writer.Write(MANAGED_PROTOCOL);
			writer.Write(sample.Duration);
			writer.Write(sample.IsCompared);

			var names = PoolEx.GetDictionary<string, int>();

			writer.Write(sample.Assemblies.Count);
			for (int i = 0; i < sample.Assemblies.Count; i++)
			{
				var assembly = sample.Assemblies[i];
				writer.Write(assembly.total_time);
				writer.Write(assembly.total_time_percentage);
				writer.Write(assembly.total_exceptions);
				writer.Write(assembly.calls);
				writer.Write(assembly.alloc);
				writer.Write(assembly.assembly_name.name);
				writer.Write(assembly.assembly_name.displayName);
				writer.Write(assembly.assembly_name.displayNameNonIncrement);
				writer.Write((int)assembly.assembly_name.profileType);

				names.Add(assembly.assembly_name.name, i);
			}

			writer.Write(sample.Calls.Count);
			for (int i = 0; i < sample.Calls.Count; i++)
			{
				var call = sample.Calls[i];
				writer.Write(call.total_time);
				writer.Write(call.total_time_percentage);
				writer.Write(call.own_time);
				writer.Write(call.own_time_percentage);
				writer.Write(call.calls);
				writer.Write(call.total_alloc);
				writer.Write(call.own_alloc);
				writer.Write(call.total_exceptions);
				writer.Write(call.own_exceptions);
				writer.Write(call.method_name);

				names.TryGetValue(call.assembly_name.name, out var id);
				writer.Write(id);
			}

			writer.Write(sample.Memory.Count);
			for (int i = 0; i < sample.Memory.Count; i++)
			{
				var memory = sample.Memory[i];
				writer.Write(memory.allocations);
				writer.Write(memory.total_alloc_size);
				writer.Write(memory.instance_size);
				writer.Write(memory.class_token);
				writer.Write(memory.class_name);

				names.TryGetValue(memory.assembly_name.name, out var id);
				writer.Write(id);
			}

			writer.Write(sample.GC.calls);
			writer.Write(sample.GC.total_time);

			PoolEx.FreeDictionary(ref names);
		}

		return memoryStream.ToArray();
	}

	public static Sample DeserializeSample(byte[] buffer)
	{
		using var memoryStream = new MemoryStream(buffer);
		using var gzipStream = new GZipStream(memoryStream, CompressionMode.Decompress);
		using var reader = new BinaryReader(gzipStream);

		var sample = Sample.Create();
		var protocol = reader.ReadUInt32();

		if (protocol != MANAGED_PROTOCOL)
		{
			throw new Exception($"Invalid protocol: {protocol} [expected {MANAGED_PROTOCOL}]");
		}

		sample.Duration = reader.ReadDouble();
		sample.IsCompared = reader.ReadBoolean();

		var names = PoolEx.GetDictionary<int, AssemblyNameEntry>();

		var assemblyLength = reader.ReadInt32();
		for (int i = 0; i < assemblyLength; i++)
		{
			var assembly = new AssemblyRecord
			{
				total_time = reader.ReadUInt64(),
				total_time_percentage = reader.ReadDouble(),
				total_exceptions = reader.ReadUInt64(),
				calls = reader.ReadUInt64(),
				alloc = reader.ReadUInt64(),
				assembly_name = new AssemblyNameEntry
				{
					name = reader.ReadString(),
					displayName = reader.ReadString(),
					displayNameNonIncrement = reader.ReadString(),
					profileType = (MonoProfilerConfig.ProfileTypes)reader.ReadInt32()
				}
			};
			sample.Assemblies.Add(assembly);
			names.Add(i, assembly.assembly_name);
		}

		var callsLength = reader.ReadInt32();
		for (int i = 0; i < callsLength; i++)
		{
			CallRecord call = new CallRecord
			{
				total_time = reader.ReadUInt64(),
				total_time_percentage = reader.ReadDouble(),
				own_time = reader.ReadUInt64(),
				own_time_percentage = reader.ReadDouble(),
				calls = reader.ReadUInt64(),
				total_alloc = reader.ReadUInt64(),
				own_alloc = reader.ReadUInt64(),
				total_exceptions = reader.ReadUInt64(),
				own_exceptions = reader.ReadUInt64(),
				method_name = reader.ReadString()
			};

			if (names.TryGetValue(reader.ReadInt32(), out var val))
			{
				call.assembly_name = val;
			}

			sample.Calls.Add(call);
		}

		var memoryLength = reader.ReadInt32();
		for (int i = 0; i < memoryLength; i++)
		{
			MemoryRecord memory = new MemoryRecord
			{
				allocations = reader.ReadUInt64(),
				total_alloc_size = reader.ReadUInt64(),
				instance_size = reader.ReadUInt32(),
				class_token = reader.ReadUInt32(),
				class_name = reader.ReadString()
			};

			var id = reader.ReadInt32();
			if (names.TryGetValue(id, out var val))
			{
				memory.assembly_name = val;
			}

			sample.Memory.Add(memory);
		}

		sample.GC.calls = reader.ReadUInt64();
		sample.GC.total_time = reader.ReadUInt64();
		sample.FromDisk = true;

		PoolEx.FreeDictionary(ref names);
		return sample;
	}

	public static bool ValidateFile(string file, out int protocol, out double duration)
	{
		try
		{
			if (!OsEx.File.Exists(file))
			{
				protocol = default;
				duration = default;
				return false;
			}

			using var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read);
			using var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);
			using var reader = new BinaryReader(gzipStream);

			protocol = reader.ReadInt32();
			duration = reader.ReadDouble();

			return protocol == MANAGED_PROTOCOL;
		}
		catch (Exception exception)
		{
			Logger.Error($"Failed MonoProfiler file validation: {file}", exception);
		}

		protocol = default;
		duration = default;
		return false;
	}
}
