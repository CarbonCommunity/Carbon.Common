﻿using Formatter = Oxide.Core.Libraries.Covalence.Formatter;

/*
 *
 * Copyright (c) 2022-2023 Carbon Community
 * All rights reserved.
 *
 */

namespace Oxide.Game.Rust.Libraries.Covalence
{
	public class RustPlayer : IPlayer
	{
		internal const string _ipPattern = @":{1}[0-9]{1}\d*";

		public object Object { get; set; }

		public BasePlayer BasePlayer => Object as BasePlayer;

		public RustPlayer() { }
		public RustPlayer(BasePlayer player)
		{
			Object = player;
			Id = player.UserIDString;
			Name = Oxide.Plugins.ExtensionMethods.Sanitize(player.displayName);
			LastCommand = 0;
			IsServer = false;
			perms = Interface.Oxide.GetLibrary<Permission>();
		}
		public RustPlayer(string id, UserData data)
		{
			Id = id;
			Name = Oxide.Plugins.ExtensionMethods.Sanitize(data.LastSeenNickname);
			LastCommand = 0;
			IsServer = false;
			perms = Interface.Oxide.GetLibrary<Permission>();
		}

		private static Permission perms;

		public CommandType LastCommand { get; set; }

		public string Name { get; set; } = string.Empty;

		public string Id { get; set; } = "server_console";

		public string Address => BasePlayer?.Connection == null ? null : Regex.Replace(BasePlayer.Connection.ipaddress, _ipPattern, string.Empty);

		public int Ping => BasePlayer == null ? 0 : Network.Net.sv.GetAveragePing(BasePlayer.Connection);

		public CultureInfo Language => CultureInfo.GetCultureInfo(IsConnected ? BasePlayer.net.connection.info.GetString("global.language", "en") : perms.GetUserData(Id).Language);

		public bool IsConnected => IsServer || (BasePlayer != null && BasePlayer.IsConnected);

		public bool IsSleeping => BasePlayer != null && BasePlayer.IsSleeping();

		public bool IsServer { get; set; } = true;

		public bool IsAdmin => ulong.TryParse(Id, out var id) && ServerUsers.Is(id, ServerUsers.UserGroup.Owner);

		public bool IsBanned => ulong.TryParse(Id, out var id) && ServerUsers.Is(id, ServerUsers.UserGroup.Banned);

		public TimeSpan BanTimeRemaining
		{
			get
			{
				if (!IsBanned)
				{
					return TimeSpan.Zero;
				}

				return TimeSpan.MaxValue;
			}
		}

		public float Health
		{
			get
			{
				return BasePlayer.health;
			}
			set
			{
				BasePlayer.health = value;
			}
		}

		public float MaxHealth
		{
			get
			{
				return BasePlayer.MaxHealth();
			}
			set
			{
				BasePlayer.SetMaxHealth(value);
			}
		}

		public void AddToGroup(string group)
		{
			if (!perms.GroupExists(group))
			{
				return;
			}
			if (!perms.GetUserData(Id).Groups.Add(group))
			{
				return;
			}

			// OnUserGroupAdded
			HookCaller.CallStaticHook(3469176166, Id, group);
		}

		public void Ban(string reason, TimeSpan duration = default)
		{
			if (IsBanned)
			{
				return;
			}

			ServerUsers.Set(BasePlayer.userID, ServerUsers.UserGroup.Banned, ((BasePlayer != null) ? BasePlayer.displayName : null) ?? "Unknown", reason, -1L);
			ServerUsers.Save();

			if (IsConnected)
			{
				Kick(reason);
			}
		}

		public bool BelongsToGroup(string group)
		{
			return perms.UserHasGroup(Id, group);
		}

		public void Command(string command, params object[] args)
		{
			BasePlayer.SendConsoleCommand(command, args);
		}

		public void GrantPermission(string perm)
		{
			perms.GrantUserPermission(Id, perm, null);
		}

		public bool HasPermission(string perm)
		{
			if (IsServer) return true;

			return perms.UserHasPermission(Id, perm);
		}

		public void Heal(float amount)
		{
			BasePlayer.Heal(amount);
		}

		public void Hurt(float amount)
		{
			BasePlayer.Hurt(amount);
		}

		public void Kick(string reason)
		{
			BasePlayer.Kick(reason);
		}

		public void Kill()
		{
			BasePlayer.Die(null);
		}

		public void Message(string message, string prefix, params object[] args)
		{
			if (string.IsNullOrEmpty(message))
			{
				return;
			}

			message = ((args.Length != 0) ? string.Format(Formatter.ToUnity(message), args) : Formatter.ToUnity(message));
			var text = (prefix != null) ? (prefix + " " + message) : message;

			if (IsServer) Carbon.Logger.Log(text);
			else BasePlayer.SendConsoleCommand("chat.add", 2, Id, text);
		}

		public void Message(string message)
		{
			Message(message, null, Array.Empty<string>());
		}

		public void Position(out float x, out float y, out float z)
		{
			var vector = BasePlayer.transform.position;
			x = vector.x;
			y = vector.y;
			z = vector.z;
		}

		public GenericPosition Position()
		{
			var position = BasePlayer.transform.position;
			return new GenericPosition(position.x, position.y, position.z);
		}

