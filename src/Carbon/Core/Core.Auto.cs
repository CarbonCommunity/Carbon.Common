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

	#region Modded

	[CarbonAutoModdedVar("recycletick", help: "Configures the recycling ticks speed.")]
	[AuthLevel(2)]
	public float RecycleTick = -1;

	[CarbonAutoModdedVar("safezonerecycletick", help: "Configures the SafeZone recycling ticks speed.")]
	[AuthLevel(2)]
	public float SafezoneRecycleTick = -1;

	[CarbonAutoModdedVar("researchduration", help: "The duration of waiting whenever researching blueprints.")]
	[AuthLevel(2)]
	public float ResearchDuration = -1;

	[CarbonAutoModdedVar("vendingmachinebuyduration", help: "The duration of transaction delay when buying from vending machines.")]
	[AuthLevel(2)]
	public float VendingMachineBuyDuration = -1;

	[CarbonAutoModdedVar("craftingspeedmultiplier", help: "The time multiplier of crafting items.")]
	[AuthLevel(2)]
	public float CraftingSpeedMultiplier = -1;

	[CarbonAutoModdedVar("mixingspeedmultiplier", help: "The speed multiplier of mixing table crafts.")]
	[AuthLevel(2)]
	public float MixingSpeedMultiplier = -1;

	[CarbonAutoModdedVar("exacavatorresourcetickrate", help: "Excavator resource tick rate.")]
	[AuthLevel(2)]
	public float ExcavatorResourceTickRate = -1;

	[CarbonAutoModdedVar("excavatortimeforfullresources", help: "Excavator time for processing full resources.")]
	[AuthLevel(2)]
	public float ExcavatorTimeForFullResources = -1;

	[CarbonAutoModdedVar("excavatorbeltspeedmax", help: "Excavator belt maximum speed.")]
	[AuthLevel(2)]
	public float ExcavatorBeltSpeedMax = -1;

	[CarbonAutoModdedVar("ovenspeedmultiplier", help: "The burning speed of ovens.")]
	[AuthLevel(2)]
	public float OvenSpeedMultiplier = -1;

	[CarbonAutoModdedVar("ovenblacklistspeedmultiplier", help: "The burning speed of blacklisted ovens.")]
	[AuthLevel(2)]
	public float OvenBlacklistSpeedMultiplier = -1;

	#endregion

	#region Vanilla

	public IEnumerable<string> OvenBlacklistCache;

	private string _ovenBlacklist = "furnace,bbq.static,furnace.large";

	[CarbonAutoVar("ovenblacklist", help: "Blacklisted oven entity prefabs.")]
	[AuthLevel(2)]
	public string OvenBlacklist
	{
		get => _ovenBlacklist;
		set
		{
			if (_ovenBlacklist != value)
			{
				OvenBlacklistCache = value.ToSplitEnumerable(',');
			}

			_ovenBlacklist = value;
		}
	}

	[CarbonAutoVar("defaultserverchatname", help: "Default server chat name.")]
	[AuthLevel(2)]
	public string DefaultServerChatName = "-1";

	[CarbonAutoVar("defaultserverchatcolor", help: "Default server chat message name color.")]
	[AuthLevel(2)]
	public string DefaultServerChatColor = "-1";

	[CarbonAutoVar("defaultserverchatid", help: "Default server chat icon SteamID.")]
	[AuthLevel(2)]
	public long DefaultServerChatId = -1;

	#endregion
#endif
}
