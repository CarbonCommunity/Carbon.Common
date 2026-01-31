using Facepunch;
using Oxide.Game.Rust.Cui;
using Timer = Oxide.Plugins.Timer;

namespace Carbon.Modules;

public partial class HammerModule : CarbonModule<HammerModule.HammerConfig, EmptyModuleData>
{
	public const string cuiName = "hammereditor.cui";

	public override string Name => "Hammer";
	public override bool EnabledByDefault => true;
	public override Type Type => typeof(HammerModule);

	public ListHashSet<Func<BaseEntity, (string name, object value, bool shouldShow)>> CustomFields = new();
	public ListHashSet<Func<BaseEntity, (string name, string color, string command, bool shouldShow)>> CustomButons = new();

	private static readonly Dictionary<ulong, BaseEntity> lastCreativeModePlayers = new();
	private static readonly Dictionary<ulong, BaseEntity> lastLastCreativeModePlayers = new();
	private static readonly ListHashSet<ulong> editingPlayers = new();
	private static readonly ListHashSet<ulong> entityMovingPlayers = new();
	private static readonly Dictionary<string, ModalModule.Modal.Field> temp = new();

	public ModalModule Modal;

	private Timer timer;

	public override void OnEnabled(bool initialized)
	{
		base.OnEnabled(initialized);
		timer?.Destroy();
		timer = Community.Runtime.Core.timer.Every(RefreshRate, TickCheck);
	}

	public override void OnDisabled(bool initialized)
	{
		base.OnDisabled(initialized);
		timer?.Destroy();
		timer = null;

		lastCreativeModePlayers.Clear();
		lastLastCreativeModePlayers.Clear();
		entityMovingPlayers.Clear();
		editingPlayers.Clear();
		for (int i = 0; i < BasePlayer.activePlayerList.Count; i++)
		{
			var player = BasePlayer.activePlayerList[i];
			ClearGUI(player);
		}
	}

	public bool IsEditing(ulong playerId)
	{
		return editingPlayers.Contains(playerId);
	}

	public bool CanBeMoved(BaseEntity entity)
	{
		if (!entity.IsValid())
		{
			return false;
		}

		return entity.ShortPrefabName switch
		{
			_ when entity.ShortPrefabName.Contains("deploy", CompareOptions.IgnoreCase) => true,
			_ when entity.ShortPrefabName.Contains("generator", CompareOptions.IgnoreCase) => true,
			_ => false
		};
	}

	public bool CanToggleAlwaysOn(BaseEntity entity)
	{
		if (entity is IAlwaysOn)
		{
			return true;
		}

		if (entity is IOEntity)
		{
			return true;
		}

		return false;
	}

	public void TickCheck()
	{
		Modal ??= GetModule<ModalModule>();

		using var missingPlayers = Pool.Get<PooledList<BasePlayer>>();
		foreach(var playerId in lastCreativeModePlayers)
		{
			var player = BasePlayer.FindAwakeOrSleepingByID(playerId.Key);
			if (!ShouldShowUI(player, out _))
			{
				missingPlayers.Add(player);
			}
		}

		lastLastCreativeModePlayers.Clear();
		foreach (var element in lastCreativeModePlayers)
		{
			lastLastCreativeModePlayers[element.Key] = element.Value;
		}

		lastCreativeModePlayers.Clear();
		for (int i = 0; i < BasePlayer.activePlayerList.Count; i++)
		{
			var player = BasePlayer.activePlayerList[i];
			if (ShouldShowUI(player, out var entity) && !IsEditing(player.userID))
			{
				if (!lastLastCreativeModePlayers.TryGetValue(player.userID, out var lastEntity) || lastEntity != entity)
				{
					ApplyGUI(player, entity, false);
				}
				lastCreativeModePlayers[player.userID] = entity;
			}
		}
		for (int i = 0; i < missingPlayers.Count; i++)
		{
			var player = missingPlayers[i];
			if (IsEditing(player.userID))
			{
				continue;
			}
			ClearGUI(player);
		}
	}

