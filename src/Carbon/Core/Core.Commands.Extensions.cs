using API.Assembly;

/*
 *
 * Copyright (c) 2022-2023 Carbon Community 
 * All rights reserved.
 *
 */

namespace Carbon.Core;

public partial class CorePlugin : CarbonPlugin
{
	[ConsoleCommand("reloadextensions", "Fully reloads all extensions.")]
	[AuthLevel(2)]
	private void ReloadExtensions(ConsoleSystem.Arg arg)
	{
		Community.Runtime.AssemblyEx.Extensions.Watcher.TriggerAll(WatcherChangeTypes.Changed);
	}

	[ConsoleCommand("reloadmodules", "Fully reloads all modules.")]
	[AuthLevel(2)]
	private void ReloadModules(ConsoleSystem.Arg arg)
	{
		Community.Runtime.AssemblyEx.Modules.Watcher.TriggerAll(WatcherChangeTypes.Changed);
	}

	[ConsoleCommand("extensions", "Prints a list of all currently loaded extensions.")]
	[AuthLevel(2)]
	private void Extensions(ConsoleSystem.Arg arg)
	{
		using var body = new StringTable("#", "Extension", "Type");
		var count = 1;

		var addonType = typeof(ICarbonAddon);
		foreach (var mod in Community.Runtime.AssemblyEx.Extensions.Loaded)
		{
			body.AddRow($"{count:n0}", Path.GetFileNameWithoutExtension(mod.Value), mod.Key.FullName);
			count++;
		}

		arg.ReplyWith(body.Write(StringTable.FormatTypes.None));
	}

	[ConsoleCommand("modulesmanaged", "Prints a list of all currently loaded extensions.")]
	[AuthLevel(2)]
	private void ModulesManaged(ConsoleSystem.Arg arg)
	{
		using var body = new StringTable("#", "Module", "Type");
		var count = 1;

		var addonType = typeof(ICarbonAddon);
		foreach (var mod in Community.Runtime.AssemblyEx.Modules.Loaded)
		{
			body.AddRow($"{count:n0}", Path.GetFileNameWithoutExtension(mod.Value), mod.Key.FullName);
			count++;
		}

		arg.ReplyWith(body.Write(StringTable.FormatTypes.None));
	}
}
