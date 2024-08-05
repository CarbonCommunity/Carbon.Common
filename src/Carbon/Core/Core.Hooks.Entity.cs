namespace Carbon.Core;

#pragma warning disable IDE0051

public partial class CorePlugin
{
	internal static object IOnEntitySaved(BaseNetworkable baseNetworkable, BaseNetworkable.SaveInfo saveInfo)
	{
		if (!Community.IsServerInitialized || saveInfo.forConnection == null)
		{
			return null;
		}

		// OnEntitySaved
		HookCaller.CallStaticHook(825712380, baseNetworkable, saveInfo);

		return null;
	}
}
