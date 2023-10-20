/*
 *
 * Copyright (c) 2022-2023 Carbon Community
 * All rights reserved.
 *
 */

using System.Text;
namespace Carbon.Core;

public partial class CorePlugin : CarbonPlugin
{
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

				if (!string.IsNullOrEmpty(path))
				{
					ApplyProcessor(Community.Runtime.ScriptProcessor);
					ApplyProcessor(Community.Runtime.WebScriptProcessor);
					ApplyProcessor(Community.Runtime.ZipScriptProcessor);
#if DEBUG
					ApplyProcessor(Community.Runtime.ZipDevScriptProcessor);
#endif

					void ApplyProcessor(IBaseProcessor processor, bool folder = false)
					{
						var newPath = folder ? Path.GetDirectoryName(path) : path;

						processor.ClearIgnore(newPath);
						processor.Prepare(newPath);
					}
					return;
				}

				var pluginFound = false;
				var pluginPrecompiled = false;

				foreach (var mod in ModLoader.LoadedPackages)
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
					Community.Runtime.WebScriptProcessor.IgnoreList.RemoveAll(x => !except.Any() || except.Any(x.Contains));
					Community.Runtime.ZipScriptProcessor.IgnoreList.RemoveAll(x => !except.Any() || except.Any(x.Contains));
#if DEBUG
					Community.Runtime.ZipDevScriptProcessor.IgnoreList.RemoveAll(x => !except.Any() || except.Any(x.Contains));
#endif

