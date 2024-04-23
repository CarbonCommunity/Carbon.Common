using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using API.Logger;
using Carbon.Profiler;
using Facepunch;
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
		MainThreadOnly = 1,
		NotInitialized = 2,
		UnknownError = 3,
	}

	public class AssemblyOutput : List<AssemblyRecord>
	{
		public bool AnyValidRecords => Count > 0;

		public string ToTable()
		{
			using var table = new StringTable("Assembly", "Total Time", "(%)", "Calls", "Memory Usage");

			foreach(var record in this)
			{
				if (!AssemblyMap.TryGetValue(record.assembly_handle, out var assemblyName))
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
			var builder = PoolEx.GetStringBuilder();

			builder.AppendLine("Assembly," +
			                   "Total Time," +
			                   "(%)," +
			                   "Calls," +
			                   "Memory Usage");

			foreach (var record in this)
			{
				if (!AssemblyMap.TryGetValue(record.assembly_handle, out var assemblyName))
				{
					continue;
				}

				builder.AppendLine($"{assemblyName}," +
				                   $"{record.total_time}ms," +
				                   $"{record.total_time_percentage:0}%," +
				                   $"{record.calls:n0}," +
				                   $"{ByteEx.Format(record.alloc).ToLower()}");
			}

			var result = builder.ToString();

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
			using var table = new StringTable("Assembly", "Method", "Total Time", "(%)", "Own Time", "(%)", "Calls", "Total Memory", "Own Memory");

			foreach (var record in this)
			{
				if (!AssemblyMap.TryGetValue(record.assembly_handle, out var assemblyName))
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
			var builder = PoolEx.GetStringBuilder();

			builder.AppendLine("Assembly," +
			                   "Method," +
			                   "Total Time," +
			                   "(%)," +
			                   "Own Time," +
			                   "(%)," +
			                   "Calls," +
			                   "Memory Usage (Total)," +
			                   "Memory Usage (Own)");

			foreach (var record in this)
			{
				if (!AssemblyMap.TryGetValue(record.assembly_handle, out var assemblyName))
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

			var result = builder.ToString();

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

			var index = 0;

			AddOrUpdate(value, index = 1, (val, arg) => index = arg++);

			return $"{value} ({index})";
		}
	}
	public class RuntimeAssemblyMap : Dictionary<ModuleHandle, string>;
	public class RuntimeAssemblyType : Dictionary<ModuleHandle, MonoProfilerConfig.ProfileTypes>;

	[StructLayout(LayoutKind.Sequential)]
	public struct AssemblyRecord
	{
		public ModuleHandle assembly_handle;
		public ulong total_time;
		public double total_time_percentage;
		public ulong calls;
		public ulong alloc;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct CallRecord
	{
		public ModuleHandle assembly_handle;
		public RuntimeMethodHandle method_handle;
		public string method_name;
		public ulong total_time;
		public double total_time_percentage;
		public ulong own_time;
		public double own_time_percentage;
		public ulong calls;
		public ulong total_alloc;
		public ulong own_alloc;
	}

	private static bool _enabled = false;
	private static bool _recording = false;

	public static bool Enabled => _enabled;
	public static bool Recording => _recording;

	public static bool IsCleared => !AssemblyRecords.Any() && !CallRecords.Any();

	public const ulong NATIVE_PROTOCOL = 0;

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

	public static void Clear()
	{
		AssemblyRecords.Clear();
		CallRecords.Clear();
		DurationTime = default;
	}
	public static void ToggleProfilingTimed(float duration = 0, bool advanced = true)
	{
		_profileTimer?.Destroy();
		_profileTimer = null;
		_profileWarningTimer?.Destroy();
		_profileWarningTimer = null;

		if (!ToggleProfiling(true).GetValueOrDefault())
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

				ToggleProfiling(advanced).GetValueOrDefault();
				PrintWarn();
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
			using var table = new StringTable("Duration", "Processing", "Basic", "Advanced");

			table.AddRow(
				TimeEx.Format(DurationTime.TotalSeconds).ToLower(),
				$"{DataProcessingTime.TotalMilliseconds:0}ms",
				AssemblyRecords.Count.ToString("n0"),
				CallRecords.Count.ToString("n0"));

			Logger.Warn(table.ToStringMinimal());
		}
	}
	public static bool? ToggleProfiling(bool advanced = true)
	{
		if (!Enabled)
		{
			Logger.Log("Profiled disabled");
			return null;
		}

		bool state;
		AssemblyRecord[] basicOutput = null;
		CallRecord[] advancedOutput = null;

		if (Recording)
		{
			_dataProcessTimer = PoolEx.GetStopwatch();
			_dataProcessTimer.Start();
		}

		var result = profiler_toggle(advanced, &state, &basicOutput, &advancedOutput, &native_string_cb, &native_iter, &native_iter);

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

		if (basicOutput != null)
		{
			AssemblyRecords.Clear();
			AssemblyRecords.AddRange(basicOutput);
			MapBasicRecords(basicOutput);
		}

		if (advancedOutput != null)
		{
			CallRecords.Clear();
			CallRecords.AddRange(advancedOutput);
		}

		CallRecords.Disabled = !advanced;

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

	private static void MapBasicRecords(AssemblyRecord[] records)
	{
		for (int i = 0; i < records.Length; i++)
		{
			ref var entry = ref records[i];

			if (!AssemblyMap.ContainsKey(entry.assembly_handle))
			{
				var name = (string)null;

				get_image_name(&name, entry.assembly_handle, &native_string_cb);

				AssemblyMap[entry.assembly_handle] = name ?? "UNKNOWN";
			}
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

		var handle = assembly.ManifestModule.ModuleHandle;

		AssemblyMap[handle] = assemblyName;

		register_profiler_assembly(handle);
	}
	private static void native_iter<T>(T[]* data, ulong length, IntPtr iter, delegate*<IntPtr, T*, bool> cb) where T: struct
	{
		*data = new T[(int)length];
		ulong index = 0;
		T inst = default;
		while (length > index && cb(iter, &inst))
		{
			(*data)[index] = inst;
			index++;
		}
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
		bool gen_advanced,
		bool* state,
		AssemblyRecord[]* basic_out,
		CallRecord[]* advanced_out,
		delegate*<string*, byte*, int, void> string_marshal,
		delegate*<AssemblyRecord[]*, ulong, IntPtr, delegate*<IntPtr, AssemblyRecord*, bool>, void> basic_iter,
		delegate*<CallRecord[]*, ulong, IntPtr, delegate*<IntPtr, CallRecord*, bool>, void> advanced_iter
		);

	#endregion
}
