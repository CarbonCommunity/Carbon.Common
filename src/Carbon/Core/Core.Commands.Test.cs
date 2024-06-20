/*
 *
 * Copyright (c) 2024 Carbon Community
 * All rights reserved.
 *
 */

using Carbon.Test;

namespace Carbon.Core;

public partial class CorePlugin : CarbonPlugin
{
	[ConsoleCommand("tests", "Prints all currently queued up tests ready to be executed.")]
	[AuthLevel(2)]
	private void TestBeds(ConsoleSystem.Arg arg)
	{
		using var table = new StringTable(string.Empty, "Bed", "Tests");

		foreach (var bed in Integrations.Banks)
		{
			table.AddRow(string.Empty, bed.Context, $"{bed.Count:n0}");
		}

		arg.ReplyWith(table.ToStringMinimal());
	}

	[ConsoleCommand("testrun", "Executes all Test Beds that are currently queued up.")]
	[AuthLevel(2)]
	private void TestRun(ConsoleSystem.Arg arg)
	{
		var delay = arg.GetFloat(0, 0.1f);

		if (delay < 0)
		{
			arg.ReplyWith("Delay must be above zero.");
			return;
		}

		Integrations.Run(delay);
	}

	[ConsoleCommand("testclear", "Clears all Test Beds that are currently queued up.")]
	[AuthLevel(2)]
	private void TestClear(ConsoleSystem.Arg arg)
	{
		Integrations.Clear();
	}
}
