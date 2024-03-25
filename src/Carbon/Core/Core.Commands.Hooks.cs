/*
 *
 * Copyright (c) 2022-2023 Carbon Community
 * All rights reserved.
 *
 */

using System.Text;
using Facepunch;
using Timer = Oxide.Plugins.Timer;

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
			var plugins = PoolEx.GetDictionary<BaseHookable, HashSet<CachedHook>>();
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
				var hook = plugin.Value.FirstOrDefault();
				pluginsTable.AddRow(string.Empty, $"{plugin.Key.Name}", hook.IsByRef, hook.IsAsync, $"{hook.TimesFired:n0}", $"{hook.HookTime.TotalMilliseconds:0}ms", ByteEx.Format(hook.MemoryUsage, stringFormat: byteFormat).ToLower());
			}

			output.AppendLine(pluginsTable.ToStringMinimal());

			var modules = PoolEx.GetDictionary<BaseHookable, HashSet<CachedHook>>();
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
				var hook = module.Value.FirstOrDefault();
				modulesTable.AddRow(string.Empty, $"{module.Key.Name}", hook.IsByRef, hook.IsAsync, $"{hook.TimesFired:n0}", $"{hook.HookTime.TotalMilliseconds:0}ms", ByteEx.Format(hook.MemoryUsage, stringFormat: byteFormat).ToLower());
			}

			output.AppendLine(modulesTable.ToStringMinimal());

			output.AppendLine($"Total hook fires:   {plugins.Sum(x => x.Value.Sum(y => y.TimesFired)) + modules.Sum(x => x.Value.Sum(y => y.TimesFired)):n0}");
			output.AppendLine($"Total hook time:    {plugins.Sum(x => x.Value.Sum(y => y.HookTime.TotalMilliseconds)) + modules.Sum(x => x.Value.Sum(y => y.HookTime.TotalMilliseconds)):0}ms");
			output.AppendLine($"Total hook memory:  {ByteEx.Format(plugins.Sum(x => x.Value.Sum(y => y.MemoryUsage)) + modules.Sum(x => x.Value.Sum(y => y.MemoryUsage))).ToLower()}");

			arg.ReplyWith(output.ToString());

			PoolEx.FreeStringBuilder(ref output);
			PoolEx.FreeDictionary(ref plugins);
			PoolEx.FreeDictionary(ref modules);
		}
	}

	#if DEBUG
	private uint _debuggedHook;
	private Timer _debuggedTimer;

	[Conditional("DEBUG")]
	[ConsoleCommand("debughook", "Enables debugging on a specific hook, which logs each time it fires. This can affect server performance, depending on how ofter the hook is firing.")]
	[AuthLevel(2)]
	private void DebugHook(ConsoleSystem.Arg arg)
	{
		DebugHookImpl(arg.GetString(0), arg.GetFloat(1), out var response);

		arg.ReplyWith(response);
	}

	[Conditional("DEBUG")]
	private void DebugHookImpl(string hookString, float time, out string response)
	{
		if (string.IsNullOrEmpty(hookString))
		{
			if (_debuggedHook != 0)
			{
				var hooksDisabled = 0;
				LoopHookableProcess(_debuggedHook, true, ref hooksDisabled);
				response = $"Disabled debugging hook {HookStringPool.GetOrAdd(_debuggedHook)}[{_debuggedHook}] (found {hooksDisabled:n0} {hooksDisabled.Plural("use", "uses")})";
			}
			else
			{
				response = "Empty string. Trust me, that won't work.";
			}

			return;
		}

		var hookId = uint.TryParse(hookString, out var alreadyIdValue) ? alreadyIdValue : HookStringPool.GetOrAdd(hookString);
		var alreadyDebugging = hookId == _debuggedHook;
		var hooksFound = 0;

		LoopHookableProcess(hookId, alreadyDebugging, ref hooksFound);

		static void LoopHookableProcess(uint hookId, bool alreadyDebugging, ref int hooksFound)
		{
			foreach (var package in ModLoader.LoadedPackages)
			{
				foreach (var plugin in package.Plugins)
				{
					ProcessHookable(plugin, hookId, alreadyDebugging, ref hooksFound);
				}
			}
			foreach (var module in Community.Runtime.ModuleProcessor.Modules)
			{
				ProcessHookable(module, hookId, alreadyDebugging, ref hooksFound);
			}
		}
		static void ProcessHookable(BaseHookable hookable, uint hookId, bool alreadyDebugging, ref int hooksFound)
		{
			foreach (var cache in hookable.HookCache)
			{
				if (cache.Key != hookId)
				{
					continue;
				}

				foreach (var hook in cache.Value)
				{
					hooksFound++;
					hook.SetDebug(!alreadyDebugging);
				}
			}
		}

		_debuggedHook = alreadyDebugging ? default : hookId;

		response = $"{(alreadyDebugging ? $"Disabled debugging hook {hookString}[{hookId}]" : $"Started debugging hook {hookString}[{hookId}]")} (found {hooksFound:n0} {hooksFound.Plural("use", "uses")})";

		_debuggedTimer?.Destroy();

		if (time > 0)
		{
			_debuggedTimer = timer.In(time, () =>
			{
				DebugHookImpl(hookString, 0, out var response);
				Logger.Log(response);
			});
		}
	}
	#endif
}
