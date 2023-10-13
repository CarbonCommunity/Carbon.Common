/*
 *
 * Copyright (c) 2022-2023 Carbon Community
 * All rights reserved.
 *
 */

using HarmonyLib;

namespace Carbon.Plugins;

public class CarbonPlugin : RustPlugin
{
	public CUI.Handler CuiHandler { get; set; }

	public override void Setup(string name, string author, VersionNumber version, string description)
	{
		base.Setup(name, author, version, description);

		CuiHandler = new CUI.Handler();
	}

	public override bool IInit()
	{
		if (!base.IInit())
		{
			return false;
		}

		if (AutoPatch)
		{
			ApplyPatch();
		}

		return true;
	}

	public override void IUnload()
	{
		UnapplyPatch();

		base.IUnload();
	}

	#region CUI

	public CUI CreateCUI()
	{
		return new CUI(CuiHandler);
	}

	#endregion

	#region Command Cooldown

	internal static Dictionary<BasePlayer, List<CooldownInstance>> _commandCooldownBuffer = new();

	public static bool IsCommandCooledDown(BasePlayer player, string command, int time, out float timeLeft, bool doCooldownIfNot = true, float appendMultiplier = 0.5f)
	{
		if (time == 0 || player == null)
		{
			timeLeft = -1;
			return false;
		}

		if (!_commandCooldownBuffer.TryGetValue(player, out var pairs))
		{
			_commandCooldownBuffer.Add(player, pairs = new List<CooldownInstance>());
		}

		var lookupCommand = pairs.FirstOrDefault(x => x.Command == command);
		if (lookupCommand == null)
		{
			pairs.Add(lookupCommand = new CooldownInstance { Command = command });
		}

		var timePassed = (DateTime.Now - lookupCommand.LastCall);
		if (timePassed.TotalMilliseconds >= time)
		{
			if (doCooldownIfNot)
			{
				lookupCommand.LastCall = DateTime.Now;
			}

			timeLeft = -1;
			return false;
		}

		timeLeft = (float)((time - timePassed.TotalMilliseconds) * 0.001f);

		if (appendMultiplier != 1)
		{
			lookupCommand.LastCall = lookupCommand.LastCall.AddMilliseconds(time * appendMultiplier);
		}

		return true;
	}

	internal sealed class CooldownInstance
	{
		public string Command;
		public DateTime LastCall;
	}

	#endregion

	#region Harmony

	public virtual bool AutoPatch => false;

	public string Domain => $"com.carbon.{Name}.{Author}";

	public Harmony _CARBON_PATCH;

	public void ApplyPatch()
	{
		UnapplyPatch();

		_CARBON_PATCH = new Harmony(Domain);
		_CARBON_PATCH.PatchAll(Type.Assembly);
	}
	public void UnapplyPatch()
	{
		if (_CARBON_PATCH == null)
		{
			return;
		}

		_CARBON_PATCH.UnpatchAll(Domain);
		_CARBON_PATCH = null;
	}

	#endregion
}
