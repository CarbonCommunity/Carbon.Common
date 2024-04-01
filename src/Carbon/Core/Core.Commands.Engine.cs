using Facepunch;
using Newtonsoft.Json;

/*
 *
 * Copyright (c) 2022-2023 Carbon Community
 * All rights reserved.
 *
 */

namespace Carbon.Core;

public partial class CorePlugin : CarbonPlugin
{
	[ConsoleCommand("shutdown", "Completely unloads Carbon from the game, rendering it fully vanilla. WARNING: This is for testing purposes only.")]
	[AuthLevel(2)]
	private void Shutdown(ConsoleSystem.Arg arg)
	{
		Community.Runtime.Uninitialize();
	}

	[ConsoleCommand("help", "Returns a brief introduction to Carbon.")]
	[AuthLevel(2)]
	private void Help(ConsoleSystem.Arg arg)
	{
		arg.ReplyWith($"To get started, run the `c.find c.` or `c.find carbon` to list all Carbon commands.\n" +
			$"To list all currently loaded plugins, execute `c.plugins`.\n" +
			$"For more information, please visit https://docs.carbonmod.gg or join the Discord server at https://discord.gg/carbonmod\n" +
			$"You're currently running {Community.Runtime.Analytics.InformationalVersion}.");
	}

	[ConsoleCommand("commit", "Information about the Git commit of this build.")]
	[AuthLevel(2)]
	private void Commit(ConsoleSystem.Arg arg)
	{
		var builder = PoolEx.GetStringBuilder();

		var added = Build.Git.Changes.Count(x => x.Type == Build.Git.AssetChange.ChangeTypes.Added);
		var modified = Build.Git.Changes.Count(x => x.Type == Build.Git.AssetChange.ChangeTypes.Modified);
		var deleted = Build.Git.Changes.Count(x => x.Type == Build.Git.AssetChange.ChangeTypes.Deleted);

		builder.AppendLine($"Branch:    {Build.Git.Branch}");
		builder.AppendLine($"Author:    {Build.Git.Author}");
		builder.AppendLine($"Comment:   {Build.Git.Comment}");
		builder.AppendLine($"Date:      {Build.Git.Date}");
		builder.AppendLine($"Tag:       {Build.Git.Tag}");
		builder.AppendLine($"Hash:      {Build.Git.HashShort} ({Build.Git.HashLong})");
		builder.AppendLine($"Url:       {Build.Git.Url}");
		builder.AppendLine($"Is Debug:  {Build.IsDebug}");
		builder.AppendLine($"Changes:   {added} added, {modified} modified, {deleted} deleted");

		arg.ReplyWith(builder.ToString());
		PoolEx.FreeStringBuilder(ref builder);
	}

