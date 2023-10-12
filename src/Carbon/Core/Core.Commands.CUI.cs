using API.Assembly;
using API.Commands;
using Carbon.Base.Interfaces;
using Newtonsoft.Json;
using Oxide.Game.Rust.Cui;

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
	[ConsoleCommand("wipeui", "Clears the entire CUI containers and their elements from the caller's client.")]
	[AuthLevel(2)]
	private void WipeUI(ConsoleSystem.Arg arg)
	{
		if (arg.Player() is BasePlayer player)
		{
			CuiHelper.DestroyActivePanelList(player);
		}
		else
		{
			arg.ReplyWith($"This command can only be called from a client.");
		}
	}
}
