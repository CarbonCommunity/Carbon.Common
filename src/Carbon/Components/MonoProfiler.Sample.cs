/*
 *
 * Copyright (c) 2024 Carbon Community
 * Copyright (c) 2024 Patrette
 * All rights reserved.
 *
 */

using ProtoBuf;
using MathEx = Carbon.Extensions.MathEx;

namespace Carbon.Components;

public partial class MonoProfiler
{
	[ProtoContract]
	public struct Sample
	{
		[ProtoMember(1 + NATIVE_PROTOCOL)] public AssemblyOutput Assemblies;
		[ProtoMember(2 + NATIVE_PROTOCOL)] public CallOutput Calls;
		[ProtoMember(3 + NATIVE_PROTOCOL)] public MemoryOutput Memory;
		[ProtoMember(4 + NATIVE_PROTOCOL)] public GCRecord GC;

		public static Sample Create() => new()
		{
			Assemblies = new(),
			Calls = new(),
			Memory = new()
		};

		public Sample Compare(Sample other)
		{
			Sample sample = default;
			sample.Assemblies = Assemblies.Compare(other.Assemblies);
			sample.Calls = Calls.Compare(other.Calls);
			sample.Memory = Memory.Compare(other.Memory);
			sample.GC = GC.Compare(other.GC);
			return sample;

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
			if (a > b)
			{
				return Difference.ValueHigher;
			}

			return a < b ? Difference.ValueLower : Difference.ValueEqual;
		}
		public static Difference Compare(uint a, uint b)
		{
			if (a > b)
			{
				return Difference.ValueHigher;
			}

			return a < b ? Difference.ValueLower : Difference.ValueEqual;
		}
		public static Difference Compare(double a, double b)
		{
			if (a > b)
			{
				return Difference.ValueHigher;
			}

			return a < b ? Difference.ValueLower : Difference.ValueEqual;
		}

		public static ulong CompareValue(ulong a, ulong b)
		{
			return MathEx.Max(a, b);
		}
		public static uint CompareValue(uint a, uint b)
		{
			return MathEx.Max(a, b);
		}
		public static double CompareValue(double a, double b)
		{
			return MathEx.Max(a, b);
		}
	}
}
