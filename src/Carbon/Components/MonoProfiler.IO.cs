/*
 *
 * Copyright (c) 2024 Carbon Community
 * Copyright (c) 2024 Patrette
 * All rights reserved.
 *
 */

using System.IO.Compression;
using Carbon.Profiler;

namespace Carbon.Components;

public partial class MonoProfiler
{
	public static bool ValidateFile(string file, out int protocol, out double duration, out bool isCompared)
	{
		try
		{
			if (!OsEx.File.Exists(file))
			{
				protocol = default;
				duration = default;
				isCompared = default;
				return false;
			}

			using var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read);
			using var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);
			using var reader = new BinaryReader(gzipStream);

			protocol = reader.ReadInt32();
			duration = reader.ReadDouble();
			isCompared = reader.ReadBoolean();
			return protocol == MANAGED_PROTOCOL;
		}
		catch (Exception exception)
		{
			Logger.Error($"Failed MonoProfiler file validation: {file}", exception);
		}

		protocol = default;
		duration = default;
		isCompared = default;
		return false;
	}

	public static byte[] SerializeSample(Sample sample)
	{
		using var memoryStream = new MemoryStream();
		using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress))
		{
			using var writer = new BinaryWriter(gzipStream);

			writer.Write(MANAGED_PROTOCOL);
			writer.Write(sample.Duration);
			writer.Write(sample.IsCompared);
			writer.Write((int)sample.Comparison.Duration);

			var mappedAssemblies = PoolEx.GetDictionary<string, int>();

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
				writer.Write(assembly.comparison.isCompared);
				writer.Write((int)assembly.comparison.total_time);
				writer.Write((int)assembly.comparison.total_exceptions);
				writer.Write((int)assembly.comparison.calls);
				writer.Write((int)assembly.comparison.alloc);

				mappedAssemblies.Add(assembly.assembly_name.name, i);
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
				mappedAssemblies.TryGetValue(call.assembly_name.name, out var id);
				writer.Write(id);
				writer.Write(call.comparison.isCompared);
				writer.Write((int)call.comparison.total_time);
				writer.Write((int)call.comparison.own_time);
				writer.Write((int)call.comparison.calls);
				writer.Write((int)call.comparison.total_alloc);
				writer.Write((int)call.comparison.own_alloc);
				writer.Write((int)call.comparison.total_exceptions);
				writer.Write((int)call.comparison.own_exceptions);
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
				mappedAssemblies.TryGetValue(memory.assembly_name.name, out var id);
				writer.Write(id);
				writer.Write(memory.comparison.isCompared);
				writer.Write((int)memory.comparison.allocations);
				writer.Write((int)memory.comparison.total_alloc_size);
			}

			writer.Write(sample.GC.calls);
			writer.Write(sample.GC.total_time);
			writer.Write(sample.GC.comparison.isCompared);
			writer.Write((int)sample.GC.comparison.calls_c);
			writer.Write((int)sample.GC.comparison.total_time_c);

			PoolEx.FreeDictionary(ref mappedAssemblies);
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
		sample.Comparison.Duration = (Sample.Difference)reader.ReadInt32();

		var names = PoolEx.GetDictionary<int, AssemblyNameEntry>();

		var assemblyLength = reader.ReadInt32();
		for (int i = 0; i < assemblyLength; i++)
		{
			AssemblyRecord record = default;
			record.total_time = reader.ReadUInt64();
			record.total_time_percentage = reader.ReadDouble();
			record.total_exceptions = reader.ReadUInt64();
			record.calls = reader.ReadUInt64();
			record.alloc = reader.ReadUInt64();
			record.assembly_name = new AssemblyNameEntry
			{
				name = reader.ReadString(),
				displayName = reader.ReadString(),
				displayNameNonIncrement = reader.ReadString(),
				profileType = (MonoProfilerConfig.ProfileTypes)reader.ReadInt32()
			};
			record.comparison.isCompared = reader.ReadBoolean();
			record.comparison.total_time = (Sample.Difference)reader.ReadInt32();
			record.comparison.total_exceptions = (Sample.Difference)reader.ReadInt32();
			record.comparison.calls = (Sample.Difference)reader.ReadInt32();
			record.comparison.alloc = (Sample.Difference)reader.ReadInt32();
			sample.Assemblies.Add(record);
			names.Add(i, record.assembly_name);
		}

		var callsLength = reader.ReadInt32();
		for (int i = 0; i < callsLength; i++)
		{
			CallRecord record = default;
			record.total_time = reader.ReadUInt64();
			record.total_time_percentage = reader.ReadDouble();
			record.own_time = reader.ReadUInt64();
			record.own_time_percentage = reader.ReadDouble();
			record.calls = reader.ReadUInt64();
			record.total_alloc = reader.ReadUInt64();
			record.own_alloc = reader.ReadUInt64();
			record.total_exceptions = reader.ReadUInt64();
			record.own_exceptions = reader.ReadUInt64();
			record.method_name = reader.ReadString();
			if (names.TryGetValue(reader.ReadInt32(), out var val))
			{
				record.assembly_name = val;
			}
			record.comparison.isCompared = reader.ReadBoolean();
			record.comparison.total_time = (Sample.Difference)reader.ReadInt32();
			record.comparison.own_time = (Sample.Difference)reader.ReadInt32();
			record.comparison.calls = (Sample.Difference)reader.ReadInt32();
			record.comparison.total_alloc = (Sample.Difference)reader.ReadInt32();
			record.comparison.own_alloc = (Sample.Difference)reader.ReadInt32();
			record.comparison.total_exceptions = (Sample.Difference)reader.ReadInt32();
			record.comparison.own_exceptions = (Sample.Difference)reader.ReadInt32();
			sample.Calls.Add(record);
		}

		var memoryLength = reader.ReadInt32();
		for (int i = 0; i < memoryLength; i++)
		{
			MemoryRecord record = default;
			record.allocations = reader.ReadUInt64();
			record.total_alloc_size = reader.ReadUInt64();
			record.instance_size = reader.ReadUInt32();
			record.class_token = reader.ReadUInt32();
			record.class_name = reader.ReadString();
			if (names.TryGetValue(reader.ReadInt32(), out var val))
			{
				record.assembly_name = val;
			}
			record.comparison.isCompared = reader.ReadBoolean();
			record.comparison.allocations = (Sample.Difference)reader.ReadInt32();
			record.comparison.total_alloc_size = (Sample.Difference)reader.ReadInt32();
			sample.Memory.Add(record);
		}

		sample.GC.calls = reader.ReadUInt64();
		sample.GC.total_time = reader.ReadUInt64();
		sample.FromDisk = true;

		PoolEx.FreeDictionary(ref names);
		return sample;
	}
}
