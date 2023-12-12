using Carbon.Client;
using Carbon.Client.SDK;
using Connection = Network.Connection;

namespace Carbon.Extensions;

public static class ClientEx
{
	public static ICarbonClient ToCarbonClient(this BasePlayer player)
	{
		return Community.Runtime.CarbonClientManager.Get(player);
	}
	public static ICarbonClient ToCarbonClient(this Connection connection)
	{
		return Community.Runtime.CarbonClientManager.Get(connection);
	}
}
