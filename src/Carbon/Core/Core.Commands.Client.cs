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

	private bool _oldRecoil;

	[CommandVar("oldrecoil", "Used by Carbon (client) servers. Any Carbon client that joins will use old properties version of recoil.")]
	[AuthLevel(2)]
	internal bool OldRecoil
	{
		get
		{
			return _oldRecoil;
		}
		set
		{
			if (_oldRecoil == value) return;
			_oldRecoil = value;

			Community.Runtime.CarbonClientManager.NetworkOldRecoil(value);
		}
	}
}
