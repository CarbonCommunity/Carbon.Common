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
		return null;

		//if (recycler.IsSafezoneRecycler())
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
	internal object ICraftDurationMultiplier(ItemBlueprint bp, float workbenchLevel, bool isInTutorial)
	{
		if (isInTutorial)
		{
			return null;
		}

		var workbench = workbenchLevel - bp.workbenchLevelRequired;

		return workbench switch
		{
			0 when CraftingSpeedMultiplierNoWB != -1 => CraftingSpeedMultiplierNoWB,
			1 when CraftingSpeedMultiplierWB1 != -1 => CraftingSpeedMultiplierWB1,
			2 when CraftingSpeedMultiplierWB2 != -1 => CraftingSpeedMultiplierWB2,
			3 when CraftingSpeedMultiplierWB3 != -1 => CraftingSpeedMultiplierWB3,
			_ => null
		};
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
