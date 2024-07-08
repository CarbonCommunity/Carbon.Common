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
		[ProtoMember(6 + NATIVE_PROTOCOL)] public bool IsCompared;

		public static Sample Create() => new()
		{
			Duration = 0,
			Assemblies = new(),
			Calls = new(),
			Memory = new(),
			GC = default
		};
		public static Sample Load(byte[] data)
		{
			return DeserializeSample(data);
		}

		[JsonIgnore] public bool FromDisk;
		[JsonIgnore] public bool IsCleared => Assemblies == null || !Assemblies.Any();

		[JsonIgnore] public Difference Duration_c;

		public Sample Compare(Sample other)
		{
			Sample sample = default;
			sample.FromDisk = true;
			sample.Duration = MathEx.Max(Duration, other.Duration) - MathEx.Min(Duration, other.Duration);
			sample.Duration_c = Compare(Duration, other.Duration);
			sample.Assemblies = Assemblies.Compare(other.Assemblies);
			sample.Calls = Calls.Compare(other.Calls);
			sample.Memory = Memory.Compare(other.Memory);
			sample.GC = GC.Compare(other.GC);
			sample.IsCompared = true;
			return sample;
		}

		public void Resample()
		{
			Clear();

			FromDisk = false;
			IsCompared = false;
			Duration = DurationTime.TotalSeconds;
			Assemblies.AddRange(AssemblyRecords);
			Calls.AddRange(CallRecords);
			Memory.AddRange(MemoryRecords);
			GC = GCStats;
		}
		public void Clear()
		{
			IsCompared = false;
			Duration = default;
			FromDisk = false;
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
			None,
			ValueHigher,
			ValueEqual,
			ValueLower
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
			if (a == b)
			{
				return Difference.ValueEqual;
			}

			return a > b ? Difference.ValueHigher : Difference.ValueLower;
		}
		public static Difference Compare(uint a, uint b)
		{
			if (a == b)
			{
				return Difference.ValueEqual;
			}

			return a > b ? Difference.ValueHigher : Difference.ValueLower;
		}
		public static Difference Compare(double a, double b)
		{
			if (a == b)
			{
				return Difference.ValueEqual;
			}

			return a > b ? Difference.ValueHigher : Difference.ValueLower;
		}

		public const string ValueHigherStr = "<color=#91ff0a>\u2191</color>";
		public const string ValueLowerStr = "<color=#ff370a>\u2193</color>";
		public const string ValueEqualStr = "<color=#fff30a>—</color>";

		public static string GetDifferenceString(Difference difference)
		{
			return difference switch
			{
				Difference.ValueHigher => ValueHigherStr,
				Difference.ValueEqual => ValueEqualStr,
				Difference.ValueLower => ValueLowerStr,
				_ => string.Empty
			};
		}

		public static ulong CompareValue(ulong a, ulong b)
		{
			return MathEx.Max(a, b) - MathEx.Min(a, b);
		}
		public static uint CompareValue(uint a, uint b)
		{
			return MathEx.Max(a, b) - MathEx.Min(a, b);
		}
		public static double CompareValue(double a, double b)
		{
			return MathEx.Max(a, b) - MathEx.Min(a, b);
		}
	}
}
