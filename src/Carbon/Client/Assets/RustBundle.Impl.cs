using Carbon.Client.Assets;
using Newtonsoft.Json;
using ProtoBuf;

namespace Carbon.Client
{
	public partial class RustBundle
	{
		public void ProcessComponents(Asset asset)
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
		public void ProcessPrefabs()
		{
			AddonManager.Instance.CreateRustPrefabs(RustPrefabs);
		}
		public void ProcessPrefabsAsync()
		{
			AddonManager.Instance.CreateRustPrefabsAsync(RustPrefabs);
		}
	}
}
