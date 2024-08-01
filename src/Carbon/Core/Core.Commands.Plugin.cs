﻿/*
 *
 * Copyright (c) 2022-2023 Carbon Community
 * All rights reserved.
 *
 */

using System.Text;
using API.Commands;

namespace Carbon.Core;

public partial class CorePlugin : CarbonPlugin
{
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
				arg.ReplyWith(new
				{
					Plugins = ModLoader.Packages,
					Unloaded = Community.Runtime.ScriptProcessor.IgnoreList,
					Failed = ModLoader.FailedCompilations.Values.Where(x => x.IsValid())
				});
				break;

			default:
				{
					using var body = new StringTable("#", "Package", "Author", "Version", "Hook Time", "Hook Fires", "Hook Memory", "Hook Lag", "Compile Time", "Uptime");
					var count = 1;

					foreach (var mod in ModLoader.Packages)
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
								? mod.Plugins.OrderBy(x => x.TotalHookFires)
								: mod.Plugins.OrderByDescending(x => x.TotalHookFires)),
							"-ls" => (flip
								? mod.Plugins.OrderBy(x => x.TotalHookLagSpikes)
								: mod.Plugins.OrderByDescending(x => x.TotalHookLagSpikes)),
							_ => (flip ? mod.Plugins.AsEnumerable().Reverse() : mod.Plugins.AsEnumerable())
						};

						foreach (var plugin in array)
						{
							body.AddRow(string.Empty, plugin.Title, plugin.Author, $"v{plugin.Version}",
								plugin.TotalHookTime.TotalMilliseconds == 0 ? string.Empty : $"{plugin.TotalHookTime.TotalMilliseconds:0}ms",
								plugin.TotalHookFires == 0 ? string.Empty : $"{plugin.TotalHookFires:n0}",
								plugin.TotalMemoryUsed == 0 ? string.Empty : $"{ByteEx.Format(plugin.TotalMemoryUsed, shortName: true, stringFormat: "{0}{1}").ToLower()}",
								plugin.TotalHookLagSpikes == 0 ? string.Empty : $"{plugin.TotalHookLagSpikes:n0}",
								plugin.IsPrecompiled
									? string.Empty
									: $"{plugin.CompileTime.TotalMilliseconds:0}ms [{plugin.InternalCallHookGenTime.TotalMilliseconds:0}ms]",
								$"{TimeEx.Format(plugin.Uptime)}");
						}

						count++;
					}

					using var unloaded = new StringTable("*", $"Unloaded Plugins ({Community.Runtime.ScriptProcessor.IgnoreList.Count:n0})");

					foreach (var unloadedPlugin in Community.Runtime.ScriptProcessor.IgnoreList)
					{
						unloaded.AddRow(string.Empty, Path.GetFileName(unloadedPlugin));
					}

					using var failed = new StringTable("*", $"Failed Plugins ({ModLoader.FailedCompilations.Count(x => x.Value.IsValid()):n0})", "Line", "Stacktrace");

					foreach (var compilation in ModLoader.FailedCompilations.Values)
					{
						if (!compilation.IsValid())
						{
							continue;
						}

						var firstError = compilation.Errors[0];

						SplitMessageUp(true, failed, compilation, firstError, 0);

						foreach (var error in compilation.Errors.Skip(1))
						{
							SplitMessageUp(true, failed, compilation, error, 0);
						}

						static void SplitMessageUp(bool initial, StringTable table, ModLoader.CompilationResult compilation, ModLoader.Trace trace, int skip)
						{
							const int size = 150;

							var isAboveSize = (trace.Message.Length - skip) > size;

							table.AddRow(
								string.Empty,
								initial ? Path.GetFileName(compilation.File) : string.Empty,
								isAboveSize || initial ? $"{trace.Line}:{trace.Column}" : string.Empty,
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

	[ConsoleCommand("reload", "Reloads all or specific mods / plugins. E.g 'c.reload * <except[]>'' to reload everything.")]
	[AuthLevel(2)]
	private void Reload(ConsoleSystem.Arg arg)
	{
		if (!arg.HasArgs(1)) return;

		RefreshOrderedFiles();

		var name = arg.GetString(0);
		switch (name)
		{
			case "*":
				Community.Runtime.ReloadPlugins(arg.Args.Skip(1));
				break;

			default:
				var path = GetPluginPath(name);

				if (!string.IsNullOrEmpty(path.Value))
				{
					Community.Runtime.ScriptProcessor.ClearIgnore(path.Value);

					if (Community.Runtime.ScriptProcessor.InstanceBuffer.TryGetValue(path.Key, out IScriptProcessor.IProcess instance))
					{
						instance.Clear();
					}

					Community.Runtime.ScriptProcessor.Prepare(path.Key, path.Value);
					return;
				}

				var pluginFound = false;
				var pluginPrecompiled = false;

				foreach (var mod in ModLoader.Packages)
				{
					var plugins = Facepunch.Pool.GetList<RustPlugin>();
					plugins.AddRange(mod.Plugins);

					foreach (var plugin in plugins)
					{
						if (plugin.IsPrecompiled) continue;

						if (plugin.Name == name)
						{
							pluginFound = true;

							if (plugin.IsPrecompiled)
							{
								pluginPrecompiled = true;
							}
							else
							{
								plugin.ProcessorProcess.Clear();
								plugin.ProcessorProcess.Dispose();
								plugin.ProcessorProcess.Execute(plugin.Processor);
								mod.Plugins.Remove(plugin);
							}
						}
					}

					Facepunch.Pool.FreeList(ref plugins);
				}

				if (!pluginFound)
				{
					Logger.Warn($"Plugin {name} was not found or was typed incorrectly.");
				}
				else if (pluginPrecompiled)
				{
					Logger.Warn($"Plugin {name} is a precompiled plugin which can only be reloaded programmatically.");
				}
				break;
		}
	}

	[ConsoleCommand("load", "Loads all mods and/or plugins. E.g 'c.load * <except[]>'' to load everything you've unloaded.")]
	[AuthLevel(2)]
	private void LoadPlugin(ConsoleSystem.Arg arg)
	{
		if (!arg.HasArgs(1))
		{
			Logger.Warn("You must provide the name of a plugin or use * to load all plugins.");
			return;
		}

		RefreshOrderedFiles();

		var name = arg.GetString(0);
		switch (name)
		{
			case "*":
				//
				// Scripts
				//
				{
					var except = arg.Args.Skip(1);

					Community.Runtime.ScriptProcessor.IgnoreList.RemoveAll(x => !except.Any() || except.Any(x.Contains));

					foreach (var plugin in OrderedFiles)
					{
						if (except.Any(plugin.Value.Contains) || Community.Runtime.ScriptProcessor.InstanceBuffer.ContainsKey(plugin.Key))
						{
							continue;
						}

						if (!Community.Runtime.ScriptProcessor.Exists(plugin.Value))
						{
							Community.Runtime.ScriptProcessor.Prepare(plugin.Key, plugin.Value);
						}
					}
					break;
				}

			default:
				{
					var path = GetPluginPath(name);
					if (!string.IsNullOrEmpty(path.Value))
					{
						Community.Runtime.ScriptProcessor.ClearIgnore(path.Value);
						Community.Runtime.ScriptProcessor.Prepare(path.Key, path.Value);
						return;
					}

					Logger.Warn($"Plugin {name} was not found or was typed incorrectly.");
					break;
				}
		}
	}

	[ConsoleCommand("unload", "Unloads all mods and/or plugins. E.g 'c.unload * <except[]>' to unload everything. They'll be marked as 'ignored'.")]
	[AuthLevel(2)]
	private void UnloadPlugin(ConsoleSystem.Arg arg)
	{
		if (!arg.HasArgs(1))
		{
			Logger.Warn("You must provide the name of a plugin or use * to unload all plugins.");
			return;
		}

		RefreshOrderedFiles();

		var name = arg.GetString(0);
		switch (name)
		{
			case "*":
				var except = arg.Args.Skip(1);

				//
				// Scripts
				//
				{
					var tempList = Facepunch.Pool.GetList<string>();

					foreach (var bufferInstance in Community.Runtime.ScriptProcessor.InstanceBuffer)
					{
						tempList.Add(bufferInstance.Value.File);
					}

					Community.Runtime.ScriptProcessor.IgnoreList.RemoveAll(x => !except.Any() || (except.Any() && !except.Any(x.Contains)));
					Community.Runtime.ScriptProcessor.Clear(except);

					foreach (var plugin in tempList)
					{
						if (except.Any(plugin.Contains))
						{
							continue;
						}

						Community.Runtime.ScriptProcessor.Ignore(plugin);
					}
				}
				break;
			default:
				{
					var path = GetPluginPath(name);
					if (!string.IsNullOrEmpty(path.Value))
					{
						Community.Runtime.ScriptProcessor.Ignore(path.Value);
					}

					var pluginFound = false;
					var pluginPrecompiled = false;

					foreach (var mod in ModLoader.Packages)
					{
						var plugins = Facepunch.Pool.GetList<RustPlugin>();
						plugins.AddRange(mod.Plugins);

						foreach (var plugin in plugins)
						{
							if (plugin.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
							{
								pluginFound = true;

								if (plugin.IsPrecompiled)
								{
									pluginPrecompiled = true;
								}
								else
								{
									plugin.ProcessorProcess?.Clear();
									plugin.ProcessorProcess?.Dispose();
									mod.Plugins.Remove(plugin);
								}
							}
						}

						Facepunch.Pool.FreeList(ref plugins);
					}

					if (!pluginFound)
					{
						if (string.IsNullOrEmpty(path.Value)) Logger.Warn($"Plugin {name} was not found or was typed incorrectly.");
						else Logger.Warn($"Plugin {name} was not loaded but was marked as ignored.");
					}
					else if (pluginPrecompiled)
					{
						Logger.Warn($"Plugin {name} is a precompiled plugin which can only be unloaded programmatically.");
					}
					break;
				}
		}
	}

	[ConsoleCommand("plugininfo", "Prints advanced information about a currently loaded plugin. From hooks, hook times, hook memory usage and other things.")]
	[AuthLevel(2)]
	private void PluginInfo(ConsoleSystem.Arg arg)
	{
		if (!arg.HasArgs(1))
		{
			Logger.Warn("You must provide the name of a plugin to print plugin advanced information.");
			return;
		}

		var name = arg.GetString(0).ToLower();
		var mode = arg.GetString(1);
		var flip = arg.GetString(2).Equals("-asc");
		var plugin = ModLoader.Packages.SelectMany(x => x.Plugins).FirstOrDefault(x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase) || x.Name.Contains(name, CompareOptions.OrdinalIgnoreCase));
		var count = 1;

		if (plugin == null)
		{
			arg.ReplyWith("Couldn't find that plugin.");
			return;
		}

		using (var table = new StringTable(string.Empty, "Id", "Hook", "Time", "Fires", "Memory", "Lag", "Subscribed", "Async & Overrides"))
		{
			IEnumerable<List<CachedHook>> array = mode switch
			{
				"-t" => (flip ? plugin.HookPool.OrderBy(x => x.Value.Hooks.Sum(x => x.HookTime.TotalMilliseconds)) : plugin.HookPool.OrderByDescending(x => x.Value.Hooks.Sum(x => x.HookTime.TotalMilliseconds))).Select(x => x.Value.Hooks),
				"-m" => (flip ? plugin.HookPool.OrderBy(x => x.Value.Hooks.Sum(x => x.MemoryUsage)) : plugin.HookPool.OrderByDescending(x => x.Value.Hooks.Sum(x => x.MemoryUsage))).Select(x => x.Value.Hooks),
				"-f" => (flip ? plugin.HookPool.OrderBy(x => x.Value.Hooks.Sum(x => x.TimesFired)) : plugin.HookPool.OrderByDescending(x => x.Value.Hooks.Sum(x => x.TimesFired))).Select(x => x.Value.Hooks),
				"-ls" => (flip ? plugin.HookPool.OrderBy(x => x.Value.Hooks.Sum(x => x.LagSpikes)) : plugin.HookPool.OrderByDescending(x => x.Value.Hooks.Sum(x => x.LagSpikes))).Select(x => x.Value.Hooks),
				_ => plugin.HookPool.Select(x => x.Value.Hooks)
			};

			foreach (var hook in array)
			{
				if (hook.Count == 0)
				{
					continue;
				}

				var current = hook[0];
				var hookName = current.Method.Name;

				var hookId = HookStringPool.GetOrAdd(hookName);

				if (!plugin.Hooks.Contains(hookId))
				{
					continue;
				}

				var hookTime = hook.Sum(x => x.HookTime.TotalMilliseconds);
				var hookMemoryUsage = hook.Sum(x => x.MemoryUsage);
				var hookCount = hook.Count;
				var hookAsyncCount = hook.Count(x => x.IsAsync);
				var hookTimesFired = hook.Sum(x => x.TimesFired);
				var hookLagSpikes = hook.Sum(x => x.LagSpikes);

				table.AddRow(string.Empty,
					hookId,
					$"{hookName}",
					hookTime == 0 ? string.Empty : $"{hookTime:0}ms",
					hookTimesFired == 0 ? string.Empty : $"{hookTimesFired:n0}",
					hookMemoryUsage == 0 ? string.Empty : $"{ByteEx.Format(hookMemoryUsage, shortName: true).ToLower()}",
					hookLagSpikes == 0 ? string.Empty : $"{hookLagSpikes:n0}",
					!plugin.IgnoredHooks.Contains(hookId) ? "*" : string.Empty,
					$"{hookAsyncCount:n0} / {hookCount:n0}");

				count++;
			}

			var builder = new StringBuilder();

			builder.AppendLine($"{plugin.Name} v{plugin.Version} by {plugin.Author}{(plugin.IsCorePlugin ? $" [core]" : string.Empty)}");
			builder.AppendLine($"  Path:                   {plugin.FilePath}");
			builder.AppendLine($"  Compile Time:           {plugin.CompileTime.TotalMilliseconds:0}ms{(plugin.IsPrecompiled ? " [precompiled]" : string.Empty)}{(plugin.IsExtension ? " [ext]" : string.Empty)}");
			builder.AppendLine($"  Int.CallHook Gen Time:  {plugin.InternalCallHookGenTime.TotalMilliseconds:0}ms{(plugin.IsPrecompiled ? " [precompiled]" : string.Empty)}{(plugin.IsExtension ? " [ext]" : string.Empty)}");
			builder.AppendLine($"  Uptime:                 {TimeEx.Format(plugin.Uptime, true).ToLower()}");
			builder.AppendLine($"  Total Hook Time:        {plugin.TotalHookTime.TotalMilliseconds:0}ms");
			builder.AppendLine($"  Total Memory Used:      {ByteEx.Format(plugin.TotalMemoryUsed, shortName: true).ToLower()}");
			builder.AppendLine($"  Internal Hook Override: {plugin.InternalCallHookOverriden}");
			builder.AppendLine($"  Has Conditionals:       {plugin.HasConditionals}");
			builder.AppendLine($"  Mod Package:            {plugin.Package.Name} ({plugin.Package.PluginCount}){((plugin.Package.IsCoreMod) ? $" [core]" : string.Empty)}");
			builder.AppendLine($"  Processor:              {(plugin.Processor == null ? "[standalone]" : $"{plugin.Processor.Name} [{plugin.Processor.Extension}]")}");

			if (plugin is CarbonPlugin carbonPlugin)
			{
				builder.AppendLine($"  Carbon CUI:             {carbonPlugin.CuiHandler.Pooled:n0} pooled, {carbonPlugin.CuiHandler.Used:n0} used");
			}

			var permissions = plugin.permission.GetPermissions(plugin);
			builder.AppendLine($"  Permissions:            {(permissions.Length > 0 ? permissions.ToString("\n                          ") : "N/A")}");

			builder.AppendLine(string.Empty);

			if (count != 1)
			{
				builder.AppendLine(table.ToStringMinimal());
			}

			arg.ReplyWith(builder.ToString());
		}
	}

	[ConsoleCommand("plugincmds", "Prints a full list of chat and console commands for a specific plugin.")]
	[AuthLevel(2)]
	private void PluginCmds(ConsoleSystem.Arg arg)
	{
		if (!arg.HasArgs(1))
		{
			Logger.Warn("You must provide the name of a plugin to print plugin command information.");
			return;
		}

		var name = arg.GetString(0).ToLower();
		var plugin = ModLoader.Packages.SelectMany(x => x.Plugins).FirstOrDefault(x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase) || x.Name.Contains(name, CompareOptions.OrdinalIgnoreCase));

		if (plugin == null)
		{
			arg.ReplyWith("Couldn't find that plugin.");
			return;
		}

		var builder = PoolEx.GetStringBuilder();
		var count = 1;

		using (var table = new StringTable("Chat Commands"))
		{
			foreach (var command in Community.Runtime.CommandManager.Chat.Where(x => x.Reference == plugin).Distinct())
			{
				if (command.HasFlag(CommandFlags.Protected) || command.HasFlag(CommandFlags.Hidden))
				{
					continue;
				}

				table.AddRow(command.Name);

				count++;
			}

			builder.AppendLine(table.ToStringMinimal());
		}

		using (var table = new StringTable("Console Commands"))
		{
			count = 1;
			foreach (var command in Community.Runtime.CommandManager.ClientConsole.Where(x => x.Reference == plugin))
			{
				if (command.HasFlag(CommandFlags.Protected) || command.HasFlag(CommandFlags.Hidden))
				{
					continue;
				}

				table.AddRow(command.Name);

				count++;
			}

			builder.AppendLine(table.ToStringMinimal());
		}

		arg.ReplyWith(builder.ToString());
		PoolEx.FreeStringBuilder(ref builder);
	}

	[ConsoleCommand("reloadconfig", "Reloads a plugin's config file. This might have unexpected results, use cautiously.")]
	[AuthLevel(2)]
	private void ReloadConfig(ConsoleSystem.Arg arg)
	{
		if (!arg.HasArgs(1))
		{
			Logger.Warn("You must provide the name of a plugin or use * to reload all plugin configs.");
			return;
		}

		RefreshOrderedFiles();

		var name = arg.GetString(0);
		switch (name)
		{
			case "*":
				{

					foreach (var package in ModLoader.Packages)
					{
						foreach (var plugin in package.Plugins)
						{
							plugin.ILoadConfig();
							plugin.Load();
							plugin.Puts($"Reloaded plugin's config.");
						}
					}

					break;
				}

			default:
				{
					var pluginFound = false;

					foreach (var mod in ModLoader.Packages)
					{
						var plugins = Facepunch.Pool.GetList<RustPlugin>();
						plugins.AddRange(mod.Plugins);

						foreach (var plugin in plugins)
						{
							if (plugin.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
							{
								plugin.ILoadConfig();
								plugin.Load();
								plugin.Puts($"Reloaded plugin's config.");
								pluginFound = true;
							}
						}

						Facepunch.Pool.FreeList(ref plugins);
					}

					if (!pluginFound)
					{
						Logger.Warn($"Plugin {name} was not found or was typed incorrectly.");
					}
					break;
				}
		}
	}

#if DEBUG
	[Conditional("DEBUG")]
	[ConsoleCommand("pluginintgen", "Generates the internal hook call override in 'carbon/plugins/debug'.")]
	[AuthLevel(2)]
	private void GenerateInternal(ConsoleSystem.Arg arg)
	{
		var plugin = ModLoader.FindPlugin(arg.GetString(0));

		if (plugin == null)
		{
			arg.ReplyWith($"Couldn't find plugin");
			return;
		}

		if (string.IsNullOrEmpty(plugin.InternalCallHookSource))
		{
			arg.ReplyWith($"No Internal CallHook override source for '{plugin.ToPrettyString()}'");
			return;
		}

		var path = Path.Combine(Defines.GetScriptDebugFolder(), $"{Path.GetFileNameWithoutExtension(plugin.FileName)}.Internal.cs");
		OsEx.File.Create(path, plugin.InternalCallHookSource);
		arg.ReplyWith($"Saved at '{path}'");
	}
#endif

	[ConsoleCommand("uninstallplugin", "Unloads and uninstalls (moves the file to the backup folder) the plugin with the name.")]
	[AuthLevel(2)]
	private void UninstallPlugin(ConsoleSystem.Arg arg)
	{
		if (!arg.HasArgs(1))
		{
			Logger.Warn("You must provide the name of a plugin to uninstall it.");
			return;
		}

		RefreshOrderedFiles();

		var name = arg.GetString(0);
		switch (name)
		{
			default:
				{
					var path = GetPluginPath(name);

					var pluginFound = false;
					var pluginPrecompiled = false;

					foreach (var mod in ModLoader.Packages)
					{
						var plugins = Facepunch.Pool.GetList<RustPlugin>();
						plugins.AddRange(mod.Plugins);

						foreach (var plugin in plugins.Where(plugin => plugin.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase)))
						{
							pluginFound = true;

							if (plugin.IsPrecompiled)
							{
								pluginPrecompiled = true;
							}
						}

						Facepunch.Pool.FreeList(ref plugins);
					}

					if (!pluginFound)
					{
						if (string.IsNullOrEmpty(path.Value)) Logger.Warn($"Plugin {name} was not found or was typed incorrectly.");
						else Logger.Warn($"Plugin {name} was not loaded but was marked as ignored.");

						return;
					}

					if (pluginPrecompiled)
					{
						Logger.Warn($"Plugin {path.Key} is a precompiled plugin which can only be unloaded/uninstalled programmatically.");
						return;
					}

					OsEx.File.Move(path.Value, Path.Combine(Defines.GetScriptBackupFolder(), Path.GetFileName(path.Value)));
					break;
				}
		}
	}

	[ConsoleCommand("installplugin", "Looks up the backups directory and moves the plugin back in the plugins folder installing it with the name.")]
	[AuthLevel(2)]
	private void InstallPlugin(ConsoleSystem.Arg arg)
	{
		if (!arg.HasArgs(1))
		{
			Logger.Warn("You must provide the name of a plugin to uninstall it.");
			return;
		}

		RefreshOrderedFiles();

		var name = arg.GetString(0);
		switch (name)
		{
			default:
				{
					var path = Path.Combine(Defines.GetScriptBackupFolder(), $"{name}.cs");

					if (!OsEx.File.Exists(path))
					{
						Logger.Warn($"Plugin {name} was not found or was typed incorrectly.");
						return;
					}

					OsEx.File.Move(path, Path.Combine(Defines.GetScriptsFolder(), Path.GetFileName(path)));
					break;
				}
		}
	}
}
