/*
 *
 * Copyright (c) 2022-2023 Carbon Community
 * All rights reserved.
 *
 */

using System.Text;
using Facepunch;

namespace Carbon.Core;

public partial class CorePlugin : CarbonPlugin
{
	[ConsoleCommand("hookinfo", "Prints advanced information about a specific hook (takes [uint|string]). From hooks, hook times, hook memory usage to plugin and modules using it and other things.")]
	[AuthLevel(2)]
	private void HookInfo(ConsoleSystem.Arg arg)
	{
		if (!arg.HasArgs(1))
		{
			Logger.Warn("You must provide the name of a hook to print plugin advanced information.");
			return;
		}

		var name = arg.GetString(0).ToLower();
		var isUid = uint.TryParse(name, out _);

		var hookName = isUid ? HookStringPool.GetOrAdd(name.ToUint()) : name;
		var hookId = isUid ? name.ToUint() : HookStringPool.GetOrAdd(name);

		var output = PoolEx.GetStringBuilder();

		const string byteFormat = "{0}{1}";

		output.AppendLine($"Information for {hookName}[{hookId}]");
		{
			var plugins = PoolEx.GetDictionary<BaseHookable, List<CachedHook>>();
			{
				foreach (var package in ModLoader.LoadedPackages)
				{
					foreach (var plugin in package.Plugins)
					{
						foreach (var hookCache in plugin.HookCache)
						{
							if (hookCache.Key == hookId)
							{
								plugins.Add(plugin, hookCache.Value);
							}
						}
					}
				}
			}

			using var pluginsTable = new StringTable("", $"Plugins ({plugins.Count:n0})", "IsByRef", "IsAsync", "Fires", "Time", "Memory");

			foreach (var plugin in plugins)
			{
				var hook = plugin.Value[0];
				pluginsTable.AddRow(string.Empty, $"{plugin.Key.Name}", hook.IsByRef, hook.IsAsync, $"{hook.TimesFired:n0}", $"{hook.HookTime:0}ms", ByteEx.Format(hook.MemoryUsage, stringFormat: byteFormat).ToLower());
			}

			output.AppendLine(pluginsTable.ToStringMinimal());

			var modules = PoolEx.GetDictionary<BaseHookable, List<CachedHook>>();
			{
				foreach (var module in Community.Runtime.ModuleProcessor.Modules)
				{
					foreach (var hookCache in module.HookCache)
					{
						if (hookCache.Key == hookId)
						{
							modules.Add(module, hookCache.Value);
							break;
						}
					}
				}
			}

			using var modulesTable = new StringTable("", $"Modules ({modules.Count:n0})", "IsByRef", "IsAsync", "Fires", "Time", "Memory");

			foreach (var module in modules)
			{
				var hook = module.Value[0];
				modulesTable.AddRow(string.Empty, $"{module.Key.Name}", hook.IsByRef, hook.IsAsync, $"{hook.TimesFired:n0}", $"{hook.HookTime:0}ms", ByteEx.Format(hook.MemoryUsage, stringFormat: byteFormat).ToLower());
			}

			output.AppendLine(modulesTable.ToStringMinimal());

			output.AppendLine($"Total hook fires:   {plugins.Sum(x => x.Value.Sum(y => y.TimesFired)) + modules.Sum(x => x.Value.Sum(y => y.TimesFired)):n0}");
			output.AppendLine($"Total hook time:    {plugins.Sum(x => x.Value.Sum(y => y.HookTime)) + modules.Sum(x => x.Value.Sum(y => y.HookTime)):0}ms");
			output.AppendLine($"Total hook memory:  {ByteEx.Format(plugins.Sum(x => x.Value.Sum(y => y.MemoryUsage)) + modules.Sum(x => x.Value.Sum(y => y.MemoryUsage))).ToLower()}");

			arg.ReplyWith(output.ToString());

			PoolEx.FreeStringBuilder(ref output);
			PoolEx.FreeDictionary(ref plugins);
			PoolEx.FreeDictionary(ref modules);
		}
	}
}
