/*
 *
 * Copyright (c) 2022-2023 Carbon Community 
 * All rights reserved.
 *
 */

#define CLIENT

namespace Carbon.Client.Assets;

public class AddonManager
{
	public static AddonManager Instance { get; internal set; } = new();

	public List<Addon> Installed { get; } = new();

#if CLIENT
	public void Deliver(CarbonClient client, List<Addon> addons)
	{
		// Send addons to the client
	}
#else

#endif
}