					foreach (var plugin in OrderedFiles)
					{
						if (except.Any(plugin.Value.Contains))
						{
							continue;
						}

						ApplyProcessor(Community.Runtime.ScriptProcessor);
						ApplyProcessor(Community.Runtime.WebScriptProcessor);
						ApplyProcessor(Community.Runtime.ZipScriptProcessor);
#if DEBUG
						ApplyProcessor(Community.Runtime.ZipDevScriptProcessor);
#endif

						void ApplyProcessor(IBaseProcessor processor)
						{
							processor.InstanceBuffer.ContainsKey(plugin.Key);

							if (!processor.Exists(plugin.Value))
							{
								processor.Prepare(plugin.Key, plugin.Value);
							}
						}
					}
					break;
				}

			default:
				{
					var path = GetPluginPath(name, true);

					if (string.IsNullOrEmpty(path))
					{
						Logger.Warn($"Plugin {name} was not found or was typed incorrectly.");
						return;
					}

					void ApplyProcessor(IBaseProcessor processor, bool folder = false)
					{
						var newPath = folder ? Path.GetDirectoryName(path) : path;

						processor.ClearIgnore(newPath);

						if (!processor.Exists(newPath))
						{
							processor.Prepare(newPath);
						}
					}

					ApplyProcessor(Community.Runtime.ScriptProcessor);
					ApplyProcessor(Community.Runtime.WebScriptProcessor);
					ApplyProcessor(Community.Runtime.ZipScriptProcessor);
#if DEBUG
					ApplyProcessor(Community.Runtime.ZipDevScriptProcessor, true);
#endif
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
			{
				var except = arg.Args.Skip(1);

				void ApplyProcessor(IBaseProcessor processor)
				{
					var tempList = Facepunch.Pool.GetList<string>();

					foreach (var bufferInstance in processor.InstanceBuffer)
					{
						tempList.Add(bufferInstance.Value.File);
					}

					processor.IgnoreList.RemoveAll(x => !except.Any() || (except.Any() && !except.Any(x.Contains)));
					processor.Clear(except);

					foreach (var plugin in tempList)
					{
						if (except.Any(plugin.Contains))
						{
							continue;
						}

						processor.Ignore(plugin);
					}

					Facepunch.Pool.FreeList(ref tempList);
				}

				ApplyProcessor(Community.Runtime.ScriptProcessor);
				ApplyProcessor(Community.Runtime.WebScriptProcessor);
				ApplyProcessor(Community.Runtime.ZipScriptProcessor);
#if DEBUG
				ApplyProcessor(Community.Runtime.ZipDevScriptProcessor);
#endif
				break;
			}

			default:
				{
					var path = GetPluginPath(name);

					if (!string.IsNullOrEmpty(path))
					{
						Community.Runtime.ScriptProcessor.Ignore(path);
						Community.Runtime.WebScriptProcessor.Ignore(path);
						Community.Runtime.ZipScriptProcessor.Ignore(path);
#if DEBUG
						Community.Runtime.ZipDevScriptProcessor.Ignore(path);
#endif
					}

					var pluginFound = false;
					var pluginPrecompiled = false;

					foreach (var mod in ModLoader.LoadedPackages)
					{
						var plugins = Facepunch.Pool.GetList<RustPlugin>();
						plugins.AddRange(mod.Plugins);

						foreach (var plugin in plugins)
						{
							if (plugin.Name == name)
							{
								pluginFound = true;

								if (plugin.IsPrecompiled)
								{
									pluginPrecompiled = true;
								}
								else
								{
									plugin.ProcessorProcess?.Dispose();
									mod.Plugins.Remove(plugin);
								}
							}
						}

						Facepunch.Pool.FreeList(ref plugins);
					}

					if (!pluginFound)
					{
						if (string.IsNullOrEmpty(path)) Logger.Warn($"Plugin {name} was not found or was typed incorrectly.");
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

		var name = arg.GetString(0);
		var plugin = ModLoader.LoadedPackages.SelectMany(x => x.Plugins).FirstOrDefault(x => string.IsNullOrEmpty(x.FileName) ? x.Name == name : x.FileName.Contains(name));
		var count = 1;

		if (plugin == null)
		{
			arg.ReplyWith("Couldn't find that plugin.");
			return;
		}

		using (var table = new StringTable("#", "Id", "Hook", "Time", "Memory"))
		{
			foreach (var hook in plugin.HookCache)
			{
				if (hook.Value.Count == 0)
				{
					continue;
				}

				var current = hook.Value[0];
				var hookName = current.Method.Name;

				var hookId = HookStringPool.GetOrAdd(hookName);
				var hookTime = hook.Value.Sum(x => x.HookTime);
				var memoryUsage = hook.Value.Sum(x => x.MemoryUsage);

				if (!plugin.Hooks.Contains(hookId))
				{
					continue;
				}

				table.AddRow(count, hookId, $"{hookName}", $"{hookTime:0}ms", $"{ByteEx.Format(memoryUsage, shortName: true).ToLower()}");

				count++;
			}

			var builder = new StringBuilder();

			builder.AppendLine($"{plugin.Name} v{plugin.Version} by {plugin.Author}{(plugin.IsCorePlugin ? $" [core]" : string.Empty)}");
			builder.AppendLine($"  Path:                   {plugin.FilePath}");
			builder.AppendLine($"  Compile Time:           {plugin.CompileTime}ms");
			builder.AppendLine($"  Uptime:                 {TimeEx.Format(plugin.Uptime, true).ToLower()}");
			builder.AppendLine($"  Total Hook Time:        {plugin.TotalHookTime:0}ms");
			builder.AppendLine($"  Total Memory Used:      {ByteEx.Format(plugin.TotalMemoryUsed, shortName: true).ToLower()}");
			builder.AppendLine($"  Internal Hook Override: {plugin.InternalCallHookOverriden}");
			builder.AppendLine($"  Has Conditionals:       {plugin.HasConditionals}");
			builder.AppendLine($"  Mod Package:            {plugin.Package?.Name} ({plugin.Package?.Plugins.Count}){((plugin.Package?.IsCoreMod).GetValueOrDefault() ? $" [core]" : string.Empty)}");

			if (count == 1)
			{
				builder.AppendLine($"No hooks found.");
			}
			else
			{
				builder.AppendLine($"Hooks:");
				builder.AppendLine(table.ToStringMinimal());
			}

			arg.ReplyWith(builder.ToString());
		}
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

					foreach (var package in ModLoader.LoadedPackages)
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

					foreach (var mod in ModLoader.LoadedPackages)
					{
						var plugins = Facepunch.Pool.GetList<RustPlugin>();
						plugins.AddRange(mod.Plugins);

						foreach (var plugin in plugins)
						{
							if (plugin.Name == name)
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

					foreach (var mod in ModLoader.LoadedPackages)
					{
						var plugins = Facepunch.Pool.GetList<RustPlugin>();
						plugins.AddRange(mod.Plugins);

						foreach (var plugin in plugins)
						{
							if (plugin.Name == name)
							{
								pluginFound = true;

								if (plugin.IsPrecompiled)
								{
									pluginPrecompiled = true;
								}
								else
								{
									plugin.ProcessorProcess?.Dispose();
									mod.Plugins.Remove(plugin);
								}
							}
						}

						Facepunch.Pool.FreeList(ref plugins);
					}

					if (!pluginFound)
					{
						if (string.IsNullOrEmpty(path)) Logger.Warn($"Plugin {name} was not found or was typed incorrectly.");
						else Logger.Warn($"Plugin {name} was not loaded but was marked as ignored.");

						return;
					}
					else if (pluginPrecompiled)
					{
						Logger.Warn($"Plugin {name} is a precompiled plugin which can only be unloaded/uninstalled programmatically.");
						return;
					}

					OsEx.File.Move(path, Path.Combine(Defines.GetScriptBackupFolder(), Path.GetFileName(path)));
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

					OsEx.File.Move(path, Path.Combine(Defines.GetScriptFolder(), Path.GetFileName(path)));
					break;
				}
		}
	}
}
