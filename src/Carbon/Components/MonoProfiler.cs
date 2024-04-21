using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using API.Logger;
using Carbon.Profiler;

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
	public static BasicOutput BasicRecords = new();
	public static AdvancedOutput AdvancedRecords = new();
	public static RuntimeAssemblyBank AssemblyBank = new();

	public static Dictionary<ModuleHandle, String> asmMap = new();

	public enum ProfilerResultCode : byte
	{
		OK = 0,
		MainThreadOnly = 1,
		NotInitialized = 2,
		UnknownError = 3,
	}

		public class BasicOutput : List<BasicRecord>
	{
		public bool AnyValidRecords => Count > 0;

		public string ToTable()
		{
			using var table = new StringTable("Assembly", "Total Time", "(%)", "Calls", "Memory Usage");

			foreach(var record in this)
			{
				if (!asmMap.TryGetValue(record.assembly_handle, out var assemblyName))
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
				if (!asmMap.TryGetValue(record.assembly_handle, out var assemblyName))
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
	}
	public class AdvancedOutput : List<AdvancedRecord>
	{
		public bool AnyValidRecords => Count > 0;
		public bool Disabled;

		public string ToTable()
		{
			using var table = new StringTable("Assembly", "Method", "Total Time", "(%)", "Own Time", "(%)", "Calls", "Total Memory", "Own Memory");

			foreach (var record in this)
			{
				if (!asmMap.TryGetValue(record.assembly_handle, out var assemblyName))
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
				if (!asmMap.TryGetValue(record.assembly_handle, out var assemblyName))
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
	}
	public class RuntimeAssemblyBank : ConcurrentDictionary<string, int>
	{
		public string Increment(string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return string.Empty;
			}

			var output = 0;

			AddOrUpdate(value, output = 1, (val, arg) => output = arg++);

			return $"{value}_#{output}";
		}
	}
	[StructLayout(LayoutKind.Sequential)]
	public struct BasicRecord
	{
		public ModuleHandle assembly_handle;
		public ulong total_time;
		public double total_time_percentage;
		public ulong calls;
		public ulong alloc;
	}
	[StructLayout(LayoutKind.Sequential)]
	public struct AdvancedRecord
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

	public static bool? ToggleProfiling(bool advanced = false)
	{
		if (!Enabled)
		{
			Logger.Log("Profiled disabled");
			return null;
		}

		bool state;
		BasicRecord[] basicOutput = null;
		AdvancedRecord[] advancedOutput = null;

		var result = profiler_toggle(advanced, &state, &basicOutput, &advancedOutput, &native_string_cb, &native_iter<BasicRecord>, &native_iter<AdvancedRecord>);

		if (result != ProfilerResultCode.OK)
		{
			Logger.Error($"Failed to toggle profiler: {result}");
			return null;
		}

		if (basicOutput != null)
			MapOutput(basicOutput);

		_recording = state;

		return state;
	}

	private static void MapOutput(BasicRecord[] records)
	{
		for (int index = 0; index < records.Length; index++)
		{
			ref BasicRecord entry = ref records[index];
			if (!asmMap.ContainsKey(entry.assembly_handle))
			{
				string name = null;
				get_image_name(&name, entry.assembly_handle, &native_string_cb);
				name ??= "UNKNOWN";
				asmMap[entry.assembly_handle] = name;
			}
		}
	}

	public static void TryStartProfileFor(MonoProfilerConfig.ProfileTypes profileType, Assembly assembly, string value, bool incremental = false)
	{
		if (!Community.Runtime.MonoProfilerConfig.IsWhitelisted(profileType, value))
		{
			return;
		}

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

		asmMap[handle] = assemblyName;

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
		BasicRecord[]* basic_out,
		AdvancedRecord[]* advanced_out,
		delegate*<string*, byte*, int, void> string_marshal,
		delegate*<BasicRecord[]*, ulong, IntPtr, delegate*<IntPtr, BasicRecord*, bool>, void> basic_iter,
		delegate*<AdvancedRecord[]*, ulong, IntPtr, delegate*<IntPtr, AdvancedRecord*, bool>, void> advanced_iter
		);

	#endregion
}