	public bool ShouldShowUI(BasePlayer player, out BaseEntity entity)
	{
		entity = null;
		if ( !player.IsInCreativeMode || player.GetActiveItem() is not Item item || item.info.itemid is not 200773292 /* Hammer */ || !Physics.Raycast(player.eyes.HeadRay(), out var hit, 3, ~0, QueryTriggerInteraction.Ignore))
		{
			return false;
		}
		return (entity = hit.GetEntity()).IsValid() && !entityMovingPlayers.Contains(player.userID);
	}

	public void ApplyGUI(BasePlayer player, BaseEntity entity, bool editMode)
	{
		const float width = 150f;
		const float optionHeight = 12.5f;
		const float optionSpacing = 15f;
		const string defaultButtonColor = ".9 .2 .3 .9";

		if (!entity.IsValid())
		{
			ClearGUI(player);
			return;
		}

		using var cui = new CUI(Community.Runtime.Core.CuiHandler);
		var heightOffset = 0f;
		var container = cui.CreateContainer(cuiName, Cache.CUI.BlackColor, xMin: X, xMax: X, yMin: Y, yMax: Y,
			OxMin: -width, OxMax: width, destroyUi: cuiName, parent: CUI.ClientPanels.Hud, needsCursor: editMode, needsKeyboard: editMode);

		var entityId = entity.IsValid() ? entity.net.ID : default;

		CreateText(cui, container, container.Name, ref heightOffset, $"{(CanBeMoved(entity) ? "<color=green><b>✓</b></color>" : "<color=red><b>✘</b></color>")} Use <color=white>RIGHT-CLICK</color> to move the entity (hold <color=white>SPRINT</color> to skip auto-snapping)\n{(CanToggleAlwaysOn(entity) ? "<color=green><b>✓</b></color>" : "<color=red><b>✘</b></color>")} Use <color=white>MIDDLE-CLICK</color> to force AlwaysOn on the entity you're looking at");
		if (entity is not BasePlayer playerEntity || !playerEntity.userID.IsSteamId())
		{
			CreateButton(cui, container, container.Name, ref heightOffset, entityId, "Destroy Entity", 1);
		}

		for (int i = 0; i < CustomButons.Count; i++)
		{
			var buttons = CustomButons[i](entity);
			if (!buttons.shouldShow)
			{
				continue;
			}
			CreateCustomButton(cui, container, container.Name, ref heightOffset, buttons.name, buttons.command, buttons.color ?? defaultButtonColor);
		}

		for (int i = 0; i < CustomFields.Count; i++)
		{
			var fields = CustomFields[i](entity);
			if (!fields.shouldShow)
			{
				continue;
			}
			CreateOption(cui, container, container.Name, ref heightOffset, entityId, fields.name, fields.value);
		}

		CreateOption(cui, container, container.Name, ref heightOffset, entityId, "Flags", entity?.flags);
		CreateOption(cui, container, container.Name, ref heightOffset, entityId, "Scale", entity?.transform.localScale);
		CreateOption(cui, container, container.Name, ref heightOffset, entityId, "Rotation", entity?.transform.rotation.eulerAngles);
		CreateOption(cui, container, container.Name, ref heightOffset, entityId, "Position", entity?.transform.position);
		if (entity.IsValid() && entity.OwnerID != 0)
		{
			CreateOption(cui, container, container.Name, ref heightOffset, entityId, "Ownership", BasePlayer.FindAwakeOrSleepingByID(entity.OwnerID)?.ToString() ?? entity.OwnerID.ToString());
		}
		if (entity is BasePlayer myPlayer)
		{
			CreateOption(cui, container, container.Name, ref heightOffset, entityId, "Display Name", myPlayer.displayName);
		}
		if (entity is BuildingBlock buildingBlock)
		{
			CreateOption(cui, container, container.Name, ref heightOffset, entityId, "Building ID", buildingBlock.buildingID);
			if (editMode)
			{
				CreateButton(cui, container, container.Name, ref heightOffset, entityId, $"Destroy Building ({buildingBlock.GetBuilding().decayEntities.Count:n0} entities)", 2);
			}
		}
		CreateOption(cui, container, container.Name, ref heightOffset, entityId, "NetID", entityId);
		CreateOption(cui, container, container.Name, ref heightOffset, entityId, "Target", entity?.ShortPrefabName);
		if (!editMode)
		{
			CreateButton(cui, container, container.Name, ref heightOffset, entityId, "Edit", 0, "#8cbf1d");
		}
		else
		{
			CreateButton(cui, container, container.Name, ref heightOffset, entityId, "End Edit", 0);
		}

		static void CreateOption(CUI cui, CuiElementContainer container, string panel, ref float offset, NetworkableId id, string name, object value)
		{
			var option = cui.CreatePanel(container, panel, ".1 .1 .1 .3", blur: true, OyMin: -optionHeight + offset, OyMax: optionHeight + offset);
			cui.CreateText(container, option, "1 1 1 .5", name, 11, xMax: .25f, align: TextAnchor.MiddleRight);
			var input = cui.CreatePanel(container, option, "0 0 0 .5", xMin: .28f);
			cui.CreateProtectedInputField(container, input, Cache.CUI.WhiteColor, value?.ToString() ?? "undefined", 11, 0, true, OxMin: 7.5f,
				align: TextAnchor.MiddleLeft);
			offset += optionHeight + optionSpacing;
		}

		static void CreateButton(CUI cui, CuiElementContainer container, string panel, ref float offset, NetworkableId id, string name, int optionId, string color = ".9 .2 .3 .9")
		{
			var option = cui.CreatePanel(container, panel, ".1 .1 .1 .3", blur: true, OyMin: -optionHeight + offset, OyMax: optionHeight + offset);
			cui.CreateProtectedButton(container, option, color, Cache.CUI.WhiteColor, name.ToUpperInvariant(), 10, font: CUI.Handler.FontTypes.RobotoCondensedBold,
				command: $"ezeditor.editoption {optionId} {id}");
			offset += optionHeight + optionSpacing;
		}

		static void CreateCustomButton(CUI cui, CuiElementContainer container, string panel, ref float offset, string name, string command, string color = ".9 .2 .3 .9")
		{
			var option = cui.CreatePanel(container, panel, ".1 .1 .1 .3", blur: true, OyMin: -optionHeight + offset, OyMax: optionHeight + offset);
			cui.CreateProtectedButton(container, option, color, Cache.CUI.WhiteColor, name.ToUpperInvariant(), 10, font: CUI.Handler.FontTypes.RobotoCondensedBold, command: command);
			offset += optionHeight + optionSpacing;
		}

		static void CreateText(CUI cui, CuiElementContainer container, string panel, ref float offset, string text)
		{
			var option = cui.CreatePanel(container, panel, ".1 .1 .1 .3", blur: true, OyMin: -optionHeight + offset, OyMax: optionHeight + offset);
			cui.CreateText(container, option, "1 1 1 .4", text, 8, OxMin: 10f, align: TextAnchor.MiddleLeft);
			offset += optionHeight + optionSpacing;
		}

		cui.Send(container, player);
	}

