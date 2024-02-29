namespace Carbon.Modules;

public partial class AdminModule
{
#if !MINIMAL
	private void OnEntityDismounted(BaseMountable entity, BasePlayer player)
	{
		var ap = GetPlayerSession(player);
		var tab = GetTab(player);
		if (!ap.GetStorage(tab, "wasviewingcam", false)) return;

		entity.Kill();
		Draw(player);

		Unsubscribe("OnEntityDismounted");
	}
	private void OnPlayerLootEnd(PlayerLoot loot)
	{
		if (EntitiesTab.LastContainerLooter != null && loot.baseEntity == EntitiesTab.LastContainerLooter.Player)
		{
			Draw(EntitiesTab.LastContainerLooter.Player);
			EntitiesTab.LastContainerLooter = null;
			Unsubscribe("OnEntityVisibilityCheck");
			Unsubscribe("OnEntityDistanceCheck");
		}
	}
	private object OnEntityDistanceCheck(BaseEntity ent, BasePlayer player, uint id, string debugName, float maximumDistance)
	{
		var ap = GetPlayerSession(player);
		var tab = GetTab(player);
		var lootedEnt = ap.GetStorage<BaseEntity>(tab, "lootedent");

		if (lootedEnt == null) return null;

		return true;
	}
	private object OnEntityVisibilityCheck(BaseEntity ent, BasePlayer player, uint id, string debugName, float maximumDistance)
	{
		var ap = GetPlayerSession(player);
		var tab = GetTab(player);
		var lootedEnt = ap.GetStorage<BaseEntity>(tab, "lootedent");

		if (lootedEnt == null) return null;

		return true;
	}
	private void OnPlayerDisconnected(BasePlayer player)
	{
		if (PlayersTab.BlindedPlayers.Contains(player)) PlayersTab.BlindedPlayers.Remove(player);

		StopSpectating(player);
	}
#endif
}
