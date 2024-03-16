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

	[ConsoleCommand("plugins", "Prints the list of mods and their loaded plugins. Eg. c.plugins [-j|--j|-json|-abc|--json|-t|-m|-f] [-asc]")]
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
				var result = string.Empty;

				// Loaded plugins
				{
					using var body = new StringTable("#", "Mod", "Author", "Version", "Hook Time", "Hook Fires", "Memory Usage", "Compile Time", "Int.CallHook Gen Time", "Uptime");
					var count = 1;

					foreach (var mod in ModLoader.LoadedPackages.AsEnumerable())
					{
						if (mod.IsCoreMod) continue;

						body.AddRow($"{count:n0}", $"{mod.Name}{(mod.Plugins.Count >= 1 ? $" ({mod.Plugins.Count:n0})" : string.Empty)}", string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);

						IEnumerable<RustPlugin> array = mode switch
						{
							"-abc" => mod.Plugins.OrderBy(x => x.Name),
							"-t" => (flip ? mod.Plugins.OrderBy(x => x.TotalHookTime) : mod.Plugins.OrderByDescending(x => x.TotalHookTime)),
							"-m" => (flip ? mod.Plugins.OrderBy(x => x.TotalMemoryUsed) : mod.Plugins.OrderByDescending(x => x.TotalMemoryUsed)),
							"-f" => (flip ? mod.Plugins.OrderBy(x => x.CurrentHookFires) : mod.Plugins.OrderByDescending(x => x.CurrentHookFires)),
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
								$"{plugin.TotalHookTime:0}ms{hookTimeAverage}",
								$"{plugin.CurrentHookFires:n0}",
								$"{ByteEx.Format(plugin.TotalMemoryUsed, shortName: true, stringFormat: "{0}{1}").ToLower()}{memoryAverage}",
								plugin.IsPrecompiled ? string.Empty : $"{plugin.CompileTime:0}ms",
								plugin.IsPrecompiled ? string.Empty : $"{plugin.InternalCallHookGenTime:0}ms",
								$"{TimeEx.Format(plugin.Uptime)}");
						}

						count++;
					}

					result += $"{body.Write(StringTable.FormatTypes.None)}\n";
				}

				// Failed plugins
				{
					using (var body = new StringTable("#", "File", "Errors", "Stack"))
					{
						var count = 1;

						foreach (var mod in ModLoader.FailedMods)
						{
							body.AddRow($"{count:n0}", $"{Path.GetFileName(mod.File)}", $"{mod.Errors.Length:n0}", $"{mod.Errors.Select(x => x.Message).ToString(", ").Truncate(75, "...")}");

							count++;
						}

						result += $"Failed plugins:\n{body.Write(StringTable.FormatTypes.None)}\nTo list the full stack trace of failed plugins, run 'c.pluginsfailed'";
					}

					arg.ReplyWith(result);
				}
				break;
		}
	}

	[ConsoleCommand("pluginsunloaded", "Prints the list of unloaded plugins.")]
	[AuthLevel(2)]
	private void PluginsUnloaded(ConsoleSystem.Arg arg)
	{
		var mode = arg.HasArgs(1) ? arg.GetString(0) : null;

		switch (mode)
		{
			case "-j":
			case "--j":
			case "-json":
			case "--json":
				arg.ReplyWith(JsonConvert.SerializeObject(Community.Runtime.ScriptProcessor.IgnoreList, Formatting.Indented));
				break;

			default:
				using (var body = new StringTable("#", "File"))
				{
					var count = 1;

					foreach (var ignored in Community.Runtime.ScriptProcessor.IgnoreList)
					{
						body.AddRow($"{count:n0}", $"{ignored}");
						count++;
					}

					arg.ReplyWith(body.Write(StringTable.FormatTypes.None));
				}
				break;
		}
	}

	[ConsoleCommand("pluginsfailed", "Prints the list of plugins that failed to load (most likely due to compilation issues).")]
	[AuthLevel(2)]
	private void PluginsFailed(ConsoleSystem.Arg arg)
	{
		var mode = arg.HasArgs(1) ? arg.GetString(0) : null;

		switch (mode)
		{
			case "-j":
			case "--j":
			case "-json":
			case "--json":
				arg.ReplyWith(ModLoader.FailedMods);
				break;

			default:
				var result = string.Empty;
				var count = 1;
				var index = 1;

				foreach (var mod in ModLoader.FailedMods)
				{
					result += $"{count:n0}. {mod.File}\n";

					foreach (var error in mod.Errors)
					{
						result += $" {index}. {error.Message} [{error.Number}]\n" +
								  $"   ({error.Column} line {error.Line})\n";

						index++;
					}

					index = 1;
					result += "\n";
					count++;
				}

				arg.ReplyWith(result);
				break;
		}
	}

	[ConsoleCommand("pluginwarns", "Prints the list of warnings of a specific plugin (or all if no arguments are set).")]
	[AuthLevel(2)]
	private void PluginWarns(ConsoleSystem.Arg arg)
	{
		var filter = arg.GetString(0);

		if (string.IsNullOrEmpty(filter))
		{
			var r = string.Empty;

			foreach (var mod in ModLoader.LoadedPackages)
			{
				foreach (var plugin in mod.Plugins)
				{
					r += $"{Print(plugin)}\n";
				}
			}

			arg.ReplyWith(r);
		}
		else
		{
			var plugin = (Plugin)null;

			foreach (var mod in ModLoader.LoadedPackages)
			{
				foreach (var p in mod.Plugins)
				{
					if (p.Name == filter)
					{
						plugin = p;
						break;
					}
				}
			}

			if (plugin == null)
			{
				arg.ReplyWith($"Couldn't find a plugin with that name: '{filter}'");
				return;
			}

			arg.ReplyWith(Print(plugin));
		}

		static string Print(Plugin plugin)
		{
			var result = string.Empty;
			var count = 1;

			result += $"{plugin.Name} v{plugin.Version} by {plugin.Author}:\n";

			if (plugin.CompileWarnings == null || plugin.CompileWarnings.Length == 0)
			{
				result += $"  No warnings available.\n";
			}
			else
			{
				foreach (var warn in plugin.CompileWarnings)
				{
					result += $"  {count:n0}. {warn.Message} [{warn.Number}]\n     ({warn.Column} line {warn.Line})\n";
					count++;
				}
			}

			return result;
		}
	}

	[ConsoleCommand("fetchhooks", "It looks up for the latest available hooks for your current protocol, downloads them, then patches them accordingly at runtime.")]
	[AuthLevel(2)]
	private void FetchHooks(ConsoleSystem.Arg arg)
	{
		Community.Runtime.HookManager.Fetch();
	}
}