	public void ClearGUI(BasePlayer player)
	{
		using var cui = new CUI(Community.Runtime.Core.CuiHandler);
		cui.Destroy(cuiName, player);
	}

	private void OnPlayerInput(BasePlayer player, InputState state)
	{
		var isMovingEntity = entityMovingPlayers.Contains(player.userID);
		if (!lastCreativeModePlayers.TryGetValue(player.userID, out var entity) && !isMovingEntity)
		{
			return;
		}

		if (state.WasJustPressed(BUTTON.FIRE_THIRD) && entity != null)
		{
			if (entity is IAlwaysOn alwaysOn)
			{
				if (alwaysOn.IsAlwaysOn())
				{
					entity.SetFlag(BaseEntity.Flags.Reserved19, false);
					entity.SetFlag(BaseEntity.Flags.On, false);
					alwaysOn.SetAlwaysOn(false);
					lastCreativeModePlayers.Remove(player.userID);
				}
				else
				{
					alwaysOn.SetAlwaysOn(true);
					entity.SetFlag(BaseEntity.Flags.Reserved19, true);
					entity.SetFlag(BaseEntity.Flags.On, true);
					lastCreativeModePlayers.Remove(player.userID);
				}
			}
			else
			{
				if (entity.HasFlag(BaseEntity.Flags.Reserved19))
				{
					entity.SetFlag(BaseEntity.Flags.Reserved19, false);
					entity.SetFlag(BaseEntity.Flags.On, false);
					lastCreativeModePlayers.Remove(player.userID);
				}
				else
				{
					entity.SetFlag(BaseEntity.Flags.Reserved19, true);
					entity.SetFlag(BaseEntity.Flags.On, true);
					lastCreativeModePlayers.Remove(player.userID);
				}
			}
		}

		if (state.WasJustPressed(BUTTON.FIRE_SECONDARY))
		{
			if (isMovingEntity)
			{
				entityMovingPlayers.Remove(player.userID);
				lastCreativeModePlayers.Remove(player.userID);
			}
			else if(CanBeMoved(entity))
			{
				entityMovingPlayers.Add(player.userID);
				lastCreativeModePlayers.Remove(player.userID);
				player.StartCoroutine(MoveEntityRoutine(player, entity));
			}
		}
	}

