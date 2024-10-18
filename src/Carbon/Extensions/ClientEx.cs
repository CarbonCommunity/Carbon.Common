using Carbon.Client.SDK;
using Connection = Network.Connection;

namespace Carbon.Extensions;

public static class ClientEx
{
	public static ICarbonClient ToCarbonClient(this BasePlayer player)
	{
		return Community.Runtime.CarbonClient.Get(player);
	}
	public static ICarbonClient ToCarbonClient(this Network.Connection connection)
	{
		return Community.Runtime.CarbonClient.Get(connection);
	}
}
