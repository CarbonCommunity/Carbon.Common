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

	[ConsoleCommand("profile", "Toggles recording status of the Carbon native Mono-profiling. Syntax: `c.profiler.toggle [-a]`, [-a] is advanced profiling.")]
	[AuthLevel(2)]
	private void ProfilerToggle(ConsoleSystem.Arg arg)
	{
		if (!ProfilerEnabled)
		{
			arg.ReplyWith("Mono profiler is disabled. Run `c.profiler true` to enable it. Must restart the server for changes to apply.");
			return;
		}

		if (MonoProfiler.ToggleProfiler(arg.GetString(0).Equals("-a")).GetValueOrDefault())
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
			$"Advanced:\n{(MonoProfiler.AdvancedRecords.Disabled ? "Advanced profiling is disabled." : MonoProfiler.AdvancedRecords.AnyValidRecords ? MonoProfiler.AdvancedRecords.ToTable() : "No valid records")}");
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
		arg.ReplyWith($"Tracked Assemblies ({Community.Runtime.MonoProfilerConfig.AssembliesToProfile.Count:n0}):\n" +
		              $"{Community.Runtime.MonoProfilerConfig.AssembliesToProfile.Select(x => $"- {x}").ToString("\n")}");
	}
}
