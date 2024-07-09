﻿using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using API.Logger;
using Carbon.Profiler;
using Newtonsoft.Json;
using Timer = Oxide.Plugins.Timer;

/*
 *
 * Copyright (c) 2024 Carbon Community
 * Copyright (c) 2024 Patrette
 * All rights reserved.
 *
 */

#pragma warning disable CS8500

namespace Carbon.Components;

[SuppressUnmanagedCodeSecurity]
public static unsafe partial class MonoProfiler
{
	public const string ProfileExtension = "cprf";

	public const ProfilerArgs AllFlags = AllNoTimingsFlags | ProfilerArgs.Timings;
	public const ProfilerArgs AllNoTimingsFlags = ProfilerArgs.Calls | ProfilerArgs.CallMemory
	                                                                 | ProfilerArgs.AdvancedMemory | ProfilerArgs.GCEvents;

	public static GCRecord GCStats;
	public static AssemblyOutput AssemblyRecords = new();
	public static CallOutput CallRecords = new();
	public static MemoryOutput MemoryRecords = new();
	public static RuntimeAssemblyBank AssemblyBank = new();
	public static ConcurrentDictionary<ModuleHandle, AssemblyNameEntry> AssemblyMap = new();
	public static Dictionary<IntPtr, string> ClassMap = new();
	public static Dictionary<IntPtr, string> MethodMap = new();
	public static TimeSpan DataProcessingTime;
	public static TimeSpan DurationTime;
	public static TimeSpan CurrentDurationTime => (_durationTimer?.Elapsed).GetValueOrDefault();

	private static Stopwatch _dataProcessTimer;
	private static Stopwatch _durationTimer;
	private static Timer _profileTimer;
	private static Timer _profileWarningTimer;

	public enum ProfilerResultCode : byte
	{
		OK = 0,
		InvalidArgs = 1,
		Aborted = 2,
		MainThreadOnly = 3,
		NotInitialized = 4,
		CorruptedState = 5,
		UnknownError = 6,
	}

	public class AssemblyNameEntry
	{
		public string name;
		public string displayName;
		public string displayNameNonIncrement;
		public MonoProfilerConfig.ProfileTypes profileType;

		public string GetDisplayName(bool isCompared)
		{
			return isCompared ? displayNameNonIncrement : displayName;
		}
	}

	public class AssemblyOutput : List<AssemblyRecord>
	{
		public bool AnyValidRecords => Count > 0;

		public AssemblyOutput Compare(AssemblyOutput other)
		{
			if (other == null)
			{
				return null;
			}

			var comparison = new AssemblyOutput();

			comparison.AddRange(
				from record in this
				let otherRecord = other.FirstOrDefault(x =>
					x.assembly_name.displayNameNonIncrement == record.assembly_name.displayNameNonIncrement)
				select new AssemblyRecord
			{
				assembly_handle = record.assembly_handle,
				assembly_name = record.assembly_name,

				isCompared = true,
				total_time = AreRecordsValid(record, otherRecord) ? Sample.CompareValue(record.total_time, otherRecord.total_time) : default,
				total_time_percentage = AreRecordsValid(record, otherRecord) ? Sample.CompareValue(record.total_time_percentage, otherRecord.total_time_percentage) : default,
				total_exceptions = AreRecordsValid(record, otherRecord) ? Sample.CompareValue(record.total_exceptions, otherRecord.total_exceptions) : default,
				calls = AreRecordsValid(record, otherRecord) ? Sample.CompareValue(record.calls, otherRecord.calls) : default,
				alloc = AreRecordsValid(record, otherRecord) ? Sample.CompareValue(record.alloc, otherRecord.alloc) : default,

				total_time_c = AreRecordsValid(record, otherRecord) ? Sample.Compare(record.total_time, otherRecord.total_time) : Sample.Difference.None,
				total_exceptions_c = AreRecordsValid(record, otherRecord) ? Sample.Compare(record.total_exceptions, otherRecord.total_exceptions) : Sample.Difference.None,
				calls_c = AreRecordsValid(record, otherRecord) ? Sample.Compare(record.calls, otherRecord.calls) : Sample.Difference.None,
				alloc_c = AreRecordsValid(record, otherRecord) ? Sample.Compare(record.alloc, otherRecord.alloc) : Sample.Difference.None
			});

			return comparison;
		}
		public static bool AreRecordsValid(AssemblyRecord recordA, AssemblyRecord recordB) =>
			recordA.IsValid && recordB.IsValid;

