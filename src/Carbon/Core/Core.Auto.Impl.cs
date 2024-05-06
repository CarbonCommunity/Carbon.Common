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
	internal object IRecyclerThinkSpeed(Recycler recycler)
	{
		if (recycler.IsSafezoneRecycler())
		{
			if (SafezoneRecycleTick != -1)
			{
				return SafezoneRecycleTick;
			}

			return null;
		}

		if (RecycleTick != -1)
		{
			return RecycleTick;
		}

		return null;
	}

	[Conditional("!MINIMAL")]
	internal object ICraftDurationMultiplier()
	{
		if (CraftingSpeedMultiplier != -1)
		{
			return CraftingSpeedMultiplier;
		}

		return null;
	}

	[Conditional("!MINIMAL")]
	internal object IMixingSpeedMultiplier(MixingTable table, float originalValue)
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
	internal object IVendingBuyDuration()
	{
		if (VendingMachineBuyDuration != -1)
		{
			return VendingMachineBuyDuration;
		}

		return null;
	}

	[Conditional("!MINIMAL")]
	internal void IOnExcavatorInit(ExcavatorArm arm)
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
	internal object IOvenSmeltSpeedMultiplier(BaseOven oven)
	{
		if (OvenBlacklistCache == null)
		{
			return null;
		}

		if (Enumerable.Contains(OvenBlacklistCache, oven.ShortPrefabName) ||
		    Enumerable.Contains(OvenBlacklistCache, oven.GetType().Name))
		{
			if (OvenBlacklistSpeedMultiplier != -1)
			{
				return OvenBlacklistSpeedMultiplier;
			}

			return null;
		}

		if (OvenSpeedMultiplier != -1)
		{
			return OvenSpeedMultiplier;
		}

		return null;
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
