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

	public static bool NomapEnabled => CommandLineEx.GetArgumentExists("+carbon.nomap");
	public static bool ClientEnabled => CommandLineEx.GetArgumentExists("+carbon.client");
	public static bool OldRecoil => Community.Runtime != null && Community.Runtime.CorePlugin is CorePlugin core && core.OldRecoil;

	public static void Init()
	{
		if (NomapEnabled)
		{
			ConVar.Server.levelurl = MAP_URL;
			ProcessConVars();
		}

		if (ClientEnabled)
		{
			CorePlugin.RecoilOverrider.Initialize();

			ProcessPatches();
		}
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
