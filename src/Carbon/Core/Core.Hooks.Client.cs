/*
 *
 * Copyright (c) 2022-2023 Carbon Community
 * All rights reserved.
 *
 */

using Carbon.Client.SDK;
using Connection = Network.Connection;

namespace Carbon.Core;
#pragma warning disable IDE0051

public partial class CorePlugin : CarbonPlugin
{
	private void IOnCarbonClientReady(ICarbonClient client)
	{
		Community.Runtime.CarbonClientManager.SendRequestToPlayer(client.Connection);
	}

	private void OnClientAddonsDownload(ICarbonClient client)
	{
		Puts($"{client.Connection} is downloading addons");
		client.IsDownloadingAddons = true;
	}

	private void OnClientAddonsFinalized(ICarbonClient client)
	{
		Puts($"{client.Connection} finished downloading addons");
		client.IsDownloadingAddons = false;
	}
}
