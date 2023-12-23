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
}
