/*
 *
 * Copyright (c) 2024 Carbon Community
 * Copyright (c) 2024 Patrette
 * All rights reserved.
 *
 */

using ProtoBuf;

namespace Carbon.Components;

public partial class MonoProfiler
{
	[ProtoContract]
	public struct Sample
	{
		[ProtoMember(1)] public AssemblyOutput Assemblies;
		[ProtoMember(2)] public CallOutput Calls;
		[ProtoMember(3)] public MemoryOutput Memory;
		[ProtoMember(4)] public GCRecord GC;

		public static Sample Create() => new()
		{
			Assemblies = new(),
			Calls = new(),
			Memory = new()
		};

		public Sample Compare(Sample other)
		{
			var comparedSample = Create();


		}
		public void Resample()
		{
			Clear();

			Assemblies.AddRange(AssemblyRecords);
			Calls.AddRange(CallRecords);
			Memory.AddRange(MemoryRecords);
			GC = GCStats;
		}
		public void Clear()
		{
			Assemblies ??= new();
			Calls ??= new();
			Memory ??= new();

			Assemblies?.Clear();
			Calls?.Clear();
			Memory?.Clear();
			GC = default;
		}

		public enum Difference
		{
			ValueHigher = 1,
			ValueEqual = 0,
			ValueLower = -1
		}

		public static Difference Compare(ulong a, ulong b)
		{
			return (Difference)a.CompareTo(b);
		}
		public static Difference Compare(uint a, uint b)
		{
			return (Difference)a.CompareTo(b);
		}
		public static Difference Compare(double a, double b)
		{
			return (Difference)a.CompareTo(b);
		}
	}
}
