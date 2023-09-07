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
#if DEBUG

	[ConditionalSymbolAttribute("DEBUG")]
	[ConsoleCommand("beginprofile", "Starts profiling the server.")]
	[AuthLevel(2)]
	private void BeginProfile(ConsoleSystem.Arg arg)
	{
		var date = DateTime.UtcNow;
		var duration = arg.GetFloat(0, -1);
		var name = arg.GetString(1, $"carbonprofile_{date.Year}-{date.Month}-{date.Day}_{date.Hour}{date.Minute}{date.Second}");

		Profiler.Make(name).Begin(duration, onEnd: duration == -1 ? null : profiler =>
		{
			Logger.Log($"Ended profiling, writing to disk: {profiler.Path}");
		});
		arg.ReplyWith("Began profiling...");
	}

	[ConditionalSymbolAttribute("DEBUG")]
	[ConsoleCommand("endprofile", "Ends profiling the server and asynchronously writes it to disk.")]
	[AuthLevel(2)]
	private void EndProfile(ConsoleSystem.Arg arg)
	{
		var path = Profiler.Singleton.Path;
		arg.ReplyWith(Profiler.End() ? $"Ended profiling, writing to disk: {path}" : "Couldn't end profile. Most likely because there's none started.");
	}

#endif
}
