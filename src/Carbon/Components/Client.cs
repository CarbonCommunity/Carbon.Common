/*
 *
 * Copyright (c) 2022-2024 Carbon Community
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

		if (Config.Enabled)
		{
			Logger.Log($" C4C: Carbon Client ready.");

			if (Analytic.Enabled)
			{
				Analytic.Include("nomap", Config.Environment.NoMap);
				Analytic.Include("oldrecoil", Config.Client.UseOldRecoil);
				Analytic.Include("clientgravity", Config.Client.ClientGravity.ToString("0.0"));
				Analytic.Send("carbon_client_init");
			}
		}
		else
		{
			Logger.Log($" C4C: Carbon Client disabled.");
		}
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
