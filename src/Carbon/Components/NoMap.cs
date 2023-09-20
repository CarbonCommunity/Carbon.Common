/*
 *
 * Copyright (c) 2022-2023 Carbon Community 
 * All rights reserved.
 *
 */

namespace Carbon.Client;

public struct NoMap
{
	public const string MAP_URL = "https://carbonmod.gg/assets/content/blank.map";

	public static bool Enabled => CommandLineEx.GetArgumentExists("+nomap");

	public static void Init()
	{
		if (!Enabled)
		{
			return;
		}

		ConVar.Server.levelurl = MAP_URL;

		ProcessConVars();
	}
	public static void TerrainPostprocess()
	{
		if (!Enabled)
		{
			return;
		}

		TerrainMeta.Collider.enabled = false;
	}
	public static void ProcessConVars()
	{
		ConVar.Spawn.max_density = 0;
	}
}
