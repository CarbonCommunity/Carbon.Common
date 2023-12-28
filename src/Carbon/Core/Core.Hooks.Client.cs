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
		if (!Community.Runtime.ClientConfig.Enabled)
		{
			return;
		}

		Logger.Log($"{client.Connection} is ready");
		Community.Runtime.CarbonClientManager.SendRequestToPlayer(client.Connection);
	}

	private void OnClientAddonsDownload(ICarbonClient client)
	{
		if (!Community.Runtime.ClientConfig.Enabled)
		{
			return;
		}

		Logger.Log($"{client.Connection} is downloading addons");
		client.IsDownloadingAddons = true;
	}

	private void OnClientAddonsFinalized(ICarbonClient client)
	{
		if (!Community.Runtime.ClientConfig.Enabled)
		{
			return;
		}

		Logger.Log($"{client.Connection} finished downloading addons");
		client.IsDownloadingAddons = false;
	}
}
