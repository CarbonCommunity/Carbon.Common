/*
 *
 * Copyright (c) 2022-2023 Carbon Community 
 * All rights reserved.
 *
 */

namespace Carbon.Core;

public partial class CorePlugin : CarbonPlugin
{
	public void OnTerrainInitialized()
	{
		Carbon.Client.Client.TerrainPostprocess();
	}
}