	[ProtectedCommand("ezeditor.editoption")]
	private void EditOption(ConsoleSystem.Arg arg)
	{
		var player = arg.Player();
		var option = arg.GetInt(0);
		var entity = BaseNetworkable.serverEntities.Find(arg.GetEntityID(1)) as BaseEntity;

		if (option != 0 && !entity.IsValid())
		{
			player.ChatMessage("Entity is now invalid");
			return;
		}

		switch (option)
		{
			case 0:
			{
				if (IsEditing(player.userID))
				{
					editingPlayers.Remove(player.userID);
				}
				else
				{
					editingPlayers.Add(player.userID);
				}
				ApplyGUI(player, entity, true);
				break;
			}
			case 1:
			{
				entity.AdminKill();
				editingPlayers.Remove(player.userID);
				ClearGUI(player);
				break;
			}
			case 2:
			{
				if (entity is not BuildingBlock block)
				{
					return;
				}
				Modal.Open(player, "Are you sure you wanna destroy that building?", temp, (player, modal) =>
				{
					var entityPool = Pool.Get<PooledList<BaseEntity>>();
					entityPool.AddRange( block.GetBuilding().decayEntities);
					player.StartCoroutine(DestroyEntitiesOverTime(entityPool));
					editingPlayers.Remove(player.userID);
					ClearGUI(player);
				});
				break;
			}
		}
	}

	private IEnumerator DestroyEntitiesOverTime(List<BaseEntity> entities)
	{
		var currentBatch = 0;
		for (int i = 0; i < entities.Count; i++)
		{
			if (currentBatch > 5)
			{
				yield return null;
				yield return null;
			}

			var entity = entities[i];
			if (entity.IsValid())
			{
				entity.AdminKill();
				currentBatch++;
			}
		}
		Pool.FreeUnmanaged(ref entities);
	}

