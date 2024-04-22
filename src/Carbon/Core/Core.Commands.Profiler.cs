using Carbon.Profiler;
using Timer = Oxide.Plugins.Timer;

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
	internal Timer _profileTimer;
	internal Timer _profileWarningTimer;

	[CommandVar("profilestatus", "Mono profiling status.")]
	[AuthLevel(2)]
	private bool IsProfiling
	{
		get { return MonoProfiler.Recording; }
		set
		{
		}
	}

	[ConsoleCommand("profile", "Toggles recording status of the Carbon native Mono-profiling. Syntax: c.profile [duration]")]
	[AuthLevel(2)]
	private void Profile(ConsoleSystem.Arg arg)
	{
		if (!MonoProfiler.Enabled)
		{
			arg.ReplyWith("Mono profiler is disabled. Run `c.profiler true` to enable it. Must restart the server for changes to apply.");
			return;
		}

		var duration = arg.GetFloat(0);

		_profileTimer?.Destroy();
		_profileTimer = null;
		_profileWarningTimer?.Destroy();
		_profileWarningTimer = null;

		if (!MonoProfiler.ToggleProfiling(true).GetValueOrDefault())
		{
			PrintWarn();
		}

		if (duration >= 1f && MonoProfiler.Recording)
		{
			Logger.Warn($"[Profiler] Profiling duration {TimeEx.Format(duration).ToLower()}..");

			_profileTimer = Community.Runtime.CorePlugin.timer.In(duration, () =>
			{
				if (!MonoProfiler.Recording)
				{
					return;
				}

				MonoProfiler.ToggleProfiling(true).GetValueOrDefault();
				PrintWarn();
			});
		}
		else if(MonoProfiler.Recording)
		{
			_profileWarningTimer = Community.Runtime.CorePlugin.timer.Every(60, () =>
			{
				Logger.Warn($" Reminder: You've been profile recording for {TimeEx.Format(MonoProfiler.CurrentDurationTime.TotalSeconds).ToLower()}..");
			});
		}

		static void PrintWarn()
		{
			using var table = new StringTable("Duration", "Processing", "Basic", "Advanced");

			table.AddRow(
				TimeEx.Format(MonoProfiler.DurationTime.TotalSeconds).ToLower(),
				$"{MonoProfiler.DataProcessingTime.TotalMilliseconds:0}ms",
				MonoProfiler.BasicRecords.Count.ToString("n0"),
				MonoProfiler.AdvancedRecords.Count.ToString("n0"));

			Logger.Warn(table.ToStringMinimal());
		}
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
		var output = string.Empty;

		switch (mode)
		{
			case "-c":
				output = $"{MonoProfiler.BasicRecords.ToCSV()}{(toFile ? $"\n{MonoProfiler.AdvancedRecords.ToCSV()}" : string.Empty)}";
				if (toFile) WriteFileString("csv", output); else arg.ReplyWith(output);
				break;

			case "-j":
				// patret magic
				break;

			case "-p":
				// patret magic
				break;

			default:
			case "-t":
				output = $"{MonoProfiler.BasicRecords.ToTable()}{(toFile ? $"\n\n{MonoProfiler.AdvancedRecords.ToTable()}" : string.Empty)}";
				if (toFile) WriteFileString("txt", output); else arg.ReplyWith(output);
				break;

		}

		static void WriteFileString(string extension, string data)
		{
			var date = DateTime.Now;
			var file = Path.Combine(Defines.GetRustRootFolder(), $"profile-{date.Year}_{date.Month}_{date.Day}_{date.Hour}{date.Minute}{date.Second}.{extension}");
			OsEx.File.Create(file, data);

			Logger.Warn($" Stored output at {file}");
		}
		// static void WriteFileByte(ConsoleSystem.Arg arg, string extension, byte[] data)
		// {
		// 	var date = DateTime.Now;
		// 	var file = Path.Combine(Defines.GetRustRootFolder(),
		// 		$"profile-{date.Year}_{date.Month}_{date.Day}_{date.Hour}{date.Minute}{date.Second}.{extension}");
		// 	OsEx.File.Create(file, data);

		// 	Logger.Log($"Saved at {file}");
		// }
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
