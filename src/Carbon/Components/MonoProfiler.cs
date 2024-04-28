using System.Collections.Concurrent;
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
public static unsafe class MonoProfiler
{
	public static AssemblyOutput AssemblyRecords = new();
	public static CallOutput CallRecords = new();
	public static RuntimeAssemblyBank AssemblyBank = new();
	public static RuntimeAssemblyMap AssemblyMap = new();
	public static RuntimeAssemblyType AssemblyType = new();
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
		Aborted = 1,
		MainThreadOnly = 2,
		NotInitialized = 3,
		UnknownError = 4,
	}

	public class AssemblyOutput : List<AssemblyRecord>
	{
		public bool AnyValidRecords => Count > 0;

		public string ToTable()
		{
			using StringTable table = new StringTable("Assembly", "Total Time", "(%)", "Calls", "Memory Usage");

			foreach(AssemblyRecord record in this)
			{
				if (!AssemblyMap.TryGetValue(record.assembly_handle, out string assemblyName))
				{
					continue;
				}

				table.AddRow($" {assemblyName}",
					record.total_time == 0 ? string.Empty : $"{record.total_time}ms",
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
				if (!AssemblyMap.TryGetValue(record.assembly_handle, out string assemblyName))
				{
					continue;
				}

				builder.AppendLine($"{assemblyName}," +
				                   $"{record.total_time}ms," +
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

		public string ToTable()
		{
			using StringTable table = new StringTable("Assembly", "Method", "Total Time", "(%)", "Own Time", "(%)", "Calls", "Total Memory", "Own Memory");

			foreach (CallRecord record in this)
			{
				if (!AssemblyMap.TryGetValue(record.assembly_handle, out string assemblyName))
				{
					continue;
				}

				table.AddRow($" {assemblyName}", $"{record.method_name}",
					record.total_time == 0 ? string.Empty : $"{record.total_time}ms",
					record.total_time_percentage == 0 ? string.Empty : $"{record.total_time_percentage:0}%",
					record.own_time == 0 ? string.Empty : $"{record.own_time}ms",
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
				if (!AssemblyMap.TryGetValue(record.assembly_handle, out string assemblyName))
				{
					continue;
				}

				builder.AppendLine($"{assemblyName}," +
				                   $"{record.method_name}," +
				                   $"{record.total_time}ms," +
				                   $"{record.total_time_percentage:0}%," +
				                   $"{record.own_time}ms," +
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
	public class RuntimeAssemblyBank : ConcurrentDictionary<string, int>
	{
		public string Increment(string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return string.Empty;
			}

			int index = 0;

			AddOrUpdate(value, index = 1, (val, arg) => index = arg++);

			return $"{value} ({index})";
		}
	}
	public class RuntimeAssemblyMap : Dictionary<ModuleHandle, string>;
	public class RuntimeAssemblyType : Dictionary<ModuleHandle, MonoProfilerConfig.ProfileTypes>;

	[StructLayout(LayoutKind.Sequential)]
	public struct AssemblyRecord
	{
		[JsonIgnore] public ModuleHandle assembly_handle;
		public ulong total_time;
		public double total_time_percentage;
		public ulong calls;
		public ulong alloc;
		public string assembly_name;
		public MonoProfilerConfig.ProfileTypes assembly_type;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct CallRecord
	{
		[JsonIgnore] public ModuleHandle assembly_handle;
		[JsonIgnore] public MonoMethod* method_handle;
		public string method_name;
		public ulong total_time;
		public double total_time_percentage;
		public ulong own_time;
		public double own_time_percentage;
		public ulong calls;
		public ulong total_alloc;
		public ulong own_alloc;
		public string assembly_name;
		public MonoProfilerConfig.ProfileTypes assembly_type;
	}

	[StructLayout(LayoutKind.Explicit)]
	private struct MonoImageUnion
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
			return new MonoImageUnion() { ptr = image }.handle;
		}
		public static MonoImage* handle_to_image(ModuleHandle handle)
		{
			return new MonoImageUnion() { handle = handle }.ptr;
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
	public enum ProfilerArgs : byte
	{
		None = 0,
		Abort = 1 << 0,
		Advanced = 1 << 1,
		Memory = 1 << 2,
		AdvancedMemory = 1 << 3,
		Timings = 1 << 4
	}

	private static bool _enabled = false;
	private static bool _recording = false;

	public static bool Enabled => _enabled;
	public static bool Recording => _recording;
	public static bool Crashed { get; private set; }

	public static bool IsCleared => !AssemblyRecords.Any() && !CallRecords.Any();

	public const ulong NATIVE_PROTOCOL = 1;

	static MonoProfiler()
	{
		try
		{
			ulong np = carbon_get_protocol();
			if (np != NATIVE_PROTOCOL)
			{
				Logger.Error($"Native protocol mismatch (native) {np} != (managed) {NATIVE_PROTOCOL}");
				_enabled = false;
				return;
			}
			_enabled = profiler_is_enabled();
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
		DurationTime = default;
	}
	public static void ToggleProfilingTimed(float duration, ProfilerArgs args = ProfilerArgs.Advanced | ProfilerArgs.AdvancedMemory | ProfilerArgs.Memory | ProfilerArgs.Timings, Action<ProfilerArgs> onTimerEnded = null)
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

		if (!ToggleProfiling(args).GetValueOrDefault())
		{
			PrintWarn();
		}

		if (duration >= 1f && Recording)
		{
			Logger.Warn($"[Profiler] Profiling duration {TimeEx.Format(duration).ToLower()}..");

			_profileTimer = Community.Runtime.CorePlugin.timer.In(duration, () =>
			{
				if (!Recording)
				{
					return;
				}

				ToggleProfiling(args).GetValueOrDefault();
				PrintWarn();

				onTimerEnded?.Invoke(args);
			});
		}
		else if(Recording)
		{
			_profileWarningTimer = Community.Runtime.CorePlugin.timer.Every(60, () =>
			{
				Logger.Warn($" Reminder: You've been profile recording for {TimeEx.Format(MonoProfiler.CurrentDurationTime.TotalSeconds).ToLower()}..");
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
	public static bool? ToggleProfiling(ProfilerArgs args = ProfilerArgs.Advanced | ProfilerArgs.AdvancedMemory | ProfilerArgs.Memory | ProfilerArgs.Timings)
	{
		if (!Enabled)
		{
			Logger.Log("Profiled disabled");
			return null;
		}

		bool state;
		AssemblyRecords.Clear();
		CallRecords.Clear();
		List<AssemblyRecord> assemblyOutput = AssemblyRecords;
		List<CallRecord> callOutput = CallRecords;

		if (Recording)
		{
			_dataProcessTimer = PoolEx.GetStopwatch();
			_dataProcessTimer.Start();
		}

		ProfilerResultCode result = profiler_toggle(args, &state, &assemblyOutput, &callOutput, &native_string_cb, &native_iter, &native_iter);

		if (result == ProfilerResultCode.Aborted)
		{
			// Handle abort;
			Logger.Warn("Profiler aborted");
			_recording = false;
			return false;
		}

		if (!state)
		{
			DataProcessingTime = _dataProcessTimer.Elapsed;
			PoolEx.FreeStopwatch(ref _dataProcessTimer);
		}

		if (result != ProfilerResultCode.OK)
		{
			Logger.Error($"Failed to toggle profiler: {result}");
			return null;
		}

		if (assemblyOutput is { Count: > 0 })
		{
			MapRecords(assemblyOutput, callOutput);
		}

		CallRecords.Disabled = callOutput.IsEmpty();

		_recording = state;

		if (state)
		{
			_durationTimer = PoolEx.GetStopwatch();
			_durationTimer.Start();
		}
		else
		{
			DurationTime = _durationTimer.Elapsed;
			PoolEx.FreeStopwatch(ref _durationTimer);
		}

		return state;
	}
	public static void Refresh()
	{
		RefreshMetadata(AssemblyRecords, CallRecords);
	}

	private static void MapRecords(IList<AssemblyRecord> assemblies, IList<CallRecord> calls)
	{
		for (int i = 0; i < assemblies.Count; i++)
		{
			AssemblyRecord entry = assemblies[i];

			 if (AssemblyMap.ContainsKey(entry.assembly_handle)) continue;
			 string name = null;

			get_image_name(&name, entry.assembly_handle, &native_string_cb);

			AssemblyMap[entry.assembly_handle] = name ?? "UNKNOWN";

			AssemblyType.TryGetValue(entry.assembly_handle, out var type);

			assemblies[i] = entry with { assembly_name = name, assembly_type = type };
		}

		for (int i = 0; i < calls.Count; i++)
		{
			CallRecord entry = calls[i];

			AssemblyMap.TryGetValue(entry.assembly_handle, out var name);
			AssemblyType.TryGetValue(entry.assembly_handle, out var type);

			calls[i] = entry with { assembly_name = name, assembly_type = type };
		}
	}
	private static void RefreshMetadata(IList<AssemblyRecord> assemblies, IList<CallRecord> calls)
	{
		for (int i = 0; i < assemblies.Count; i++)
		{
			AssemblyRecord entry = assemblies[i];

			AssemblyMap.TryGetValue(entry.assembly_handle, out var name);
			AssemblyType.TryGetValue(entry.assembly_handle, out var type);

			assemblies[i] = entry with { assembly_name = name, assembly_type = type };
		}

		for (int i = 0; i < calls.Count; i++)
		{
			CallRecord entry = calls[i];

			AssemblyMap.TryGetValue(entry.assembly_handle, out var name);
			AssemblyType.TryGetValue(entry.assembly_handle, out var type);

			calls[i] = entry with { assembly_name = name, assembly_type = type };
		}
	}

	public static void TryStartProfileFor(MonoProfilerConfig.ProfileTypes profileType, Assembly assembly, string value, bool incremental = false)
	{
		if (!Community.Runtime.MonoProfilerConfig.IsWhitelisted(profileType, value))
		{
			return;
		}

		AssemblyType[assembly.ManifestModule.ModuleHandle] = profileType;
		ProfileAssembly(assembly, value, incremental);
	}
	public static void ProfileAssembly(Assembly assembly, string assemblyName, bool incremental)
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

		AssemblyMap[handle] = assemblyName;

		register_profiler_assembly(handle);
	}

	#region PInvokes

	public enum LogSource : uint
	{
		Native,
		Profiler
	}

	[DllImport("CarbonNative")]
	private static extern ulong register_profiler_assembly(ModuleHandle handle);

	[DllImport("CarbonNative")]
	private static extern bool profiler_is_enabled();

	[DllImport("CarbonNative")]
	private static extern void carbon_init_logger(delegate*<Severity, int, byte*, int, LogSource, void> logger);

	[DllImport("CarbonNative")]
	private static extern ulong carbon_get_protocol();

	[DllImport("CarbonNative")]
	private static extern void get_image_name(string* str, ModuleHandle handle, delegate*<string*, byte*, int, void> string_marshal);

	[DllImport("CarbonNative")]
	private static extern ProfilerResultCode profiler_toggle(
		ProfilerArgs args,
		bool* state,
		List<AssemblyRecord>* basic_out,
		List<CallRecord>* advanced_out,
		delegate*<string*, byte*, int, void> string_marshal,
		delegate*<List<AssemblyRecord>*, ulong, IntPtr, delegate*<IntPtr, out AssemblyRecord, bool>, void> basic_iter,
		delegate*<List<CallRecord>*, ulong, IntPtr, delegate*<IntPtr, out CallRecord, bool>, void> advanced_iter
		);

	#endregion
}
