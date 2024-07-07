using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using API.Logger;
using Carbon.Profiler;
using Newtonsoft.Json;
using ProtoBuf;
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
	public const ProfilerArgs AllFlags = AllNoTimingsFlags | ProfilerArgs.Timings;
	public const ProfilerArgs AllNoTimingsFlags = ProfilerArgs.Calls | ProfilerArgs.CallMemory
	                                                                 | ProfilerArgs.AdvancedMemory | ProfilerArgs.GCEvents;

	public static GCRecord GCStats;
	public static AssemblyOutput AssemblyRecords = new();
	public static CallOutput CallRecords = new();
	public static MemoryOutput MemoryRecords = new();
	public static RuntimeAssemblyBank AssemblyBank = new();
	public static Dictionary<ModuleHandle, AssemblyNameEntry> AssemblyMap = new();
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

	[ProtoContract]
	public class AssemblyNameEntry
	{
		[ProtoMember(1 + NATIVE_PROTOCOL)] public string name;
		[ProtoMember(2 + NATIVE_PROTOCOL)] public string displayName;
		[ProtoMember(3 + NATIVE_PROTOCOL)] public MonoProfilerConfig.ProfileTypes profileType;
	}

	[ProtoContract]
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

			for(int i = 0; i < this.Count; i++)
			{
				var record = this[i];
				var otherRecord = other.FirstOrDefault(x => x.assembly_name.displayName == record.assembly_name.displayName);

				if (otherRecord.assembly_name == null)
				{
					break;
				}

				comparison.Add(new AssemblyRecord
				{
					assembly_handle = record.assembly_handle,
					assembly_name = record.assembly_name,

					total_time = record.total_time - otherRecord.total_time,
					total_time_percentage = record.total_time_percentage - otherRecord.total_time_percentage,
					total_exceptions = record.total_exceptions - otherRecord.total_exceptions,
					calls = record.calls - otherRecord.calls,
					alloc = record.alloc - otherRecord.alloc,

					total_time_c = Sample.Compare(record.total_time, otherRecord.total_time),
					total_time_percentage_c = Sample.Compare(record.total_time_percentage, otherRecord.total_time_percentage),
					total_exceptions_c = Sample.Compare(record.total_exceptions, otherRecord.total_exceptions),
					calls_c = Sample.Compare(record.calls, otherRecord.calls),
					alloc_c = Sample.Compare(record.alloc, otherRecord.alloc)
				});
			}

			return comparison;
		}

		public string ToTable()
		{
			using StringTable table = new StringTable("Assembly", "Total Time", "(%)", "Calls", "Memory Usage");

			foreach(AssemblyRecord record in this)
			{
				if (!AssemblyMap.TryGetValue(record.assembly_handle, out AssemblyNameEntry assemblyName))
				{
					continue;
				}

				table.AddRow($" {assemblyName.displayName}",
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

				builder.AppendLine($"{assemblyName.displayName}," +
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

	[ProtoContract]
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

			for(int i = 0; i < this.Count; i++)
			{
				var record = this[i];
				var otherRecord = other.FirstOrDefault(x =>
					x.assembly_name.displayName == record.assembly_name.displayName &&
				    x.method_name == record.method_name);

				if (otherRecord.assembly_name == null)
				{
					break;
				}

				comparison.Add(new CallRecord
				{
					assembly_handle = record.assembly_handle,
					assembly_name = record.assembly_name,
					method_handle = record.method_handle,
					method_name = record.method_name,

					total_time = record.total_time - otherRecord.total_time,
					total_time_percentage = record.total_time_percentage - otherRecord.total_time_percentage,
					own_time = record.own_time - otherRecord.own_time,
					own_time_percentage = record.own_time_percentage - otherRecord.own_time_percentage,
					calls = record.calls - otherRecord.calls,
					total_alloc = record.total_alloc - otherRecord.total_alloc,
					own_alloc = record.own_alloc - otherRecord.own_alloc,
					total_exceptions = record.total_exceptions - otherRecord.total_exceptions,
					own_exceptions = record.own_exceptions - otherRecord.own_exceptions,

					total_time_c = Sample.Compare(record.total_time, otherRecord.total_time),
					total_time_percentage_c = Sample.Compare(record.total_time_percentage, otherRecord.total_time_percentage),
					own_time_c = Sample.Compare(record.own_time, otherRecord.own_time),
					own_time_percentage_c = Sample.Compare(record.own_time_percentage, otherRecord.own_time_percentage),
					calls_c = Sample.Compare(record.calls, otherRecord.calls),
					total_alloc_c = Sample.Compare(record.total_alloc, otherRecord.total_alloc),
					own_alloc_c = Sample.Compare(record.own_alloc, otherRecord.own_alloc),
					total_exceptions_c = Sample.Compare(record.total_exceptions, otherRecord.total_exceptions),
					own_exceptions_c = Sample.Compare(record.own_exceptions, otherRecord.own_exceptions)
				});
			}

			return comparison;
		}

		public string ToTable()
		{
			using StringTable table = new StringTable("Assembly", "Method", "Total Time", "(%)", "Own Time", "(%)", "Calls", "Total Memory", "Own Memory");

			foreach (CallRecord record in this)
			{
				if (!AssemblyMap.TryGetValue(record.assembly_handle, out AssemblyNameEntry assemblyName))
				{
					continue;
				}

				table.AddRow($" {assemblyName.displayName}", $"{record.method_name}",
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

				builder.AppendLine($"{assemblyName.displayName}," +
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

	[ProtoContract]
	public class MemoryOutput : List<MemoryRecord>
	{
		public MemoryOutput Compare(MemoryOutput other)
		{
			if (other == null)
			{
				return null;
			}

			var comparison = new MemoryOutput();

			for(int i = 0; i < this.Count; i++)
			{
				var record = this[i];
				var otherRecord = other.FirstOrDefault(x => x.assembly_name.displayName == record.assembly_name.displayName && x.class_name == record.class_name);

				if (otherRecord.assembly_name == null)
				{
					break;
				}

				comparison.Add(new MemoryRecord
				{
					assembly_handle = record.assembly_handle,
					assembly_name = record.assembly_name,
					class_name = record.class_name,

					class_token = record.class_token,
					allocations = record.allocations - otherRecord.allocations,
					total_alloc_size = record.total_alloc_size - otherRecord.total_alloc_size,
					instance_size = record.instance_size - otherRecord.instance_size,

					allocations_c = Sample.Compare(record.allocations, otherRecord.allocations),
					total_alloc_size_c = Sample.Compare(record.total_alloc_size, otherRecord.total_alloc_size),
					instance_size_c = Sample.Compare(record.instance_size, otherRecord.instance_size)
				});
			}

			return comparison;
		}

		public string ToTable()
		{
			using StringTable table = new StringTable("Assembly", "Class", "Allocations", "Total Alloc. Size", "Instance Size");

			foreach (MemoryRecord record in this)
			{
				if (!AssemblyMap.TryGetValue(record.assembly_handle, out AssemblyNameEntry assemblyName))
				{
					continue;
				}

				table.AddRow($" {assemblyName.displayName}", $"{record.class_name}",
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

				builder.AppendLine($"{assemblyName.displayName}," +
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

	[ProtoContract]
	[StructLayout(LayoutKind.Sequential)]
	public struct GCRecord
	{
		[ProtoMember(1 + NATIVE_PROTOCOL)] public ulong calls;
		[ProtoMember(2 + NATIVE_PROTOCOL)] public ulong total_time;

		[JsonIgnore] public Sample.Difference calls_c;
		[JsonIgnore] public Sample.Difference total_time_c;

		public GCRecord Compare(GCRecord other)
		{
			GCRecord record = default;
			record.calls = calls - other.calls;
			record.total_time = total_time - other.total_time;
			record.calls_c = Sample.Compare(record.calls, other.calls);
			record.total_time_c = Sample.Compare(record.total_time, other.total_time);
			return record;
		}

		// managed
		public double total_time_ms => total_time * 0.001f;

		public string GetTotalTime() => (total_time_ms < 1 ? $"{total_time:n0}μs" : $"{total_time_ms:n0}ms");
	}

	[ProtoContract]
	[StructLayout(LayoutKind.Sequential)]
	public struct AssemblyRecord
	{
		[JsonIgnore] public ModuleHandle assembly_handle;

		[ProtoMember(1 + NATIVE_PROTOCOL)] public ulong total_time;
		[ProtoMember(2 + NATIVE_PROTOCOL)] public double total_time_percentage;
		[ProtoMember(3 + NATIVE_PROTOCOL)] public ulong total_exceptions;
		[ProtoMember(4 + NATIVE_PROTOCOL)] public ulong calls;
		[ProtoMember(5 + NATIVE_PROTOCOL)] public ulong alloc;

		// managed
		[ProtoMember(6 + NATIVE_PROTOCOL)] public AssemblyNameEntry assembly_name;
		public double total_time_ms => total_time * 0.001f;

		[JsonIgnore] public Sample.Difference total_time_c;
		[JsonIgnore] public Sample.Difference total_time_percentage_c;
		[JsonIgnore] public Sample.Difference total_exceptions_c;
		[JsonIgnore] public Sample.Difference calls_c;
		[JsonIgnore] public Sample.Difference alloc_c;

		public string GetTotalTime() => (total_time_ms < 1 ? $"{total_time:n0}μs" : $"{total_time_ms:n0}ms");
	}

	[ProtoContract]
	[StructLayout(LayoutKind.Sequential)]
	public struct MemoryRecord
	{
		public ModuleHandle assembly_handle;
		public IntPtr class_handle;
		[ProtoMember(1 + NATIVE_PROTOCOL)] public ulong allocations;
		[ProtoMember(2 + NATIVE_PROTOCOL)] public ulong total_alloc_size;
		[ProtoMember(3 + NATIVE_PROTOCOL)] public uint instance_size;
		[ProtoMember(4 + NATIVE_PROTOCOL)] public uint class_token;

		// managed
		[ProtoMember(5 + NATIVE_PROTOCOL)] public string class_name;
		[ProtoMember(6 + NATIVE_PROTOCOL)] public AssemblyNameEntry assembly_name;

		[JsonIgnore] public Sample.Difference allocations_c;
		[JsonIgnore] public Sample.Difference total_alloc_size_c;
		[JsonIgnore] public Sample.Difference instance_size_c;
	}

	[ProtoContract]
	[StructLayout(LayoutKind.Sequential)]
	public struct CallRecord
	{
		[JsonIgnore] public ModuleHandle assembly_handle;
		[JsonIgnore] public MonoMethod* method_handle;
		[ProtoMember(1 + NATIVE_PROTOCOL)] public ulong total_time;
		[ProtoMember(2 + NATIVE_PROTOCOL)] public double total_time_percentage;
		[ProtoMember(3 + NATIVE_PROTOCOL)] public ulong own_time;
		[ProtoMember(4 + NATIVE_PROTOCOL)] public double own_time_percentage;
		[ProtoMember(5 + NATIVE_PROTOCOL)] public ulong calls;
		[ProtoMember(6 + NATIVE_PROTOCOL)] public ulong total_alloc;
		[ProtoMember(7 + NATIVE_PROTOCOL)] public ulong own_alloc;
		[ProtoMember(8 + NATIVE_PROTOCOL)] public ulong total_exceptions;
		[ProtoMember(9 + NATIVE_PROTOCOL)] public ulong own_exceptions;

		// managed
		[ProtoMember(10 + NATIVE_PROTOCOL)] public string method_name;
		[ProtoMember(11 + NATIVE_PROTOCOL)] public AssemblyNameEntry assembly_name;
		public double total_time_ms => total_time * 0.001f;
		public double own_time_ms => own_time * 0.001f;

		[JsonIgnore] public Sample.Difference total_time_c;
		[JsonIgnore] public Sample.Difference total_time_percentage_c;
		[JsonIgnore] public Sample.Difference own_time_c;
		[JsonIgnore] public Sample.Difference own_time_percentage_c;
		[JsonIgnore] public Sample.Difference calls_c;
		[JsonIgnore] public Sample.Difference total_alloc_c;
		[JsonIgnore] public Sample.Difference own_alloc_c;
		[JsonIgnore] public Sample.Difference total_exceptions_c;
		[JsonIgnore] public Sample.Difference own_exceptions_c;

		public string GetTotalTime() => (total_time_ms < 1 ? $"{total_time:n0}μs" : $"{total_time_ms:n0}ms");
		public string GetOwnTime() => (own_time_ms < 1 ? $"{own_time:n0}μs" : $"{own_time_ms:n0}ms");
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
			});
		}
		else if(IsRecording && logging)
		{
			_profileWarningTimer = Community.Runtime.Core.timer.Every(60 * 5, () =>
			{
				Logger.Warn($" Reminder: You've been profiling for {TimeEx.Format(MonoProfiler.CurrentDurationTime.TotalSeconds).ToLower()}..");
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
			MapMethodRecords(callOutput);
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
					name = name, displayName = name, profileType = MonoProfilerConfig.ProfileTypes.Assembly
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
					name = name, displayName = name, profileType = MonoProfilerConfig.ProfileTypes.Assembly
				};
				AssemblyMap[entry.assembly_handle] = asmName;
				entry.assembly_name = asmName;
			}

			records[i] = entry;
		}
	}
	private static void MapMethodRecords(List<CallRecord> records)
	{
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
					name = name, displayName = name, profileType = MonoProfilerConfig.ProfileTypes.Assembly
				};
				AssemblyMap[entry.assembly_handle] = asmName;
				entry.assembly_name = asmName;
			}

			records[i] = entry;
		}
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

		if (incremental)
		{
			assemblyName = AssemblyBank.Increment(assemblyName);
		}

		ModuleHandle handle = assembly.ManifestModule.ModuleHandle;

		AssemblyMap[handle] = new AssemblyNameEntry
		{
			name = assembly.GetName().Name,
			displayName = assemblyName,
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
