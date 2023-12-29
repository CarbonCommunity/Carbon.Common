/*
 *
 * Copyright (c) 2022-2023 Carbon Community
 * All rights reserved.
 *
 */

using Connection = Network.Connection;

namespace Carbon.Core;
#pragma warning disable IDE0051

public partial class CorePlugin : CarbonPlugin
{
	private void IOnEntitySaved(BaseNetworkable baseNetworkable, BaseNetworkable.SaveInfo saveInfo)
	{
		if (!Community.IsServerInitialized || saveInfo.forConnection == null)
		{
			return;
		}

		// OnEntitySaved
		HookCaller.CallStaticHook(3947573992, baseNetworkable, saveInfo);
	}
}
