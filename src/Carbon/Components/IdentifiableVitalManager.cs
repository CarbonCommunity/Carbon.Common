using Facepunch;
using ProtoBuf;

namespace Carbon.Components;

public static class IdentifiableVitalManager
{
	private static IdentifiableVitalDictionary sharedVitals = new();
	private static Dictionary<ulong, IdentifiableVitalDictionary> playerVitals = [];

	public static CustomVitalInfo RentVitalInfo(
		string icon = null, Color iconColor = default,
		Color backgroundColor = default,
		string leftText = null, Color leftTextColor = default,
		string rightText = null, Color rightTextColor = default,
		int timeLeft = 0, bool active = true)
	{
		var vitalInfo = Pool.Get<CustomVitalInfo>();
		vitalInfo.icon = icon;
		vitalInfo.iconColor = iconColor;
		vitalInfo.backgroundColor = backgroundColor;
		vitalInfo.leftText = leftText;
		vitalInfo.leftTextColor = leftTextColor;
		vitalInfo.rightText = rightText;
		vitalInfo.rightTextColor = rightTextColor;
		vitalInfo.active = active;
		vitalInfo.timeLeft = timeLeft;
		return vitalInfo;
	}

	/// <summary>
	/// Use RentVitalInfo to get a vital instance to add it to a player
	/// </summary>
	public static IdentifiableVital AddVital(BasePlayer player, CustomVitalInfo vital, bool sendUpdate = true)
	{
		if (!playerVitals.TryGetValue(player.userID, out var vitals))
		{
			playerVitals[player.userID] = vitals = new();
		}
		var identifiableVital = vitals.AddVital(vital);
		if (sendUpdate)
		{
			SendVitals(player);
		}
		return identifiableVital;
	}

	/// <summary>
	/// Use RentVitalInfo to get a vital instance to add it for all connected players (shared vital)
	/// </summary>
	public static IdentifiableVital AddSharedVital(CustomVitalInfo vital, bool sendUpdate = true)
	{
		var identifiableVital = sharedVitals.AddVital(vital);
		if (sendUpdate)
		{
			SendVitalsToEveryone();
		}
		return identifiableVital;
	}

	public static bool RemoveVital(BasePlayer player, IdentifiableVital vital, bool sendUpdate = true) => RemoveVital(player, vital.id, sendUpdate);

	public static bool RemoveVital(BasePlayer player, uint id, bool sendUpdate = true)
	{
		if (!player.IsValid() || !playerVitals.TryGetValue(player.userID, out var vitals))
		{
			return false;
		}
		if (!vitals.RemoveVital(id))
		{
			return false;
		}
		if (sendUpdate)
		{
			SendVitals(player);
		}
		return true;
	}

	public static bool RemoveSharedVital(IdentifiableVital vital, bool sendUpdate = true) => RemoveSharedVital(vital.id, sendUpdate);

	public static bool RemoveSharedVital(uint id, bool sendUpdate = true)
	{
		if (!sharedVitals.RemoveVital(id))
		{
			return false;
		}
		if (sendUpdate)
		{
			SendVitalsToEveryone();
		}
		return true;
	}

	public static void ClearVitals(BasePlayer player, bool sendUpdate = true)
	{
		if (!player.IsValid() || !playerVitals.TryGetValue(player.userID, out var vitals))
		{
			return;
		}
		vitals.ClearVitals();
		if (sendUpdate)
		{
			SendVitals(player);
		}
	}

	public static void ClearSharedVitals(bool sendUpdate = true)
	{
		sharedVitals.ClearVitals();
		if (sendUpdate)
		{
			SendVitalsToEveryone();
		}
	}

	public static void SendVitals(BasePlayer player)
	{
		var vitals = Pool.Get<CustomVitals>();
		vitals.vitals = Pool.Get<List<CustomVitalInfo>>();
		if (playerVitals.TryGetValue(player.userID, out var pv))
		{
			pv.AppendVitals(vitals);
		}
		sharedVitals.AppendVitals(vitals);
		CommunityEntity.ServerInstance.SendCustomVitals(player, vitals);
		Pool.Free(ref vitals.vitals);
		Pool.Free(ref vitals);
	}

	public static void SendVitalsToEveryone()
	{
		for (int i = 0; i < BasePlayer.activePlayerList.Count; i++)
		{
			SendVitals(BasePlayer.activePlayerList[i]);
		}
	}

	public class IdentifiableVitalDictionary
	{
		private ListDictionary<uint, IdentifiableVital> buffer = [];

		public static uint nextVitalId = 100;

		public int Count => buffer.Count;

		public bool HasAny() => Count > 0;

		public void GetVitals(List<IdentifiableVital> vitals)
		{
			for (int i = 0; i < buffer.Count; i++)
			{
				vitals.Add(buffer.Values[i]);
			}
		}

		public void AppendVitals(CustomVitals vitals)
		{
			var values = buffer.Values;
			for (int i = 0; i < Count; i++)
			{
				var identifiableVital = values[i];
				if (identifiableVital.info.timeLeft > 0)
				{
					var newTimeLeft = Mathf.Max(identifiableVital.totalTimeLeft - (int)identifiableVital.sinceTimeLeftStart, 0);
					if (newTimeLeft != 0)
					{
						identifiableVital.info.timeLeft = newTimeLeft;
					}
				}
				vitals.vitals.Add(identifiableVital.info);
			}
		}

		public IdentifiableVital AddVital(CustomVitalInfo vital)
		{
			IdentifiableVital identifiableVital = default;
			identifiableVital.id = ++nextVitalId;
			identifiableVital.info = vital;
			identifiableVital.totalTimeLeft = vital.timeLeft;
			identifiableVital.sinceTimeLeftStart = 0;
			buffer.Add(identifiableVital.id, identifiableVital);
			return identifiableVital;
		}

		public bool RemoveVital(IdentifiableVital vital) => RemoveVital(vital.id);

		public bool RemoveVital(uint id)
		{
			if (!buffer.TryGetValue(id, out var vital))
			{
				return false;
			}
			var value = vital.info;
			Pool.Free(ref value);
			return buffer.Remove(id);
		}

		public void ClearVitals()
		{
			for (int i = 0; i < buffer.Count; i++)
			{
				var vitalInfo = buffer.Values[i].info;
				Pool.Free(ref vitalInfo);
			}
			buffer.Clear();
		}
	}

	public struct IdentifiableVital
	{
		public uint id;
		public CustomVitalInfo info;
		public TimeSince sinceTimeLeftStart;
		public int totalTimeLeft;

		public void MarkDirty()
		{
			SendVitalsToEveryone();
		}
	}
}
