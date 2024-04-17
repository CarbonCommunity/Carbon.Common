using API.Assembly;
using API.Commands;
using Carbon.Base.Interfaces;
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
	[ConsoleCommand("profiler.toggle", "Toggles recording status of the Carbon native Mono-profiling.")]
	[AuthLevel(2)]
	private void ProfilerToggle(ConsoleSystem.Arg arg)
	{
		if (MonoProfiler.ToggleProfiler().GetValueOrDefault())
		{
			return;
		}

		arg.ReplyWith(
			$"Basic:\n{(MonoProfiler.BasicRecords.AnyValidRecords ? MonoProfiler.BasicRecords.ToTable() : "No valid records")}\n\n" +
			$"Advanced:\n{(MonoProfiler.AdvancedRecords.AnyValidRecords ? MonoProfiler.AdvancedRecords.ToTable() : "No valid records")}");
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
			$"Advanced:\n{(MonoProfiler.AdvancedRecords.AnyValidRecords ? MonoProfiler.AdvancedRecords.ToTable() : "No valid records")}");
	}
}
