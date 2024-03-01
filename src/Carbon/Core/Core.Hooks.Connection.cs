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
	private void IOnPlayerConnected(BasePlayer player)
	{
		lang.SetLanguage(player.net.connection.info.GetString("global.language", "en"), player.UserIDString);
		player.SendEntitySnapshot(CommunityEntity.ServerInstance);

		permission.RefreshUser(player);

		// OnPlayerConnected
		HookCaller.CallStaticHook(3704844088, player);

		// OnUserConnected
		HookCaller.CallStaticHook(1971459992, player.AsIPlayer());

	}

	private object IOnUserApprove(Connection connection)
	{
		var username = connection.username;
		var text = connection.userid.ToString();
		var obj = Regex.Replace(connection.ipaddress, global::Oxide.Game.Rust.Libraries.Player.ipPattern, string.Empty);

		// CanClientLogin
		var canClient = HookCaller.CallStaticHook(351619588, connection);

		// CanUserLogin
		var canUser = HookCaller.CallStaticHook(459292092, username, text, obj);

		var obj4 = (canClient == null) ? canUser : canClient;
		if (obj4 is string || (obj4 is bool obj4Value && !obj4Value))
		{
			ConnectionAuth.Reject(connection, (obj4 is string) ? obj4.ToString() : "Connection was rejected", null);
			return true;
		}

		Community.Runtime.CarbonClientManager.OnConnected(connection);

		// OnUserApprove
		if (HookCaller.CallStaticHook(1855397793, connection) != null)
			// OnUserApproved
			return HookCaller.CallStaticHook(2225250284, username, text, obj);

		return null;
	}
	private void OnPlayerKicked(BasePlayer basePlayer, string reason)
	{
		// OnUserKicked
		HookCaller.CallStaticHook(3026194467, basePlayer.AsIPlayer(), reason);
	}
	private object OnPlayerRespawn(BasePlayer basePlayer)
	{
		// OnUserRespawn
		return HookCaller.CallStaticHook(2545052102, basePlayer.AsIPlayer());
	}
	private void OnPlayerRespawned(BasePlayer basePlayer)
	{
		// OnUserRespawned
		HookCaller.CallStaticHook(3161392945, basePlayer.AsIPlayer());
	}
	private void IOnPlayerBanned(Connection connection, AuthResponse status)
	{
		// OnPlayerBanned
		HookCaller.CallStaticHook(2433979267, connection, status.ToString());
	}
	private void OnClientAuth(Connection connection)
	{
		connection.username = Regex.Replace(connection.username, @"<[^>]*>", string.Empty);
	}
}
