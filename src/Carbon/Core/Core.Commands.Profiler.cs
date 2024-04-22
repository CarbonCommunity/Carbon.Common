using API.Assembly;
using API.Commands;
using Carbon.Base.Interfaces;
using Carbon.Profiler;
using HarmonyLib;
using Newtonsoft.Json;
using Oxide.Game.Rust.Cui;

/*
 *
 * Copyright (c) 2022-2023 Carbon Community
 * All rights reserved.
 *
 */

namespace Carbon.Core;
#pragma warning disable IDE0051

public partial class CorePlugin : CarbonPlugin
{
	[CommandVar("profiler", "Mono profiling status. Must restart the server for changes to apply.")]
	[AuthLevel(2)]
	private bool ProfilerEnabled
	{
		get { return Community.Runtime.MonoProfilerConfig.Enabled; }
		set
		{
			Community.Runtime.MonoProfilerConfig.Enabled = value;
			Community.Runtime.SaveMonoProfilerConfig();
		}
	}

	[ConsoleCommand("profile", "Toggles recording status of the Carbon native Mono-profiling. Syntax: `c.profile [-a]`, [-a] to include advanced profiling when profiling stops.")]
	[AuthLevel(2)]
	private void ProfilerToggle(ConsoleSystem.Arg arg)
	{
		if (!ProfilerEnabled)
		{
			arg.ReplyWith("Mono profiler is disabled. Run `c.profiler true` to enable it. Must restart the server for changes to apply.");
			return;
		}

		if (MonoProfiler.ToggleProfiling(arg.GetString(0).Equals("-a")).GetValueOrDefault())
		{
			return;
		}

		arg.ReplyWith(" Use: c.profiler.print [-table|-csv] [-f] to see the output.");
	}

	[ConsoleCommand("profiler.print", "If any parsed data available, it'll print basic and advanced information.")]
	[AuthLevel(2)]
	private void ProfilerPrint(ConsoleSystem.Arg arg)
	{
		if (MonoProfiler.Recording)
		{
			arg.ReplyWith("Profiler is actively recording.");
			return;
		}

		var mode = arg.GetString(0);
		var toFile = arg.HasArg("-f");

		var basicOutput = MonoProfiler.BasicRecords.ToTable();
		var advancedOutput = MonoProfiler.AdvancedRecords.ToTable();

		switch (mode)
		{
			case "-csv":
				basicOutput = MonoProfiler.BasicRecords.ToCSV();
				advancedOutput = MonoProfiler.AdvancedRecords.ToCSV();
				break;
		}

		if (toFile)
		{
			var date = DateTime.Now;
			var file = Path.Combine(Defines.GetRustRootFolder(),
				$"profile-{date.Year}_{date.Month}_{date.Day}_{date.Hour}{date.Minute}{date.Second}.txt");
			OsEx.File.Create(file, $"{basicOutput}\n\n{advancedOutput}");

			arg.ReplyWith($"Saved at {file}");
		}
		else
		{
			arg.ReplyWith(
				$"Basic:\n{(MonoProfiler.BasicRecords.AnyValidRecords ? basicOutput : "No valid records")}\n\n" +
				$"Advanced:\n{(MonoProfiler.AdvancedRecords.Disabled ? "Advanced profiling is disabled. Use [-a] to enable advanced profiling." : MonoProfiler.AdvancedRecords.AnyValidRecords ? advancedOutput : "No valid records")}");
		}
	}

	[CommandVar("profiler.allocs", "Once the Mono profiler gets initialized, enhanced allocation data will be tracked. Must restart the server for changes to apply.")]
	[AuthLevel(2)]
	private bool ProfilerAllocations
	{
		get { return Community.Runtime.MonoProfilerConfig.Allocations; }
		set
		{
			Community.Runtime.MonoProfilerConfig.Allocations = value;
			Community.Runtime.SaveMonoProfilerConfig();
		}
	}

