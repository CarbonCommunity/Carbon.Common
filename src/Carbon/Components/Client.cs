/*
 *
 * Copyright (c) 2022-2023 Carbon Community
 * All rights reserved.
 *
 */

namespace Carbon.Client;

public struct Client
{
	public const string MAP_URL = "https://carbonmod.gg/assets/content/blank.map";

	public static bool NomapEnabled => Community.Runtime.ClientConfig.Enabled;
	public static bool ClientEnabled => Community.Runtime.ClientConfig.Enabled;
	public static bool OldRecoil => Community.Runtime != null && Community.Runtime.ClientConfig.Gameplay.UseOldRecoil;

	public static void Init()
	{
		if (ClientEnabled)
		{
			Logger.Warn($" C4C: Carbon Client enabled @ protocol {Protocol.VERSION}.");
		}

		if (NomapEnabled)
		{
			ConVar.Server.levelurl = MAP_URL;
			ProcessConVars();

			Logger.Log($" C4C: NoMap enabled.");
		}

		if (ClientEnabled)
		{
			CorePlugin.RecoilOverrider.Initialize();

			ProcessPatches();
		}

		Logger.Log($" C4C: Carbon Client ready.");

	}
	public static void TerrainPostprocess()
	{
		if (!NomapEnabled)
		{
			return;
		}

		TerrainMeta.Collider.enabled = false;
	}
	public static void ProcessConVars()
	{
		ConVar.Spawn.max_density = 0;
		ConVar.Server.events = false;
	}
	public static void ProcessPatches()
	{
		Community.Runtime.CarbonClientManager.ApplyPatch();
	}
}
