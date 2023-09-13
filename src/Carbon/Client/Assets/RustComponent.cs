using Newtonsoft.Json;
using ProtoBuf;

namespace Carbon.Client
{
	[ProtoContract]
	public class RustComponent : MonoBehaviour
	{
		[ProtoMember(1)]
		public bool IsServer;

		[ProtoMember(2)]
		public bool IsClient;

		[ProtoMember(3)]
		public string TargetType;

		[ProtoMember(4)]
		public Member[] Members;

		[Serializable, ProtoContract]
		public class Member
		{
			[ProtoMember(1)]
			public string Name;

			[ProtoMember(2)]
			public string Value;
		}

		public Component _instance;

		public static readonly char[] LayerSplitter = new char[] { '|' };

		public void ApplyComponent(GameObject go)
		{
			if (!IsServer || _instance != null)
			{
				return;
			}

			var type = AccessToolsEx.TypeByName(TargetType);
			_instance = go.AddComponent(type);

			const BindingFlags _monoFlags = BindingFlags.Instance | BindingFlags.Public;

			var trigger = go.GetComponent<TriggerBase>();

			if (trigger != null)
			{
				switch (trigger)
				{
					case TriggerLadder ladder:
						ladder.interestLayers = new LayerMask { value = 131072 };
						break;

					case TriggerSafeZone safeZone:
						safeZone.interestLayers = new LayerMask { value = 163840 };
						break;

					case TriggerRadiation radiation:
						radiation.interestLayers = new LayerMask { value = 131072 };
						break;
				}
			}
			
			if (Members != null && Members.Length > 0)
			{
				foreach (var member in Members)
				{
					try
					{
						var field = type.GetField(member.Name, _monoFlags);
						var memberType = field.FieldType;
						var value = (object)null;

						if (memberType == typeof(LayerMask))
						{

						}
						else if (memberType.IsEnum)
						{
							value = Enum.Parse(memberType, member.Value);
						}
						else
						{
							value = Convert.ChangeType(member.Value, memberType);
						}

						if (field != null)
						{
							field?.SetValue(_instance, value);
						}
						else
						{
							Logger.Error($" Couldn't find member '{member.Name}' for '{TargetType}' on '{go.transform.GetRecursiveName()}'");
						}
					}
					catch (Exception ex)
					{
						Logger.Error($"Failed assigning Rust component member '{member.Name}' to {go.transform.GetRecursiveName()}", ex);
					}
				}
			}
		}
	}
}
