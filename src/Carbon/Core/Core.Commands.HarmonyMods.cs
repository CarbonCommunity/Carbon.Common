﻿/*
 *
 * Copyright (c) 2022-2023 Carbon Community
 * All rights reserved.
 *
 */

using API.Assembly;

namespace Carbon.Core;

public partial class CorePlugin : CarbonPlugin
{
	[ConsoleCommand("harmonyload", "Loads a mod from 'carbon/harmony'. The equivalent of Rust's `harmony.load` that's been stripped away under framework management.")]
	[AuthLevel(2)]
	private void HarmonyLoad(ConsoleSystem.Arg args)
	{
		var mod = args.GetString(0);

		if (string.IsNullOrEmpty(mod))
		{
			args.ReplyWith($"Please provide a HarmonyMod name.");
			return;
		}

		foreach (var ext in Community.Runtime.AssemblyEx.Extensions.Loaded)
		{
			var folder = Path.GetDirectoryName(ext.Value.Key);
			var file = Path.GetFileNameWithoutExtension(ext.Value.Key);

			if (folder.Equals(Defines.GetHarmonyFolder(), StringComparison.InvariantCultureIgnoreCase) &&
			    file.Equals(mod, StringComparison.InvariantCultureIgnoreCase))
			{
				args.ReplyWith($"HarmonyMod with name '{file}' already loaded. Unload it first with 'c.harmonyunload'.");
				return;
			}
		}

		Community.Runtime.AssemblyEx.Extensions.CurrentExtensionType =
			IExtensionManager.ExtensionTypes.HarmonyModHotload;
		Community.Runtime.AssemblyEx.Extensions.Load(Path.Combine(Defines.GetHarmonyFolder(), $"{mod}.dll"), "Command");
	}

	[ConsoleCommand("harmonyunload", "Unloads a mod from 'carbon/harmony'. The equivalent of Rust's `harmony.unload` that's been stripped away under framework management.")]
	[AuthLevel(2)]
	private void HarmonyUnload(ConsoleSystem.Arg args)
	{
		var mod = args.GetString(0);

		if (string.IsNullOrEmpty(mod))
		{
			args.ReplyWith($"Please provide a HarmonyMod name.");
			return;
		}

		foreach (var ext in Community.Runtime.AssemblyEx.Extensions.Loaded)
		{
			var folder = Path.GetDirectoryName(ext.Value.Key);
			var file = Path.GetFileNameWithoutExtension(ext.Value.Key);

			if (folder.Equals(Defines.GetHarmonyFolder(), StringComparison.InvariantCultureIgnoreCase) &&
			    file.Equals(mod, StringComparison.InvariantCultureIgnoreCase))
			{
				Community.Runtime.AssemblyEx.Extensions.CurrentExtensionType =
					IExtensionManager.ExtensionTypes.HarmonyModHotload;
				Community.Runtime.AssemblyEx.Extensions.Unload(ext.Value.Key, "Command");
				break;
			}
		}
	}

	[ConsoleCommand("harmonymods", "Prints all currently loaded and processed HarmonyMods.")]
	[AuthLevel(2)]
	private void HarmonyMods(ConsoleSystem.Arg args)
	{
		using var table = new StringTable($"HarmonyMod ({Harmony.ModHooks.Count:n0})", "Version", "Assembly");

		foreach (var mod in Assemblies.Harmony)
		{
			var name = mod.Value.CurrentAssembly.GetName();
			table.AddRow(mod.Key, name.Version, name.Name);
		}

		args.ReplyWith($"{table.ToStringMinimal()}");
	}
}