	[ConsoleCommand("profiler.tracks", "All tracking lists present in the config which are used by the Mono profiler for tracking.")]
	[AuthLevel(2)]
	private void ProfilerTracked(ConsoleSystem.Arg arg)
	{
		arg.ReplyWith($"Tracked Assemblies ({Community.Runtime.MonoProfilerConfig.Assemblies.Count:n0}):\n" +
		              $"{Community.Runtime.MonoProfilerConfig.Assemblies.Select(x => $"- {x}").ToString("\n")}\n" +
		              $"Tracked Plugins ({Community.Runtime.MonoProfilerConfig.Plugins.Count:n0}):\n" +
		              $"{Community.Runtime.MonoProfilerConfig.Plugins.Select(x => $"- {x}").ToString("\n")}\n" +
		              $"Tracked Modules ({Community.Runtime.MonoProfilerConfig.Modules.Count:n0}):\n" +
		              $"{Community.Runtime.MonoProfilerConfig.Modules.Select(x => $"- {x}").ToString("\n")}\n" +
		              $"Tracked Extensions ({Community.Runtime.MonoProfilerConfig.Extensions.Count:n0}):\n" +
		              $"{Community.Runtime.MonoProfilerConfig.Extensions.Select(x => $"- {x}").ToString("\n")}\n" +
		              $"Use wildcard (*) to include all.");
	}

	[ConsoleCommand("profiler.track", "Adds an object to be tracked. Reloading the plugin will start tracking. Restarting required for assemblies, modules and extensions.")]
	[AuthLevel(2)]
	private void ProfilerTrackPlugin(ConsoleSystem.Arg arg)
	{
		if (!arg.HasArgs(2))
		{
			InvalidReturn(arg);
			return;
		}

		var type = arg.GetString(0);
		var value = arg.GetString(1);
		MonoProfilerConfig.ProfileTypes returnType = default;

		var returnVal = type switch
		{
			"assembly" => Community.Runtime.MonoProfilerConfig.AppendProfile(
				returnType = MonoProfilerConfig.ProfileTypes.Assembly, value),
			"plugin" => Community.Runtime.MonoProfilerConfig.AppendProfile(
				returnType = MonoProfilerConfig.ProfileTypes.Plugin, value),
			"module" => Community.Runtime.MonoProfilerConfig.AppendProfile(
				returnType = MonoProfilerConfig.ProfileTypes.Module, value),
			"ext" => Community.Runtime.MonoProfilerConfig.AppendProfile(
				returnType = MonoProfilerConfig.ProfileTypes.Extension, value),
			_ => InvalidReturn(arg)
		};

		arg.ReplyWith(returnVal
			? $" Added {returnType} object '{value}' to tracking"
			: $" Couldn't add {returnType} object '{value}' for tracking");

		if (returnVal) Community.Runtime.SaveMonoProfilerConfig();

		static bool InvalidReturn(ConsoleSystem.Arg arg)
		{
			arg.ReplyWith("Syntax: c.profiler.track (assembly|plugin|module|ext) value");
			return false;
		}
	}

	[ConsoleCommand("profiler.untrack", "Removes a plugin from being tracked. Reloading the plugin will remove it from being tracked. Restarting required for assemblies, modules and extensions.")]
	[AuthLevel(2)]
	private void ProfilerRemovePlugin(ConsoleSystem.Arg arg)
	{
		if (!arg.HasArgs(2))
		{
			InvalidReturn(arg);
			return;
		}

		var type = arg.GetString(0);
		var value = arg.GetString(1);
		MonoProfilerConfig.ProfileTypes returnType = default;

		var returnVal = type switch
		{
			"assembly" => Community.Runtime.MonoProfilerConfig.RemoveProfile(
				returnType = MonoProfilerConfig.ProfileTypes.Assembly, value),
			"plugin" => Community.Runtime.MonoProfilerConfig.RemoveProfile(
				returnType = MonoProfilerConfig.ProfileTypes.Plugin, value),
			"module" => Community.Runtime.MonoProfilerConfig.RemoveProfile(
				returnType = MonoProfilerConfig.ProfileTypes.Module, value),
			"ext" => Community.Runtime.MonoProfilerConfig.RemoveProfile(
				returnType = MonoProfilerConfig.ProfileTypes.Extension, value),
			_ => InvalidReturn(arg)
		};

		arg.ReplyWith(returnVal
			? $" Removed {returnType} object '{value}' from tracking"
			: $" Couldn't remove {returnType} object '{value}' for tracking");

		if (returnVal) Community.Runtime.SaveMonoProfilerConfig();

		static bool InvalidReturn(ConsoleSystem.Arg arg)
		{
			arg.ReplyWith("Syntax: c.profiler.untrack (assembly|plugin|module|ext) value");
			return false;
		}
	}
}
