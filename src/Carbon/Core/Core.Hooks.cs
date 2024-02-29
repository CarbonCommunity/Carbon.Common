/*
 *
 * Copyright (c) 2022-2023 Carbon Community
 * All rights reserved.
 *
 */

namespace Carbon.Core;

#pragma warning disable IDE0051

public partial class CorePlugin : CarbonPlugin
{
	internal static bool _isPlayerTakingDamage = false;
	internal static readonly string[] _emptyStringArray = new string[0];

	internal static object IOnServerInitialized()
	{
		if (!Community.IsServerInitialized)
		{
			Community.IsServerInitialized = true;

			if (Analytic.Enabled)
			{
				Analytic.Include("plugin_count", ModLoader.LoadedPackages.Sum(x => x.Plugins.Count));
				Analytic.Include("plugins_totalmemoryused",
					$"{ByteEx.Format(ModLoader.LoadedPackages.Sum(x => x.Plugins.Sum(y => y.TotalMemoryUsed)), valueFormat: "0", stringFormat: "{0}{1}").ToLower()}");
				Analytic.Include("plugins_totalhooktime",
					$"{ModLoader.LoadedPackages.Sum(x => x.Plugins.Sum(y => y.TotalHookTime)).RoundUpToNearestCount(100):0}ms");
				Analytic.Include("extension_count", Community.Runtime.AssemblyEx.Extensions.Loaded.Count);
				Analytic.Include("module_count", Community.Runtime.AssemblyEx.Modules.Loaded.Count);
				Analytic.Include("hook_count",
					Community.Runtime.HookManager.LoadedDynamicHooks.Count(x => x.IsInstalled) +
					Community.Runtime.HookManager.LoadedStaticHooks.Count(x => x.IsInstalled));
				Analytic.Send("on_server_initialized");
			}
		}

		return null;
	}
	internal static object IOnServerShutdown()
	{
		Logger.Log($"Saving plugin configuration and data..");

		// OnServerShutdown
		HookCaller.CallStaticHook(1708437245);

		// OnServerSave
		HookCaller.CallStaticHook(2032593992);

		Logger.Log($"Shutting down Carbon..");
		Interface.Oxide.OnShutdown();
		Community.Runtime.ScriptProcessor.Clear();

		return null;
	}
}
