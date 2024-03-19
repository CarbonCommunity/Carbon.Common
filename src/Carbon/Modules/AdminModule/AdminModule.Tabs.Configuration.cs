﻿#if !MINIMAL

/*
 *
 * Copyright (c) 2022-2023 Carbon Community
 * All rights reserved.
 *
 */

using System.Windows.Controls;
using API.Commands;
using Newtonsoft.Json;
using Oxide.Game.Rust.Cui;
using UnityEngine.UI;

namespace Carbon.Modules;

public partial class AdminModule
{
	public class ConfigurationTab : Tab
	{
		public ConfigurationTab(string id, string name, RustPlugin plugin, Action<PlayerSession, Tab> onChange = null) :
			base(id, name, plugin, onChange)
		{
		}

		internal static ConfigurationTab _instance;
		internal const float _applyChangesCooldown = 60;
		internal static TimeSince _applyChangesTimeSince = _applyChangesCooldown / 2;

		public static readonly string[] AuthLevels = new[] { "User", "Moderator", "Admin", "Developer" };

		public enum ConfigTabs
		{
			ConVars,
			Items
		}

		public static ConfigurationTab GetOrCache() => _instance ?? (_instance = Make());

		public static ConfigurationTab Make()
		{
			var tab = new ConfigurationTab("configuration", "Configuration", Community.Runtime.CorePlugin,
				(session, tab) =>
				{
					session.ClearStorage(null, "itemtabitem");
					Refresh(tab, session);
				}) { Fullscreen = true };
			tab.Over = (t, cui, container, panel, ap) =>
			{
				var currentItem = ap.GetStorage<ItemDefinition>(null, "itemtabitem");

				if (currentItem != null)
				{
					var main = cui.CreatePanel(container, panel, "0.1 0.1 0.1 0.5", blur: true, xMin: 0.5f);

					var xButton = cui.CreateProtectedButton(container, main, "0.7 0.1 0.05 1", Cache.CUI.BlankColor,
						string.Empty, 0,
						command: "adminmodule.itemclear", xMin: 0.92f, xMax: 0.98f, yMin: 0.94f, yMax: 0.99f);
					cui.CreateImage(container, xButton, "close", "1 0.4 0.35 0.9", xMin: 0.2f, xMax: 0.8f, yMin: 0.2f,
						yMax: 0.8f);

					var icon = cui.CreatePanel(container, main, "0.1 0.1 0.1 0.5",
						xMin: 0.06f, xMax: 0.42f, yMin: 0.6f, yMax: 0.9f);
					cui.CreateItemImage(container, icon, currentItem.itemid, 0, "1 1 1 1",
						xMin: 0.05f, xMax: 0.95f, yMin: 0.05f, yMax: 0.95f);

					cui.CreateText(container, main, "1 1 1 1", currentItem.displayName.english, 16,
						xMin: 0.45f, yMax: 0.88f, align: TextAnchor.UpperLeft,
						font: CUI.Handler.FontTypes.RobotoCondensedBold);
					cui.CreateText(container, main, "0.8 0.2 0.15 1",
						currentItem.category.ToString().ToUpper().SpacedString(1), 10,
						xMin: 0.45f, yMax: 0.84f, align: TextAnchor.UpperLeft,
						font: CUI.Handler.FontTypes.RobotoCondensedBold);

					cui.CreateText(container, main, "0.8 0.8 0.8 1", "DESCRIPTION", 11,
						xMin: 0.45f, yMax: 0.79f, align: TextAnchor.UpperLeft,
						font: CUI.Handler.FontTypes.RobotoCondensedBold);
					cui.CreateText(container, main, "0.8 0.8 0.8 0.6", currentItem.displayDescription.english, 11,
						xMin: 0.45f, xMax: 0.8f, yMax: 0.75f, align: TextAnchor.UpperLeft,
						font: CUI.Handler.FontTypes.RobotoCondensedRegular);

					cui.CreatePanel(container, main, "0.8 0.8 0.8 0.2",
						xMin: 0.06f, xMax: 0.94f, yMin: 0.54f, yMax: 0.55f);

					cui.CreateText(container, main, "1 1 1 1", "CREATE", 16,
						xMin: 0.07f, yMax: 0.5f, align: TextAnchor.UpperLeft,
						font: CUI.Handler.FontTypes.RobotoCondensedBold);

					cui.CreateText(container, main, "1 1 1 0.5", "Generate an inventory item based on this definition.",
						10,
						xMin: 0.07f, yMax: 0.46f, align: TextAnchor.UpperLeft,
						font: CUI.Handler.FontTypes.RobotoCondensedRegular);

					cui.CreateText(container, main, "1 1 1 0.7", "CUSTOM NAME", 12,
						xMin: 0.07f, yMax: 0.4f, align: TextAnchor.UpperLeft,
						font: CUI.Handler.FontTypes.RobotoCondensedRegular);
					{
						var name = cui.CreatePanel(container, main, "0.1 0.1 0.1 0.95",
							xMin: 0.07f, xMax: 0.45f, yMin: 0.31f, yMax: 0.36f);
						cui.CreateProtectedInputField(container, name, "1 1 1 1",
							ap.GetStorage<string>(null, "itemscustomname"), 12, xMin: 0.05f,
							characterLimit: 0, readOnly: false, command: "adminmodule.itemsetting customname",
							align: TextAnchor.MiddleLeft);
					}

					var yOffset = 0;
					var xOffset = 0;

					cui.CreateText(container, main, "1 1 1 0.7", "SKIN", 12,
						xMin: 0.07f, yMax: 0.4f, OxMax: xOffset += 200, OxMin: xOffset, align: TextAnchor.UpperLeft,
						font: CUI.Handler.FontTypes.RobotoCondensedRegular);
					{
						var name = cui.CreatePanel(container, main, "0.1 0.1 0.1 0.95",
							xMin: 0.07f, xMax: 0.45f, yMin: 0.31f, yMax: 0.36f, OxMax: xOffset, OxMin: xOffset);
						cui.CreateProtectedInputField(container, name, "1 1 1 1",
							ap.GetStorage<ulong>(null, "itemsskin").ToString(), 12, xMin: 0.05f,
							characterLimit: 0, readOnly: false, command: "adminmodule.itemsetting skin",
							align: TextAnchor.MiddleLeft);
					}

					cui.CreateText(container, main, "1 1 1 0.7", "AMOUNT", 12,
						xMin: 0.07f, yMax: 0.4f, OyMax: yOffset -= 60, OyMin: yOffset, align: TextAnchor.UpperLeft,
						font: CUI.Handler.FontTypes.RobotoCondensedRegular);
					{
						var name = cui.CreatePanel(container, main, "0.1 0.1 0.1 0.95",
							xMin: 0.07f, xMax: 0.45f, yMin: 0.31f, yMax: 0.36f, OyMax: yOffset, OyMin: yOffset);
						cui.CreateProtectedInputField(container, name, "1 1 1 1",
							ap.GetStorage(null, "itemsamount", 1).ToString(), 12, xMin: 0.05f,
							characterLimit: 0, readOnly: false, command: "adminmodule.itemsetting amount",
							align: TextAnchor.MiddleLeft);
					}

					cui.CreateText(container, main, "1 1 1 0.7", "BLUEPRINT", 12,
						xMin: 0.07f, yMax: 0.4f, OxMax: xOffset, OxMin: xOffset, OyMax: yOffset, OyMin: yOffset,
						align: TextAnchor.UpperLeft, font: CUI.Handler.FontTypes.RobotoCondensedRegular);
					{
						var name = cui.CreateProtectedButton(container, main, "0.1 0.1 0.1 0.95", text: string.Empty,
							textColor: Cache.CUI.BlankColor, size: 0,
							xMin: 0.07f, xMax: 0.125f, yMin: 0.31f, yMax: 0.36f, OxMax: xOffset, OxMin: xOffset,
							OyMax: yOffset, OyMin: yOffset, command: "adminmodule.itemsetting blueprint");

						if (ap.GetStorage<bool>(null, "itemsblueprint"))
						{
							cui.CreateImage(container, name, "checkmark", "0.4 0.9 0.4 0.9", xMin: 0.15f, xMax: 0.85f,
								yMin: 0.15f, yMax: 0.85f);
						}
					}

					cui.CreateText(container, main, "1 1 1 0.7", "ITEM TEXT", 12,
						xMin: 0.07f, yMax: 0.4f, OyMax: yOffset -= 60, OyMin: yOffset,
						align: TextAnchor.UpperLeft, font: CUI.Handler.FontTypes.RobotoCondensedRegular);
					{
						var name = cui.CreateProtectedButton(container, main, "0.1 0.1 0.1 0.95", text: string.Empty,
							textColor: Cache.CUI.BlankColor, size: 0,
							xMin: 0.07f, xMax: 0.875f, yMin: 0.26f, yMax: 0.36f,
							OyMax: yOffset, OyMin: yOffset, command: string.Empty);

						cui.CreateProtectedInputField(container, name, "1 1 1 1",
							ap.GetStorage(null, "itemstext", string.Empty).ToString(), 12, xMin: 0.02f, yMax: 0.9f,
							characterLimit: 0, readOnly: false, command: "adminmodule.itemsetting text",
							align: TextAnchor.UpperLeft, needsKeyboard: true, lineType: InputField.LineType.MultiLineNewline);
					}

					cui.CreateProtectedButton(container, main, "0.4 0.6 0.3 1", "0.8 1 0.7 1", "CREATE ITEM", 10,
						xMin: 0.07f, xMax: 0.25f, yMin: 0.1f, yMax: 0.15f, OyMin: 200f, OyMax: 200f, OxMin: 290, OxMax: 290,
						font: CUI.Handler.FontTypes.RobotoCondensedBold, command: "adminmodule.itemcreate");
				}
			};

			static void Refresh(Tab tab, PlayerSession session)
			{
				tab.ClearColumn(0);
				{
					tab.AddButton(0, "< Go Back", ap => Singleton.SetTab(session.Player, 0),
						ap => AdminModule.Tab.OptionButton.Types.Selected);

					tab.AddName(0, "Configuration");
					tab.AddDropdown(0, "Minimum Auth-Level", ap => Singleton.ConfigInstance.MinimumAuthLevel,
						(ap, index) => Singleton.ConfigInstance.MinimumAuthLevel = index, AuthLevels);

					tab.AddName(0, "Tabs");
					tab.AddToggle(0, "Display Entities",
						ap => Singleton.ConfigInstance.DisableEntitiesTab =
							!Singleton.ConfigInstance.DisableEntitiesTab,
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
						},
						ap => _applyChangesTimeSince > _applyChangesCooldown
							? OptionButton.Types.Selected
							: OptionButton.Types.None);
					tab.AddToggle(0, "Spectating Info Overlay",
						ap => Singleton.ConfigInstance.SpectatingInfoOverlay =
							!Singleton.ConfigInstance.SpectatingInfoOverlay,
						ap => !Singleton.ConfigInstance.SpectatingInfoOverlay);
					tab.AddToggle(0, "Hide Plugin Icons (Plugins tab)",
						ap => Singleton.DataInstance.HidePluginIcons = Singleton.DataInstance.HidePluginIcons,
						ap => Singleton.DataInstance.HidePluginIcons);

					tab.AddName(0, "Customization");

					tab.AddRange(0, "Title Underline Opacity", 0f, 100f,
						ap => Singleton.DataInstance.Colors.TitleUnderlineOpacity * 100f,
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
							Singleton.DataInstance.Colors.SelectedTabColor =
								CUI.HexToRustColor($"#{color1}", includeAlpha: false);
							Singleton.Draw(ap.Player);
						});
					tab.AddColor(0, "Editable Input Highlight",
						() => Singleton.DataInstance.Colors.EditableInputHighlight,
						(ap, color1, color2, value) =>
						{
							Singleton.DataInstance.Colors.EditableInputHighlight =
								CUI.HexToRustColor($"#{color1}", includeAlpha: false);
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
							Singleton.DataInstance.Colors.ButtonImportantColor =
								CUI.HexToRustColor($"#{color1}", value);
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
					var configTab = session.GetStorage(tab, "configtab", ConfigTabs.ConVars);

					tab.AddButtonArray(1,
						new OptionButton("ConVars", ap =>
						{
							session.SetStorage(tab, "configtab", ConfigTabs.ConVars);
							Refresh(tab, ap);
						}, ap => configTab == ConfigTabs.ConVars ? OptionButton.Types.Selected : OptionButton.Types.None),
						new OptionButton("Items", ap =>
						{
							session.SetStorage(tab, "configtab", ConfigTabs.Items);
							Refresh(tab, ap);
						}, ap => configTab == ConfigTabs.Items ? OptionButton.Types.Selected : OptionButton.Types.None));

					tab.AddName(1, configTab.ToString());

					switch (configTab)
					{
						case ConfigTabs.ConVars:
						{
							var convarSearch = session.GetStorage(tab, "convarsearch", string.Empty);
							var currentlyDisplaying = ConVarSnapshots.Snapshots.Count(x =>
								string.IsNullOrEmpty(convarSearch) || x.Key.Contains(convarSearch));

							if (string.IsNullOrEmpty(convarSearch))
							{
								tab.AddInput(1, $"Search ({currentlyDisplaying:n0})", ap => convarSearch, 0, false,
									(ap, args) =>
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
								var fields = type.GetFields(BindingFlags.Static | BindingFlags.Public).Where(x =>
									x.GetCustomAttribute<ServerVar>() != null && (string.IsNullOrEmpty(convarSearch)
										? true
										: $"{factoryName}.{x.Name}".Contains(convarSearch)));

								if (!fields.Any())
								{
									continue;
								}

								tab.AddText(1, $"<color=orange>></color> {type.Name.ToCamelCase()}— {factoryName}.*",
									13, "1 1 1 0.4", TextAnchor.LowerLeft);

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
										tab.AddToggle(1, $"{field.Name} (default: {snapshot.Value})",
											ap => field.SetValue(null, !(bool)field.GetValue(null)),
											ap => (bool)field.GetValue(null),
											tooltip: serverVar.Help);
									}
									else if (field.FieldType == typeof(float))
									{
										tab.AddInputButton(1, field.Name, 0.2f,
											new OptionInput(string.Empty, ap => $"{field.GetValue(null):n0}", 0,
												false,
												(ap, args) => field.SetValue(null, args.ToString(" ").ToFloat())),
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
												false,
												(ap, args) => field.SetValue(null, args.ToString(" ").ToUlong())),
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

							break;
						}

						case ConfigTabs.Items:
						{
							var itemSearch = session.GetStorage(tab, "itemsearch", string.Empty);
							var items = ItemManager.itemList.Where(x =>
								string.IsNullOrEmpty(itemSearch) || ((x.displayName.english.Contains(itemSearch, CompareOptions.IgnoreCase)) ||
								                                     x.shortname.Contains(itemSearch, CompareOptions.IgnoreCase) || x.itemid.ToString().Contains(itemSearch)));
							var currentlyDisplaying = items.Count();

							if (string.IsNullOrEmpty(itemSearch))
							{
								tab.AddInput(1, $"Search ({currentlyDisplaying:n0})", ap => itemSearch, 0, false,
									(ap, args) =>
									{
										ap.SetStorage(tab, "itemsearch", args.ToString(" "));
										Refresh(tab, ap);
									});
							}
							else
							{
								tab.AddInputButton(1, $"Search ({currentlyDisplaying:n0})", 0.08f,
									new OptionInput(string.Empty, ap => itemSearch, 0, false, (ap, args) =>
									{
										ap.SetStorage(tab, "itemsearch", args.ToString(" "));
										Refresh(tab, ap);
									}),
									new OptionButton("X", ap =>
									{
										ap.SetStorage(tab, "itemsearch", string.Empty);
										Refresh(tab, ap);
									}, ap => OptionButton.Types.Important));
							}

							foreach (var category in Enum.GetNames(typeof(ItemCategory)))
							{
								var parsedCategory = (ItemCategory)Enum.Parse(typeof(ItemCategory), category);
								var filteredItems = items.Where(x => x.category == parsedCategory);

								if (!filteredItems.Any()) continue;

								tab.AddName(1, $"<color=orange>></color> {category}");

								foreach (var item in filteredItems)
								{
									tab.AddButton(1, $"{item.displayName.english}  ({item.shortname})", ap =>
									{
										ap.SetStorage(null, "itemtabitem", item);
										Singleton.Draw(ap.Player);
									});
								}
							}

							break;
						}
					}
				}
			}

			return tab;
		}
	}

	[Conditional("!MINIMAL")]
	[ProtectedCommand("adminmodule.itemsetting")]
	private void ItemSetting(ConsoleSystem.Arg arg)
	{
		var player = arg.Player();
		var session = GetPlayerSession(player);
		var setting = arg.GetString(0);
		var value = arg.Args.Skip(1).ToString(" ");

		switch (setting)
		{
			case "customname":
				session.SetStorage(null, "itemscustomname", value);
				break;

			case "amount":
				session.SetStorage(null, "itemsamount", (int.TryParse(value, out var intValue) ? intValue : 1).Clamp(1, int.MaxValue));
				break;

			case "skin":
				session.SetStorage(null, "itemsskin", value.ToUlong());
				break;

			case "text":
				session.SetStorage(null, "itemstext", value);
				break;

			case "blueprint":
				session.SetStorage(null, "itemsblueprint", !session.GetStorage(null, "itemsblueprint", false));
				break;
		}

		Draw(player);
	}

	[Conditional("!MINIMAL")]
	[ProtectedCommand("adminmodule.itemcreate")]
	private void ItemCreate(ConsoleSystem.Arg arg)
	{
		var player = arg.Player();
		var session = GetPlayerSession(player);

		var item = session.GetStorage<ItemDefinition>(null, "itemtabitem");
		if (item == null) return;

		var isBlueprint = session.GetStorage(null, "itemsblueprint", false);

		var resultItem = ItemManager.CreateByName(isBlueprint ? "blueprintbase" : item.shortname, session.GetStorage<int>(null, "itemsamount", 1),
			session.GetStorage<ulong>(null, "itemsskin", 0));

		resultItem.name = session.GetStorage<string>(null, "itemscustomname", string.Empty);
		resultItem.skin = session.GetStorage<ulong>(null, "itemsskin");
		resultItem.text = session.GetStorage<string>(null, "itemstext", string.Empty);

		if (isBlueprint)
		{
			resultItem.blueprintTarget = item.itemid;
		}

		player.GiveItem(resultItem);

		session.ClearStorage(null, "itemtabitem");

		Draw(session.Player);
	}

	[Conditional("!MINIMAL")]
	[ProtectedCommand("adminmodule.itemclear")]
	private void ItemClear(ConsoleSystem.Arg arg)
	{
		var player = arg.Player();
		var session = GetPlayerSession(player);

		session.ClearStorage(null, "itemtabitem");

		Draw(session.Player);
	}
}

#endif
