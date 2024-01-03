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

	public static ClientConfig Config => Community.Runtime.ClientConfig;

	public static void Init()
	{
		if (Config.Enabled)
		{
			Logger.Warn($" C4C: Carbon Client enabled @ protocol {Protocol.VERSION}.");
		}

		if (Config.Environment.NoMap)
		{
			ConVar.Server.levelurl = MAP_URL;
			ProcessConVars();

			Logger.Log($" C4C: NoMap enabled.");
		}

		if (Config.Enabled)
		{
			CorePlugin.RecoilOverrider.Initialize();

			ProcessPatches();
		}

		Logger.Log($" C4C: Carbon Client ready.");
	}
	public static void TerrainPostprocess()
	{
		if (!Config.Environment.NoMap)
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
