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
	public class PlayersTab
	{
		internal static RustPlugin Core = Community.Runtime.CorePlugin;
		internal static List<BasePlayer> BlindedPlayers = new();

		public static Tab Get()
		{
			var players = new Tab("players", "Players", Community.Runtime.CorePlugin, (instance, tab) =>
			{
				tab.ClearColumn(1);
				RefreshPlayers(tab, instance);
			}, "players.use");

			players.AddColumn(0);
			players.AddColumn(1);

			return players;
		}

		public static void RefreshPlayers(Tab tab, PlayerSession ap)
		{
			tab.ClearColumn(0);

			tab.AddInput(0, "Search", ap => ap?.GetStorage<string>(tab, "playerfilter"), (ap2, args) =>
			{
				ap2.SetStorage(tab, "playerfilter", args.ToString(" "));
				RefreshPlayers(tab, ap2);
			});

			var onlinePlayers = BasePlayer.allPlayerList.Distinct().Where(x => x.userID.IsSteamId() && x.IsConnected)
				.OrderBy(x => x.Connection?.connectionTime);
			tab.AddName(0, $"Online ({onlinePlayers.Count():n0})");
			foreach (var player in onlinePlayers)
			{
				AddPlayer(tab, ap, player);
			}

			if (onlinePlayers.Count() == 0) tab.AddText(0, "No online players found.", 10, "1 1 1 0.4");

			var offlinePlayers = BasePlayer.allPlayerList.Distinct().Where(x => x.userID.IsSteamId() && !x.IsConnected);
			tab.AddName(0, $"Offline ({offlinePlayers.Count():n0})");
			foreach (var player in offlinePlayers)
			{
				AddPlayer(tab, ap, player);
			}

			if (offlinePlayers.Count() == 0) tab.AddText(0, "No offline players found.", 10, "1 1 1 0.4");
		}

		public static void AddPlayer(Tab tab, PlayerSession ap, BasePlayer player)
		{
			if (ap != null)
			{
				var filter = ap.GetStorage<string>(tab, "playerfilter");

				if (!string.IsNullOrEmpty(filter) && !(player.displayName.ToLower().Contains(filter.ToLower()) || player.UserIDString.Contains(filter))) return;
			}

			tab.AddButton(0, $"{player.displayName}", _ =>
			{
				ap.SetStorage(tab, "playerfilterpl", player);
				ShowInfo(tab, ap, player);
			}, aap => aap == null || !(aap.GetStorage<BasePlayer>(tab, "playerfilterpl", null) == player) ? Tab.OptionButton.Types.None : Tab.OptionButton.Types.Selected);
		}
		public static void ShowInfo(Tab tab, PlayerSession aap, BasePlayer player)
		{
			tab.ClearColumn(1);

			tab.AddName(1, $"Player Information", TextAnchor.MiddleLeft);
			tab.AddInput(1, "Name", _ => player.displayName, (_, args) =>
			{
				player.AsIPlayer().Rename(args.ToString(" "));
			});
			tab.AddInput(1, "Steam ID", _ => player.UserIDString, null);
			tab.AddInput(1, "Net ID", _ => $"{player.net?.ID}", null);
			if (Singleton.HasAccess(aap.Player, "players.see_ips"))
			{
				tab.AddInput(1, "IP", _ => $"{player.net?.connection?.ipaddress}", null, hidden: true);
			}
			try
			{
				var position = player.transform.position;
				tab.AddInput(1, "Position", _ => $"{position} [{PhoneController.PositionToGridCoord(position)}]", null);
			}
			catch { }

			if (Singleton.HasAccess(aap.Player, "permissions.use"))
			{
				tab.AddName(1, $"Permissions", TextAnchor.MiddleLeft);
				{
					tab.AddButton(1, "View Permissions", ap =>
					{
						var perms = Singleton.FindTab("permissions");
						var permission = Community.Runtime.CorePlugin.permission;
						Singleton.SetTab(ap.Player, "permissions");

						ap.SetStorage(tab, "player", player.UserIDString);
						PermissionsTab.GeneratePlayers(perms, permission, ap);
						PermissionsTab.GenerateHookables(perms, ap, permission, permission.FindUser(player.UserIDString), null, PermissionsTab.HookableTypes.Plugin);
					}, _ => Tab.OptionButton.Types.Important);
				}
			}

			if (Singleton.Permissions.UserHasPermission(aap.Player.UserIDString, "carbon.cmod"))
			{
				tab.AddButtonArray(1, new Tab.OptionButton("Kick", _ =>
				{
					Singleton.Modal.Open(aap.Player, $"Kick {player.displayName}", new Dictionary<string, ModalModule.Modal.Field>
					{
						["reason"] = ModalModule.Modal.Field.Make("Reason", ModalModule.Modal.Field.FieldTypes.String, @default: "Stop doing that.")
					}, onConfirm: (_, m) =>
					{
						player.Kick(m.Get<string>("reason"));
					});
				}), new Tab.OptionButton("Ban", ap =>
				{
					Singleton.Modal.Open(aap.Player, $"Ban {player.displayName}", new Dictionary<string, ModalModule.Modal.Field>
					{
						["reason"] = ModalModule.Modal.Field.Make("Reason", ModalModule.Modal.Field.FieldTypes.String, @default: "Stop doing that."),
						["until"] = ModalModule.Modal.ButtonField.MakeButton("Until", "Select Date", _ =>
						{
							Core.NextTick(() => Singleton.DatePicker.Draw(ap.Player, date => ap.SetStorage(tab, "date", date)));
						})
					}, onConfirm: (_, m) =>
					{
						var date = ap.GetStorage(tab, "date", DateTime.UtcNow.AddYears(100));
						var now = DateTime.UtcNow;
						date = new DateTime(date.Year, date.Month, date.Day, now.Hour, now.Minute, now.Second, DateTimeKind.Utc);
						var then = now - date;

						player.AsIPlayer().Ban(m.Get<string>("reason"), then);
					});
				}), new Tab.OptionButton(player.IsSleeping() ? "End Sleep" : "Sleep", ap =>
				{
					if (player.IsSleeping())
					{
						player.EndSleeping();
					}
					else
					{
						player.StartSleeping();
					}

					ShowInfo(tab, ap, player);
				}), new Tab.OptionButton("Hostility", ap =>
				{
					var fields = new Dictionary<string, ModalModule.Modal.Field>
					{
						["duration"] = ModalModule.Modal.Field.Make("Duration",
							ModalModule.Modal.Field.FieldTypes.Float, true, 60f)
					};

					Singleton.Modal.Open(ap.Player, "Player Hostile", fields, (ap, modal) =>
					{
						var duration = modal.Get<float>("duration").Clamp(0f, float.MaxValue);
						player.State.unHostileTimestamp = Network.TimeEx.currentTimestamp + duration;
						player.DirtyPlayerState();
						player.ClientRPCPlayer(null, player, "SetHostileLength", duration);
						fields.Clear();
						fields = null;
						ShowInfo(tab, aap, player);
						Singleton.Draw(aap.Player);
					}, () =>
					{
						fields.Clear();
						fields = null;
					});
				}));
			}
			else tab.AddText(1, $"You need 'carbon.cmod' permission to kick, ban, sleep or change player hostility",
				10, "1 1 1 0.4");

			tab.AddName(1, $"Actions", TextAnchor.MiddleLeft);

			if (Singleton.HasAccess(aap.Player, "entities.tp_entity"))
			{
				tab.AddButtonArray(1,
					new Tab.OptionButton("TeleportTo", ap => { ap.Player.Teleport(player.transform.position); }),
					new Tab.OptionButton("Teleport2Me", _ =>
					{
						tab.CreateDialog($"Are you sure about that?", ap =>
						{
							player.Teleport(ap.Player.transform.position);
						}, null);
					}));
			}

			if (Singleton.HasAccess(aap.Player, "entities.loot_players"))
			{
				tab.AddButtonArray(1,
					new Tab.OptionButton("Loot", ap =>
					{
						EntitiesTab.LastContainerLooter = ap;
						ap.SetStorage(tab, "lootedent", player);
						EntitiesTab.SendEntityToPlayer(ap.Player, player);

						Core.timer.In(0.2f, () => Singleton.Close(ap.Player));
						Core.timer.In(0.5f, () =>
						{
							EntitiesTab.SendEntityToPlayer(ap.Player, player);

							ap.Player.inventory.loot.Clear();
							ap.Player.inventory.loot.PositionChecks = false;
							ap.Player.inventory.loot.entitySource = RelationshipManager.ServerInstance;
							ap.Player.inventory.loot.itemSource = null;
							ap.Player.inventory.loot.AddContainer(player.inventory.containerMain);
							ap.Player.inventory.loot.AddContainer(player.inventory.containerWear);
							ap.Player.inventory.loot.AddContainer(player.inventory.containerBelt);
							ap.Player.inventory.loot.MarkDirty();
							ap.Player.inventory.loot.SendImmediate();

							ap.Player.ClientRPCPlayer(null, ap.Player, "RPC_OpenLootPanel", "player_corpse");
						});
					}),
					new Tab.OptionButton("Strip", ap =>
					{
						player.inventory.Strip();
					}),
					new Tab.OptionButton("Respawn", _ =>
					{
						tab.CreateDialog($"Are you sure about that?", _ =>
						{
							player.Hurt(player.MaxHealth());
							player.Respawn();
							player.EndSleeping();
						}, null);
					}));
			}

			if (Singleton.HasAccess(aap.Player, "players.inventory_management"))
			{
				tab.AddName(1, "Inventory Lock");
				tab.AddButtonArray(1,
					new Tab.OptionButton("Main", _ =>
						{
							player.inventory.containerMain.SetLocked(!player.inventory.containerMain.IsLocked());
						},
						_ => player.inventory.containerMain.IsLocked()
							? Tab.OptionButton.Types.Important
							: Tab.OptionButton.Types.None),
					new Tab.OptionButton("Belt", _ =>
						{
							player.inventory.containerBelt.SetLocked(!player.inventory.containerBelt.IsLocked());
						},
						_ => player.inventory.containerBelt.IsLocked()
							? Tab.OptionButton.Types.Important
							: Tab.OptionButton.Types.None),
					new Tab.OptionButton("Wear", _ =>
						{
							player.inventory.containerWear.SetLocked(!player.inventory.containerWear.IsLocked());
						},
						_ => player.inventory.containerWear.IsLocked()
							? Tab.OptionButton.Types.Important
							: Tab.OptionButton.Types.None));
			}

			if (Singleton.HasTab("entities"))
			{
				tab.AddButton(1, "Select Entity", ap2 =>
				{
					Singleton.SetTab(ap2.Player, "entities");
					var tab = Singleton.GetTab(ap2.Player);
					EntitiesTab.SelectEntity(tab, ap2, player);
					EntitiesTab.DrawEntities(tab, ap2);
					EntitiesTab.DrawEntitySettings(tab, 1, ap2);
				});
			}

			tab.AddInput(1, "PM", null, (ap, args) => { player.ChatMessage($"[{ap.Player.displayName}]: {args.ToString(" ")}"); });
			if (aap.Player != player && aap.Player.spectateFilter != player.UserIDString)
			{
				tab.AddButton(1, "Spectate", ap =>
				{
					StartSpectating(ap.Player, player);
					ShowInfo(tab, ap, player);
				});
			}

			if (Singleton.HasAccess(aap.Player, "entities.spectate_players"))
			{
				if (!string.IsNullOrEmpty(aap.Player.spectateFilter) &&
				    (aap.Player.UserIDString == player.UserIDString ||
				     aap.Player.spectateFilter == player.UserIDString))
				{
					tab.AddButton(1, "End Spectating", ap =>
					{
						StopSpectating(ap.Player);
						ShowInfo(tab, ap, player);
					}, _ => Tab.OptionButton.Types.Selected);
				}
			}

			if (Singleton.HasAccess(aap.Player, "entities.blind_players"))
			{
				if (!BlindedPlayers.Contains(player))
				{
					tab.AddButton(1, "Blind Player", _ =>
					{
						tab.CreateDialog("Are you sure you want to blind the player?", ap =>
						{
							using var cui = new CUI(Singleton.Handler);
							var container = cui.CreateContainer("blindingpanel", "0 0 0 1", needsCursor: true,
								needsKeyboard: Singleton.HandleEnableNeedsKeyboard(ap));
							cui.CreateClientImage(container, "blindingpanel",
								"https://carbonmod.gg/assets/media/cui/bsod.png", "1 1 1 1");
							cui.Send(container, player);
							BlindedPlayers.Add(player);
							ShowInfo(tab, ap, player);

							if (ap.Player == player) Core.timer.In(1, () => { Singleton.Close(player); });
						}, null);
					});
				}
				else
				{
					tab.AddButton(1, "Unblind Player", ap =>
					{
						using var cui = new CUI(Singleton.Handler);
						cui.Destroy("blindingpanel", player);
						BlindedPlayers.Remove(player);
						ShowInfo(tab, ap, player);
					}, _ => Tab.OptionButton.Types.Selected);
				}
			}

			tab.AddName(1, "Stats");
			tab.AddName(1, "Combat");
			tab.AddRange(1, "Health", 0, player.MaxHealth(), _ => player.health, (_, value) => player.SetHealth(value), _ => $"{player.health:0}");

			tab.AddRange(1, "Thirst", 0, player.metabolism.hydration.max, _ => player.metabolism.hydration.value, (_, value) => player.metabolism.hydration.SetValue(value), _ => $"{player.metabolism.hydration.value:0}");
			tab.AddRange(1, "Hunger", 0, player.metabolism.calories.max, _ => player.metabolism.calories.value, (_, value) => player.metabolism.calories.SetValue(value), _ => $"{player.metabolism.calories.value:0}");
			tab.AddRange(1, "Radiation", 0, player.metabolism.radiation_poison.max, _ => player.metabolism.radiation_poison.value, (_, value) => player.metabolism.radiation_poison.SetValue(value), _ => $"{player.metabolism.radiation_poison.value:0}");
			tab.AddRange(1, "Bleeding", 0, player.metabolism.bleeding.max, _ => player.metabolism.bleeding.value, (_, value) => player.metabolism.bleeding.SetValue(value), _ => $"{player.metabolism.bleeding.value:0}");
			tab.AddButton(1, "Empower Stats", ap =>
			{
				player.SetHealth(player.MaxHealth());
				player.metabolism.hydration.SetValue(player.metabolism.hydration.max);
				player.metabolism.calories.SetValue(player.metabolism.calories.max);
				player.metabolism.radiation_poison.SetValue(0);
				player.metabolism.bleeding.SetValue(0);
			});

			if (Singleton.HasAccess(aap.Player, "players.craft_queue"))
			{
				tab.AddName(1, "Crafting");

				var queue = player.inventory.crafting.queue.Where(x => !x.cancelled);
				foreach (var craft in queue)
				{
					tab.AddInputButton(1,
						$"{craft.blueprint.targetItem.displayName.english} (x{craft.amount}, {TimeEx.Format(craft.endTime - UnityEngine.Time.realtimeSinceStartup)})",
						0.1f,
						new Tab.OptionInput(null,
							_ =>
								$"<size=8>{craft.takenItems.Select(x => $"{x.info.displayName.english} x {x.amount}").ToString(", ")}</size>",
							0, true, null),
						new Tab.OptionButton("X", TextAnchor.MiddleCenter, ap =>
						{
							player.inventory.crafting.CancelTask(craft.taskUID, true);
							ShowInfo(tab, ap, player);
						}, _ => Tab.OptionButton.Types.Important));
				}

				if (!queue.Any())
				{
					tab.AddText(1, "No crafts.", 8, "1 1 1 0.5");
				}
			}
		}
	}
}

#endif
