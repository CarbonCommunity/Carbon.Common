/*
 *
 * Copyright (c) 2022-2023 Carbon Community
 * All rights reserved.
 *
 */

namespace Carbon.Core;
#pragma warning disable IDE0051

public partial class CorePlugin : CarbonPlugin
{
	internal const string _blankZero = "0";
	internal const string _blankUnnamed = "Unnamed";

	private void OnServerUserSet(ulong steamId, ServerUsers.UserGroup group, string playerName, string reason, long expiry)
	{
		if (Community.IsServerInitialized && group == ServerUsers.UserGroup.Banned)
		{
			var playerId = steamId.ToString();
			var player = BasePlayer.FindByID(steamId)?.AsIPlayer();

			// OnPlayerBanned
			HookCaller.CallStaticHook(2433979267, playerName, steamId, player == null ? _blankZero : player.Address, reason, expiry);

			// OnUserBanned
			HookCaller.CallStaticHook(274222292, playerName, playerId, player == null ? _blankZero : player.Address, reason, expiry);
		}
	}
	private void OnServerUserRemove(ulong steamId)
	{
		if (Community.IsServerInitialized &&
		    ServerUsers.users.ContainsKey(steamId) &&
		    ServerUsers.users[steamId].group == ServerUsers.UserGroup.Banned)
		{
			var playerId = steamId.ToString();
			var player = BasePlayer.FindByID(steamId)?.AsIPlayer();

			// OnPlayerUnbanned
			HookCaller.CallStaticHook(3462729840, player == null || string.IsNullOrEmpty(player.Name) ? _blankUnnamed : player.Name, playerId, player == null || string.IsNullOrEmpty(player.Address) ? _blankZero : player.Address);

			// OnUserUnbanned
			HookCaller.CallStaticHook(4090556101, player == null || string.IsNullOrEmpty(player.Name) ? _blankUnnamed : player.Name, playerId, player == null || string.IsNullOrEmpty(player.Address) ? _blankZero : player.Address);
		}
	}
}
