/*
 *
 * Copyright (c) 2022-2023 Carbon Community
 * All rights reserved.
 *
 */

namespace Carbon.Core;

public partial class CorePlugin : CarbonPlugin
{
	[CommandVar("defaultplayergroup", "The default group for any player with the regular authority level they get assigned to.")]
	[AuthLevel(2)]
	private string DefaultPlayerGroup { get { return Community.Runtime.Config.PlayerDefaultGroup; } set { Community.Runtime.Config.PlayerDefaultGroup = value; } }

	[CommandVar("defaultadmingroup", "The default group players with the admin flag get assigned to.")]
	[AuthLevel(2)]
	private string DefaultAdminGroup { get { return Community.Runtime.Config.AdminDefaultGroup; } set { Community.Runtime.Config.AdminDefaultGroup = value; } }

	[ConsoleCommand("grant", "Grant one or more permissions to users or groups. Do 'c.grant' for syntax info.")]
	[AuthLevel(2)]
	private void Grant(ConsoleSystem.Arg arg)
	{
		void PrintWarn()
		{
			arg.ReplyWith($"Syntax: c.grant <user|group> <name|id> <perm>");
		}

		if (!arg.HasArgs(3))
		{
			PrintWarn();
			return;
		}

		var action = arg.GetString(0);
		var name = arg.GetString(1);
		var perm = arg.GetString(2);
		var user = permission.FindUser(name);

		switch (action)
		{
			case "user":
				if (permission.UserHasPermission(user.Key, perm))
				{
					arg.ReplyWith($"Already has that permission assigned.");
				}
				else if (permission.GrantUserPermission(user.Key, perm, null))
				{
					arg.ReplyWith($"Granted user '{user.Value.LastSeenNickname}' permission '{perm}'");
				}
				else
				{
					arg.ReplyWith($"Couldn't grant user permission.");
				}
				break;

			case "group":
				if (permission.GroupHasPermission(name, perm))
				{
					arg.ReplyWith($"Already has that permission assigned.");
				}
				else if (permission.GrantGroupPermission(name, perm, null))
				{
					arg.ReplyWith($"Granted group '{name}' permission '{perm}'");
				}
				else
				{
					arg.ReplyWith($"Couldn't grant group permission.");
				}
				break;

			default:
				PrintWarn();
				break;
		}
	}

	[ConsoleCommand("revoke", "Revoke one or more permissions from users or groups. Do 'c.revoke' for syntax info.")]
	[AuthLevel(2)]
	private void Revoke(ConsoleSystem.Arg arg)
	{
		void PrintWarn()
		{
			arg.ReplyWith($"Syntax: c.revoke <user|group> <name|id> <perm>");
		}

		if (!arg.HasArgs(3))
		{
			PrintWarn();
			return;
		}

		var action = arg.GetString(0);
		var name = arg.GetString(1);
		var perm = arg.GetString(2);
		var user = permission.FindUser(name);

		switch (action)
		{
			case "user":
				if (!permission.UserHasPermission(user.Key, perm))
				{
					arg.ReplyWith($"User does not have that permission assigned.");
				}
				else if (permission.RevokeUserPermission(user.Key, perm))
				{
					arg.ReplyWith($"Revoked user '{user.Value?.LastSeenNickname}' permission '{perm}'");
				}
				else
				{
					arg.ReplyWith($"Couldn't revoke user permission.");
				}
				break;

			case "group":
				if (!permission.GroupHasPermission(name, perm))
				{
					arg.ReplyWith($"Group does not have that permission assigned.");
				}
				else if (permission.RevokeGroupPermission(name, perm))
				{
					arg.ReplyWith($"Revoked group '{name}' permission '{perm}'");
				}
				else
				{
					arg.ReplyWith($"Couldn't revoke group permission.");
				}
				break;

			default:
				PrintWarn();
				break;
		}
	}

	[ConsoleCommand("show", "Displays information about a specific player or group (incl. permissions, groups and user list). Do 'c.show' for syntax info.")]
	[AuthLevel(2)]
	private void Show(ConsoleSystem.Arg arg)
	{
		void PrintWarn()
		{
			arg.ReplyWith($"Syntax: c.show <groups|perms>\n" +
				$"Syntax: c.show <group|user> <name|id>");
		}

		if (!arg.HasArgs(1)) { PrintWarn(); return; }

		var action = arg.GetString(0);

		switch (action)
		{
			case "user":
				{
					if (!arg.HasArgs(2)) { PrintWarn(); return; }

					var name = arg.GetString(1);
					var user = permission.FindUser(name);

					if (user.Value == null)
					{
						arg.ReplyWith($"Couldn't find that user.");
						return;
					}

					var permissions = permission.GetUserPermissions(user.Key);
					arg.ReplyWith($"User {user.Value.LastSeenNickname}[{user.Key}] found in {user.Value.Groups.Count:n0} groups:\n  {user.Value.Groups.Select(x => x).ToString(", ", " and ")}\n" +
						$"and has {permissions.Count():n0} permissions:\n  {permissions.ToString(", ")}");
					break;
				}
			case "group":
				{
					if (!arg.HasArgs(2)) { PrintWarn(); return; }

					var name = arg.GetString(1);

					if (!permission.GroupExists(name))
					{
						arg.ReplyWith($"Couldn't find that group.");
						return;
					}

					var users = permission.GetUsersInGroup(name);
					var permissions = permission.GetGroupPermissions(name, false);
					arg.ReplyWith($"Group {name} has {users.Length:n0} users:\n  {users.Select(x => x).ToString(", ")}\n" +
						$"and has {permissions.Length:n0} permissions:\n  {permissions.Select(x => x).ToString(", ")}");
					break;
				}
			case "groups":
				{
					var groups = permission.GetGroups();
					if (groups.Count() == 0)
					{
						arg.ReplyWith($"Couldn't find any group.");
						return;
					}

					arg.ReplyWith($"Groups:\n {groups.ToString(", ")}");
					break;
				}
			case "perms":
				{
					var perms = permission.GetPermissions();
					if (perms.Count() == 0)
					{
						arg.ReplyWith($"Couldn't find any permission.");
					}

					arg.ReplyWith($"Permissions:\n {perms.ToString(", ")}");

					break;
				}

			default:
				PrintWarn();
				break;
		}
	}

