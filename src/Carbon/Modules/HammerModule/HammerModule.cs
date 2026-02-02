using Facepunch;
using Oxide.Game.Rust.Cui;
using Timer = Oxide.Plugins.Timer;

namespace Carbon.Modules;

public partial class HammerModule : CarbonModule<HammerModule.HammerConfig, EmptyModuleData>
{
	public const string cuiName = "hammereditor.cui";

	public override string Name => "Hammer";
	public override VersionNumber Version => new(1, 0, 0);
	public override bool EnabledByDefault => true;
	public override Type Type => typeof(HammerModule);

	public ListHashSet<Func<BaseEntity, bool, (string name, object value, bool shouldShow)>> CustomFields = new();
	public ListHashSet<Func<BaseEntity, bool, (string name, string color, string command, bool shouldShow)>> CustomButons = new();

	private static readonly Translate.Phrase destroyingBuildingPhrase = new("destroyedbuilding", "Destroying building: <color=white>{0}</color>/{1} entities");
	private static readonly Translate.Phrase repairedPhrase = new("repaired", "Repairing: <color=white>{0}</color>/{1} entities");

	private static readonly Dictionary<ulong, BaseEntity> lastCreativeModePlayers = new();
	private static readonly Dictionary<ulong, BaseEntity> lastLastCreativeModePlayers = new();
	private static readonly ListHashSet<ulong> editingPlayers = new();
	private static readonly ListHashSet<ulong> entityMovingPlayers = new();
	private static readonly ListHashSet<ulong> repairingDestroyingPlayers = new();
	private static readonly Dictionary<string, ModalModule.Modal.Field> temp = new();
	private static CuiDraggableComponent cachedDraggable = new();

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

		switch (entity)
		{
			case BuildingBlock:
				return false;
		}

		if (MoveEverything)
		{
			return true;
		}

