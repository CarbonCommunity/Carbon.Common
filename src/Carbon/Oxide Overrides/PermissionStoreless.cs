/*
 *
 * Copyright (c) 2022-2023 Carbon Community
 * All rights reserved.
 *
 */

namespace Carbon.Oxide;

public class PermissionStoreless : Permission
{
	public override void SaveData()
	{

	}
	public override void LoadFromDatafile()
	{
		userdata = new Dictionary<string, UserData>();
		groupdata = new Dictionary<string, GroupData>();

		var playerDefaultGroup = Community.Runtime.Config.Permissions.PlayerDefaultGroup;
		var adminDefaultGroup = Community.Runtime.Config.Permissions.AdminDefaultGroup;

		if (!GroupExists(playerDefaultGroup))
		{
			CreateGroup(playerDefaultGroup, playerDefaultGroup?.ToCamelCase(), 0);
		}
		if (!GroupExists(adminDefaultGroup))
		{
			CreateGroup(adminDefaultGroup, adminDefaultGroup?.ToCamelCase(), 1);
		}

		IsLoaded = true;
	}
}
