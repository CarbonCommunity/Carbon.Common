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
		var module = hookable?.To<IModule>();

		if (module == null)
		{
			arg.ReplyWith($"Couldn't find that module. Try 'c.modules' to print them all.");
			return;
		}
		else if (module is BaseModule baseModule && baseModule.ForceEnabled)
		{
			arg.ReplyWith($"That module is forcefully enabled, you may not change its status.");
			return;
		}

		var previousEnabled = module.GetEnabled();
		var newEnabled = arg.GetBool(1);

		if (previousEnabled != newEnabled)
		{
			module.SetEnabled(newEnabled);
			module.Save();
		}

		arg.ReplyWith($"{module.Name} marked {(module.GetEnabled() ? "enabled" : "disabled")}.");
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
		module.Load();
		if (module.GetEnabled()) module.OnEnableStatus();

		arg.ReplyWith($"Reloaded '{module.Name}' module config.");
	}

	[ConsoleCommand("modules", "Prints a list of all available modules.")]
	[AuthLevel(2)]
	private void Modules(ConsoleSystem.Arg arg)
	{
		var count = 1;
		using var print = new StringTable("#", "Name", "Enabled", "Version", "Hook Time", "Memory Usage", "Uptime", "Quick");
		foreach (var hookable in Community.Runtime.ModuleProcessor.Modules)
		{
			if (hookable is not BaseModule module) continue;

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
			print.AddRow(count, hookable.Name, module.GetEnabled(), module.Version, $"{module.TotalHookTime:0}ms{hookTimeAverage}", $"{ByteEx.Format(module.TotalMemoryUsed, shortName: true, stringFormat: "{0}{1}").ToLower()}{memoryAverage}", $"{TimeEx.Format(module.Uptime)}", $"c.setmodule \"{hookable.Name}\" [0|1]");
			count++;
		}

		arg.ReplyWith(print.Write(StringTable.FormatTypes.None));
	}
}
