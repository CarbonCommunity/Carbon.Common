using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using API.Logger;

/*
 *
 * Copyright (c) 2024 Carbon Community
 * Copyright (c) 2024 Patrette
 * All rights reserved.
 *
 */

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type

namespace Carbon.Components;

[SuppressUnmanagedCodeSecurity]
public static unsafe partial class MonoProfiler
{
	public static BasicOutput BasicRecords = new();
	public static AdvancedOutput AdvancedRecords = new();
	public static RuntimeAssemblyBank AssemblyBank = new();

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
				table.AddRow($" {record.Assembly}",
					record.TotalTime == 0 ? string.Empty : $"{record.TotalTime:n0}ms",
					record.TotalTimePercentage == 0 ? string.Empty : $"{record.TotalTimePercentage:0}%",
					record.Calls == 0 ? string.Empty : $"{record.Calls:n0}",
					$"{ByteEx.Format(record.MemoryUsage).ToLower()}");
			}

			return table.ToStringMinimal().Trim();
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
				table.AddRow($" {record.Assembly}", $"{record.Method}",
					record.TotalTime == 0 ? string.Empty : $"{record.TotalTime:n0}ms",
					record.TotalTimePercentage == 0 ? string.Empty : $"{record.TotalTimePercentage:0}%",
					record.OwnTime == 0 ? string.Empty : $"{record.OwnTime:n0}ms",
					record.OwnTimePercentage == 0 ? string.Empty : $"{record.OwnTimePercentage:0}%",
					record.Calls == 0 ? string.Empty : $"{record.Calls:n0}",
					record.TotalMemoryUsage == 0 ? string.Empty : $"{ByteEx.Format(record.TotalMemoryUsage).ToLower()}",
					record.OwnMemoryUsage == 0 ? string.Empty : $"{ByteEx.Format(record.OwnMemoryUsage).ToLower()}");
			}

			return table.ToStringMinimal().Trim();
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

	public struct BasicRecord
	{
		public string Assembly;
		public long TotalTime;
		public float TotalTimePercentage;
		public long Calls;
		public long MemoryUsage;
	}
	public struct AdvancedRecord
	{
		public string Assembly;
		public string Method;
		public long TotalTime;
		public float TotalTimePercentage;
		public long OwnTime;
		public float OwnTimePercentage;
		public long Calls;
		public long TotalMemoryUsage;
		public long OwnMemoryUsage;
	}

	private static bool _enabled = false;
	private static bool _recording = false;

	public static bool Enabled => _enabled;
	public static bool Recording => _recording;

	static MonoProfiler()
	{
		try
		{
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

	public static bool? ToggleProfiler(bool profileAdvanced = false)
	{
		if (!Enabled)
		{
			Logger.Log("Profiled disabled");
			return null;
		}

		bool state;
		string basic = null;
		string advanced = null;

		var result = profiler_toggle(profileAdvanced, &state, &basic, &advanced, &native_string_cb);

		if (result != ProfilerResultCode.OK)
		{
			Logger.Error($"Failed to toggle profiler: {result}");
			return null;
		}

		if (basic != null)
		{
			ParseBasicRecords(basic);
		}

		if (advanced != null && profileAdvanced)
		{
			ParseAdvancedRecords(advanced);
		}

		AdvancedRecords.Disabled = !profileAdvanced;

		_recording = state;

		return state;
	}
	public static void MarkAssemblyForProfiling(Assembly assembly, string assemblyName)
	{
		if (!Enabled)
		{
			return;
		}

		assemblyName = AssemblyBank.Increment(assemblyName);

		if (!string.IsNullOrWhiteSpace(assemblyName))
		{
			fixed (char* csptr = assemblyName)
			{
				register_profiler_assembly(assembly.ManifestModule.ModuleHandle, csptr, assemblyName.Length);
			}
		}
		else
		{
			register_profiler_assembly(assembly.ManifestModule.ModuleHandle, null, 0);
		}
	}

	#region PInvokes

	public enum LogSource : uint
	{
		Native,
		Profiler
	}

	[DllImport("CarbonNative")]
	private static extern void register_profiler_assembly(ModuleHandle handle, char* csptr, int len);

	[DllImport("CarbonNative")]
	private static extern bool profiler_is_enabled();

	[DllImport("CarbonNative")]
	private static extern void carbon_init_logger(delegate*<Severity, int, byte*, int, LogSource, void> logger);

	[DllImport("CarbonNative")]
	private static extern ProfilerResultCode profiler_toggle(bool gen_advanced, bool* state, string* basic_out, string* advanced_out, delegate*<string*, byte*, int, void> cb);

	#endregion

	#region Parsing

	private static string[] _space = new string[] { " " };

	private static void ParseBasicRecords(string data)
	{
		BasicRecords.Clear();

		using var lines = TemporaryArray<string>.New(data.Split('\n'));

		for(int i = 1; i < lines.Length; i++)
		{
			using var line = TemporaryArray<string>.New(lines.Get(i).Split(_space, StringSplitOptions.RemoveEmptyEntries));

			BasicRecords.Add(new BasicRecord
			{
				Assembly = line.Get(0),
				TotalTime = line.Get(1).ToLong(),
				TotalTimePercentage = line.Get(2).ToFloat(),
				Calls = line.Get(3).ToLong(),
				MemoryUsage = line.Get(4).ToLong()
			});
		}
	}
	private static void ParseAdvancedRecords(string data)
	{
		AdvancedRecords.Clear();

		using var lines = TemporaryArray<string>.New(data.Split('\n'));

		for (int i = 1; i < lines.Length; i++)
		{
			using var line = TemporaryArray<string>.New(lines.Get(i).Split(_space, StringSplitOptions.RemoveEmptyEntries));

			AdvancedRecords.Add(new AdvancedRecord
			{
				Assembly = line.Get(0),
				Method = line.Get(1),
				TotalTime = line.Get(2).ToLong(),
				TotalTimePercentage = line.Get(3).ToFloat(),
				OwnTime = line.Get(4).ToLong(),
				OwnTimePercentage = line.Get(5).ToFloat(),
				Calls = line.Get(6).ToLong(),
				TotalMemoryUsage = line.Get(7).ToLong(),
				OwnMemoryUsage = line.Get(8).ToLong()
			});
		}
	}

	#endregion
}
