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
	private object IOnLoseCondition(Item item, float amount)
	{
		var args = new object[] { item, amount };
		HookCaller.CallStaticHook(3503014187, args, keepArgs: true);
		amount = (float)args[1];

		var condition = item.condition;
		item.condition -= amount;
		if (item.condition <= 0f && item.condition < condition)
		{
			item.OnBroken();
		}

		return true;
	}
}
