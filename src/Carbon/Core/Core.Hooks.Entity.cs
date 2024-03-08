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
	internal static object IOnEntitySaved(BaseNetworkable baseNetworkable, BaseNetworkable.SaveInfo saveInfo)
	{
		if (!Community.IsServerInitialized || saveInfo.forConnection == null)
		{
			return null;
		}

		// OnEntitySaved
		HookCaller.CallStaticHook(3947573992, baseNetworkable, saveInfo);

		return null;
	}
}
