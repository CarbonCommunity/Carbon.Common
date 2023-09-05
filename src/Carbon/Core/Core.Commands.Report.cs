/*
 *
 * Copyright (c) 2022-2023 Carbon Community 
 * All rights reserved.
 *
 */

namespace Carbon.Core;

public partial class CorePlugin : CarbonPlugin
{
	[ConsoleCommand("report", "Reloads all current plugins, and returns a report based on them at the output path.")]
	[AuthLevel(2)]
	private void Report(ConsoleSystem.Arg arg)
	{
		new Carbon.Components.Report().Init();
	}
}
