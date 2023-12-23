using API.Assembly;
using API.Commands;
using Carbon.Base.Interfaces;
using Carbon.Client;
using Newtonsoft.Json;
using Oxide.Game.Rust.Cui;

/*
 *
 * Copyright (c) 2022-2023 Carbon Community
 * All rights reserved.
 *
 */

namespace Carbon.Core;
#pragma warning disable IDE0051

public partial class CorePlugin : CarbonPlugin
{
	public bool ClientEnabledCheck()
	{
		if (!Community.Runtime.ClientConfig.Enabled)
		{
			Logger.Warn($" Carbon Client is not enabled. You may not execute that command.");
			return false;
		}

		return true;
	}

	[ConsoleCommand("client.loadconfig", "Loads Carbon Client config from file.")]
	[AuthLevel(2)]
	private void ClientLoadConfig(ConsoleSystem.Arg arg)
	{
		if (Community.Runtime == null) return;

		Community.Runtime.LoadClientConfig();

		arg.ReplyWith("Loaded Carbon Client config.");
	}

	[ConsoleCommand("client.saveconfig", "Saves Carbon Client config to file.")]
	[AuthLevel(2)]
	private void ClientSaveConfig(ConsoleSystem.Arg arg)
	{
		if (Community.Runtime == null) return;

		Community.Runtime.SaveClientConfig();

		arg.ReplyWith("Saved Carbon Client config.");
	}

	[CommandVar("client.enabled", "Enable this if the server is Carbon client-enabled server. [Only applies on server restart]")]
	[AuthLevel(2)]
	private bool ClientEnabled { get { return Community.Runtime.ClientConfig.Enabled; } set { Community.Runtime.ClientConfig.Enabled = value; Community.Runtime.SaveClientConfig(); } }

	[ConsoleCommand("client.addons", "Prints a list of the currently available addons.")]
	[AuthLevel(2)]
	private void ClientAddons(ConsoleSystem.Arg args)
	{
		var builder = PoolEx.GetStringBuilder();

		builder.AppendLine($"Client Addons ({Community.Runtime.ClientConfig.Addons.Count:n0})");

		foreach (var addon in Community.Runtime.ClientConfig.Addons)
		{
			builder.AppendLine($" {addon.Url} [{(addon.Enabled ? "ENABLED" : "DISABLED")}]");
		}

		args.ReplyWith(builder.ToString());

		PoolEx.FreeStringBuilder(ref builder);
	}

	[ConsoleCommand("client.addons_add", "Attempts to add a new addon to the addons list if not already existent.")]
	[AuthLevel(2)]
	private void ClientAddonsAdd(ConsoleSystem.Arg args)
	{
		var addonUrl = args.GetString(0);
		var addonEnabled = args.GetBool(1, true);

		if (Community.Runtime.ClientConfig.Addons.Any(x =>
			    x.Url.Equals(addonUrl, StringComparison.InvariantCultureIgnoreCase)))
		{
			args.ReplyWith("Addon with that URL already in the list.");
		}
		else
		{
			Community.Runtime.ClientConfig.Addons.Add(new ClientConfig.AddonEntry
			{
				Url = addonUrl,
				Enabled = addonEnabled
			});
			Community.Runtime.SaveClientConfig();

			args.ReplyWith("Addon added to the list.");
		}
	}

	[ConsoleCommand("client.addons_del", "Attempts to remove an addon from the addons list if exists.")]
	[AuthLevel(2)]
	private void ClientAddonsDel(ConsoleSystem.Arg args)
	{
		var addonUrl = args.GetString(0);

		if (Community.Runtime.ClientConfig.Addons.RemoveAll(x =>
			    x.Url.Equals(addonUrl, StringComparison.InvariantCultureIgnoreCase)) > 0)
		{
			Community.Runtime.SaveClientConfig();

			args.ReplyWith("Addon removed from the list.");
		}
		else
		{
			args.ReplyWith("Could not find addon with that URL.");
		}
	}

	[ConsoleCommand("client.addons_restart", "Unloads all currently loaded addons then reloads them relative to the config changes. (Options: async [bool])")]
	[AuthLevel(2)]
	private void ClientAddonsRestart(ConsoleSystem.Arg args)
	{
		if (!ClientEnabledCheck())
		{
			return;
		}

		ReloadCarbonClientAddons(args.GetBool(0, false));
	}

	[ConsoleCommand("client.addons_uninstall", "Unloads all currently loaded addons then reloads them relative to the config changes.")]
	[AuthLevel(2)]
	private void ClientAddonsUninstall(ConsoleSystem.Arg args)
	{
		if (!ClientEnabledCheck())
		{
			return;
		}

		Community.Runtime.CarbonClientManager.UninstallAddons();
	}

	public void ReloadCarbonClientAddons(bool async = false)
	{
		var urls = Community.Runtime.ClientConfig.Addons.Where(x => x.Enabled).Select(x => x.Url).ToArray();

		Logger.Log("HERE WE GO");
		Community.Runtime.CarbonClientManager.UninstallAddons();

		if (async)
		{
			Community.Runtime.CarbonClientManager.InstallAddonsAsync(urls);
		}
		else
		{
			Community.Runtime.CarbonClientManager.InstallAddons(urls);
		}
	}
}