	[ConsoleCommand("usergroup", "Adds or removes a player from a group. Do 'c.usergroup' for syntax info.")]
	[AuthLevel(2)]
	private void UserGroup(ConsoleSystem.Arg arg)
	{
		void PrintWarn()
		{
			arg.ReplyWith($"Syntax: c.usergroup <add|remove> <player> <group>");
		}

		if (!arg.HasArgs(3))
		{
			PrintWarn();
			return;
		}

		var action = arg.GetString(0);
		var player = arg.GetString(1);
		var group = arg.GetString(2);

		var user = permission.FindUser(player);

		if (user.Value == null)
		{
			arg.ReplyWith($"Couldn't find that player.");
			return;
		}

		if (!permission.GroupExists(group))
		{
			arg.ReplyWith($"Group '{group}' could not be found.");
			return;
		}

		switch (action)
		{
			case "add":
				if (permission.UserHasGroup(user.Key, group))
				{
					arg.ReplyWith($"{user.Value.LastSeenNickname}[{user.Key}] is already in '{group}' group.");
					return;
				}

				permission.AddUserGroup(user.Key, group);
				arg.ReplyWith($"Added {user.Value.LastSeenNickname}[{user.Key}] to '{group}' group.");
				break;

			case "remove":
				if (!permission.UserHasGroup(user.Key, group))
				{
					arg.ReplyWith($"{user.Value.LastSeenNickname}[{user.Key}] isn't in '{group}' group.");
					return;
				}

				permission.RemoveUserGroup(user.Key, group);
				arg.ReplyWith($"Removed {user.Value.LastSeenNickname}[{user.Key}] from '{group}' group.");
				break;

			default:
				PrintWarn();
				break;
		}
	}

	[ConsoleCommand("group", "Adds or removes a group. Do 'c.group' for syntax info.")]
	[AuthLevel(2)]
	private void Group(ConsoleSystem.Arg arg)
	{
		void PrintWarn()
		{
			arg.ReplyWith($"Syntax: c.group add <group> [<displayName>] [<rank>]\n" +
				$"Syntax: c.group remove <group>\n" +
				$"Syntax: c.group set <group> <title|rank> <value>\n" +
				$"Syntax: c.group parent <group> [<parent>]");
		}

		if (!arg.HasArgs(1)) { PrintWarn(); return; }

		var action = arg.GetString(0);

		switch (action)
		{
			case "add":
				{
					if (!arg.HasArgs(2)) { PrintWarn(); return; }

					var group = arg.GetString(1);

					if (permission.GroupExists(group))
					{
						arg.ReplyWith($"Group '{group}' already exists. To set any values for this group, use 'c.group set'.");
						return;
					}

					if (permission.CreateGroup(group, arg.HasArgs(3) ? arg.GetString(2) : group, arg.HasArgs(4) ? arg.GetInt(3) : 0))
					{
						arg.ReplyWith($"Created '{group}' group.");
					}
				}
				break;

			case "set":
				{
					if (!arg.HasArgs(4)) { PrintWarn(); return; }

					var group = arg.GetString(1);

					if (!permission.GroupExists(group))
					{
						arg.ReplyWith($"Group '{group}' does not exists.");
						return;
					}

					var set = arg.GetString(2);
					var value = arg.GetString(3);

					switch (set)
					{
						case "title":
							permission.SetGroupTitle(group, value);
							break;

						case "rank":
							permission.SetGroupRank(group, value.ToInt());
							break;
					}

					arg.ReplyWith($"Set '{group}' group.");
				}
				break;

			case "remove":
				{
					if (!arg.HasArgs(2)) { PrintWarn(); return; }

					var group = arg.GetString(1);

					if (permission.RemoveGroup(group)) arg.ReplyWith($"Removed '{group}' group.");
					else arg.ReplyWith($"Couldn't remove '{group}' group.");
				}
				break;

			case "parent":
				{
					if (!arg.HasArgs(3)) { PrintWarn(); return; }

					var group = arg.GetString(1);
					var parent = arg.GetString(2);

					if (permission.SetGroupParent(group, parent)) arg.ReplyWith($"Changed '{group}' group's parent to '{parent}'.");
					else arg.ReplyWith($"Couldn't change '{group}' group's parent to '{parent}'.");
				}
				break;

			default:
				PrintWarn();
				break;
		}
	}
}
