using API.Logger;

/*
 *
 * Copyright (c) 2022-2024 Carbon Community
 * All rights reserved.
 *
 */

namespace Carbon.Core;

[Serializable]
public class Config
{
	public bool IsModded { get; set; } = true;
	public List<string> CommandPrefixes { get; set; }
	public bool Rcon { get; set; } = true;
	public string Language { get; set; } = "en";
	public string WebRequestIp { get; set; }

	public WatchersConfig Watchers { get; set; } = new();
	public PermissionsConfig Permissions { get; set; } = new();
	public DebuggingConfig Debugging { get; set; } = new();
	public LoggingConfig Logging { get; set; } = new();
	public CompilerConfig Compiler { get; set; } = new();
	public ProfilerConfig Profiler { get; set; } = new();
	public MiscConfig Misc { get; set; } = new();

	public class CompilerConfig
	{
		public bool UnloadOnFailure { get; set; } = false;
		public List<string> ConditionalCompilationSymbols { get; set; }
	}

	public class ProfilerConfig
	{
		public bool RecordingWarnings { get; set; } = true;
	}

	public class WatchersConfig
	{
		public bool ScriptWatchers { get; set; } = true;
		public bool ZipScriptWatchers { get; set; } = true;
		public SearchOption ScriptWatcherOption { get; set; } = SearchOption.TopDirectoryOnly;
	}

	public class PermissionsConfig
	{
		public string PlayerDefaultGroup { get; set; } = "default";
		public string AdminDefaultGroup { get; set; } = "admin";
		public bool BypassAdminCooldowns { get; set; } = false;
		public Permission.SerializationMode PermissionSerialization { get; set; } = Permission.SerializationMode.Protobuf;
	}

	public class DebuggingConfig
	{
		public string ScriptDebuggingOrigin = string.Empty;
		public bool UnityStacktrace { get; set; } =
#if DEBUG
			true;
#else
			true; // Set false when we're out of development
#endif
		public int HookLagSpikeThreshold = 1000;
	}

	public class LoggingConfig
	{
		public double LogSplitSize { get; set; } = 2.5;
		public Severity LogSeverity { get; set; } = Severity.Notice;
		public int LogFileMode { get; set; } = 2;
		public int LogVerbosity { get; set; } = 0;
	}

	public class MiscConfig
	{
		public bool oCommandChecks { get; set; } = true;

#if WIN
		public bool ShowConsoleInfo { get; set; } = true;
#endif
	}
}
