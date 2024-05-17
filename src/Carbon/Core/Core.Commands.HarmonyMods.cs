/*
 *
 * Copyright (c) 2022-2023 Carbon Community
 * All rights reserved.
 *
 */

using Carbon.Client;

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
				Community.Runtime.AssemblyEx.Extensions.Unload(ext.Value.Key, "Command");
				break;
			}
		}
	}
}
