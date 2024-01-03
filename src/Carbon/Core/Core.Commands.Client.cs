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

	public struct RecoilOverrider
	{
		internal static Dictionary<string, RecoilCache> _recoilCache = new();

		public class RecoilCache
		{
			public bool WasOldApplied;
			public RecoilProperties NewRecoil;
			public RecoilProperties OldRecoil;
		}

		public static void Initialize()
		{
			foreach (var item in ItemManager.GetItemDefinitions())
			{
				foreach (var mod in item.itemMods)
				{
					if (mod is ItemModEntity modEntity)
					{
						var entity = modEntity.entityPrefab.GetEntity();

						if (entity is BaseProjectile projectile)
						{
							CacheRecoil(projectile);
						}
					}
				}
			}
		}

		public static void CacheRecoil(BaseProjectile weapon)
		{
			if (weapon.recoil == null)
			{
				return;
			}

			if (!_recoilCache.TryGetValue(weapon.PrefabName, out var cache))
			{
				_recoilCache.Add(weapon.PrefabName, new RecoilCache
				{
					WasOldApplied = false,
					NewRecoil = weapon.recoil.newRecoilOverride,
					OldRecoil = weapon.recoil
				});
			}
		}

		public static void ApplyOldRecoil()
		{
			foreach (var recoil in _recoilCache)
			{
				if (recoil.Value.WasOldApplied)
				{
					continue;
				}

				recoil.Value.OldRecoil.newRecoilOverride = null;
				recoil.Value.WasOldApplied = true;
			}
		}

		public static void RestoreOriginals()
		{
			foreach (var recoil in _recoilCache)
			{
				if (!recoil.Value.WasOldApplied)
				{
					continue;
				}

				recoil.Value.OldRecoil.newRecoilOverride = recoil.Value.NewRecoil;
				recoil.Value.WasOldApplied = false;
			}
		}
	}

	[CommandVar("oldrecoil", "Used by Carbon (client) servers. Any Carbon client that joins will use old properties version of recoil.")]
	[AuthLevel(2)]
	internal bool OldRecoil
	{
		get
		{
			return Community.Runtime.ClientConfig.Client.UseOldRecoil;
		}
		set
		{
			if (Community.Runtime.ClientConfig.Client.UseOldRecoil == value) return;
			Community.Runtime.ClientConfig.Client.UseOldRecoil = value;

			if (Community.Runtime.ClientConfig.Client.UseOldRecoil)
			{
				RecoilOverrider.ApplyOldRecoil();
			}
			else
			{
				RecoilOverrider.RestoreOriginals();
			}

			Community.Runtime.CarbonClientManager.NetworkClientConfiguration(Community.Runtime.ClientConfig.Client);
			Community.Runtime.SaveClientConfig();
		}
	}

	[CommandVar("clientgravity", "Used by Carbon (client) servers. Any Carbon client that joins will use this value for gravity.")]
	[AuthLevel(2)]
	internal float ClientGravity
	{
		get
		{
			return Community.Runtime.ClientConfig.Client.ClientGravity;
		}
		set
		{
			if (Community.Runtime.ClientConfig.Client.ClientGravity == value) return;
			Community.Runtime.ClientConfig.Client.ClientGravity = value;

			Community.Runtime.CarbonClientManager.NetworkClientConfiguration(Community.Runtime.ClientConfig.Client);
			Community.Runtime.SaveClientConfig();
		}
	}
}
