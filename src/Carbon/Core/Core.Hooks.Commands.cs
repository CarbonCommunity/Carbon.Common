/*
 *
 * Copyright (c) 2022-2023 Carbon Community
 * All rights reserved.
 *
 */

using API.Commands;
using ConVar;
using Command = API.Commands.Command;

namespace Carbon.Core;
#pragma warning disable IDE0051

public partial class CorePlugin : CarbonPlugin
{
	public static object IOnPlayerCommand(BasePlayer player, string message, Command.Prefix prefix)
	{
		if (Community.Runtime == null) return Cache.True;

		try
		{
			var fullString = message[1..];

			if (string.IsNullOrEmpty(fullString))
			{
				return Cache.False;
			}

			using var split = TemporaryArray<string>.New(fullString.Split(ConsoleArgEx.CommandSpacing, StringSplitOptions.RemoveEmptyEntries));
			var command = split.Get(0).Trim();
			var args = split.Length > 1 ? Facepunch.Extend.StringExtensions.SplitQuotesStrings(fullString[(command.Length + 1)..]) : _emptyStringArray;

			// OnUserCommand
			if (HookCaller.CallStaticHook(1077563450, player, command, args) != null)
			{
				return Cache.False;
			}

			// OnUserCommand
			if (HookCaller.CallStaticHook(2623980812, player.AsIPlayer(), command, args) != null)
			{
				return Cache.False;
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
				return Cache.False;
			}

			if (player.Connection.authLevel >= prefix.SuggestionAuthLevel && Suggestions.Lookup(command, Community.Runtime.CommandManager.Chat.Select(x => x.Name)) is var result && result is { Confidence: <= 5 })
			{
				player.ChatMessage($"<color=orange>Unknown command:</color> {message}\n<size=12s>Suggesting: /{result.Result}</size>");
			}
			else
			{
				player.ChatMessage($"<color=orange>Unknown command:</color> {message}");
			}

			if (HookCaller.CallStaticHook(554444971, player, command, args) != null)
			{
				return Cache.False;
			}
		}
		catch (Exception ex) { Logger.Error($"Failed IOnPlayerCommand.", ex); }

		return Cache.False;
	}
	internal static object IOnServerCommand(ConsoleSystem.Arg arg)
	{
		if (arg != null && arg.cmd != null && arg.Player() != null && arg.cmd.FullName == "chat.say") return null;

		// OnServerCommand
		return HookCaller.CallStaticHook(3282920085, arg) != null ? Cache.True : null;
	}
	public static object IOnPlayerChat(ulong playerId, string playerName, string message, Chat.ChatChannel channel, BasePlayer basePlayer)
	{
		if (string.IsNullOrEmpty(message) || message.Equals("text"))
		{
			return Cache.True;
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

		return hook1 ?? hook2;
	}

	internal static object IOnRconInitialize()
	{
		return !Community.Runtime.Config.Rcon ? Cache.False : null;
	}
	internal static object IOnRunCommandLine()
	{
		foreach (var @switch in Facepunch.CommandLine.GetSwitches())
		{
			var value = @switch.Value;

			if (value == "")
			{
				value = "1";
			}

			var key = @switch.Key.Substring(1);
			var options = ConsoleSystem.Option.Unrestricted;
			options.PrintOutput = false;

			ConsoleSystem.Run(options, key, value);
		}

		return Cache.False;
	}
}
