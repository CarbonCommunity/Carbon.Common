/*
 *
 * Copyright (c) 2022-2023 Carbon Community
 * All rights reserved.
 *
 */

namespace Carbon.Client;

public struct Client
{
	public const int PROTOCOL = 100;

	public const string MAP_URL = "https://carbonmod.gg/assets/content/blank.map";

	public static bool NomapEnabled => CommandLineEx.GetArgumentExists("+carbon.nomap");
	public static bool ClientEnabled => CommandLineEx.GetArgumentExists("+carbon.client");

	public static void Init()
	{
		if (NomapEnabled)
		{
			ConVar.Server.levelurl = MAP_URL;
			ProcessConVars();
		}

		if (ClientEnabled)
		{
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
