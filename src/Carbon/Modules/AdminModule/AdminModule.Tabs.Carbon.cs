using System.Net;
#if !MINIMAL

/*
 *
 * Copyright (c) 2022-2023 Carbon Community
 * All rights reserved.
 *
 */

namespace Carbon.Modules;

public partial class AdminModule
{
	public class CarbonTab
	{
		public static Tab Instance;
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

		internal static readonly string[] TabTypes = new string[]
		{
			"Quick Actions"
		};

		public static Tab Get()
		{
			Instance = new Tab("carbon", "Carbon", Community.Runtime.CorePlugin, (ap, t) => { Refresh(t, ap); }, "carbon.use");
			Instance.AddColumn(0);
			Instance.AddColumn(1);

			return Instance;
		}

		public static void Refresh(Tab tab, PlayerSession ap)
		{
			tab.ClearColumn(0);
			tab.ClearColumn(1);

			if (!Singleton.HasAccess(ap.Player, "carbon.use"))
			{
				return;
			}

			if (Singleton.HasAccess(ap.Player, "carbon.server_settings"))
			{
				tab.AddInput(0, Singleton.GetPhrase("hostname", ap.Player.UserIDString),
					ap => $"{ConVar.Server.hostname}", (ap2, args) => { ConVar.Server.hostname = args.ToString(" "); });
				tab.AddInput(0, Singleton.GetPhrase("level", ap.Player.UserIDString), ap => $"{ConVar.Server.level}",
					null);
			}

			if (Singleton.HasAccess(ap.Player, "carbon.server_info"))
			{
				tab.AddName(0, Singleton.GetPhrase("info", ap.Player.UserIDString), TextAnchor.MiddleLeft);
				{
					tab.AddInput(0, Singleton.GetPhrase("version", ap.Player.UserIDString),
						ap => $"{Community.Runtime.Analytics.Version}", null);
					tab.AddInput(0, Singleton.GetPhrase("version2", ap.Player.UserIDString),
						ap => $"{Community.Runtime.Analytics.InformationalVersion}", null);

					var loadedHooks = Community.Runtime.HookManager.LoadedDynamicHooks.Count(x => x.IsInstalled) +
					                  Community.Runtime.HookManager.LoadedStaticHooks.Count(x => x.IsInstalled);
					var totalHooks = Community.Runtime.HookManager.LoadedDynamicHooks.Count() +
					                 Community.Runtime.HookManager.LoadedStaticHooks.Count();
					tab.AddInput(0, Singleton.GetPhrase("hooks", ap.Player.UserIDString),
						ap => $"<b>{loadedHooks:n0}</b> / {totalHooks:n0} loaded", null);
					tab.AddInput(0, Singleton.GetPhrase("statichooks", ap.Player.UserIDString),
						ap => $"{Community.Runtime.HookManager.LoadedStaticHooks.Count():n0}", null);
					tab.AddInput(0, Singleton.GetPhrase("dynamichooks", ap.Player.UserIDString),
						ap => $"{Community.Runtime.HookManager.LoadedDynamicHooks.Count():n0}", null);

					tab.AddName(0, Singleton.GetPhrase("plugins", ap.Player.UserIDString), TextAnchor.MiddleLeft);
					tab.AddInput(0, Singleton.GetPhrase("mods", ap.Player.UserIDString),
						ap => $"{Community.Runtime.Plugins.Plugins.Count:n0}", null);

					if (Singleton.HasAccess(ap.Player, "carbon.server_console"))
					{
						tab.AddName(0, Singleton.GetPhrase("console", ap.Player.UserIDString), TextAnchor.MiddleLeft);
						foreach (var log in _logQueue)
						{
							tab.AddText(0, log, 8, "1 1 1 0.85", TextAnchor.MiddleLeft,
								CUI.Handler.FontTypes.DroidSansMono, isInput: true);
						}

						tab.AddInputButton(0, Singleton.GetPhrase("execservercmd", ap.Player.UserIDString), 0.2f,
							new Tab.OptionInput(null, null, 0, false, (ap, args) =>
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

			if (Singleton.HasAccess(ap.Player, "carbon.quick_actions"))
			{
				tab.AddName(1, Singleton.GetPhrase("quickactions", ap.Player.UserIDString), TextAnchor.MiddleLeft);
				{
					var editMode = ap.GetStorage(tab, "carbontabedit", false);
					foreach (var action in Singleton.ConfigInstance.QuickActions)
					{
						tab.AddButton(1, editMode ? $"{action.Key} ({action.Value})" : action.Key, ap =>
						{
							if (editMode)
							{
								Singleton.ConfigInstance.QuickActions.Remove(action.Key);
								Refresh(tab, ap);
								return;
							}

							ConsoleSystem.Run(ConsoleSystem.Option.Server, action.Value);
						}, ap => Tab.OptionButton.Types.Selected);
					}

					if (editMode)
					{
						tab.AddText(1, "Click on existent buttons above to delete.", 10, "1 1 1 0.5");
						tab.AddInput(1, "Button Name", ap => ap.GetStorage(tab, "carbontabbtnname", string.Empty),
							(ap, args) =>
							{
								ap.SetStorage(tab, "carbontabbtnname", args.ToString(" "));
							});
						tab.AddInputButton(1, "Command", 0.15f, new Tab.OptionInput(null,
							ap => ap.GetStorage(tab, "carbontabbtncmd", string.Empty), 30, false, (ap, args) =>
							{
								ap.SetStorage(tab, "carbontabbtncmd", args.ToString(" "));
							}), new Tab.OptionButton("Add", ap =>
						{
							var name = ap.GetStorage(tab, "carbontabbtnname", string.Empty);
							var cmd = ap.GetStorage(tab, "carbontabbtncmd", string.Empty);

							if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(cmd))
							{
								return;
							}

							Singleton.ConfigInstance.QuickActions[name] = cmd;
							ap.ClearStorage(tab, "carbontabbtnname");
							ap.ClearStorage(tab, "carbontabbtncmd");

							Refresh(tab, ap);
						}, ap => Tab.OptionButton.Types.Selected));
					}

					tab.AddButton(1, editMode ? "Stop Editing" : "Edit", ap =>
					{
						ap.SetStorage(tab, "carbontabedit", !editMode);
						Refresh(tab, ap);
					}, ap => editMode ? Tab.OptionButton.Types.Important : Tab.OptionButton.Types.None);
				}
			}

			if (Singleton.HasAccess(ap.Player, "carbon.server_config"))
			{
				tab.AddName(1, Singleton.GetPhrase("general", ap.Player.UserIDString), TextAnchor.MiddleLeft);
				{
					tab.AddToggle(1, Singleton.GetPhrase("ismodded", ap.Player.UserIDString), ap => { Config.IsModded = !Config.IsModded; Community.Runtime.SaveConfig(); }, ap => Config.IsModded, Singleton.GetPhrase("ismodded_help", ap.Player.UserIDString));
					tab.AddToggle(1, Singleton.GetPhrase("scriptwatchers", ap.Player.UserIDString), ap => { Config.Watchers.ScriptWatchers = !Config.Watchers.ScriptWatchers; Community.Runtime.SaveConfig(); }, ap => Config.Watchers.ScriptWatchers, Singleton.GetPhrase("scriptwatchers_help", ap.Player.UserIDString));
					tab.AddDropdown(1, Singleton.GetPhrase("scriptwatchersoption", ap.Player.UserIDString), ap => (int)Config.Watchers.ScriptWatcherOption, (ap, index) =>
					{
						Config.Watchers.ScriptWatcherOption = (SearchOption)index;
						Community.Runtime.ScriptProcessor.IncludeSubdirectories = index == (int)SearchOption.AllDirectories;
						Community.Runtime.SaveConfig();
					}, SearchDirectories, tooltip: Singleton.GetPhrase("scriptwatchersoption_help", ap.Player.UserIDString));
					tab.AddToggle(1, Singleton.GetPhrase("zipscriptwatchers", ap.Player.UserIDString), ap => { Config.Watchers.ZipScriptWatchers = !Config.Watchers.ZipScriptWatchers; Community.Runtime.SaveConfig(); }, ap => Config.Watchers.ZipScriptWatchers, Singleton.GetPhrase("zipscriptwatchers_help", ap.Player.UserIDString));
					tab.AddToggle(1, Singleton.GetPhrase("filenamecheck", ap.Player.UserIDString), ap => { Config.Watchers.FileNameCheck = !Config.Watchers.FileNameCheck; Community.Runtime.SaveConfig(); }, ap => Config.Watchers.FileNameCheck, Singleton.GetPhrase("filenamecheck_help", ap.Player.UserIDString));
				}

				tab.AddName(1, Singleton.GetPhrase("logging", ap.Player.UserIDString), TextAnchor.MiddleLeft);
				{
					tab.AddDropdown(1, Singleton.GetPhrase("logfilemode", ap.Player.UserIDString), ap => Config.Logging.LogFileMode, (ap, index) => { Config.Logging.LogFileMode = index; Community.Runtime.SaveConfig(); }, LogFileModes);
					tab.AddDropdown(1, Singleton.GetPhrase("logverbosity", ap.Player.UserIDString), ap => Config.Logging.LogVerbosity, (ap, index) => { Config.Logging.LogVerbosity = index; Community.Runtime.SaveConfig(); }, LogVerbosity);
					tab.AddDropdown(1, Singleton.GetPhrase("logseverity", ap.Player.UserIDString), ap => (int)Config.Logging.LogSeverity, (ap, index) => { Config.Logging.LogSeverity = (API.Logger.Severity)index; Community.Runtime.SaveConfig(); }, Enum.GetNames(typeof(API.Logger.Severity)));
				}
				tab.AddName(1, Singleton.GetPhrase("misc", ap.Player.UserIDString), TextAnchor.MiddleLeft);
				{
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
						Config.Permissions.PermissionSerialization += back ? -1 : 1;

						if (Config.Permissions.PermissionSerialization < (Permission.SerializationMode)(-1))
							Config.Permissions.PermissionSerialization = (Permission.SerializationMode)(e.Length - 2);
						else if ((int)Config.Permissions.PermissionSerialization >= e.Length - 1)
							Config.Permissions.PermissionSerialization = (Permission.SerializationMode)(-1);

						Community.Runtime.SaveConfig();
					}, ap => Config.Permissions.PermissionSerialization.ToString());
				}
					#if WIN
					tab.AddToggle(1, Singleton.GetPhrase("consoleinfo", ap.Player.UserIDString), ap =>
					{
						Config.Misc.ShowConsoleInfo = !Config.Misc.ShowConsoleInfo;

						if (Config.Misc.ShowConsoleInfo)
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
					}, ap => Config.Misc.ShowConsoleInfo, Singleton.GetPhrase("consoleinfo_help", ap.Player.UserIDString));
					#endif

					tab.AddName(1, Singleton.GetPhrase("permissions", ap.Player.UserIDString), TextAnchor.MiddleLeft);
					tab.AddInput(1, Singleton.GetPhrase("playerdefgroup", ap.Player.UserIDString), ap => Config.Permissions.PlayerDefaultGroup, (ap, args) => { Config.Permissions.PlayerDefaultGroup = args.ToString(string.Empty); Community.Runtime.SaveConfig(); });
					tab.AddInput(1, Singleton.GetPhrase("admindefgroup", ap.Player.UserIDString), ap => Config.Permissions.AdminDefaultGroup, (ap, args) => { Config.Permissions.AdminDefaultGroup = args.ToString(string.Empty); Community.Runtime.SaveConfig(); });

					tab.AddName(1, Singleton.GetPhrase("conditionals", ap.Player.UserIDString), TextAnchor.MiddleLeft);

					for(int i = 0; i < Config.Debugging.ConditionalCompilationSymbols.Count; i++)
					{
						var index = i;
						var symbol = Config.Debugging.ConditionalCompilationSymbols[i];

						tab.AddInputButton(1, string.Empty, 0.075f,
							new Tab.OptionInput(null, ap => symbol, 0, false,
								(ap, args) =>
								{
									Config.Debugging.ConditionalCompilationSymbols[index] = args.ToString(string.Empty).ToUpper().Trim();
									Refresh(tab, ap);
									Community.Runtime.SaveConfig();
								}),
							new Tab.OptionButton("X", ap =>
							{
								Config.Debugging.ConditionalCompilationSymbols.RemoveAt(index);
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
								Config.Debugging.ConditionalCompilationSymbols.Add(value);
								ap.SetStorage(tab, "conditional", string.Empty);
								Refresh(tab, ap);
								Community.Runtime.SaveConfig();
							}
						}, ap => Tab.OptionButton.Types.Selected));

					tab.AddName(1, Singleton.GetPhrase("debugging", ap.Player.UserIDString), TextAnchor.MiddleLeft);
					tab.AddInput(1, Singleton.GetPhrase("scriptdebugorigin", ap.Player.UserIDString), ap => Config.Debugging.ScriptDebuggingOrigin, (ap, args) => { Config.Debugging.ScriptDebuggingOrigin = args.ToString(string.Empty); Community.Runtime.SaveConfig(); }, Singleton.GetPhrase("scriptdebugorigin_help", ap.Player.UserIDString));
				}
		}
	}
}

#endif