		return entity switch
		{
			ModularCar or BasicCar => true,
			DecayEntity => true,
			_ => entity.ShortPrefabName switch
			{
				_ when entity.ShortPrefabName.Contains("deploy", CompareOptions.IgnoreCase) => true,
				_ when entity.ShortPrefabName.Contains("generator", CompareOptions.IgnoreCase) => true,
				_ when entity.ShortPrefabName.Contains("arcade", CompareOptions.IgnoreCase) => true,
				_ => false
			}
		};
	}

	public bool CanBeToggled(BaseEntity entity)
	{
		return entity switch
		{
			Door => true,
			IOEntity ioEntity when ioEntity.inputs.Length > 0 => true,
			StorageContainer => true,
			IAlwaysOn => true,
			MiningQuarry or EngineSwitch => true,
			_ => false
		};
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
		if ( !player.IsInCreativeMode || player.GetActiveItem() is not Item item || item.info.itemid is not 200773292 /* Hammer */ || !Physics.Raycast(player.eyes.HeadRay(), out var hit, Distance, ~0, QueryTriggerInteraction.Ignore))
		{
			return false;
		}
		return (entity = hit.GetEntity()).IsValid() && !entityMovingPlayers.Contains(player.userID);
	}

	public void ApplyGUI(BasePlayer player, BaseEntity entity, bool showExtra)
	{
		const float width = 150f;
		const float optionHeight = 10f;
		const float optionSpacing = 12.5f;
		const string defaultButtonColor = ".9 .2 .3 .9";

		if (!entity.IsValid())
		{
			ClearGUI(player);
			return;
		}

		using var cui = new CUI(Community.Runtime.Core.CuiHandler);
		var heightOffset = 0f;
		var coordinates = GetCoordinates(player);
		var container = cui.CreateContainer(cuiName, Cache.CUI.BlackColor, xMin: coordinates.x, xMax: coordinates.x, yMin: coordinates.y, yMax: coordinates.y,
			OxMin: -width, OxMax: width, destroyUi: cuiName, parent: CUI.ClientPanels.Hud, needsCursor: showExtra, needsKeyboard: showExtra);

		var primaryPanel = container[0];
		primaryPanel.Components.Add(cachedDraggable);
		cachedDraggable.LimitToParent = true;
		cachedDraggable.ParentLimitIndex = 1;
		cachedDraggable.DragAlpha = .5f;
		cachedDraggable.PositionRPC = CommunityEntity.DraggablePositionSendType.NormalizedScreen;

		var entityId = entity.IsValid() ? entity.net.ID : default;

		CreateText(cui, container, container.Name, ref heightOffset, $"{(CanBeMoved(entity) ? "<color=green><b>✓</b></color>" : "<color=red><b>✘</b></color>")} Use <color=white>RIGHT-CLICK</color> to move the entity (hold <color=white>SPRINT</color> to skip auto-snapping)\n{(CanBeToggled(entity) ? "<color=green><b>✓</b></color>" : "<color=red><b>✘</b></color>")} Use <color=white>MIDDLE-CLICK</color> to toggle the entity (hold <color=white>SPRINT</color> to lock/unlock)");
		if (entity is not BasePlayer playerEntity || !playerEntity.userID.IsSteamId())
		{
			CreateButton(cui, container, container.Name, ref heightOffset, entityId, "Destroy Entity", 1);
		}

		for (int i = 0; i < CustomButons.Count; i++)
		{
			var buttons = CustomButons[i](entity, showExtra);
			if (!buttons.shouldShow)
			{
				continue;
			}
			CreateCustomButton(cui, container, container.Name, ref heightOffset, buttons.name, buttons.command, buttons.color ?? defaultButtonColor);
		}

		for (int i = 0; i < CustomFields.Count; i++)
		{
			var fields = CustomFields[i](entity, showExtra);
			if (!fields.shouldShow)
			{
				continue;
			}
			CreateOption(cui, container, container.Name, ref heightOffset, entityId, fields.name, fields.value);
		}

		switch (entity)
		{
			case SleepingBag sleepingBag:
			{
				CreateOption(cui, container, container.Name, ref heightOffset, entityId, "Assigned To", BasePlayer.FindAwakeOrSleepingByID(sleepingBag.deployerUserID)?.ToString() ?? sleepingBag.deployerUserID.ToString());
				break;
			}
			case MiningQuarry miningQuarry:
			{
				CreateOption(cui, container, container.Name, ref heightOffset, entityId, "Static Type", miningQuarry.staticType);
				break;
			}
		}

		if (entity?.flags != 0)
		{
			CreateOption(cui, container, container.Name, ref heightOffset, entityId, "Flags", entity?.flags);
		}
		if (entity?.skinID != 0)
		{
			CreateOption(cui, container, container.Name, ref heightOffset, entityId, "Skin ID", entity?.skinID);
		}
		if (entity?.transform.localScale != Vector3.one)
		{
			CreateOption(cui, container, container.Name, ref heightOffset, entityId, "Scale", entity?.transform.localScale);
		}
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
			if (showExtra)
			{
				CreateButton(cui, container, container.Name, ref heightOffset, entityId, $"Destroy Building ({buildingBlock.GetBuilding().decayEntities.Count:n0} entities)", 2);
			}
		}
		CreateOption(cui, container, container.Name, ref heightOffset, entityId, "NetID", entityId);
		CreateOption(cui, container, container.Name, ref heightOffset, entityId, "Target", entity?.ShortPrefabName);
		if (!showExtra)
		{
			CreateButton(cui, container, container.Name, ref heightOffset, entityId, "Show extra settings", 0, "#8cbf1d");
		}
		else
		{
			CreateButton(cui, container, container.Name, ref heightOffset, entityId, "Show fewer settings", 0);
		}

		static void CreateOption(CUI cui, CuiElementContainer container, string panel, ref float offset, NetworkableId id, string name, object value)
		{
			var option = cui.CreatePanel(container, panel, ".1 .1 .1 .3", blur: true, OyMin: -optionHeight + offset, OyMax: optionHeight + offset);
			cui.CreateText(container, option, "1 1 1 .5", name, 10, xMax: .25f, align: TextAnchor.MiddleRight);
			var input = cui.CreatePanel(container, option, "0 0 0 .5", xMin: .28f);
			cui.CreateProtectedInputField(container, input, Cache.CUI.WhiteColor, value?.ToString() ?? "undefined", 10, 0, true, OxMin: 7.5f,
				align: TextAnchor.MiddleLeft);
			offset += optionHeight + optionSpacing;
		}

		static void CreateButton(CUI cui, CuiElementContainer container, string panel, ref float offset, NetworkableId id, string name, int optionId, string color = ".9 .2 .3 .9")
		{
			var option = cui.CreatePanel(container, panel, ".1 .1 .1 .3", blur: true, OyMin: -optionHeight + offset, OyMax: optionHeight + offset);
			cui.CreateProtectedButton(container, option, color, Cache.CUI.WhiteColor, name.ToUpperInvariant(), 9, font: CUI.Handler.FontTypes.RobotoCondensedBold,
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
			const float height = optionHeight + 2.5f;
			var option = cui.CreatePanel(container, panel, ".1 .1 .1 .3", blur: true, OyMin: -height + offset, OyMax: height + offset);
			cui.CreateText(container, option, "1 1 1 .4", text, 8, OxMin: 10f, align: TextAnchor.MiddleLeft);
			offset += height + optionSpacing;
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
		if (lastCreativeModePlayers.TryGetValue(player.userID, out var entity) && state.WasJustPressed(BUTTON.FIRE_THIRD) && CanBeToggled(entity))
		{
			var wantsLock = state.IsDown(BUTTON.SPRINT);
			var openFlag = wantsLock ? BaseEntity.Flags.Locked : BaseEntity.Flags.Open;
			var onFlag = wantsLock ? BaseEntity.Flags.Locked : BaseEntity.Flags.On;
			switch (entity)
			{
				case Door:
				{
					entity.SetFlag(openFlag, !entity.HasFlag(openFlag));
					break;
				}
				case IOEntity:
				{
					var isOn = entity.HasFlag(onFlag);
					entity.SetFlag(onFlag, !isOn);
					entity.SetFlag(BaseEntity.Flags.Reserved8, !isOn);
					break;
				}
				case StorageContainer:
				{
					entity.SetFlag(onFlag, !entity.HasFlag(onFlag));
					break;
				}
				case EngineSwitch:
				{
					if (entity.GetParentEntity() is MiningQuarry quarry)
					{
						quarry.EngineSwitch(!quarry.IsOn());
					}
					break;
				}
				case MiningQuarry quarry:
				{
					quarry.staticType++;
					if ((int)quarry.staticType > 3)
					{
						quarry.staticType = 0;
					}
					quarry.UpdateStaticDeposit();
					break;
				}
			}
			lastCreativeModePlayers.Remove(player.userID);
		}

		if (state.WasJustPressed(BUTTON.FIRE_SECONDARY))
		{
			if (entityMovingPlayers.Contains(player.userID))
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

	private object OnHammerHit(BasePlayer player, HitInfo info)
	{
		if (!player.IsInCreativeMode || info == null)
		{
			return null;
		}
		if (info.HitEntity is BuildingBlock block && block.GetBuilding() is BuildingManager.Building building && !repairingDestroyingPlayers.Contains(player.userID))
		{
			var entityPool = Pool.Get<PooledList<BaseCombatEntity>>();
			entityPool.AddRange(building.decayEntities);
			player.StartCoroutine(RepairEntitiesOverTime(player, entityPool));
			return Cache.False;
		}
		return null;
	}

	private void OnCuiDraggableDrag(BasePlayer player, string name, Vector3 position, CommunityEntity.DraggablePositionSendType type)
	{
		if (!name.Equals(cuiName) || type != CommunityEntity.DraggablePositionSendType.NormalizedParent)
		{
			return;
		}
		SetCoordinates(player, position);
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

				if (ShouldShowUI(player, out _))
				{
					ApplyGUI(player, entity, true);
				}
				else
				{
					ClearGUI(player);
				}
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
				if (entity is not BuildingBlock block || repairingDestroyingPlayers.Contains(player.userID))
				{
					return;
				}
				Modal.Open(player, "Are you sure you wanna destroy that building?", temp, (player, modal) =>
				{
					var entityPool = Pool.Get<PooledList<BaseEntity>>();
					entityPool.AddRange(block.GetBuilding().decayEntities);
					player.StartCoroutine(DestroyEntitiesOverTime(player, entityPool));
					editingPlayers.Remove(player.userID);
					ClearGUI(player);
				});
				break;
			}
		}
	}

	private IEnumerator DestroyEntitiesOverTime(BasePlayer player, List<BaseEntity> entities)
	{
		repairingDestroyingPlayers.Add(player.userID);
		var currentBatch = 0;
		for (int i = 0; i < entities.Count; i++)
		{
			if (currentBatch > DestroyBatch)
			{
				yield return null;
				yield return null;
				yield return CoroutineEx.waitForSeconds(.25f);
				player.ShowToast(GameTip.Styles.Red_Normal, destroyingBuildingPhrase, false, (i + 1).ToString("n0"), entities.Count.ToString("n0"));
				currentBatch = 0;
			}

			var entity = entities[i];
			if (entity.IsValid())
			{
				entity.AdminKill();
				currentBatch++;
			}
		}
		player.ShowToast(GameTip.Styles.Red_Normal, destroyingBuildingPhrase, false, entities.Count.ToString("n0"), entities.Count.ToString("n0"));
		Pool.FreeUnmanaged(ref entities);
		repairingDestroyingPlayers.Remove(player.userID);
	}

	private IEnumerator RepairEntitiesOverTime(BasePlayer player, List<BaseCombatEntity> entities)
	{
		repairingDestroyingPlayers.Add(player.userID);
		var currentBatch = 0;
		for (int i = 0; i < entities.Count; i++)
		{
			if (currentBatch > RepairBatch)
			{
				currentBatch = 0;
				player.ShowToast(GameTip.Styles.Blue_Normal, repairedPhrase, false, (i + 1).ToString("n0"), entities.Count.ToString("n0"));
				yield return CoroutineEx.waitForSeconds(.25f);
			}

			var entity = entities[i];
			if (entity.IsValid())
			{
				entity.Heal(float.MaxValue);
				currentBatch++;
				yield return null;
			}
		}
		player.ShowToast(GameTip.Styles.Blue_Normal, repairedPhrase, false, entities.Count.ToString("n0"), entities.Count.ToString("n0"));
		Pool.FreeUnmanaged(ref entities);
		repairingDestroyingPlayers.Remove(player.userID);
	}

	private IEnumerator MoveEntityRoutine(BasePlayer player, BaseEntity entity)
	{
		const int layer = Rust.Layers.World + Rust.Layers.Terrain + Rust.Layers.Deployed + Rust.Layers.Construction;
		var rotation = Vector3.up * 180f;
		var hits = Pool.Get<List<RaycastHit>>();
		var hasContact = true;
		var rigidbody = entity.GetComponent<Rigidbody>() ?? entity.GetComponentInChildren<Rigidbody>() ?? entity.GetComponentInParent<Rigidbody>();
		var wasKinematic = rigidbody?.isKinematic;
		rigidbody?.isKinematic = true;
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
			while (entity.IsValid() && (entity.transform.position - targetPosition).magnitude > .01f)
			{
				var delta = UnityEngine.Time.deltaTime * Lerp;
				transform.position = Vector3.Lerp(entity.transform.position, targetPosition, delta);
				transform.rotation = Quaternion.Slerp(transform.rotation, (Quaternion.FromToRotation(Vector3.up, hit2.normal) * Quaternion.Euler(rotation)) * Quaternion.Euler(player.eyes.GetLookRotation().eulerAngles.WithX(0)), delta);
				entity.SendNetworkUpdate_Position();
				yield return null;
			}

			if (entity.IsValid())
			{
				entity.transform.position = targetPosition;
				entity.SendNetworkUpdate_Position();
			}
			if (hit2.GetEntity() is BaseEntity parentEntity && parentEntity != entity && parentEntity is not BasePlayer && entity is not BasePlayer)
			{
				entity?.SetParent(parentEntity, true);
			}
		}
		else if(entity.IsValid() && hit.GetEntity() is BaseEntity subParentEntity && subParentEntity != entity && entity is not BasePlayer && subParentEntity is not BasePlayer)
		{
			entity.SetParent(subParentEntity, true);
		}

		ClearGUI(player);
		entityMovingPlayers.Remove(player.userID);
		if (rigidbody != null)
		{
			rigidbody.isKinematic = wasKinematic ?? false;
		}
		Pool.FreeUnmanaged(ref hits);

		if (entity is not BaseCorpse && !entity.HasEntityInParents(player) && !player.HasEntityInParents(entity))
		{
			ReconstructEntity(entity);
		}
	}

	private void ReconstructEntity(BaseEntity entity)
	{
		if (!entity.IsValid())
		{
			return;
		}
		for (int i = 0; i < entity.net.group.subscribers.Count; i++)
		{
			entity.DestroyOnClient(entity.net.group.subscribers[i]);
		}
		if (entity.children != null)
		{
			for (int i = 0; i < entity.children.Count; i++)
			{
				ReconstructEntity(entity.children[i]);
			}
		}
		entity.SendNetworkUpdateImmediate();
	}

	public Vector2 GetCoordinates(BasePlayer player)
	{
		var id = "coord-" + player.userID;
		return PlayerPrefs.HasKey(id) ? Vector2Ex.Parse(PlayerPrefs.GetString(id)) : new Vector2(DefaultX, DefaultY);
	}

	public void SetCoordinates(BasePlayer player, Vector2 coordinates)
	{
		PlayerPrefs.SetString("coord-" + player.userID, coordinates.ToParsableString());
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
	public float DefaultX
	{
		get => ConfigInstance.DefaultX;
		set
		{
			ConfigInstance.DefaultX = value.Clamp(0f, 1f);
			Save();
		}
	}

	[CommandVar("hammer.y"), AuthLevel(1)]
	public float DefaultY
	{
		get => ConfigInstance.DefaultY;
		set
		{
			ConfigInstance.DefaultY = value.Clamp(0f, 1f);
			Save();
		}
	}

	[CommandVar("hammer.repairbatch"), AuthLevel(1)]
	public int RepairBatch
	{
		get => ConfigInstance.RepairBatch;
		set
		{
			ConfigInstance.RepairBatch = value.Clamp(1, 100);
			Save();
		}
	}

	[CommandVar("hammer.destroybatch"), AuthLevel(1)]
	public int DestroyBatch
	{
		get => ConfigInstance.DestroyBatch;
		set
		{
			ConfigInstance.DestroyBatch = value.Clamp(1, 100);
			Save();
		}
	}

	[CommandVar("hammer.moveeverything"), AuthLevel(2)]
	public bool MoveEverything
	{
		get => ConfigInstance.MoveEverything;
		set
		{
			ConfigInstance.MoveEverything = value;
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
		public int RepairBatch = 5;
		public int DestroyBatch = 3;
		public float DefaultX = .75f;
		public float DefaultY = .25f;
		public bool MoveEverything = false;
	}
}