		public string ToTable()
		{
			using StringTable table = new StringTable("Assembly", "Total Time", "(%)", "Calls", "Memory Usage");

			foreach(AssemblyRecord record in this)
			{
				if (!AssemblyMap.TryGetValue(record.assembly_handle, out AssemblyNameEntry assemblyName))
				{
					continue;
				}

				table.AddRow($" {assemblyName.GetDisplayName(record.isCompared)}",
					record.total_time == 0 ? string.Empty : record.GetTotalTime(),
					record.total_time_percentage == 0 ? string.Empty : $"{record.total_time_percentage:0}%",
					record.calls == 0 ? string.Empty : $"{record.calls:n0}",
					$"{ByteEx.Format(record.alloc).ToLower()}");
			}

			return table.ToStringMinimal().Trim();
		}
		public string ToCSV()
		{
			StringBuilder builder = PoolEx.GetStringBuilder();

			builder.AppendLine("Assembly," +
			                   "Total Time," +
			                   "(%)," +
			                   "Calls," +
			                   "Memory Usage");

			foreach (AssemblyRecord record in this)
			{
				if (!AssemblyMap.TryGetValue(record.assembly_handle, out AssemblyNameEntry assemblyName))
				{
					continue;
				}

				builder.AppendLine($"{assemblyName.GetDisplayName(record.isCompared)}," +
				                   $"{record.GetTotalTime()}," +
				                   $"{record.total_time_percentage:0}%," +
				                   $"{record.calls:n0}," +
				                   $"{ByteEx.Format(record.alloc).ToLower()}");
			}

			string result = builder.ToString();

			PoolEx.FreeStringBuilder(ref builder);
			return result;
		}
		public string ToJson(bool indented)
		{
			return JsonConvert.SerializeObject(this, indented ? Formatting.Indented : Formatting.None);
		}
	}

	public class CallOutput : List<CallRecord>
	{
		public bool AnyValidRecords => Count > 0;
		public bool Disabled;

		public CallOutput Compare(CallOutput other)
		{
			if (other == null)
			{
				return null;
			}

			var comparison = new CallOutput();

			comparison.AddRange(
				from record in this
				let otherRecord = other.FirstOrDefault(x =>
					x.assembly_name.displayNameNonIncrement == record.assembly_name.displayNameNonIncrement &&
				    x.method_name == record.method_name)
				select new CallRecord
			{
				assembly_handle = record.assembly_handle,
				assembly_name = record.assembly_name,
				method_handle = record.method_handle,
				method_name = record.method_name,

				isCompared = true,
				total_time = AreRecordsValid(record, otherRecord) ? Sample.CompareValue(record.total_time, otherRecord.total_time) : default,
				total_time_percentage = AreRecordsValid(record, otherRecord) ? Sample.CompareValue(record.total_time_percentage, otherRecord.total_time_percentage) : default,
				own_time = AreRecordsValid(record, otherRecord) ? Sample.CompareValue(record.own_time, otherRecord.own_time) : default,
				own_time_percentage = AreRecordsValid(record, otherRecord) ? Sample.CompareValue(record.own_time_percentage, otherRecord.own_time_percentage) : default,
				calls = AreRecordsValid(record, otherRecord) ? Sample.CompareValue(record.calls, otherRecord.calls) : default,
				total_alloc = AreRecordsValid(record, otherRecord) ? Sample.CompareValue(record.total_alloc, otherRecord.total_alloc) : default,
				own_alloc = AreRecordsValid(record, otherRecord) ? Sample.CompareValue(record.own_alloc, otherRecord.own_alloc) : default,
				total_exceptions = AreRecordsValid(record, otherRecord) ? Sample.CompareValue(record.total_exceptions, otherRecord.total_exceptions) : default,
				own_exceptions = AreRecordsValid(record, otherRecord) ? Sample.CompareValue(record.own_exceptions, otherRecord.own_exceptions) : default,

				total_time_c = AreRecordsValid(record, otherRecord) ? Sample.Compare(record.total_time, otherRecord.total_time) : Sample.Difference.None,
				own_time_c = AreRecordsValid(record, otherRecord) ? Sample.Compare(record.own_time, otherRecord.own_time) : Sample.Difference.None,
				calls_c = AreRecordsValid(record, otherRecord) ? Sample.Compare(record.calls, otherRecord.calls) : Sample.Difference.None,
				total_alloc_c = AreRecordsValid(record, otherRecord) ? Sample.Compare(record.total_alloc, otherRecord.total_alloc) : Sample.Difference.None,
				own_alloc_c = AreRecordsValid(record, otherRecord) ? Sample.Compare(record.own_alloc, otherRecord.own_alloc) : Sample.Difference.None,
				total_exceptions_c = AreRecordsValid(record, otherRecord) ? Sample.Compare(record.total_exceptions, otherRecord.total_exceptions) : Sample.Difference.None,
				own_exceptions_c = AreRecordsValid(record, otherRecord) ? Sample.Compare(record.own_exceptions, otherRecord.own_exceptions) : Sample.Difference.None
			});

			return comparison;
		}
		public static bool AreRecordsValid(CallRecord recordA, CallRecord recordB) =>
			recordA.IsValid && recordB.IsValid;

