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
#if !MINIMAL
	[CommandVar("recycletick", help: "Configures the recycling ticks speed.", saved: true)]
	[AuthLevel(2)]
	public float RecycleTick = -1;

	[CommandVar("researchduration", help: "The duration of waiting whenever researching blueprints.", saved: true)]
	[AuthLevel(2)]
	public float ResearchDuration = -1;

	[CommandVar("vendingmachinebuyduration", help: "The duration of transaction delay when buying from vending machines.", saved: true)]
	[AuthLevel(2)]
	public float VendingMachineBuyDuration = -1;

	[CommandVar("craftingspeedmultiplier", help: "The time multiplier of crafting items.", saved: true)]
	[AuthLevel(2)]
	public float CraftingSpeedMultiplier = -1;

	[CommandVar("mixingspeedmultiplier", help: "The speed multiplier of mixing table crafts.", saved: true)]
	[AuthLevel(2)]
	public float MixingSpeedMultiplier = -1;

	[CommandVar("exacavatorresourcetickrate", help: "Excavator resource tick rate.", saved: true)]
	[AuthLevel(2)]
	public float ExcavatorResourceTickRate = -1;

	[CommandVar("excavatortimeforfullresources", help: "Excavator time for processing full resources.", saved: true)]
	[AuthLevel(2)]
	public float ExcavatorTimeForFullResources = -1;

	[CommandVar("excavatorbeltspeedmax", help: "Excavator belt maximum speed.", saved: true)]
	[AuthLevel(2)]
	public float ExcavatorBeltSpeedMax = -1;

	[CommandVar("defaultserverchatname", help: "Default server chat name.", saved: true)]
	[AuthLevel(2)]
	public string DefaultServerChatName = "-1";

	[CommandVar("defaultserverchatcolor", help: "Default server chat message name color.", saved: true)]
	[AuthLevel(2)]
	public string DefaultServerChatColor = "-1";

	[CommandVar("defaultserverchatid", help: "Default server chat icon SteamID.", saved: true)]
	[AuthLevel(2)]
	public long DefaultServerChatId = -1;
#endif
}
