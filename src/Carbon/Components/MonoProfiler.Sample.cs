/*
 *
 * Copyright (c) 2024 Carbon Community
 * Copyright (c) 2024 Patrette
 * All rights reserved.
 *
 */

using Newtonsoft.Json;
using ProtoBuf;
using MathEx = Carbon.Extensions.MathEx;

namespace Carbon.Components;

public partial class MonoProfiler
{
	[ProtoContract]
	public struct Sample
	{
		[ProtoMember(1 + NATIVE_PROTOCOL)] public double Duration;
		[ProtoMember(2 + NATIVE_PROTOCOL)] public AssemblyOutput Assemblies;
		[ProtoMember(3 + NATIVE_PROTOCOL)] public CallOutput Calls;
		[ProtoMember(4 + NATIVE_PROTOCOL)] public MemoryOutput Memory;
		[ProtoMember(5 + NATIVE_PROTOCOL)] public GCRecord GC;

		public static Sample Create() => new()
		{
			Assemblies = new(),
			Calls = new(),
			Memory = new()
		};
		public static Sample Load(byte[] data)
		{
			return DeserializeSample(data);
		}

		[JsonIgnore] public bool IsCleared => Assemblies == null || !Assemblies.Any();

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

			Duration = DurationTime.TotalSeconds;
			Assemblies.AddRange(AssemblyRecords);
			Calls.AddRange(CallRecords);
			Memory.AddRange(MemoryRecords);
			GC = GCStats;
		}
		public void Clear()
		{
			Duration = default;
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

		public string ToTable()
		{
			var builder = PoolEx.GetStringBuilder();

			builder.AppendLine(Assemblies.ToTable());
			builder.AppendLine(Calls.ToTable());
			builder.AppendLine(Memory.ToTable());
			builder.AppendLine(GC.ToTable());

			var result = builder.ToString();
			PoolEx.FreeStringBuilder(ref builder);
			return result;
		}
		public string ToCSV()
		{
			var builder = PoolEx.GetStringBuilder();

			builder.AppendLine(Assemblies.ToCSV());
			builder.AppendLine(Calls.ToCSV());
			builder.AppendLine(Memory.ToCSV());
			builder.AppendLine(GC.ToCSV());

			var result = builder.ToString();
			PoolEx.FreeStringBuilder(ref builder);
			return result;
		}
		public string ToJson(bool indented)
		{
			return JsonConvert.SerializeObject(this, indented ? Formatting.Indented : Formatting.None);
		}
		public byte[] ToProto()
		{
			return SerializeSample(this);
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