		public void RemoveFromGroup(string name)
		{
			if (!perms.GroupExists(name))
			{
				return;
			}

			var userData = perms.GetUserData(Id);
			if (name.Equals("*"))
			{
				if (userData.Groups.Count <= 0)
				{
					return;
				}
				userData.Groups.Clear();
				return;
			}
			else
			{
				if (!userData.Groups.Remove(name))
				{
					return;
				}

				// OnUserGroupRemoved
				HookCaller.CallStaticHook(2616322405, Id, name);
				return;
			}
		}

		public void Rename(string name)
		{
			name = (string.IsNullOrEmpty(name.Trim()) ? BasePlayer.displayName : name);
			SingletonComponent<ServerMgr>.Instance.persistance.SetPlayerName(BasePlayer.userID, name);
			BasePlayer.net.connection.username = name;
			BasePlayer.displayName = name;
			BasePlayer._name = name;
			BasePlayer.SendNetworkUpdateImmediate(false);
			var iPlayer = BasePlayer.AsIPlayer();
			iPlayer.Name = name;
			perms.UpdateNickname(BasePlayer.UserIDString, name);
			var position = BasePlayer.transform.position;
			Teleport(position.x, position.y, position.z);
		}

		public void Reply(string message, string prefix, params object[] args)
		{
			Message(message, prefix, args);
		}

		public void Reply(string message)
		{
			Message(message);
		}

		public void RevokePermission(string permission)
		{
			if (string.IsNullOrEmpty(permission))
			{
				return;
			}
			var userData = perms.GetUserData(Id);
			if (permission.EndsWith("*"))
			{
				if (!permission.Equals("*"))
				{
					userData.Perms.RemoveWhere((string p) => p.StartsWith(permission.TrimEnd('*'), StringComparison.OrdinalIgnoreCase));
					return;
				}
				if (userData.Perms.Count <= 0)
				{
					return;
				}
				userData.Perms.Clear();
				return;
			}
			else
			{
				if (!userData.Perms.Remove(permission))
				{
					return;
				}

				// OnUserPermissionRevoked
				HookCaller.CallStaticHook(1216290467, Id, permission);
				return;
			}
		}

		public void Teleport(float x, float y, float z)
		{
			if (BasePlayer.IsAlive() && !BasePlayer.IsSpectating())
			{
				try
				{
					var position = new Vector3(x, y, z);

					BasePlayer.EnsureDismounted();
					BasePlayer.SetParent(null, true, true);
					BasePlayer.SetServerFall(true);
					BasePlayer.MovePosition(position);
					BasePlayer.ClientRPC(RpcTarget.Player("ForcePositionTo", BasePlayer), position);
				}
				finally
				{
					BasePlayer.SetServerFall(false);
				}
			}
		}

		public void Teleport(GenericPosition position)
		{
			Teleport(position.X, position.Y, position.Z);
		}

		public void Unban()
		{
			if (!IsBanned)
			{
				return;
			}
			ServerUsers.Remove(BasePlayer.userID);
			ServerUsers.Save();
		}
	}

	public struct RustConsolePlayer : IPlayer
	{
		public object Object => null;

		public CommandType LastCommand { get => CommandType.Console; set { } }

		public string Name { get => "Server Console"; set { } }
		public string Id => "server_console";

		public CultureInfo Language => CultureInfo.InstalledUICulture;
		public string Address => "127.0.0.1";

		public int Ping => 0;
		public bool IsAdmin => true;
		public bool IsBanned => false;
		public bool IsConnected => true;
		public bool IsSleeping => false;
		public bool IsServer => true;

		public void Ban(string reason, TimeSpan duration) { }
		public TimeSpan BanTimeRemaining => TimeSpan.Zero;

		public void Heal(float amount) { }
		public float Health { get; set; }
		public void Hurt(float amount) { }

		public void Kick(string reason) { }
		public void Kill() { }

		public float MaxHealth { get; set; }

		public void Rename(string name) { }

		public void Teleport(float x, float y, float z) { }

		public void Teleport(GenericPosition pos)
		{
			Teleport(pos.X, pos.Y, pos.Z);
		}

		public void Unban() { }
		public void Position(out float x, out float y, out float z)
		{
			x = 0f;
			y = 0f;
			z = 0f;
		}

		public GenericPosition Position()
		{
			return new GenericPosition(0f, 0f, 0f);
		}

		public void Message(string message, string prefix, params object[] args)
		{
			message = (args != null && args.Length != 0) ? string.Format(message, args) : message;
			var format = (prefix != null) ? (prefix + " " + message) : message;
			Carbon.Logger.Log(format);
		}
		public void Message(string message)
		{
			Message(message, null, null);
		}
		public void Reply(string message, string prefix, params object[] args)
		{
			Message(message, prefix, args);
		}
		public void Reply(string message)
		{
			Message(message, null, null);
		}
		public void Command(string command, params object[] args)
		{
			ConsoleSystem.Run(ConsoleSystem.Option.Server, command, args);
		}
		public bool HasPermission(string perm)
		{
			return true;
		}
		public void GrantPermission(string perm) { }
		public void RevokePermission(string perm) { }
		public bool BelongsToGroup(string group)
		{
			return false;
		}
		public void AddToGroup(string group) { }
		public void RemoveFromGroup(string group) { }
	}
}