	[ConsoleCommand("plugins", "Prints the list of mods and their loaded plugins. Eg. c.plugins [-j|--j|-json|-abc|--json|-t|-m|-f|-ls] [-asc]")]
	[AuthLevel(2)]
	private void Plugins(ConsoleSystem.Arg arg)
	{
		if (!arg.IsPlayerCalledOrAdmin()) return;

		var mode = arg.GetString(0);
		var flip = arg.GetString(0).Equals("-asc") || arg.GetString(1).Equals("-asc");

		switch (mode)
		{
			case "-j":
			case "--j":
			case "-json":
			case "--json":
				arg.ReplyWith(ModLoader.LoadedPackages);
				break;

			default:
			{
				using var body = new StringTable("#", "Package", "Author", "Version", "Hook Time", "Hook Fires", "Hook Memory", "Hook Lag", "Compile Time", "Uptime");
				var count = 1;

				foreach (var mod in ModLoader.LoadedPackages)
				{
					body.AddRow($"{count:n0}",
						$"{mod.Name}{(mod.Plugins.Count >= 1 ? $" ({mod.Plugins.Count:n0})" : string.Empty)}",
						string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
						string.Empty, string.Empty);

					IEnumerable<RustPlugin> array = mode switch
					{
						"-abc" => mod.Plugins.OrderBy(x => x.Name),
						"-t" => (flip
							? mod.Plugins.OrderBy(x => x.TotalHookTime)
							: mod.Plugins.OrderByDescending(x => x.TotalHookTime)),
						"-m" => (flip
							? mod.Plugins.OrderBy(x => x.TotalMemoryUsed)
							: mod.Plugins.OrderByDescending(x => x.TotalMemoryUsed)),
						"-f" => (flip
							? mod.Plugins.OrderBy(x => x.CurrentHookFires)
							: mod.Plugins.OrderByDescending(x => x.CurrentHookFires)),
						"-ls" => (flip
							? mod.Plugins.OrderBy(x => x.CurrentLagSpikes)
							: mod.Plugins.OrderByDescending(x => x.CurrentLagSpikes)),
						_ => (flip ? mod.Plugins.AsEnumerable().Reverse() : mod.Plugins.AsEnumerable())
					};

					foreach (var plugin in array)
					{
						var hookTimeAverageValue =
#if DEBUG
							(float)plugin.HookTimeAverage.CalculateAverage();
#else
								0;
#endif
						var memoryAverageValue =
#if DEBUG
							(float)plugin.MemoryAverage.CalculateAverage();
#else
								0;
#endif
						var hookTimeAverage = Mathf.RoundToInt(hookTimeAverageValue) == 0
							? string.Empty
							: $" (avg {hookTimeAverageValue:0}ms)";
						var memoryAverage = Mathf.RoundToInt(memoryAverageValue) == 0
							? string.Empty
							: $" (avg {ByteEx.Format(memoryAverageValue, shortName: true, stringFormat: "{0}{1}").ToLower()})";
						body.AddRow(string.Empty, plugin.Name, plugin.Author, $"v{plugin.Version}",
							plugin.TotalHookTime.TotalMilliseconds == 0 ? string.Empty : $"{plugin.TotalHookTime.TotalMilliseconds:0}ms{hookTimeAverage}",
							plugin.CurrentHookFires == 0 ? string.Empty : $"{plugin.CurrentHookFires}",
							plugin.TotalMemoryUsed == 0 ? string.Empty : $"{ByteEx.Format(plugin.TotalMemoryUsed, shortName: true, stringFormat: "{0}{1}").ToLower()}{memoryAverage}",
							plugin.CurrentLagSpikes == 0 ? string.Empty : $"{plugin.CurrentLagSpikes}",
							plugin.IsPrecompiled
								? string.Empty
								: $"{plugin.CompileTime.TotalMilliseconds:0}ms [{plugin.InternalCallHookGenTime.TotalMilliseconds:0}ms]",
							$"{TimeEx.Format(plugin.Uptime)}");
					}

					count++;
				}

				using var unloaded = new StringTable("*", $"Unloaded Plugins ({Community.Runtime.ScriptProcessor.IgnoreList.Count})");

				foreach (var unloadedPlugin in Community.Runtime.ScriptProcessor.IgnoreList)
				{
					unloaded.AddRow(string.Empty, Path.GetFileName(unloadedPlugin));
				}

				using var failed = new StringTable("*", $"Failed Plugins ({ModLoader.FailedCompilations.Count})", "Line", "Column", "Stacktrace");

				foreach (var compilation in ModLoader.FailedCompilations)
				{
					var firstError = compilation.Errors[0];

					SplitMessageUp(true, failed, compilation, firstError, 0);

					foreach (var error in compilation.Errors.Skip(1))
					{
						SplitMessageUp(true, failed, compilation, error, 0);
					}

					static void SplitMessageUp(bool initial, StringTable table, ModLoader.FailedCompilation compilation, ModLoader.Trace trace, int skip)
					{
						const int size = 150;

						var isAboveSize = (trace.Message.Length - skip) > size;

						table.AddRow(
							string.Empty,
							initial ? Path.GetFileName(compilation.File) : string.Empty,
							isAboveSize || initial ? $"{trace.Line}" : string.Empty,
							isAboveSize || initial ? $"{trace.Column}" : string.Empty,
							$"{trace.Message.Substring(skip, size.Clamp(0, trace.Message.Length - skip))}{(isAboveSize ? "..." : string.Empty)}");

						if (isAboveSize)
						{
							SplitMessageUp(false, table, compilation, trace, skip + size);
						}
					}
				}

				arg.ReplyWith($"{body.Write(StringTable.FormatTypes.None)}\n{unloaded.Write(StringTable.FormatTypes.None)}\n{failed.Write(StringTable.FormatTypes.None)}");
				break;
			}
		}
	}

	[ConsoleCommand("fetchhooks", "It looks up for the latest available hooks for your current protocol, downloads them, then patches them accordingly at runtime.")]
	[AuthLevel(2)]
	private void FetchHooks(ConsoleSystem.Arg arg)
	{
		Community.Runtime.HookManager.Fetch();
	}
}
