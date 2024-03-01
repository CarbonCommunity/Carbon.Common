/*
 *
 * Copyright (c) 2022-2023 Carbon Community
 * All rights reserved.
 *
 */

using API.Commands;
using ConVar;

namespace Carbon.Core;
#pragma warning disable IDE0051

public partial class CorePlugin : CarbonPlugin
{
	private object IOnPlayerCommand(BasePlayer player, string message)
	{
		if (Community.Runtime == null) return true;

		try
		{
			var fullString = message[1..];

			if (string.IsNullOrEmpty(fullString))
			{
				return false;
			}

			var split = fullString.Split(ConsoleArgEx.CommandSpacing, StringSplitOptions.RemoveEmptyEntries);
			var command = split[0].Trim();
			var args = split.Length > 1 ? Facepunch.Extend.StringExtensions.SplitQuotesStrings(fullString[(command.Length + 1)..]) : _emptyStringArray;
			Array.Clear(split, 0, split.Length);

			// OnUserCommand
			if (HookCaller.CallStaticHook(1077563450, player, command, args) != null)
			{
				return false;
			}

			// OnUserCommand
			if (HookCaller.CallStaticHook(2623980812, player.AsIPlayer(), command, args) != null)
			{
				return false;
			}

			if (Community.Runtime.CommandManager.Contains(Community.Runtime.CommandManager.Chat, command, out var cmd))
			{
				var commandArgs = Facepunch.Pool.Get<PlayerArgs>();
				commandArgs.Type = cmd.Type;
				commandArgs.Arguments = args;
				commandArgs.Player = player;
				commandArgs.PrintOutput = true;

				Community.Runtime.CommandManager.Execute(cmd, commandArgs);

				commandArgs.Dispose();
				Facepunch.Pool.Free(ref commandArgs);
				return false;
			}

			if (HookCaller.CallStaticHook(554444971, player, command, args) != null)
			{
				return false;
			}
		}
		catch (Exception ex) { Logger.Error($"Failed IOnPlayerCommand.", ex); }

		return true;
	}
	private object IOnServerCommand(ConsoleSystem.Arg arg)
	{
		if (arg != null && arg.cmd != null && arg.Player() != null && arg.cmd.FullName == "chat.say") return null;

		// OnServerCommand
		if (HookCaller.CallStaticHook(3282920085, arg) == null)
		{
			return null;
		}

		return true;
	}
	private object IOnPlayerChat(ulong playerId, string playerName, string message, Chat.ChatChannel channel, BasePlayer basePlayer)
	{
		if (string.IsNullOrEmpty(message) || message.Equals("text"))
		{
			return true;
		}
		if (basePlayer == null || !basePlayer.IsConnected)
		{
			// OnPlayerOfflineChat
			return HookCaller.CallStaticHook(3391949391, playerId, playerName, message, channel);
		}

		// OnPlayerChat
		var hook1 = HookCaller.CallStaticHook(735197859, basePlayer, message, channel);

		// OnUserChat
		var hook2 = HookCaller.CallStaticHook(2410402155, basePlayer.AsIPlayer(), message);

		if (hook1 != null)
		{
			return hook1;
		}

		return hook2;
	}
}
