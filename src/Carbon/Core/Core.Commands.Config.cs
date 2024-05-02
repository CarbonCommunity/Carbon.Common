/*
 *
 * Copyright (c) 2022-2023 Carbon Community
 * All rights reserved.
 *
 */

namespace Carbon.Core;

public partial class CorePlugin : CarbonPlugin
{
	[ConsoleCommand("loadconfig", "Loads Carbon config from file.")]
	[AuthLevel(2)]
	private void CarbonLoadConfig(ConsoleSystem.Arg arg)
	{
		if (Community.Runtime == null) return;

		Community.Runtime.LoadConfig();

		arg.ReplyWith("Loaded Carbon config.");
	}

	[ConsoleCommand("saveconfig", "Saves Carbon config to file.")]
	[AuthLevel(2)]
	private void CarbonSaveConfig(ConsoleSystem.Arg arg)
	{
		if (Community.Runtime == null) return;

		Community.Runtime.SaveConfig();

		arg.ReplyWith("Saved Carbon config.");
	}

	[CommandVar("modding", "Mark this server as modded or not.")]
	[AuthLevel(2)]
	private bool Modding { get { return Community.Runtime.Config.IsModded; } set { Community.Runtime.Config.IsModded = value; Community.Runtime.SaveConfig(); } }

	[CommandVar("scriptwatchers", "When disabled, you must load/unload plugins manually with `c.load` or `c.unload`.")]
	[AuthLevel(2)]
	private bool ScriptWatchers { get { return Community.Runtime.Config.Watchers.ScriptWatchers; } set { Community.Runtime.Config.Watchers.ScriptWatchers = value; Community.Runtime.SaveConfig(); } }

	[CommandVar("scriptwatchersoption", "Indicates wether the script watcher (whenever enabled) listens to the 'carbon/plugins' folder only, or its subfolders. (0 = Top-only directories, 1 = All directories)")]
	[AuthLevel(2)]
	private int ScriptWatchersOption
	{
		get
		{
			return (int)Community.Runtime.Config.Watchers.ScriptWatcherOption;
		}
		set
		{
			Community.Runtime.Config.Watchers.ScriptWatcherOption = (SearchOption)value;
			Community.Runtime.ScriptProcessor.IncludeSubdirectories = value == (int)SearchOption.AllDirectories;
			Community.Runtime.SaveConfig();
		}
	}

	[CommandVar("debug", "The level of debug logging for Carbon. Helpful for very detailed logs in case things break. (Set it to -1 to disable debug logging.)")]
	[AuthLevel(2)]
	private int CarbonDebug { get { return Community.Runtime.Config.Logging.LogVerbosity; } set { Community.Runtime.Config.Logging.LogVerbosity = value; Community.Runtime.SaveConfig(); } }

	[CommandVar("logfiletype", "The mode for writing the log to file. (0=disabled, 1=saves updates every 5 seconds, 2=saves immediately)")]
	[AuthLevel(2)]
	private int LogFileType { get { return Community.Runtime.Config.Logging.LogFileMode; } set { Community.Runtime.Config.Logging.LogFileMode = Mathf.Clamp(value, 0, 2); Community.Runtime.SaveConfig(); } }

	[CommandVar("unitystacktrace", "Enables a big chunk of detail of Unity's default stacktrace. Recommended to be disabled as a lot of it is internal and unnecessary for the average user.")]
	[AuthLevel(2)]
	private bool UnityStacktrace
	{
		get { return Community.Runtime.Config.Debugging.UnityStacktrace; }
		set
		{
			Community.Runtime.Config.Debugging.UnityStacktrace = value;
			Community.Runtime.SaveConfig();
			ApplyStacktrace();
		}
	}

	[CommandVar("language", "Server language used by the Language API.")]
	[AuthLevel(2)]
	private string Language { get { return Community.Runtime.Config.Language; } set { Community.Runtime.Config.Language = value; Community.Runtime.SaveConfig(); } }

	[CommandVar("unloadonfailure", "Unload already loaded plugins when recompilation attempt fails. (Disabled by default)")]
	[AuthLevel(2)]
	private bool UnloadOnFailure { get { return Community.Runtime.Config.Compiler.UnloadOnFailure; } set { Community.Runtime.Config.Compiler.UnloadOnFailure = value; Community.Runtime.SaveConfig(); } }

	[CommandVar("bypassadmincooldowns", "Bypasses the command cooldowns for admin-authed players.")]
	[AuthLevel(2)]
	private bool BypassAdminCooldowns { get { return Community.Runtime.Config.Permissions.BypassAdminCooldowns; } set { Community.Runtime.Config.Permissions.BypassAdminCooldowns = value; Community.Runtime.SaveConfig(); } }

	[CommandVar("logsplitsize", "The size for each log (in megabytes) required for it to be split into separate chunks.")]
	[AuthLevel(2)]
	private double LogSplitSize { get { return Community.Runtime.Config.Logging.LogSplitSize; } set { Community.Runtime.Config.Logging.LogSplitSize = value; Community.Runtime.SaveConfig(); } }

#if WIN
	[CommandVar("consoleinfo", "Show the Windows-only Carbon information at the bottom of the console.")]
	[AuthLevel(2)]
	private bool ConsoleInfo
	{
		get { return Community.Runtime.Config.Misc.ShowConsoleInfo; }
		set
		{
			Community.Runtime.Config.Misc.ShowConsoleInfo = value;

			if (value)
			{
				Community.Runtime.RefreshConsoleInfo();
			}
			else
			{
				if (ServerConsole.Instance != null && ServerConsole.Instance.input != null)
				{
					ServerConsole.Instance.input.statusText = new string[3];
				}
			}
		}
	}
#endif

	[CommandVar("ocommandchecks", "Prints a reminding warning if RCON/console attempts at calling an o.* command.")]
	[AuthLevel(2)]
	private bool oCommandChecks { get { return Community.Runtime.Config.Misc.oCommandChecks; } set { Community.Runtime.Config.Misc.oCommandChecks = value; Community.Runtime.SaveConfig(); } }
}
