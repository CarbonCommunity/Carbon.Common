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
	internal static object IOnPlayerConnected(BasePlayer player)
	{
		var core = Singleton<CorePlugin>();

		core.lang.SetLanguage(player.net.connection.info.GetString("global.language", "en"), player.UserIDString);
		player.SendEntitySnapshot(CommunityEntity.ServerInstance);

		core.permission.RefreshUser(player);

		// OnPlayerConnected
		HookCaller.CallStaticHook(2848347654, player);

		// OnUserConnected
		HookCaller.CallStaticHook(1253832323, player.AsIPlayer());

		return null;
	}
	internal static object IOnUserApprove(Connection connection)
	{
		var username = connection.username;
		var text = connection.userid.ToString();
		var obj = Regex.Replace(connection.ipaddress, Player.ipPattern, string.Empty);

		// CanClientLogin
		var canClient = HookCaller.CallStaticHook(3081308902, connection);

		// CanUserLogin
		var canUser = HookCaller.CallStaticHook(1045800646, username, text, obj);

		var obj4 = (canClient == null) ? canUser : canClient;
		if (obj4 is string || (obj4 is bool obj4Value && !obj4Value))
		{
			ConnectionAuth.Reject(connection, (obj4 is string) ? obj4.ToString() : "Connection was rejected", null);
			return Cache.True;
		}

		// OnUserApprove
		if (HookCaller.CallStaticHook(2666432541, connection) != null)
			// OnUserApproved
			return HookCaller.CallStaticHook(1330253375, username, text, obj);

		return null;
	}
	internal static object IOnPlayerBanned(Connection connection, AuthResponse status)
	{
		// OnPlayerBanned
		HookCaller.CallStaticHook(140408349, connection, status.ToString());

		return null;
	}

	private void OnPlayerKicked(BasePlayer basePlayer, string reason)
	{
		// OnUserKicked
		HookCaller.CallStaticHook(3928650942, basePlayer.AsIPlayer(), reason);
	}
	private object OnPlayerRespawn(BasePlayer basePlayer)
	{
		// OnUserRespawn
		return HookCaller.CallStaticHook(3398288406, basePlayer.AsIPlayer());
	}
	private void OnPlayerRespawned(BasePlayer basePlayer)
	{
		// OnUserRespawned
		HookCaller.CallStaticHook(960522643, basePlayer.AsIPlayer());
	}
	private void OnClientAuth(Connection connection)
	{
		connection.username = Regex.Replace(connection.username, @"<[^>]*>", string.Empty);
	}
}