		public string ToTable()
		{
			using StringTable table = new StringTable("Assembly", "Method", "Total Time", "(%)", "Own Time", "(%)", "Calls", "Total Memory", "Own Memory");

			foreach (CallRecord record in this)
			{
				if (!AssemblyMap.TryGetValue(record.assembly_handle, out AssemblyNameEntry assemblyName))
				{
					continue;
				}

				table.AddRow($" {assemblyName.GetDisplayName(record.isCompared)}", $"{record.method_name}",
					record.total_time == 0 ? string.Empty : record.GetTotalTime(),
					record.total_time_percentage == 0 ? string.Empty : $"{record.total_time_percentage:0}%",
					record.own_time == 0 ? string.Empty : record.GetOwnTime(),
					record.own_time_percentage == 0 ? string.Empty : $"{record.own_time_percentage:0}%",
					record.calls == 0 ? string.Empty : $"{record.calls:n0}",
					record.total_alloc == 0 ? string.Empty : $"{ByteEx.Format(record.total_alloc).ToLower()}",
					record.own_alloc == 0 ? string.Empty : $"{ByteEx.Format(record.own_alloc).ToLower()}");
			}

			return table.ToStringMinimal().Trim();
		}
		public string ToCSV()
		{
			StringBuilder builder = PoolEx.GetStringBuilder();

			builder.AppendLine("Assembly," +
			                   "Method," +
			                   "Total Time," +
			                   "(%)," +
			                   "Own Time," +
			                   "(%)," +
			                   "Calls," +
			                   "Memory Usage (Total)," +
			                   "Memory Usage (Own)");

			foreach (CallRecord record in this)
			{
				if (!AssemblyMap.TryGetValue(record.assembly_handle, out AssemblyNameEntry assemblyName))
				{
					continue;
				}

				builder.AppendLine($"{assemblyName.GetDisplayName(record.isCompared)}," +
				                   $"{record.method_name}," +
				                   $"{record.GetTotalTime()}," +
				                   $"{record.total_time_percentage:0}%," +
				                   $"{record.GetOwnTime()}," +
				                   $"{record.own_time_percentage:0}%," +
				                   $"{record.calls:n0}," +
				                   $"{ByteEx.Format(record.total_alloc).ToLower()}," +
				                   $"{ByteEx.Format(record.own_alloc).ToLower()}");
			}

			string result = builder.ToString();

			PoolEx.FreeStringBuilder(ref builder);
			return result;
		}
		public string ToJson(bool indented)
		{
			return JsonConvert.SerializeObject(this, indented ? Formatting.Indented : Formatting.None);
		}
	}

	public class MemoryOutput : List<MemoryRecord>
	{
		public MemoryOutput Compare(MemoryOutput other)
		{
			if (other == null)
			{
				return null;
			}

			var comparison = new MemoryOutput();

			comparison.AddRange(
				from record in this
				let otherRecord = other.FirstOrDefault(x =>
					x.assembly_name.displayNameNonIncrement == record.assembly_name.displayNameNonIncrement &&
					x.class_name == record.class_name)
				select new MemoryRecord
			{
				assembly_handle = record.assembly_handle,
				assembly_name = record.assembly_name,
				class_name = record.class_name,
				class_token = record.class_token,

				isCompared = true,
				allocations = AreRecordsValid(record, otherRecord) ? Sample.CompareValue(record.allocations, otherRecord.allocations) : default,
				total_alloc_size = AreRecordsValid(record, otherRecord) ? Sample.CompareValue(record.total_alloc_size, otherRecord.total_alloc_size) : default,
				instance_size = AreRecordsValid(record, otherRecord) ? Sample.CompareValue(record.instance_size, otherRecord.instance_size) : default,

				allocations_c = AreRecordsValid(record, otherRecord) ? Sample.Compare(record.allocations, otherRecord.allocations) : Sample.Difference.None,
				total_alloc_size_c = AreRecordsValid(record, otherRecord) ? Sample.Compare(record.total_alloc_size, otherRecord.total_alloc_size) : Sample.Difference.None,
				instance_size_c = AreRecordsValid(record, otherRecord) ? Sample.Compare(record.instance_size, otherRecord.instance_size) : Sample.Difference.None
			});

			return comparison;
		}
		public static bool AreRecordsValid(MemoryRecord recordA, MemoryRecord recordB) =>
			recordA.IsValid && recordB.IsValid;

