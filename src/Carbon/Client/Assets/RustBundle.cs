using Carbon.Client.Assets;
using Newtonsoft.Json;
using ProtoBuf;

namespace Carbon.Client
{
	[ProtoContract]
	public class RustBundle
	{
		[ProtoMember(1)]
		public Dictionary<string, List<RustComponent>> Components = new Dictionary<string, List<RustComponent>>();

		[ProtoMember(2)]
		public List<RustPrefab> RustPrefabs = new List<RustPrefab>();

		public void Process(Asset asset)
		{
			foreach (var path in asset.CachedBundle.GetAllAssetNames())
			{
				var unityAsset = asset.CachedBundle.LoadAsset<UnityEngine.Object>(path);

				if (unityAsset is GameObject go)
				{
					Recurse(go.transform);

					void Recurse(Transform transform)
					{
						if (Components.TryGetValue(transform.GetRecursiveName().ToLower(), out var components))
						{
							foreach (var component in components)
							{
								component.Apply(transform.gameObject);
							}
						}

						foreach (Transform subTransform in transform)
						{
							Recurse(subTransform);
						}
					}
				}
			}
		}
	}
}
