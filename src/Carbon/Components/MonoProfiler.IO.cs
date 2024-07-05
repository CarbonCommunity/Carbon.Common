/*
 *
 * Copyright (c) 2024 Carbon Community
 * Copyright (c) 2024 Patrette
 * All rights reserved.
 *
 */

namespace Carbon.Components;

public partial class MonoProfiler
{
	public static byte[] SerializeSample(Sample sample)
	{
		using var stream = new MemoryStream();
		ProtoBuf.Serializer.Serialize(stream, sample);
		return stream.ToArray();
	}

	public static Sample DeserializeSample(byte[] buffer)
	{
		using var stream = new MemoryStream(buffer);
		return ProtoBuf.Serializer.Deserialize<Sample>(stream);
	}

}