		public string ToTable()
		{
			using StringTable table = new StringTable("Assembly", "Class", "Allocations", "Total Alloc. Size", "Instance Size");

			foreach (MemoryRecord record in this)
			{
				if (!AssemblyMap.TryGetValue(record.assembly_handle, out AssemblyNameEntry assemblyName))
				{
					continue;
				}

				table.AddRow($" {assemblyName.GetDisplayName(record.isCompared)}", $"{record.class_name}",
					record.allocations == 0 ? string.Empty : record.allocations.ToString("n0"),
					record.total_alloc_size == 0 ? string.Empty : $"{ByteEx.Format(record.total_alloc_size).ToLower()}",
					record.instance_size == 0 ? string.Empty : $"{record.instance_size:n0}b");
			}

			return table.ToStringMinimal().Trim();
		}
		public string ToCSV()
		{
			StringBuilder builder = PoolEx.GetStringBuilder();

			builder.AppendLine("Assembly," +
			                   "Class," +
			                   "Allocations," +
			                   "Total Alloc. Size," +
			                   "Instance Size");

			foreach (MemoryRecord record in this)
			{
				if (!AssemblyMap.TryGetValue(record.assembly_handle, out AssemblyNameEntry assemblyName))
				{
					continue;
				}

				builder.AppendLine($"{assemblyName.GetDisplayName(record.isCompared)}," +
				                   $"{record.class_name}," +
				                   $"{record.allocations.ToString("n0")}," +
				                   $"{ByteEx.Format(record.total_alloc_size).ToLower()}," +
				                   $"{record.instance_size:n0}b");
			}

			string result = builder.ToString();

			PoolEx.FreeStringBuilder(ref builder);
			return result;
		}
		public string ToJson(bool indented)
		{
			return JsonConvert.SerializeObject(this, indented ? Formatting.Indented : Formatting.None);
		}
	}

