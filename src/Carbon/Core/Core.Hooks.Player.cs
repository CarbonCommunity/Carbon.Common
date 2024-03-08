/*
 *
 * Copyright (c) 2022-2023 Carbon Community
 * All rights reserved.
 *
 */

using Connection = Network.Connection;

namespace Carbon.Core;
#pragma warning disable IDE0051

public partial class CorePlugin : CarbonPlugin
{
	internal static object IOnBasePlayerAttacked(BasePlayer basePlayer, HitInfo hitInfo)
	{
		if (!Community.IsServerInitialized || _isPlayerTakingDamage || basePlayer == null || hitInfo == null || basePlayer.IsDead() || basePlayer is NPCPlayer)
		{
			return Cache.Null.Value;
		}

		// OnEntityTakeDamage
		if (HookCaller.CallStaticHook(2713007450, basePlayer, hitInfo) != null)
		{
			return Cache.True;
		}

		_isPlayerTakingDamage = true;

		try
		{
			basePlayer.OnAttacked(hitInfo);
		}
		finally
		{
			_isPlayerTakingDamage = false;
		}

		return Cache.True;
	}
	internal static object IOnBasePlayerHurt(BasePlayer basePlayer, HitInfo hitInfo)
	{
		if (!_isPlayerTakingDamage)
		{
			// OnEntityTakeDamage
			return HookCaller.CallStaticHook(2713007450, basePlayer, hitInfo);
		}

		return Cache.Null.Value;
	}
	internal static object IOnBaseCombatEntityHurt(BaseCombatEntity entity, HitInfo hitInfo)
	{
		if (entity is not BasePlayer)
		{
			// OnEntityTakeDamage
			return HookCaller.CallStaticHook(2713007450, entity, hitInfo);
		}

		return Cache.Null.Value;
	}
	internal static object ICanPickupEntity(BasePlayer basePlayer, DoorCloser entity)
	{
		// CanPickupEntity
		if (HookCaller.CallStaticHook(385185486, basePlayer, entity) is bool result)
		{
			return result;
		}

		return Cache.Null.Value;
	}

	private void OnPlayerSetInfo(Connection connection, string key, string val)
	{
		switch (key)
		{
			case "global.language":
				lang.SetLanguage(val, connection.userid.ToString());

				if (connection.player is BasePlayer player)
				{
					// OnPlayerLanguageChanged
					HookCaller.CallStaticHook(1960580409, player, val);
					HookCaller.CallStaticHook(1960580409, player.AsIPlayer(), val);
				}
				break;
		}
	}
}
