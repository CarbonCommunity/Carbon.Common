using API.Commands;

/*
 *
 * Copyright (c) 2022-2023 Carbon Community
 * All rights reserved.
 *
 */

namespace Carbon.Core;

public partial class CorePlugin : CarbonPlugin
{
	[ConsoleCommand("find", "Searches through Carbon-processed console commands.")]
	[AuthLevel(2)]
	private void Find(ConsoleSystem.Arg arg)
	{
		using var body = new StringTable("Console Command", "Value", "Help");
		var filter = arg.Args != null && arg.Args.Length > 0 ? arg.GetString(0) : null;

		foreach (var command in Community.Runtime.CommandManager.ClientConsole)
		{
			if (command.HasFlag(CommandFlags.Hidden) || (!string.IsNullOrEmpty(filter) && !command.Name.Contains(filter))) continue;

			var value = " ";

			if (command.Token != null)
			{
				if (command.Token is FieldInfo field) value = field.GetValue(command.Reference)?.ToString();
				else if (command.Token is PropertyInfo property) value = property.GetValue(command.Reference)?.ToString();
			}

			if (command.HasFlag(CommandFlags.Protected))
			{
				value = new string('*', value.Length);
			}

			body.AddRow($" {command.Name}", value, command.Help);
		}

		arg.ReplyWith(body.Write(StringTable.FormatTypes.None));
	}

	[ConsoleCommand("findchat", "Searches through Carbon-processed chat commands.")]
	[AuthLevel(2)]
	private void FindChat(ConsoleSystem.Arg arg)
	{
		using var body = new StringTable("Chat Command", "Help");
		var filter = arg.Args != null && arg.Args.Length > 0 ? arg.GetString(0) : null;

		foreach (var command in Community.Runtime.CommandManager.Chat)
		{
			if (command.HasFlag(CommandFlags.Hidden) || (!string.IsNullOrEmpty(filter) && !command.Name.Contains(filter))) continue;

			body.AddRow($" {command.Name}", command.Help);
		}

		arg.ReplyWith(body.Write(StringTable.FormatTypes.None));
	}
}
