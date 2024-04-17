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
public static unsafe class MonoProfiler
{
	public enum ProfilerResultCode : byte
	{
		OK = 0,
		MainThreadOnly = 1,
		NotInitialized = 2,
		UnknownError = 3,
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

	public static bool? ToggleProfiler()
	{
		if (!Enabled)
		{
			Logger.Log("Profiled disabled");
			return null;
		}

		bool state;
		string basic = null;
		string advanced = null;

		var result = profiler_toggle(true, &state, &basic, &advanced, &native_string_cb);

		if (result != ProfilerResultCode.OK)
		{
			Logger.Error($"Failed to toggle profiler: {result}");
			return null;
		}

		if (basic != null)
		{
			Logger.Log($"Basic Results:\n\n{basic}\n");
		}

		if (advanced != null)
		{
			Logger.Log($"Advanced Results:\n\n{advanced}\n");
		}

		_recording = state;

		return state;
	}
	public static void MarkAssemblyForProfiling(Assembly assembly, string assemblyName)
	{
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
}