	public class RuntimeAssemblyBank : ConcurrentDictionary<string, int>
	{
		public string Increment(string value)
		{
			return string.IsNullOrEmpty(value) ? string.Empty : $"{value} ({AddOrUpdate(value, 1, (_, arg) => arg + 1)})";
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct GCRecord
	{
		public ulong calls;
		public ulong total_time;

		[JsonIgnore] public bool isCompared;
		[JsonIgnore] public Sample.Difference calls_c;
		[JsonIgnore] public Sample.Difference total_time_c;

		public GCRecord Compare(GCRecord other)
		{
			GCRecord record = default;
			record.calls = Sample.CompareValue(calls, other.calls);
			record.total_time = Sample.CompareValue(total_time, other.total_time);

			record.isCompared = true;
			record.calls_c = Sample.Compare(record.calls, other.calls);
			record.total_time_c = Sample.Compare(record.total_time, other.total_time);
			return record;
		}

		public string ToTable()
		{
			using StringTable table = new StringTable("Calls", "Total Time");

			table.AddRow($" {calls:n0}", $"{GetTotalTime()}");

			return table.ToStringMinimal().Trim();
		}
		public string ToCSV()
		{
			StringBuilder builder = PoolEx.GetStringBuilder();

			builder.AppendLine("Calls," +
			                   "Total Time");

			builder.AppendLine($"{calls}," +
			                   $"{GetTotalTime()}");

			string result = builder.ToString();

			PoolEx.FreeStringBuilder(ref builder);
			return result;
		}
		public string ToJson(bool indented)
		{
			return JsonConvert.SerializeObject(this, indented ? Formatting.Indented : Formatting.None);
		}

		// managed
		public double total_time_ms => total_time * 0.001f;

		[JsonIgnore] public string total_time_ms_str;

		public string GetTotalTime() => total_time_ms_str ??= (total_time_ms < 10 ? $"{total_time:n0}μs" : $"{total_time_ms:n0}ms");
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct AssemblyRecord
	{
		[JsonIgnore] public ModuleHandle assembly_handle;

		public ulong total_time;
		public double total_time_percentage;
		public ulong total_exceptions;
		public ulong calls;
		public ulong alloc;

		// managed
		public AssemblyNameEntry assembly_name;
		public double total_time_ms => total_time * 0.001f;

		[JsonIgnore] public bool IsValid => assembly_name != null;

		[JsonIgnore] public bool isCompared;
		[JsonIgnore] public Sample.Difference total_time_c;
		[JsonIgnore] public Sample.Difference total_exceptions_c;
		[JsonIgnore] public Sample.Difference calls_c;
		[JsonIgnore] public Sample.Difference alloc_c;

		[JsonIgnore] public string total_time_ms_str;

		public string GetTotalTime() => total_time_ms_str ??= total_time_ms < 10 ? $"{total_time:n0}μs" : $"{total_time_ms:n0}ms";
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct MemoryRecord
	{
		[JsonIgnore] public ModuleHandle assembly_handle;
		[JsonIgnore] public IntPtr class_handle;
		public ulong allocations;
		public ulong total_alloc_size;
		public uint instance_size;
		public uint class_token;

		// managed
		public string class_name;
		public AssemblyNameEntry assembly_name;

		[JsonIgnore] public bool IsValid => assembly_name != null;

		[JsonIgnore] public bool isCompared;
		[JsonIgnore] public Sample.Difference allocations_c;
		[JsonIgnore] public Sample.Difference total_alloc_size_c;
		[JsonIgnore] public Sample.Difference instance_size_c;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct CallRecord
	{
		[JsonIgnore] public ModuleHandle assembly_handle;
		[JsonIgnore] public MonoMethod* method_handle;
		public ulong total_time;
		public double total_time_percentage;
		public ulong own_time;
		public double own_time_percentage;
		public ulong calls;
		public ulong total_alloc;
		public ulong own_alloc;
		public ulong total_exceptions;
		public ulong own_exceptions;

		// managed
		public string method_name;
		public AssemblyNameEntry assembly_name;
		public double total_time_ms => total_time * 0.001f;
		public double own_time_ms => own_time * 0.001f;

		[JsonIgnore] public bool IsValid => assembly_name != null;

		[JsonIgnore] public bool isCompared;
		[JsonIgnore] public Sample.Difference total_time_c;
		[JsonIgnore] public Sample.Difference own_time_c;
		[JsonIgnore] public Sample.Difference calls_c;
		[JsonIgnore] public Sample.Difference total_alloc_c;
		[JsonIgnore] public Sample.Difference own_alloc_c;
		[JsonIgnore] public Sample.Difference total_exceptions_c;
		[JsonIgnore] public Sample.Difference own_exceptions_c;

		[JsonIgnore] public string total_time_ms_str;
		[JsonIgnore] public string own_time_ms_str;

		public string GetTotalTime() => total_time_ms_str ??= total_time_ms < 10 ? $"{total_time:n0}μs" : $"{total_time_ms:n0}ms";
		public string GetOwnTime() => own_time_ms_str ??= own_time_ms < 10 ? $"{own_time:n0}μs" : $"{own_time_ms:n0}ms";
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct MonoImageUnion
	{
		[FieldOffset(0)]
		public ModuleHandle handle;
		[FieldOffset(0)]
		public MonoImage* ptr;
	}

	[StructLayout(LayoutKind.Sequential)]
	public readonly struct MonoImage
	{
		public readonly int ref_count;
		public readonly void* storage;

		/* Aliases storage->raw_data when storage is non-NULL. Otherwise NULL. */
		public readonly byte* raw_data;
		public readonly uint raw_data_len;

		public static ModuleHandle image_to_handle(MonoImage* image)
		{
			return new MonoImageUnion { ptr = image }.handle;
		}
		public static MonoImage* handle_to_image(ModuleHandle handle)
		{
			return new MonoImageUnion { handle = handle }.ptr;
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	public readonly struct MonoMethod
	{
		public readonly ushort flags;
		public readonly ushort iflags;
		public readonly uint token;
		public readonly void* klass;
		public readonly void* signature;
		public readonly byte* name;
	}

	[Flags]
	public enum ProfilerArgs : ushort
	{
		None = 0,
		Abort = 1 << 0,
		CallMemory = 1 << 1,
		AdvancedMemory = 1 << 2,
		Timings = 1 << 3,
		Calls = 1 << 4,
		FastResume = 1 << 5, // Pass this when you're toggling the profiler multiple times on the same frame
		GCEvents = 1 << 6
	}


	public static bool Enabled { get; }
	public static bool IsRecording { get; private set; }
	public static bool Crashed { get; }

	public static bool IsCleared => !AssemblyRecords.Any() && !CallRecords.Any();

	public const int NATIVE_PROTOCOL = 3;
	public const int MANAGED_PROTOCOL = NATIVE_PROTOCOL + 121;

	static MonoProfiler()
	{
		try
		{
			ulong np = carbon_get_protocol();
			if (np != NATIVE_PROTOCOL)
			{
				Logger.Error($"Native protocol mismatch (native) {np} != (managed) {NATIVE_PROTOCOL}");
				Enabled = false;
				Crashed = true;
				return;
			}

			ProfilerCallbacks callbacks = new();
			profiler_register_callbacks(&callbacks);
			Enabled = profiler_is_enabled();
			carbon_init_logger(&native_logger);
		}
		catch (Exception ex)
		{
			Crashed = true; // TODO: print an error when running commands if this is true
			Logger.Error("NativeInitFailure", ex);
		}
	}

	private static void native_logger(Severity level, int verbosity, byte* data, int length, LogSource source)
	{
		Logger.Write(level, $"[{source}] {Encoding.UTF8.GetString(data, length)}", verbosity: verbosity);
	}
	private static void native_string_cb(string* target, byte* ptr, int len)
	{
		*target = Encoding.UTF8.GetString(ptr, len);
	}

	private static void memcpy_array_cb<T>(T[]* target, T* src, ulong len)
	{
		T[] data = new T[len];
		*target = data;
		ulong bytes = len * (uint)sizeof(T);
		fixed (T* dst = data)
		{
			Buffer.MemoryCopy(src,dst, bytes, bytes);
		}
	}
	private static void native_iter<T>(List<T>* data, ulong length, IntPtr iter, delegate*<IntPtr, out T, bool> cb) where T: struct
	{
		if (*data == null)
		{
			*data = new((int)length);
		}
		else if (length > (ulong)data->Capacity)
		{
			data->Capacity = (int)length;
		}
		while (cb(iter, out T inst))
		{
			data->Add(inst);
		}
	}

	public static void Clear()
	{
		AssemblyRecords.Clear();
		CallRecords.Clear();
		MemoryRecords.Clear();
		DurationTime = default;
		GCStats = default;
	}
	public static void ToggleProfilingTimed(float duration, ProfilerArgs args = AllFlags, Action<ProfilerArgs> onTimerEnded = null, bool logging = true)
	{
		if (Crashed)
		{
			Logger.Error($"CarbonNative did not properly initialize. Please report to the developers.");
			return;
		}

		_profileTimer?.Destroy();
		_profileTimer = null;
		_profileWarningTimer?.Destroy();
		_profileWarningTimer = null;

		if (!ToggleProfiling(args, logging).GetValueOrDefault())
		{
			if (logging)
			{
				PrintWarn();
			}
		}

		if (duration >= 1f && IsRecording)
		{
			if (logging)
			{
				Logger.Warn($"[MonoProfiler] Profiling duration {TimeEx.Format(duration).ToLower()}..");
			}

			_profileTimer = Community.Runtime.Core.timer.In(duration, () =>
			{
				if (!IsRecording)
				{
					return;
				}

				ToggleProfiling(args, logging).GetValueOrDefault();

				if (logging)
				{
					PrintWarn();
				}

				onTimerEnded?.Invoke(args);

				Clear();
			});
		}
		else if(IsRecording && logging)
		{
			_profileWarningTimer = Community.Runtime.Core.timer.Every(60 * 5, () =>
			{
				Logger.Warn($" Reminder: You've been profiling for {TimeEx.Format(CurrentDurationTime.TotalSeconds).ToLower()}..");
			});
		}

		return;

		static void PrintWarn()
		{
			using StringTable table = new StringTable(" Duration", "Processing", "Assemblies", "Calls");

			table.AddRow(
				$" {TimeEx.Format(DurationTime.TotalSeconds).ToLower()}",
				$"{DataProcessingTime.TotalMilliseconds:0}ms",
				AssemblyRecords.Count,
				CallRecords.Count);

			Logger.Warn(table.ToStringMinimal());
		}
	}
	public static bool? ToggleProfiling(ProfilerArgs args = AllFlags, bool logging = true)
	{
		if (!Enabled)
		{
			Logger.Log("Profiler disabled");
			return null;
		}

		bool state;
		AssemblyRecords.Clear();
		CallRecords.Clear();
		MemoryRecords.Clear();
		List<AssemblyRecord> assemblyOutput = AssemblyRecords;
		List<CallRecord> callOutput = CallRecords;
		List<MemoryRecord> memoryOutput = MemoryRecords;
		GCRecord gcOutput = default;

		if (IsRecording)
		{
			_dataProcessTimer = PoolEx.GetStopwatch();
			_dataProcessTimer.Start();
		}

		ProfilerResultCode result = profiler_toggle(args, &state, &gcOutput, &assemblyOutput, &callOutput, &memoryOutput);

		if (result == ProfilerResultCode.Aborted)
		{
			// Handle abort;
			if (logging)
			{
				Logger.Warn("[MonoProfiler] Profiler aborted");
			}
			IsRecording = false;
			return false;
		}

		if (!state)
		{
			DataProcessingTime = _dataProcessTimer.Elapsed;
			PoolEx.FreeStopwatch(ref _dataProcessTimer);
		}

		if (result != ProfilerResultCode.OK)
		{
			Logger.Error($"[MonoProfiler] Failed to toggle profiler: {result}");
			return null;
		}

		if (assemblyOutput is { Count: > 0 })
		{
			MapAssemblyRecords(assemblyOutput);
		}
		if (callOutput is { Count: > 0 })
		{
			MapCallRecords(callOutput);
		}
		if (memoryOutput is { Count: > 0 })
		{
			MapMemoryRecords(memoryOutput);
		}

		GCStats = gcOutput;

		CallRecords.Disabled = callOutput.IsEmpty();

		IsRecording = state;

		if (state)
		{
			if (logging)
			{
				Logger.Warn($"[MonoProfiler] Started recording..");
			}

			_durationTimer = PoolEx.GetStopwatch();
			_durationTimer.Start();
		}
		else
		{
			if (logging)
			{
				Logger.Warn($"[MonoProfiler] Recording ended");
			}

			DurationTime = _durationTimer.Elapsed;
			PoolEx.FreeStopwatch(ref _durationTimer);
		}

		return state;
	}

	private static void MapAssemblyRecords(List<AssemblyRecord> records)
	{
		for (int i = 0; i < records.Count; i++)
		{
			AssemblyRecord entry = records[i];

			if (AssemblyMap.TryGetValue(entry.assembly_handle, out AssemblyNameEntry asmName))
			{
				entry.assembly_name = asmName;
			}
			else
			{
				string name;
				get_image_name(&name, entry.assembly_handle);
				if (name == null) throw new NullReferenceException();
				asmName = new AssemblyNameEntry
				{
					name = name, displayName = name, displayNameNonIncrement = name, profileType = MonoProfilerConfig.ProfileTypes.Assembly
				};
				AssemblyMap[entry.assembly_handle] = asmName;
				entry.assembly_name = asmName;
			}

			records[i] = entry;
		}
	}
	private static void MapMemoryRecords(List<MemoryRecord> records)
	{
		for (int i = 0; i < records.Count; i++)
		{
			MemoryRecord entry = records[i];

			if (ClassMap.TryGetValue(entry.class_handle, out string className))
			{
				entry.class_name = className;
			}
			else
			{
				get_class_name(&className, entry.class_handle);
				if (className == null) throw new NullReferenceException();
				ClassMap[entry.class_handle] = className;
				entry.class_name = className;
			}

			if (AssemblyMap.TryGetValue(entry.assembly_handle, out AssemblyNameEntry asmName))
			{
				entry.assembly_name = asmName;
			}
			else
			{
				string name;
				get_image_name(&name, entry.assembly_handle);
				if (name == null) throw new NullReferenceException();
				asmName = new AssemblyNameEntry
				{
					name = name, displayName = name, displayNameNonIncrement = name, profileType = MonoProfilerConfig.ProfileTypes.Assembly
				};
				AssemblyMap[entry.assembly_handle] = asmName;
				entry.assembly_name = asmName;
			}

			records[i] = entry;
		}
	}
	private static void MapCallRecords(List<CallRecord> records)
	{
		var temp = PoolEx.GetDictionary<string, CallRecord>();

		for (int i = 0; i < records.Count; i++)
		{
			CallRecord entry = records[i];

			if (MethodMap.TryGetValue((IntPtr)entry.method_handle, out string methName))
			{
				entry.method_name = methName;
			}
			else
			{
				get_method_name(&methName, entry.method_handle);
				MethodMap[(IntPtr)entry.method_handle] = methName ?? throw new NullReferenceException();
				entry.method_name = methName;
			}

			if (AssemblyMap.TryGetValue(entry.assembly_handle, out AssemblyNameEntry asmName))
			{
				entry.assembly_name = asmName;
			}
			else
			{
				string name;
				get_image_name(&name, entry.assembly_handle);
				if (name == null) throw new NullReferenceException();
				asmName = new AssemblyNameEntry
				{
					name = name, displayName = name, displayNameNonIncrement = name, profileType = MonoProfilerConfig.ProfileTypes.Assembly
				};
				AssemblyMap[entry.assembly_handle] = asmName;
				entry.assembly_name = asmName;
			}

			if (temp.TryGetValue(entry.method_name, out var existingRecord))
			{
				existingRecord.total_time += entry.total_time;
				existingRecord.total_time_percentage += entry.total_time_percentage;
				existingRecord.own_time += entry.own_time;
				existingRecord.own_time_percentage += entry.own_time_percentage;
				existingRecord.calls += entry.calls;
				existingRecord.total_alloc += entry.total_alloc;
				existingRecord.own_alloc += entry.own_alloc;
				existingRecord.total_exceptions += entry.total_exceptions;
				existingRecord.own_exceptions += entry.own_exceptions;
				temp[entry.method_name] = existingRecord;
			}
			else
			{
				temp[entry.method_name] = entry;
			}
		}

		records.Clear();
		records.AddRange(temp.Values);

		PoolEx.FreeDictionary(ref temp);
	}

	public static void TryStartProfileFor(MonoProfilerConfig.ProfileTypes profileType, Assembly assembly, string value, bool incremental = false)
	{
		if (!Community.Runtime.MonoProfilerConfig.IsWhitelisted(profileType, value))
		{
			return;
		}

		ProfileAssembly(assembly, value, incremental, profileType);
	}
	public static void ProfileAssembly(Assembly assembly, string assemblyName, bool incremental, MonoProfilerConfig.ProfileTypes profileType)
	{
		if (!Enabled)
		{
			return;
		}

		var incrementedValue = assemblyName;

		if (incremental)
		{
			incrementedValue = AssemblyBank.Increment(assemblyName);
		}

		ModuleHandle handle = assembly.ManifestModule.ModuleHandle;

		AssemblyMap[handle] = new AssemblyNameEntry
		{
			name = assembly.GetName().Name,
			displayName = incrementedValue,
			displayNameNonIncrement = assemblyName,
			profileType = profileType
		};

		register_profiler_assembly(handle);
	}

	#region PInvokes

	[StructLayout(LayoutKind.Sequential)]
	struct ProfilerCallbacks
	{
		private delegate*<string*, byte*, int, void> string_marshal;
		private delegate*<byte[]*, byte*, ulong, void> bytes_marshal;
		private delegate*<List<AssemblyRecord>*, ulong, IntPtr, delegate*<IntPtr, out AssemblyRecord, bool>, void> basic_iter;
		private delegate*<List<CallRecord>*, ulong, IntPtr, delegate*<IntPtr, out CallRecord, bool>, void> advanced_iter;
		private delegate*<List<MemoryRecord>*, ulong, IntPtr, delegate*<IntPtr, out MemoryRecord, bool>, void> memory_iter;

		public ProfilerCallbacks()
		{
			string_marshal = &native_string_cb;
			bytes_marshal = &memcpy_array_cb;
			basic_iter = &native_iter;
			advanced_iter = &native_iter;
			memory_iter = &native_iter;
		}
	}

	public enum LogSource : uint
	{
		Native,
		Profiler
	}

	[DllImport("CarbonNative")]
	private static extern void profiler_register_callbacks(ProfilerCallbacks* callbacks);

	[DllImport("CarbonNative")]
	private static extern void register_profiler_assembly(ModuleHandle handle);

	[DllImport("CarbonNative")]
	private static extern bool profiler_is_enabled();

	[DllImport("CarbonNative")]
	private static extern void carbon_init_logger(delegate*<Severity, int, byte*, int, LogSource, void> logger);

	[DllImport("CarbonNative")]
	private static extern ulong carbon_get_protocol();

	[DllImport("CarbonNative")]
	private static extern void get_image_name(string* str, ModuleHandle handle);

	[DllImport("CarbonNative")]
	private static extern void get_class_name(string* str, IntPtr handle);

	[DllImport("CarbonNative")]
	private static extern void get_method_name(string* str, MonoMethod* handle);

	[DllImport("CarbonNative")]
	private static extern ProfilerResultCode profiler_toggle(
		ProfilerArgs args,
		bool* state,
		GCRecord* gc_out,
		List<AssemblyRecord>* basic_out,
		List<CallRecord>* advanced_out,
		List<MemoryRecord>* mem_out
		);

	#endregion
}
