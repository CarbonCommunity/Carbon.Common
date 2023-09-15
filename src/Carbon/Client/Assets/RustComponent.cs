using ProtoBuf;

namespace Carbon.Client
{
	[ProtoContract]
	public partial class RustComponent : MonoBehaviour
	{
		[ProtoMember(1)]
		public Controls CreateComponentOn = new Controls();

		[ProtoMember(2)]
		public Controls DisableObjectOn = new Controls();

		[ProtoMember(3)]
		public Controls DestroyObjectOn = new Controls();

		[ProtoMember(4)]
		public string TargetType;

		[ProtoMember(5)]
		public Member[] Members;

		[Serializable, ProtoContract]
		public class Member
		{
			[ProtoMember(1)]
			public string Name;

			[ProtoMember(2)]
			public string Value;
		}

		[Serializable, ProtoContract]
		public class Controls
		{
			[ProtoMember(1)]
			public bool Server;

			[ProtoMember(2)]
			public bool Client;
		}
	}
}
