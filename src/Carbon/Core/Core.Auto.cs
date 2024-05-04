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
	[CommandVar("isforcemodded", help: "Is the server forcefully set to modded due to options affecting significant gameplay changes in Carbon Auto?")]
	[AuthLevel(2)]
	public bool IsForceModded { get { return CarbonAuto.Singleton.IsForceModded(); } set { } }

	[CarbonAutoVar("recycletick", help: "Configures the recycling ticks speed.", forceModded: true)]
	[AuthLevel(2)]
	public float RecycleTick = -1;

	[CarbonAutoVar("safezonerecycletick", help: "Configures the SafeZone recycling ticks speed.", forceModded: true)]
	[AuthLevel(2)]
	public float SafezoneRecycleTick = -1;

	[CarbonAutoVar("researchduration", help: "The duration of waiting whenever researching blueprints.", forceModded: true)]
	[AuthLevel(2)]
	public float ResearchDuration = -1;

	[CarbonAutoVar("vendingmachinebuyduration", help: "The duration of transaction delay when buying from vending machines.", forceModded: true)]
	[AuthLevel(2)]
	public float VendingMachineBuyDuration = -1;

	[CarbonAutoVar("craftingspeedmultiplier", help: "The time multiplier of crafting items.", forceModded: true)]
	[AuthLevel(2)]
	public float CraftingSpeedMultiplier = -1;

	[CarbonAutoVar("mixingspeedmultiplier", help: "The speed multiplier of mixing table crafts.", forceModded: true)]
	[AuthLevel(2)]
	public float MixingSpeedMultiplier = -1;

	[CarbonAutoVar("exacavatorresourcetickrate", help: "Excavator resource tick rate.", forceModded: true)]
	[AuthLevel(2)]
	public float ExcavatorResourceTickRate = -1;

	[CarbonAutoVar("excavatortimeforfullresources", help: "Excavator time for processing full resources.", forceModded: true)]
	[AuthLevel(2)]
	public float ExcavatorTimeForFullResources = -1;

	[CarbonAutoVar("excavatorbeltspeedmax", help: "Excavator belt maximum speed.", forceModded: true)]
	[AuthLevel(2)]
	public float ExcavatorBeltSpeedMax = -1;

	[CarbonAutoVar("defaultserverchatname", help: "Default server chat name.")]
	[AuthLevel(2)]
	public string DefaultServerChatName = "-1";

	[CarbonAutoVar("defaultserverchatcolor", help: "Default server chat message name color.")]
	[AuthLevel(2)]
	public string DefaultServerChatColor = "-1";

	[CarbonAutoVar("defaultserverchatid", help: "Default server chat icon SteamID.")]
	[AuthLevel(2)]
	public long DefaultServerChatId = -1;
#endif
}