	private IEnumerator MoveEntityRoutine(BasePlayer player, BaseEntity entity)
	{
		const int layer = Rust.Layers.World + Rust.Layers.Terrain + Rust.Layers.Deployed + Rust.Layers.Construction;
		var rotation = Vector3.zero;
		var hits = Pool.Get<List<RaycastHit>>();
		var hasContact = true;
		ClearGUI(player);
		RaycastHit hit = default;
		entity?.SetParent(null, true);
		var transform = entity?.transform;
		while (player.IsValid() && entity.IsValid() && entityMovingPlayers.Contains(player.userID))
		{
			hits.Clear();
			hit = default;
			GamePhysics.TraceAll(player.eyes.HeadRay(), 0f, hits, Distance, layer, QueryTriggerInteraction.Ignore);
			for (int i = 0; i < hits.Count; i++)
			{
				var currentHit = hits[i];
				var currentEntity = currentHit.GetEntity();
				if (currentEntity == entity || currentEntity.HasEntityInParents(entity))
				{
					continue;
				}
				hit = currentHit;
				break;
			}

			hasContact = hit.point != Vector3.zero;

			if (!hasContact)
			{
				hit.point = player.eyes.position + (player.eyes.HeadForward() * Distance);
			}

			if (player.serverInput.WasJustPressed(BUTTON.RELOAD))
			{
				rotation += Vector3.up * 90f;
				player.serverInput.SwallowButton(BUTTON.RELOAD);
			}

			var delta = UnityEngine.Time.deltaTime * Lerp;
			transform.position = Vector3.Lerp(transform.position, hit.point, delta);
			transform.localRotation = Quaternion.Slerp(transform.localRotation, (Quaternion.FromToRotation(Vector3.up, hit.normal) * Quaternion.Euler(rotation)) * Quaternion.Euler(player.eyes.GetLookRotation().eulerAngles.WithX(0)), delta);
			entity.SendNetworkUpdate_Position();
			yield return null;
		}

		if (!hasContact && entity.IsValid() && !player.serverInput.IsDown(BUTTON.SPRINT))
		{
			var position = entity.transform.position;
			Physics.Raycast(position,  Vector3.down, out RaycastHit hit2, float.MaxValue, ~0, QueryTriggerInteraction.Ignore);
			var targetPosition = hit2.point;
			while ((entity.transform.position - targetPosition).magnitude > .01f)
			{
				var delta = UnityEngine.Time.deltaTime * Lerp;
				transform.position = Vector3.Lerp(entity.transform.position, targetPosition, delta);
				transform.rotation = Quaternion.Slerp(transform.rotation, (Quaternion.FromToRotation(Vector3.up, hit2.normal) * Quaternion.Euler(rotation)) * Quaternion.Euler(player.eyes.GetLookRotation().eulerAngles.WithX(0)), delta);
				entity.SendNetworkUpdate_Position();
				yield return null;
			}
			entity.transform.position = targetPosition;
			if (hit2.GetEntity() is BaseEntity parentEntity && parentEntity != entity)
			{
				entity.SetParent(parentEntity, true);
			}
			entity.SendNetworkUpdate_Position();
		}
		else if(entity.IsValid() && hit.GetEntity() is BaseEntity subParentEntity && subParentEntity != entity)
		{
			entity.SetParent(subParentEntity, true);
		}

		ClearGUI(player);
		entityMovingPlayers.Remove(player.userID);
		Pool.FreeUnmanaged(ref hits);
	}

	[CommandVar("hammer.distance"), AuthLevel(1)]
	public float Distance
	{
		get => ConfigInstance.Distance;
		set
		{
			ConfigInstance.Distance = value.Clamp(.5f, 20f);
			Save();
		}
	}

	[CommandVar("hammer.lerp"), AuthLevel(1)]
	public float Lerp
	{
		get => ConfigInstance.Lerp;
		set
		{
			ConfigInstance.Lerp = value.Clamp(1, 20f);
			Save();
		}
	}

	[CommandVar("hammer.x"), AuthLevel(1)]
	public float X
	{
		get => ConfigInstance.X;
		set
		{
			ConfigInstance.X = value.Clamp(0f, 1f);
			Save();
		}
	}

	[CommandVar("hammer.y"), AuthLevel(1)]
	public float Y
	{
		get => ConfigInstance.Y;
		set
		{
			ConfigInstance.Y = value.Clamp(0f, 1f);
			Save();
		}
	}

	[CommandVar("hammer.refreshrate"), AuthLevel(1)]
	public float RefreshRate
	{
		get => ConfigInstance.RefreshRate;
		set
		{
			ConfigInstance.RefreshRate = value.Clamp(0f, 2.5f);
			timer?.Destroy();
			timer = Community.Runtime.Core.timer.Every(ConfigInstance.RefreshRate, TickCheck);
			Save();
		}
	}

	public class HammerConfig
	{
		public float Distance = 5f;
		public float Lerp = 10f;
		public float RefreshRate = .1f;
		public float X = .75f;
		public float Y = .25f;
	}
}
