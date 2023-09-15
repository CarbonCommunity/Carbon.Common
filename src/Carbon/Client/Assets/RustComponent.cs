using System;
using ProtoBuf;
using UnityEngine;

namespace Carbon.Client
{
	[ProtoContract]
	public partial class RustComponent : MonoBehaviour
	{
		[ProtoMember(1)]
		public Platform DisableObjectOn = new Platform();

		[ProtoMember(2)]
		public Platform DestroyObjectOn = new Platform();

		[ProtoMember(3)]
		public ComponentInfo Component = new ComponentInfo();

		[Serializable, ProtoContract]
		public class Member
		{
			[ProtoMember(1)]
			public string Name;

			[ProtoMember(2)]
			public string Value;
		}

		[Serializable, ProtoContract]
		public class Platform
		{
			[ProtoMember(1)]
			public bool Server;

			[ProtoMember(2)]
			public bool Client;
		}

		[Serializable, ProtoContract]
		public class ComponentInfo
		{
			[ProtoMember(1)]
			public Platform CreateOn = new Platform();

			[ProtoMember(2)]
			public string Type;

			[ProtoMember(3)]
			public Member[] Members;
		}
	}
}
