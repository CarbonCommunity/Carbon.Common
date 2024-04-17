using API.Assembly;
using API.Commands;
using Carbon.Base.Interfaces;
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

		arg.ReplyWith(
			$"Basic:\n{(MonoProfiler.BasicRecords.AnyValidRecords ? MonoProfiler.BasicRecords.ToTable() : "No valid records")}\n\n" +
			$"Advanced:\n{(MonoProfiler.AdvancedRecords.Disabled ? "Advanced profiling is disabled. Use [-a] to enable advanced profiling." : MonoProfiler.AdvancedRecords.AnyValidRecords ? MonoProfiler.AdvancedRecords.ToTable() : "No valid records")}");
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

		arg.ReplyWith(
			$"Basic:\n{(MonoProfiler.BasicRecords.AnyValidRecords ? MonoProfiler.BasicRecords.ToTable() : "No valid records")}\n\n" +
			$"Advanced:\n{(MonoProfiler.AdvancedRecords.Disabled ? "Advanced profiling is disabled. Use [-a] to enable advanced profiling." : MonoProfiler.AdvancedRecords.AnyValidRecords ? MonoProfiler.AdvancedRecords.ToTable() : "No valid records")}");
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

	[ConsoleCommand("profiler.assemblies", "The entire list of assembly names that are used by the Mono profiler for tracking.")]
	[AuthLevel(2)]
	private void ProfilerAssemblies(ConsoleSystem.Arg arg)
	{
		arg.ReplyWith($"Tracked Assemblies ({Community.Runtime.MonoProfilerConfig.ProfiledAssemblies.Count:n0}):\n" +
		              $"{Community.Runtime.MonoProfilerConfig.ProfiledAssemblies.Select(x => $"- {x}").ToString("\n")}\nUse wildcard (*) to include all assemblies loaded.");
	}

	[ConsoleCommand("profiler.plugins", "The entire list of plugins names that are used by the Mono profiler for tracking.")]
	[AuthLevel(2)]
	private void ProfilerPlugins(ConsoleSystem.Arg arg)
	{
		arg.ReplyWith($"Tracked Plugins ({Community.Runtime.MonoProfilerConfig.ProfiledPlugins.Count:n0}):\n" +
		              $"{Community.Runtime.MonoProfilerConfig.ProfiledPlugins.Select(x => $"- {x}").ToString("\n")}\nUse wildcard (*) to include all plugins loaded or will be loaded.");
	}

	[ConsoleCommand("profiler.trackplugin", "Adds a plugin to be tracked. Reloading the plugin will start tracking.")]
	[AuthLevel(2)]
	private void ProfilerTrackPlugin(ConsoleSystem.Arg arg)
	{
		var plugin = arg.GetString(0);

		if (string.IsNullOrEmpty(plugin))
		{
			arg.ReplyWith("Input is empty");
			return;
		}

		if (Community.Runtime.MonoProfilerConfig.AddPlugin(plugin))
		{
			arg.ReplyWith($"Added '{plugin}' to the tracking list.");

			if (MonoProfiler.Enabled)
			{
				Logger.Warn(" The plugin must reload to start tracking..");
			}
		}
		else
		{
			arg.ReplyWith($"Couldn't add '{plugin}' - probably because it's already in the list.");
		}	}

	[ConsoleCommand("profiler.untrackplugin", "Removes a plugin from being tracked. Reloading the plugin will remove it from being tracked.")]
	[AuthLevel(2)]
	private void ProfilerRemovePlugin(ConsoleSystem.Arg arg)
	{
		var plugin = arg.GetString(0);

		if (string.IsNullOrEmpty(plugin))
		{
			arg.ReplyWith("Input is empty");
			return;
		}

		if (Community.Runtime.MonoProfilerConfig.RemovePlugin(plugin))
		{
			arg.ReplyWith($"Removed '{plugin}' from the tracking list.");

			if (MonoProfiler.Enabled)
			{
				Logger.Warn(" The plugin must reload to stop tracking..");
			}
		}
		else
		{
			arg.ReplyWith($"Couldn't remove '{plugin}' - probably because it's not in the list.");
		}
	}
}
