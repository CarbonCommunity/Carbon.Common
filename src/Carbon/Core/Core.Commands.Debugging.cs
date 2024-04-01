/*
 *
 * Copyright (c) 2022-2023 Carbon Community
 * All rights reserved.
 *
 */

namespace Carbon.Core;

public partial class CorePlugin : CarbonPlugin
{
#if DEBUG
	[CommandVar("scriptdebugorigin", "[For debugging purposes] Overrides the script directory to this value so remote debugging is possible.")]
	[AuthLevel(2)]
	private string ScriptDebuggingOrigin { get { return Community.Runtime.Config.Debugging.ScriptDebuggingOrigin; } set { Community.Runtime.Config.Debugging.ScriptDebuggingOrigin = value; } }
#endif

	[CommandVar("hooklsthreshold", "The threshold value used by the hook caller to determine what minimum time is considered as a server lag spike. Defaults to 1000ms.")]
	[AuthLevel(2)]
	private int HookLagSpikeThreshold { get { return Community.Runtime.Config.Debugging.HookLagSpikeThreshold; } set { Community.Runtime.Config.Debugging.HookLagSpikeThreshold = value.Clamp(100, 10000); } }

	[ConsoleCommand("resethooks", "Clears all progress on all of the current hooks (hook time, fires, memory usage and lag spikes).")]
	[AuthLevel(2)]
	private void ResetHooks(ConsoleSystem.Arg arg)
	{
		foreach (var package in ModLoader.LoadedPackages)
		{
			foreach (var plugin in package.Plugins)
			{
				plugin.TotalHookTime = default;
				plugin.TotalMemoryUsed = default;
				plugin.HookPool.Reset();
			}
		}

		foreach (var module in Community.Runtime.ModuleProcessor.Modules)
		{
			module.TotalHookTime = default;
			module.TotalMemoryUsed = default;
			module.HookPool.Reset();
		}

		arg.ReplyWith($"All plugin and module hook cache has been reset.");
	}
}
