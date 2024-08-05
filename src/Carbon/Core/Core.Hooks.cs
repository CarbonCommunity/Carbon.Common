using Facepunch;

namespace Carbon.Core;

#pragma warning disable IDE0051

public partial class CorePlugin
{
	internal static bool _isPlayerTakingDamage = false;
	internal static readonly string[] _emptyStringArray = new string[0];

	internal static object IOnServerInitialized()
	{
		if (!Community.IsServerInitialized)
		{
			Community.IsServerInitialized = true;

			Analytics.on_server_initialized();
		}

		return null;
	}
	internal static object IOnServerShutdown()
	{
		Logger.Log($"Saving plugin configuration and data..");

		var temp = Pool.GetList<BaseHookable>();
		temp.AddRange(Community.Runtime.ModuleProcessor.Modules);

		foreach (var module in temp)
		{
			if (module is BaseModule m)
			{
				try
				{
					m.Shutdown();
				}
				catch (Exception ex)
				{
					Logger.Error($"Failed shutting down module '{m.Name} v{m.Version}'", ex);
				}
			}
		}

		Pool.FreeList(ref temp);

		// OnServerShutdown
		HookCaller.CallStaticHook(2414711472);

		// OnServerSave
		HookCaller.CallStaticHook(2396958305);

		Logger.Log($"Shutting down Carbon..");
		Interface.Oxide.OnShutdown();
		Community.Runtime.ScriptProcessor.Clear();

		return null;
	}
}
