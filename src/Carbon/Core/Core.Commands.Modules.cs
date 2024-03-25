using System.Text;
using API.Assembly;
using Carbon.Base.Interfaces;

/*
 *
 * Copyright (c) 2022-2023 Carbon Community
 * All rights reserved.
 *
 */

namespace Carbon.Core;

public partial class CorePlugin : CarbonPlugin
{
	[ConsoleCommand("setmodule", "Enables or disables Carbon modules. Visit root/carbon/modules and use the config file names as IDs.")]
	[AuthLevel(2)]
	private void SetModule(ConsoleSystem.Arg arg)
	{
		if (!arg.HasArgs(2)) return;

		var moduleName = arg.GetString(0);
		var hookable = Community.Runtime.ModuleProcessor.Modules.FirstOrDefault(x => x.Name == moduleName);
		var module = hookable?.To<BaseModule>();

		if (module == null)
		{
			arg.ReplyWith($"Couldn't find that module. Try 'c.modules' to print them all.");
			return;
		}
		else if (module.ForceEnabled)
		{
			arg.ReplyWith($"That module is forcefully enabled, you may not change its status.");
			return;
		}
		else if (module.ForceDisabled)
		{
			arg.ReplyWith($"That module is forcefully disabled, you may not change its status.");
			return;
		}

		var previousEnabled = module.GetEnabled();
		var newEnabled = arg.GetBool(1);

		if (previousEnabled != newEnabled)
		{
			module.SetEnabled(newEnabled);

			module.Save();
			arg.ReplyWith($"{module.Name} marked {(module.GetEnabled() ? "enabled" : "disabled")}.");
		}
		else
		{
			arg.ReplyWith($"{module.Name} is already {(module.GetEnabled() ? "enabled" : "disabled")}.");
		}
	}

	[ConsoleCommand("saveallmodules", "Saves the configs and data files of all available modules.")]
	[AuthLevel(2)]
	private void SaveAllModules(ConsoleSystem.Arg arg)
	{
		foreach (var hookable in Community.Runtime.ModuleProcessor.Modules)
		{
			var module = hookable.To<IModule>();
			module.Save();
		}

		arg.ReplyWith($"Saved {Community.Runtime.ModuleProcessor.Modules.Count:n0} module configs and data files.");
	}

	[ConsoleCommand("savemoduleconfig", "Saves Carbon module config & data file.")]
	[AuthLevel(2)]
	private void SaveModuleConfig(ConsoleSystem.Arg arg)
	{
		if (!arg.HasArgs(1)) return;

		var hookable = Community.Runtime.ModuleProcessor.Modules.FirstOrDefault(x => x.Name == arg.Args[0]);
		var module = hookable.To<IModule>();

		if (module == null)
		{
			arg.ReplyWith($"Couldn't find that module.");
			return;
		}

		module.Save();

		arg.ReplyWith($"Saved '{module.Name}' module config & data file.");
	}

	[ConsoleCommand("loadmoduleconfig", "Loads Carbon module config & data file.")]
	[AuthLevel(2)]
	private void LoadModuleConfig(ConsoleSystem.Arg arg)
	{
		if (!arg.HasArgs(1)) return;

		var moduleName = arg.GetString(0);
		var hookable = Community.Runtime.ModuleProcessor.Modules.FirstOrDefault(x => x.Name == moduleName);
		var module = hookable.To<IModule>();

		if (module == null)
		{
			arg.ReplyWith($"Couldn't find that module.");
			return;
		}

		if (module.GetEnabled()) module.SetEnabled(false);

		try
		{
			module.Load();

			if (module.GetEnabled()) module.OnEnableStatus();

			arg.ReplyWith($"Reloaded '{module.Name}' module config.");
		}
		catch (Exception ex)
		{
			Logger.Error($"Failed module Load for {module.Name} [Reload Request]", ex);
		}
	}

