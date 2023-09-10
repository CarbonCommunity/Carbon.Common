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
			if (_instance != null)
			{
				return;
			}

			var type = AccessToolsEx.TypeByName(TargetType);
			_instance = go.AddComponent(type);

			const BindingFlags _monoFlags = BindingFlags.Instance | BindingFlags.Public;

			foreach (var member in Members)
			{
				try
				{
					var field = type.GetField(member.Name, _monoFlags);
					var memberType = field.FieldType;
					var value = (object)null;

					if (memberType == typeof(LayerMask))
					{
						if (int.TryParse(member.Value, out var intValue))
						{
							value = new LayerMask { value = intValue };
						}
						else
						{
							var layer = LayerMask.GetMask(member.Value.Split(LayerSplitter, StringSplitOptions.RemoveEmptyEntries));
							value = new LayerMask { value = layer };
						}
					}
					else if (memberType.IsEnum)
					{
						value = Enum.Parse(memberType, member.Value);
					}
					else
					{
						value = Convert.ChangeType(member.Value, field.FieldType);
					}

					if (field != null)
					{
						field?.SetValue(_instance, value);
						Debug.Log($" Assigned member '{member.Name}'");
					}
					else
					{
						Debug.LogWarning($" Couldn't find member '{member.Name}'");
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
