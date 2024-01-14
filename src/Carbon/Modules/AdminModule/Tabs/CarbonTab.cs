using System.Net;
#if !MINIMAL

/*
 *
 * Copyright (c) 2022-2023 Carbon Community
 * All rights reserved.
 *
 */

namespace Carbon.Modules;

public partial class AdminModule : CarbonModule<AdminConfig, AdminData>
{
	public class CarbonTab
	{
		public static Config Config => Community.Runtime.Config;

		internal static readonly string[] LogFileModes = new string[]
		{
			"Disabled",
			"Save every 5 min.",
			"Save immediately"
		};
		internal static readonly string[] LogVerbosity = new string[]
		{
			"Normal",
			"Level 1",
			"Level 2",
			"Level 3",
			"Level 4",
			"Level 5",
			"Level 6"
		};
		internal static readonly string[] SearchDirectories = new string[]
		{
			"Primary",
			"All"
		};

		public static Tab Get()
		{
			var tab = new Tab("carbon", "Carbon", Community.Runtime.CorePlugin, (ap, t) => { Refresh(t, ap); }, 2);
			tab.AddColumn(0);
			tab.AddColumn(1);

			return tab;
		}

		public static void Refresh(Tab tab, PlayerSession ap)
		{
			tab.ClearColumn(0);
			tab.ClearColumn(1);

			if (Singleton.HasAccessLevel(ap.Player, 2))
			{
				tab.AddInput(0, Singleton.GetPhrase("hostname", ap.Player.UserIDString), ap => $"{ConVar.Server.hostname}", Singleton.HasAccessLevel(ap.Player, 3) ? (ap2, args) => { ConVar.Server.hostname = args.ToString(" "); } : null);
				tab.AddInput(0, Singleton.GetPhrase("level", ap.Player.UserIDString), ap => $"{ConVar.Server.level}", null);

				tab.AddName(0, Singleton.GetPhrase("info", ap.Player.UserIDString), TextAnchor.MiddleLeft);
				{
					tab.AddInput(0, Singleton.GetPhrase("version", ap.Player.UserIDString), ap => $"{Community.Runtime.Analytics.Version}", null);
					tab.AddInput(0, Singleton.GetPhrase("version2", ap.Player.UserIDString), ap => $"{Community.Runtime.Analytics.InformationalVersion}", null);

					var loadedHooks = Community.Runtime.HookManager.LoadedDynamicHooks.Count(x => x.IsInstalled) + Community.Runtime.HookManager.LoadedStaticHooks.Count(x => x.IsInstalled);
					var totalHooks = Community.Runtime.HookManager.LoadedDynamicHooks.Count() + Community.Runtime.HookManager.LoadedStaticHooks.Count();
					tab.AddInput(0, Singleton.GetPhrase("hooks", ap.Player.UserIDString), ap => $"<b>{loadedHooks:n0}</b> / {totalHooks:n0} loaded", null);
					tab.AddInput(0, Singleton.GetPhrase("statichooks", ap.Player.UserIDString), ap => $"{Community.Runtime.HookManager.LoadedStaticHooks.Count():n0}", null);
					tab.AddInput(0, Singleton.GetPhrase("dynamichooks", ap.Player.UserIDString), ap => $"{Community.Runtime.HookManager.LoadedDynamicHooks.Count():n0}", null);

					tab.AddName(0, Singleton.GetPhrase("plugins", ap.Player.UserIDString), TextAnchor.MiddleLeft);
					tab.AddInput(0, Singleton.GetPhrase("mods", ap.Player.UserIDString), ap => $"{Community.Runtime.Plugins.Plugins.Count:n0}", null);

					if (Singleton.HasAccessLevel(ap.Player, 3))
					{
						tab.AddName(0, Singleton.GetPhrase("console", ap.Player.UserIDString), TextAnchor.MiddleLeft);
						foreach (var log in _logQueue)
						{
							tab.AddText(0, log, 8, "1 1 1 0.85", TextAnchor.MiddleLeft, CUI.Handler.FontTypes.DroidSansMono, isInput: true);
						}
						tab.AddInputButton(0, Singleton.GetPhrase("execservercmd", ap.Player.UserIDString), 0.2f, new Tab.OptionInput(null, null, 0, false, (ap, args) =>
						{
							ConsoleSystem.Run(ConsoleSystem.Option.Server, args.ToString(" "), null);
							Refresh(tab, ap);
						}), new Tab.OptionButton("Refresh", ap =>
						{
							Refresh(tab, ap);
						}));
					}
				}
			}

			if (Singleton.HasAccessLevel(ap.Player, 3))
			{
				tab.AddName(1, Singleton.GetPhrase("config", ap.Player.UserIDString), TextAnchor.MiddleLeft);
				{
					tab.AddToggle(1, Singleton.GetPhrase("ismodded", ap.Player.UserIDString), ap => { Config.IsModded = !Config.IsModded; Community.Runtime.SaveConfig(); }, ap => Config.IsModded, Singleton.GetPhrase("ismodded_help", ap.Player.UserIDString));

					tab.AddName(1, Singleton.GetPhrase("general", ap.Player.UserIDString), TextAnchor.MiddleLeft);

					tab.AddName(1, Singleton.GetPhrase("watchers", ap.Player.UserIDString), TextAnchor.MiddleLeft);
					tab.AddToggle(1, Singleton.GetPhrase("scriptwatchers", ap.Player.UserIDString), ap => { Config.ScriptWatchers = !Config.ScriptWatchers; Community.Runtime.SaveConfig(); }, ap => Config.ScriptWatchers, Singleton.GetPhrase("scriptwatchers_help", ap.Player.UserIDString));
					tab.AddDropdown(1, Singleton.GetPhrase("scriptwatchersoption", ap.Player.UserIDString), ap => (int)Config.ScriptWatcherOption, (ap, index) =>
					{
						Config.ScriptWatcherOption = (SearchOption)index;
						Community.Runtime.ScriptProcessor.IncludeSubdirectories = index == (int)SearchOption.AllDirectories;
						Community.Runtime.SaveConfig();
					}, SearchDirectories, tooltip: Singleton.GetPhrase("scriptwatchersoption_help", ap.Player.UserIDString));
					tab.AddToggle(1, Singleton.GetPhrase("zipscriptwatchers", ap.Player.UserIDString), ap => { Config.ZipScriptWatchers = !Config.ZipScriptWatchers; Community.Runtime.SaveConfig(); }, ap => Config.ZipScriptWatchers, Singleton.GetPhrase("zipscriptwatchers_help", ap.Player.UserIDString));
					tab.AddToggle(1, Singleton.GetPhrase("filenamecheck", ap.Player.UserIDString), ap => { Config.FileNameCheck = !Config.FileNameCheck; Community.Runtime.SaveConfig(); }, ap => Config.FileNameCheck, Singleton.GetPhrase("filenamecheck_help", ap.Player.UserIDString));

					tab.AddName(1, Singleton.GetPhrase("logging", ap.Player.UserIDString), TextAnchor.MiddleLeft);
					tab.AddDropdown(1, Singleton.GetPhrase("logfilemode", ap.Player.UserIDString), ap => Config.LogFileMode, (ap, index) => { Config.LogFileMode = index; Community.Runtime.SaveConfig(); }, LogFileModes);
					tab.AddDropdown(1, Singleton.GetPhrase("logverbosity", ap.Player.UserIDString), ap => Config.LogVerbosity, (ap, index) => { Config.LogVerbosity = index; Community.Runtime.SaveConfig(); }, LogVerbosity);
					tab.AddDropdown(1, Singleton.GetPhrase("logseverity", ap.Player.UserIDString), ap => (int)Config.LogSeverity, (ap, index) => { Config.LogSeverity = (API.Logger.Severity)index; Community.Runtime.SaveConfig(); }, Enum.GetNames(typeof(API.Logger.Severity)));

					tab.AddName(1, Singleton.GetPhrase("misc", ap.Player.UserIDString), TextAnchor.MiddleLeft);
					tab.AddInput(1, Singleton.GetPhrase("serverlang", ap.Player.UserIDString), ap => Config.Language, (ap, args) => { Config.Language = args.ToString(" "); Community.Runtime.SaveConfig(); });
					tab.AddInput(1, Singleton.GetPhrase("webreqip", ap.Player.UserIDString), ap => Config.WebRequestIp, (ap, args) =>
					{
						var ip = args.ToString(" ");

						if (string.IsNullOrEmpty(ip) || (IPAddress.TryParse(ip, out _) && ip.Contains(".")))
						{
							Config.WebRequestIp = ip;
							Community.Runtime.SaveConfig();
						}
					});
					tab.AddEnum(1, Singleton.GetPhrase("permmode", ap.Player.UserIDString), (ap, back) =>
					{
						var e = Enum.GetNames(typeof(Permission.SerializationMode));
						Config.PermissionSerialization += back ? -1 : 1;

						if (Config.PermissionSerialization < (Permission.SerializationMode)(-1))
							Config.PermissionSerialization = (Permission.SerializationMode)(e.Length - 2);
						else if ((int)Config.PermissionSerialization >= e.Length - 1)
							Config.PermissionSerialization = (Permission.SerializationMode)(-1);

						Community.Runtime.SaveConfig();
					}, ap => Config.PermissionSerialization.ToString());

					#if WIN
					tab.AddToggle(1, Singleton.GetPhrase("consoleinfo", ap.Player.UserIDString), ap =>
					{
						Config.ShowConsoleInfo = !Config.ShowConsoleInfo;

						if (Config.ShowConsoleInfo)
						{
							Community.Runtime.RefreshConsoleInfo();
						}
						else
						{
							if (ServerConsole.Instance != null && ServerConsole.Instance.input != null)
							{
								ServerConsole.Instance.input.statusText = new string[3];
							}
						};

						Community.Runtime.SaveConfig();
					}, ap => Config.ShowConsoleInfo, Singleton.GetPhrase("consoleinfo_help", ap.Player.UserIDString));
					#endif

					tab.AddName(1, Singleton.GetPhrase("permissions", ap.Player.UserIDString), TextAnchor.MiddleLeft);
					tab.AddInput(1, Singleton.GetPhrase("playerdefgroup", ap.Player.UserIDString), ap => Config.PlayerDefaultGroup, (ap, args) => { Config.PlayerDefaultGroup = args.ToString(string.Empty); Community.Runtime.SaveConfig(); });
					tab.AddInput(1, Singleton.GetPhrase("admindefgroup", ap.Player.UserIDString), ap => Config.AdminDefaultGroup, (ap, args) => { Config.AdminDefaultGroup = args.ToString(string.Empty); Community.Runtime.SaveConfig(); });

					tab.AddName(1, Singleton.GetPhrase("conditionals", ap.Player.UserIDString), TextAnchor.MiddleLeft);

					for(int i = 0; i < Config.ConditionalCompilationSymbols.Count; i++)
					{
						var index = i;
						var symbol = Config.ConditionalCompilationSymbols[i];

						tab.AddInputButton(1, string.Empty, 0.075f,
							new Tab.OptionInput(null, ap => symbol, 0, false,
								(ap, args) =>
								{
									Config.ConditionalCompilationSymbols[index] = args.ToString(string.Empty).ToUpper().Trim();
									Refresh(tab, ap);
									Community.Runtime.SaveConfig();
								}),
							new Tab.OptionButton("X", ap =>
							{
								Config.ConditionalCompilationSymbols.RemoveAt(index);
								Refresh(tab, ap);
								Community.Runtime.SaveConfig();
							}, ap => Tab.OptionButton.Types.Important));
					}

					tab.AddInputButton(1, string.Empty, 0.075f,
						new Tab.OptionInput(null, ap => ap.GetStorage<string>(tab, "conditional"), 0, false,
							(ap, args) =>
							{
								ap.SetStorage(tab, "conditional", args.ToString(string.Empty).ToUpper().Trim());
							}),
						new Tab.OptionButton("+", ap =>
						{
							var value = ap.GetStorage<string>(tab, "conditional");
							if (!string.IsNullOrEmpty(value))
							{
								Config.ConditionalCompilationSymbols.Add(value);
								ap.SetStorage(tab, "conditional", string.Empty);
								Refresh(tab, ap);
								Community.Runtime.SaveConfig();
							}
						}, ap => Tab.OptionButton.Types.Selected));

					tab.AddName(1, Singleton.GetPhrase("debugging", ap.Player.UserIDString), TextAnchor.MiddleLeft);
					tab.AddInput(1, Singleton.GetPhrase("scriptdebugorigin", ap.Player.UserIDString), ap => Config.ScriptDebuggingOrigin, (ap, args) => { Config.ScriptDebuggingOrigin = args.ToString(string.Empty); Community.Runtime.SaveConfig(); }, Singleton.GetPhrase("scriptdebugorigin_help", ap.Player.UserIDString));
				}
			}
		}
	}
}

#endif