	[ConsoleCommand("modules", "Prints a list of all available modules. Eg. c.modules [-abc|--json|-t|-m|-f] [-asc]")]
	[AuthLevel(2)]
	private void Modules(ConsoleSystem.Arg arg)
	{
		var mode = arg.GetString(0);
		var flip = arg.GetString(0).Equals("-asc") || arg.GetString(1).Equals("-asc");

		var count = 1;
		using var print = new StringTable("#", "Name", "Enabled", "Version", "Hook Time", "Hook Fires", "Memory Usage", "Uptime");

		IEnumerable<BaseHookable> array = mode switch
		{
			"-abc" => Community.Runtime.ModuleProcessor.Modules.OrderBy(x => x.Name),
			"-t" => (flip ? Community.Runtime.ModuleProcessor.Modules.OrderBy(x => x.TotalHookTime) : Community.Runtime.ModuleProcessor.Modules.OrderByDescending(x => x.TotalHookTime)),
			"-m" => (flip ? Community.Runtime.ModuleProcessor.Modules.OrderBy(x => x.TotalMemoryUsed) : Community.Runtime.ModuleProcessor.Modules.OrderByDescending(x => x.TotalMemoryUsed)),
			"-f" => (flip ? Community.Runtime.ModuleProcessor.Modules.OrderBy(x => x.CurrentHookFires) : Community.Runtime.ModuleProcessor.Modules.OrderByDescending(x => x.CurrentHookFires)),
			_ => (flip ? Community.Runtime.ModuleProcessor.Modules.AsEnumerable().Reverse() : Community.Runtime.ModuleProcessor.Modules.AsEnumerable())
		};

		foreach (var hookable in array)
		{
			if (hookable is not BaseModule module)
			{
				Logger.Warn($" Not a module {hookable.GetType()}");
				continue;
			}

			var hookTimeAverageValue =
#if DEBUG
				(float)module.HookTimeAverage.CalculateAverage();
#else
				0;
#endif
			var memoryAverageValue =
#if DEBUG
				(float)module.MemoryAverage.CalculateAverage();
#else
				0;
#endif
			var hookTimeAverage = Mathf.RoundToInt(hookTimeAverageValue) == 0 ? string.Empty : $" (avg {hookTimeAverageValue:0}ms)";
			var memoryAverage = Mathf.RoundToInt(memoryAverageValue) == 0 ? string.Empty : $" (avg {ByteEx.Format(memoryAverageValue, shortName: true, stringFormat: "{0}{1}").ToLower()})";
			print.AddRow(count, hookable.Name, module.GetEnabled(), module.Version,
				$"{module.TotalHookTime.TotalMilliseconds:0}ms{hookTimeAverage}",
				$"{module.CurrentHookFires:0}",
				$"{ByteEx.Format(module.TotalMemoryUsed, shortName: true, stringFormat: "{0}{1}").ToLower()}{memoryAverage}",
				$"{TimeEx.Format(module.Uptime)}");
			count++;
		}

		arg.ReplyWith(print.Write(StringTable.FormatTypes.None));
	}

	[ConsoleCommand("modulesmanaged", "Prints a list of all currently loaded extensions.")]
	[AuthLevel(2)]
	private void ModulesManaged(ConsoleSystem.Arg arg)
	{
		using var body = new StringTable("#", "Module", "Type");
		var count = 1;

		foreach (var mod in Community.Runtime.AssemblyEx.Modules.Loaded)
		{
			body.AddRow($"{count:n0}", Path.GetFileNameWithoutExtension(mod.Value.Key), mod.Key.FullName);
			count++;
		}

		arg.ReplyWith(body.Write(StringTable.FormatTypes.None));
	}

