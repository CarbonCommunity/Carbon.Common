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

			Analytics.on_server_initialized();
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
