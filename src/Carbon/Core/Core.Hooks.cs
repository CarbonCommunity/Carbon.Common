/*
 *
 * Copyright (c) 2022-2023 Carbon Community
 * All rights reserved.
 *
 */

using API.Commands;
using ConVar;
using Connection = Network.Connection;

namespace Carbon.Core;
#pragma warning disable IDE0051

public partial class CorePlugin : CarbonPlugin
{
	internal static bool _isPlayerTakingDamage = false;
	internal readonly string[] _emptyStringArray = new string[0];

	private void IOnServerInitialized()
	{
		if (!Community.IsServerInitialized)
		{
			Community.IsServerInitialized = true;

			Community.Runtime.Analytics.LogEvent("on_server_initialized",
				segments: Community.Runtime.Analytics.Segments,
				metrics: new Dictionary<string, object> {
					{ "plugin_count", ModLoader.LoadedPackages.Sum(x => x.Plugins.Count) },
					{ "plugins_totalmemoryused", $"{ByteEx.Format(ModLoader.LoadedPackages.Sum(x => x.Plugins.Sum(y => y.TotalMemoryUsed)), valueFormat: "0", stringFormat: "{0}{1}").ToLower()}" },
					{ "plugins_totalhooktime", $"{ModLoader.LoadedPackages.Sum(x => x.Plugins.Sum(y => y.TotalHookTime)).RoundUpToNearestCount(100):0}ms" },
					{ "extension_count",Community.Runtime.AssemblyEx.Extensions.Loaded.Count },
					{ "module_count", Community.Runtime.AssemblyEx.Modules.Loaded.Count },
					{ "hook_count", Community.Runtime.HookManager.LoadedDynamicHooks.Count(x => x.IsInstalled) + Community.Runtime.HookManager.LoadedStaticHooks.Count(x => x.IsInstalled) }
				}
			);
		}
	}
	private void IOnServerShutdown()
	{
		Logger.Log($"Saving plugin configuration and data..");

		// OnServerSave
		HookCaller.CallStaticHook(2032593992);

		Logger.Log($"Shutting down Carbon..");
		Interface.Oxide.OnShutdown();
		Community.Runtime.ScriptProcessor.Clear();
	}
}
