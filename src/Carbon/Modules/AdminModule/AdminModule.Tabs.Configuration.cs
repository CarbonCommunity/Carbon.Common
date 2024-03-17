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

		internal const float _applyChangesCooldown = 60;
		internal static TimeSince _applyChangesTimeSince = _applyChangesCooldown / 2;

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
					tab.AddButton(0, "< Go Back", ap => Singleton.SetTab(session.Player, 0), ap => AdminModule.Tab.OptionButton.Types.Selected);

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
					tab.AddButton(0, "Apply Changes", ap =>
					{
						if (_applyChangesTimeSince > _applyChangesCooldown)
						{
							Singleton.GenerateTabs();
							_applyChangesTimeSince = 0;
							Refresh(tab, session);
							Singleton.Draw(ap.Player);
						}
					}, ap => _applyChangesTimeSince > _applyChangesCooldown ? OptionButton.Types.Selected : OptionButton.Types.None);
					tab.AddToggle(0, "Spectating Info Overlay",
						ap => Singleton.ConfigInstance.SpectatingInfoOverlay = !Singleton.ConfigInstance.SpectatingInfoOverlay,
						ap => !Singleton.ConfigInstance.SpectatingInfoOverlay);
					tab.AddToggle(0, "Hide Plugin Icons (Plugins tab)",
						ap => Singleton.DataInstance.HidePluginIcons = Singleton.DataInstance.HidePluginIcons,
						ap => Singleton.DataInstance.HidePluginIcons);

					tab.AddName(0, "Customization");

					tab.AddRange(0, "Title Underline Opacity", 0f, 100f, ap => Singleton.DataInstance.Colors.TitleUnderlineOpacity * 100f,
						(ap, value) =>
						{
							Singleton.DataInstance.Colors.TitleUnderlineOpacity = value * 0.01f;
							Singleton.Draw(ap.Player);
						}, ap => Singleton.DataInstance.Colors.TitleUnderlineOpacity.ToString("0.0"));
					tab.AddRange(0, "Option Width", 20f, 80f, ap => Singleton.DataInstance.Colors.OptionWidth * 100f,
						(ap, value) =>
						{
							Singleton.DataInstance.Colors.OptionWidth = value * 0.01f;
							Singleton.Draw(ap.Player);
						}, ap => Singleton.DataInstance.Colors.OptionWidth.ToString("0.0"));

					tab.AddColor(0, "Selected Tab Color", () => Singleton.DataInstance.Colors.SelectedTabColor,
						(ap, color1, color2, value) =>
						{
							Singleton.DataInstance.Colors.SelectedTabColor = CUI.HexToRustColor($"#{color1}", includeAlpha: false);
							Singleton.Draw(ap.Player);
						});
					tab.AddColor(0, "Editable Input Highlight", () => Singleton.DataInstance.Colors.EditableInputHighlight,
						(ap, color1, color2, value) =>
						{
							Singleton.DataInstance.Colors.EditableInputHighlight = CUI.HexToRustColor($"#{color1}", includeAlpha: false);
							Singleton.Draw(ap.Player);
						});
					tab.AddColor(0, "Name Text Color", () => Singleton.DataInstance.Colors.NameTextColor,
						(ap, color1, color2, value) =>
						{
							Singleton.DataInstance.Colors.NameTextColor = CUI.HexToRustColor($"#{color1}", value);
							Singleton.Draw(ap.Player);
						});
					tab.AddColor(0, "Option Name Color", () => Singleton.DataInstance.Colors.OptionNameColor,
						(ap, color1, color2, value) =>
						{
							Singleton.DataInstance.Colors.OptionNameColor = CUI.HexToRustColor($"#{color1}", value);
							Singleton.Draw(ap.Player);
						});

					tab.AddColor(0, "Button Selected Color", () => Singleton.DataInstance.Colors.ButtonSelectedColor,
						(ap, color1, color2, value) =>
						{
							Singleton.DataInstance.Colors.ButtonSelectedColor = CUI.HexToRustColor($"#{color1}", value);
							Singleton.Draw(ap.Player);
						});
					tab.AddColor(0, "Button Warned Color", () => Singleton.DataInstance.Colors.ButtonWarnedColor,
						(ap, color1, color2, value) =>
						{
							Singleton.DataInstance.Colors.ButtonWarnedColor = CUI.HexToRustColor($"#{color1}", value);
							Singleton.Draw(ap.Player);
						});
					tab.AddColor(0, "Button Important Color", () => Singleton.DataInstance.Colors.ButtonImportantColor,
						(ap, color1, color2, value) =>
						{
							Singleton.DataInstance.Colors.ButtonImportantColor = CUI.HexToRustColor($"#{color1}", value);
							Singleton.Draw(ap.Player);
						});

					tab.AddColor(0, "Option Color (1st)", () => Singleton.DataInstance.Colors.OptionColor,
						(ap, color1, color2, value) =>
						{
							Singleton.DataInstance.Colors.OptionColor = CUI.HexToRustColor($"#{color1}", value);
							Singleton.Draw(ap.Player);
						});
					tab.AddColor(0, "Option Color (2nd)", () => Singleton.DataInstance.Colors.OptionColor2,
						(ap, color1, color2, value) =>
						{
							Singleton.DataInstance.Colors.OptionColor2 = CUI.HexToRustColor($"#{color1}", value);
							Singleton.Draw(ap.Player);
						});
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
