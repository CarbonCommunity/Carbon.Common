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

	[CommandVar("higherpriorityhookwarns", "Print warns if hooks with higher priority conflict with other hooks. Best to keep this disabled. Same-priority hooks will be printed.")]
	[AuthLevel(2)]
	private bool HigherPriorityHookWarns { get { return Community.Runtime.Config.HigherPriorityHookWarns; } set { Community.Runtime.Config.HigherPriorityHookWarns = value; Community.Runtime.SaveConfig(); } }

	[CommandVar("scriptwatchers", "When disabled, you must load/unload plugins manually with `c.load` or `c.unload`.")]
	[AuthLevel(2)]
	private bool ScriptWatchers { get { return Community.Runtime.Config.ScriptWatchers; } set { Community.Runtime.Config.ScriptWatchers = value; Community.Runtime.SaveConfig(); } }

	[CommandVar("scriptwatchersoption", "Indicates wether the script watcher (whenever enabled) listens to the 'carbon/plugins' folder only, or its subfolders. (0 = Top-only directories, 1 = All directories)")]
	[AuthLevel(2)]
	private int ScriptWatchersOption
	{
		get
		{
			return (int)Community.Runtime.Config.ScriptWatcherOption;
		}
		set
		{
			Community.Runtime.Config.ScriptWatcherOption = (SearchOption)value;
			Community.Runtime.ScriptProcessor.IncludeSubdirectories = value == (int)SearchOption.AllDirectories;
			Community.Runtime.SaveConfig();
		}
	}

	[CommandVar("debug", "The level of debug logging for Carbon. Helpful for very detailed logs in case things break. (Set it to -1 to disable debug logging.)")]
	[AuthLevel(2)]
	private int CarbonDebug { get { return Community.Runtime.Config.LogVerbosity; } set { Community.Runtime.Config.LogVerbosity = value; Community.Runtime.SaveConfig(); } }

	[CommandVar("logfiletype", "The mode for writing the log to file. (0=disabled, 1=saves updates every 5 seconds, 2=saves immediately)")]
	[AuthLevel(2)]
	private int LogFileType { get { return Community.Runtime.Config.LogFileMode; } set { Community.Runtime.Config.LogFileMode = Mathf.Clamp(value, 0, 2); Community.Runtime.SaveConfig(); } }

	[CommandVar("unitystacktrace", "Enables a big chunk of detail of Unity's default stacktrace. Recommended to be disabled as a lot of it is internal and unnecessary for the average user.")]
	[AuthLevel(2)]
	private bool UnityStacktrace
	{
		get { return Community.Runtime.Config.UnityStacktrace; }
		set
		{
			Community.Runtime.Config.UnityStacktrace = value;
			Community.Runtime.SaveConfig();
			ApplyStacktrace();
		}
	}

	[CommandVar("filenamecheck", "It checks if the file name and the plugin name matches. (only applies to scripts)")]
	[AuthLevel(2)]
	private bool FileNameCheck { get { return Community.Runtime.Config.FileNameCheck; } set { Community.Runtime.Config.FileNameCheck = value; Community.Runtime.SaveConfig(); } }
	
	[CommandVar("language", "Server language used by the Language API.")]
	[AuthLevel(2)]
	private string Language { get { return Community.Runtime.Config.Language; } set { Community.Runtime.Config.Language = value; Community.Runtime.SaveConfig(); } }

	[CommandVar("bypassadmincooldowns", "Bypasses the command cooldowns for admin-authed players.")]
	[AuthLevel(2)]
	private bool BypassAdminCooldowns { get { return Community.Runtime.Config.BypassAdminCooldowns; } set { Community.Runtime.Config.BypassAdminCooldowns = value; Community.Runtime.SaveConfig(); } }

#if DEBUG
	[CommandVar("plugintrackingtime", "Plugin average time value for memory and hook time tracking. [DEBUG]")]
	[AuthLevel(2)]
	private float PluginTrackingTime
	{
		get { return Community.Runtime.Config.PluginTrackingTime; }
		set
		{
			Community.Runtime.Config.PluginTrackingTime = value;

			foreach(var mod in ModLoader.LoadedPackages)
			{
				foreach(var plugin in mod.Plugins)
				{
					plugin.MemoryAverage.Time = value;
					plugin.HookTimeAverage.Time = value;
				}
			}

			Community.Runtime.SaveConfig();
		}
	}
#endif

#if WIN
	[CommandVar("consoleinfo", "Show the Windows-only Carbon information at the bottom of the console.")]
	[AuthLevel(2)]
	private bool ConsoleInfo
	{
		get { return Community.Runtime.Config.ShowConsoleInfo; }
		set
		{
			Community.Runtime.Config.ShowConsoleInfo = value;

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
	private bool oCommandChecks { get { return Community.Runtime.Config.oCommandChecks; } set { Community.Runtime.Config.oCommandChecks = value; Community.Runtime.SaveConfig(); } }
}
