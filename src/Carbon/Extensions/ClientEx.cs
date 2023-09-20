using Carbon.Client;
using Carbon.Client.SDK;

namespace Carbon.Extensions;

public static class ClientEx
{
	public static ICarbonClient ToCarbonClient(this BasePlayer player)
	{
		return Community.Runtime.CarbonClientManager.Get(player);
	}
}
