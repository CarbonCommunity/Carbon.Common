namespace Carbon.Client
{
	public class RustComponent : MonoBehaviour
	{
		public bool IsClient;
		public bool IsServer;

		public string BaseType;
		public ValuePair[] Members;

		internal Component _instance;

		[Serializable]
		public class ValuePair
		{
			public string MemberName;
			public string Value;
		}

		public void ApplyComponent()
		{
			if (_instance != null)
			{
				return;
			}

			var type = AccessToolsEx.TypeByName(BaseType);
			_instance = gameObject.AddComponent(type);

			const BindingFlags _monoFlags = BindingFlags.Instance | BindingFlags.Public;

			foreach (var member in Members)
			{
				var typeMember = type.GetField(member.MemberName, _monoFlags);
				typeMember.SetValue(this, Convert.ChangeType(member.Value, typeMember.FieldType));
			}
		}
	}
}
