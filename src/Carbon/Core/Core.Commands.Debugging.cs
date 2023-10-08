/*
 *
 * Copyright (c) 2022-2023 Carbon Community
 * All rights reserved.
 *
 */

namespace Carbon.Core;

public partial class CorePlugin : CarbonPlugin
{
	[CommandVar("scriptdebugorigin", "[For debugging purposes] Overrides the script directory to this value so remote debugging is possible.")]
	private string ScriptDebuggingOrigin { get { return Community.Runtime.Config.ScriptDebuggingOrigin; } set { Community.Runtime.Config.ScriptDebuggingOrigin = value; } }
}
