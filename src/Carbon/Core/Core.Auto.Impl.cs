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
	#region Implementation

	[Conditional("!MINIMAL")]
	private object IRecyclerThinkSpeed(Recycler recycler)
	{
		if (recycler.IsSafezoneRecycler() && SafezoneRecycleTick != -1)
		{
			return SafezoneRecycleTick;
		}

		if (RecycleTick != -1)
		{
			return RecycleTick;
		}

		return null;
	}
	[Conditional("!MINIMAL")]
	private object ICraftDurationMultiplier()
	{
		if (CraftingSpeedMultiplier != -1)
		{
			return CraftingSpeedMultiplier;
		}

		return null;
	}
	[Conditional("!MINIMAL")]
	private object IMixingSpeedMultiplier(MixingTable table, float originalValue)
	{
		if (MixingSpeedMultiplier == -1 || table.currentRecipe == null)
		{
			return null;
		}

		if (originalValue == table.currentRecipe.MixingDuration * table.currentQuantity)
		{
			return MixingSpeedMultiplier;
		}

		return null;
	}
	[Conditional("!MINIMAL")]
	private object IVendingBuyDuration()
	{
		if (VendingMachineBuyDuration != -1)
		{
			return VendingMachineBuyDuration;
		}

		return null;
	}
	[Conditional("!MINIMAL")]
	private void IOnExcavatorInit(ExcavatorArm arm)
	{
		if (ExcavatorResourceTickRate != -1)
		{
			arm.resourceProductionTickRate = ExcavatorResourceTickRate;
		}

		if (ExcavatorTimeForFullResources != -1)
		{
			arm.timeForFullResources = ExcavatorTimeForFullResources;
		}

		if (ExcavatorBeltSpeedMax != -1)
		{
			arm.beltSpeedMax = ExcavatorBeltSpeedMax;
		}
	}
	[Conditional("!MINIMAL")]
	private void OnItemResearch(ResearchTable table, Item targetItem, BasePlayer player)
	{
		if (ResearchDuration != -1)
		{
			table.researchDuration = ResearchDuration;
		}
	}

	#endregion
#endif
}
