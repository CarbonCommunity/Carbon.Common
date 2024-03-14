#if !MINIMAL

/*
 *
 * Copyright (c) 2022-2023 Carbon Community
 * All rights reserved.
 *
 */

using API.Commands;
using Newtonsoft.Json;
using Oxide.Game.Rust.Cui;

namespace Carbon.Modules;

public partial class AdminModule
{
	public class ConfigurationTab : Tab
	{
		public ConfigurationTab(string id, string name, RustPlugin plugin, Action<PlayerSession, Tab> onChange = null) : base(id, name, plugin, onChange)
		{
		}

		public static readonly string[] AuthLevels = new[]
		{
			"User",
			"Moderator",
			"Admin",
			"Developer"
		};

		public static ConfigurationTab Make()
		{
			var tab = new ConfigurationTab("configuration", "Configuration", Community.Runtime.CorePlugin, (session, tab) =>
			{
				Refresh(tab, session);
			}) { Fullscreen = true };

			static void Refresh(Tab tab, PlayerSession session)
			{
				tab.ClearColumn(0);
				{
					tab.AddButton(0, "< Go Back", ap => Singleton.SetTab(session.Player, 0), ap => OptionButton.Types.Selected);

					tab.AddName(0, "Configuration");
					tab.AddDropdown(0, "Minimum Auth-Level", ap => Singleton.ConfigInstance.MinimumAuthLevel,
						(ap, index) => Singleton.ConfigInstance.MinimumAuthLevel = index, AuthLevels);

					tab.AddName(0, "Tabs");
					tab.AddToggle(0, "Display Entities",
						ap => Singleton.ConfigInstance.DisableEntitiesTab = !Singleton.ConfigInstance.DisableEntitiesTab,
						ap => !Singleton.ConfigInstance.DisableEntitiesTab);
					tab.AddToggle(0, "Display Plugins",
						ap => Singleton.ConfigInstance.DisablePluginsTab = !Singleton.ConfigInstance.DisablePluginsTab,
						ap => !Singleton.ConfigInstance.DisablePluginsTab);
					tab.AddButton(0, "Apply Changes", ap => Singleton.GenerateTabs());
				}

				tab.ClearColumn(1);
				{
					var convarSearch = session.GetStorage(tab, "convarsearch", string.Empty);
					var currentlyDisplaying = ConVarSnapshots.Snapshots.Count(x => string.IsNullOrEmpty(convarSearch) || x.Key.Contains(convarSearch));

					tab.AddName(1, "ConVars");

					if (string.IsNullOrEmpty(convarSearch))
					{
						tab.AddInput(1, $"Search ({currentlyDisplaying:n0})", ap => convarSearch, 0, false, (ap, args) =>
						{
							ap.SetStorage(tab, "convarsearch", args.ToString(" "));
							Refresh(tab, ap);
						});
					}
					else
					{
						tab.AddInputButton(1, $"Search ({currentlyDisplaying:n0})", 0.08f,
							new OptionInput(string.Empty, ap => convarSearch, 0, false, (ap, args) =>
							{
								ap.SetStorage(tab, "convarsearch", args.ToString(" "));
								Refresh(tab, ap);
							}),
							new OptionButton("X", ap =>
							{
								ap.SetStorage(tab, "convarsearch", string.Empty);
								Refresh(tab, ap);
							}, ap => OptionButton.Types.Important));
					}

					var convarTypes = typeof(BasePlayer).Assembly.GetExportedTypes();

					foreach (var type in convarTypes)
					{
						var factory = type.GetCustomAttribute<ConsoleSystem.Factory>();
						var factoryName = factory == null ? type.Name.ToLower() : factory.Name;
						var fields = type.GetFields(BindingFlags.Static | BindingFlags.Public).Where(x => x.GetCustomAttribute<ServerVar>() != null && (string.IsNullOrEmpty(convarSearch) ? true : $"{factoryName}.{x.Name}".Contains(convarSearch)));

						if (!fields.Any())
						{
							continue;
						}

						tab.AddText(1, $"<color=orange>></color> {type.Name.ToCamelCase()}— {factoryName}.*", 13, "1 1 1 0.4", TextAnchor.LowerLeft);

						foreach (var field in fields)
						{
							var serverVar = field.GetCustomAttribute<ServerVar>();
							var nameFormatted = $"{factoryName}.{field.Name}";

							var snapshot = ConVarSnapshots.Snapshots[nameFormatted];

							if (field.FieldType == typeof(string))
							{
								tab.AddInput(1, field.Name, ap => field.GetValue(null)?.ToString(), 0, false,
									(ap, args) => field.SetValue(null, args.ToString(" ")),
									tooltip: serverVar.Help);
							}
							else if (field.FieldType == typeof(bool))
							{
								tab.AddToggle(1, $"{field.Name} (default: {snapshot.Value})", ap => field.SetValue(null, !(bool)field.GetValue(null)), ap => (bool)field.GetValue(null),
									tooltip: serverVar.Help);
							}
							else if (field.FieldType == typeof(float))
							{
								tab.AddInputButton(1, field.Name, 0.2f,
									new OptionInput(string.Empty, ap => $"{field.GetValue(null):n0}", 0,
										false, (ap, args) => field.SetValue(null, args.ToString(" ").ToFloat())),
									new OptionButton($"<size=8>{snapshot.Value:n0}</size>",
										ap => field.SetValue(null, snapshot.Value)),
									tooltip: serverVar.Help);
							}
							else if (field.FieldType == typeof(int))
							{
								tab.AddInputButton(1, field.Name, 0.2f,
									new OptionInput(string.Empty, ap => $"{field.GetValue(null):n0}", 0,
										false, (ap, args) => field.SetValue(null, args.ToString(" ").ToInt())),
									new OptionButton($"<size=8>{snapshot.Value:n0}</size>",
										ap => field.SetValue(null, snapshot.Value)),
									tooltip: serverVar.Help);
							}
							else if (field.FieldType == typeof(long))
							{
								tab.AddInputButton(1, field.Name, 0.2f,
									new OptionInput(string.Empty, ap => $"{field.GetValue(null):n0}", 0,
										false, (ap, args) => field.SetValue(null, args.ToString(" ").ToLong())),
									new OptionButton($"<size=8>{snapshot.Value:n0}</size>",
										ap => field.SetValue(null, snapshot.Value)),
									tooltip: serverVar.Help);
							}
							else if (field.FieldType == typeof(ulong))
							{
								tab.AddInputButton(1, field.Name, 0.2f,
									new OptionInput(string.Empty, ap => $"{field.GetValue(null):n0}", 0,
										false, (ap, args) => field.SetValue(null, args.ToString(" ").ToUlong())),
									new OptionButton($"<size=8>{snapshot.Value:n0}</size>",
										ap => field.SetValue(null, snapshot.Value)),
									tooltip: serverVar.Help);
							}
							else if (field.FieldType == typeof(uint))
							{
								tab.AddInputButton(1, field.Name, 0.2f,
									new OptionInput(string.Empty, ap => $"{field.GetValue(null):n0}", 0,
										false, (ap, args) => field.SetValue(null, args.ToString(" ").ToUint())),
									new OptionButton($"<size=8>{snapshot.Value:n0}</size>",
										ap => field.SetValue(null, snapshot.Value)),
									tooltip: serverVar.Help);
							}
							else
							{
								tab.AddText(1, $"{field.Name} ({field.FieldType})", 10, "1 1 1 1");
							}
						}
					}
				}
			}

			return tab;
		}
	}
}

#endif