	[ConsoleCommand("moduleinfo", "Prints advanced information about a currently loaded module. From hooks, hook times, hook memory usage and other things.")]
	[AuthLevel(2)]
	private void ModuleInfo(ConsoleSystem.Arg arg)
	{
		if (!arg.HasArgs(1))
		{
			Logger.Warn("You must provide the name of a module to print module advanced information.");
			return;
		}

		var name = arg.GetString(0);
		var mode = arg.GetString(1);
		var flip = arg.GetString(2).Equals("-asc");
		var module = Community.Runtime.ModuleProcessor.Modules.FirstOrDefault(x => x.Name == name) as BaseModule;
		var count = 1;

		if (module == null)
		{
			arg.ReplyWith("Couldn't find that module.");
			return;
		}

		using (var table = new StringTable("#", "Id", "Hook", "Time", "Memory", "Fires", "Subscribed", "Async/Overrides"))
		{
			IEnumerable<HashSet<CachedHook>> array = mode switch
			{
				"-t" => (flip ? module.HookCache.OrderBy(x => x.Value.Sum(x => x.HookTime.TotalMilliseconds)) : module.HookCache.OrderByDescending(x => x.Value.Sum(x => x.HookTime.TotalMilliseconds))).Select(x => x.Value),
				"-m" => (flip ? module.HookCache.OrderBy(x => x.Value.Sum(x => x.MemoryUsage)) : module.HookCache.OrderByDescending(x => x.Value.Sum(x => x.MemoryUsage))).Select(x => x.Value),
				"-f" => (flip ? module.HookCache.OrderBy(x => x.Value.Sum(x => x.TimesFired)) : module.HookCache.OrderByDescending(x => x.Value.Sum(x => x.TimesFired))).Select(x => x.Value),
				_ => module.HookCache.Select(x => x.Value)
			};

			foreach (var hook in array)
			{
				if (hook.Count == 0)
				{
					continue;
				}

				var current = hook.FirstOrDefault();
				var hookId = HookStringPool.GetOrAdd(current.Method.Name);

				if (!module.Hooks.Contains(hookId))
				{
					continue;
				}

				var hookName = current.Method.Name;

				var hookTime = hook.Sum(x => x.HookTime.TotalMilliseconds);
				var hookMemoryUsage = hook.Sum(x => x.MemoryUsage);
				var hookCount = hook.Count;
				var hookAsyncCount = hook.Count(x => x.IsAsync);
				var hooksTimesFired = hook.Sum(x => x.TimesFired);

				if (!module.Hooks.Contains(hookId))
				{
					continue;
				}

				table.AddRow(count, hookId, $"{hookName}", $"{hookTime:0}ms", $"{ByteEx.Format(hookMemoryUsage, shortName: true).ToLower()}", $"{hooksTimesFired:n0}", !module.IgnoredHooks.Contains(hookId), $"{hookAsyncCount:n0}/{hookCount:n0}");

				count++;
			}

			var builder = new StringBuilder();

			builder.AppendLine($"Additional information for {module.Name} v{module.Version}{(module.ForceEnabled ? $" [force enabled]" : string.Empty)}");
			builder.AppendLine($"  Enabled:                {module.GetEnabled()}");
			builder.AppendLine($"  Enabled (default):      {module.EnabledByDefault}");
			builder.AppendLine($"  Context:                {module.Context}");
			builder.AppendLine($"  Uptime:                 {TimeEx.Format(module.Uptime, true).ToLower()}");
			builder.AppendLine($"  Total Hook Time:        {module.TotalHookTime.TotalMilliseconds:0}ms");
			builder.AppendLine($"  Total Memory Used:      {ByteEx.Format(module.TotalMemoryUsed, shortName: true).ToLower()}");
			builder.AppendLine($"  Internal Hook Override: {module.InternalCallHookOverriden}");
			builder.AppendLine($"Hooks:");
			builder.AppendLine(table.ToStringMinimal());

			arg.ReplyWith(builder.ToString());
		}
	}

	[ConsoleCommand("reloadmodules", "Fully reloads all modules.")]
	[AuthLevel(2)]
	private void ReloadModules(ConsoleSystem.Arg arg)
	{
		Community.Runtime.AssemblyEx.Modules.Watcher.TriggerAll(WatcherChangeTypes.Changed);
	}

	[ConsoleCommand("reloadmodule", "Reloads a currently loaded module assembly entirely.")]
	[AuthLevel(2)]
	private void ReloadModule(ConsoleSystem.Arg arg)
	{
		if (!arg.HasArgs(1)) return;

		var hookable = Community.Runtime.ModuleProcessor.Modules.FirstOrDefault(x => x.Name == arg.Args[0]);
		var module = hookable.To<IModule>();

		if (module == null)
		{
			arg.ReplyWith($"Couldn't find that module.");
			return;
		}

		module.Reload();

		arg.ReplyWith($"Reloaded '{module.Name}' module.");
	}
}
