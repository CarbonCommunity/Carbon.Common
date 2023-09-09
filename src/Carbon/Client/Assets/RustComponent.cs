using Newtonsoft.Json;
using ProtoBuf;

namespace Carbon.Client
{
	[ProtoContract]
	public class RustComponent : MonoBehaviour
	{
		[Header("Installation")]

		[ProtoMember(1)]
		public bool IsServer;

		[ProtoMember(2)]
		public bool IsClient;

		[Header("Member Configuration")]

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

		internal Component _instance;

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
				var typeMember = type.GetField(member.Name, _monoFlags);
				typeMember.SetValue(this, Convert.ChangeType(member.Value, typeMember.FieldType));
			}
		}
	}
}
