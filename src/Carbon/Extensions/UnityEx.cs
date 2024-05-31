/*
 *
 * Copyright (c) 2022-2024 Carbon Community
 * All rights reserved.
 *
 */

namespace Carbon.Extensions;

public static class UnityEx
{
	public static T AddComponentCache<T>(this GameObject go) where T : MonoBehaviour
	{
		return go == null ? default : ComponentCacheBank.Instance.Add<T>(go);
	}

	public static MonoBehaviour AddComponentCache(this GameObject go, Type type)
	{
		return go == null ? default : ComponentCacheBank.Instance.Add(go, type);
	}

	public static T GetComponentCache<T>(this GameObject go) where T : MonoBehaviour
	{
		return go == null ? default : ComponentCacheBank.Instance.Get<T>(go);
	}

	public static MonoBehaviour GetComponentCache(this GameObject go, Type type)
	{
		return go == null ? default : ComponentCacheBank.Instance.Get(go, type);
	}

	public static bool RemoveComponentCache<T>(this GameObject go) where T : MonoBehaviour
	{
		return go == null ? default : ComponentCacheBank.Instance.Remove<T>(go);
	}

	public static bool RemoveComponentCache<T>(this GameObject go, Type type)
	{
		return go == null ? default : ComponentCacheBank.Instance.Remove(go, type);
	}

	public static T TryGetOrAddComponentCache<T>(this GameObject go) where T : MonoBehaviour
	{
		return go.GetComponentCache<T>() ?? go.AddComponentCache<T>();
	}

	public static MonoBehaviour TryGetOrAddComponentCache(this GameObject go, Type type)
	{
		return go.GetComponentCache(type) ?? go.AddComponentCache(type);
	}

	public static bool DestroyCache(this GameObject go)
	{
		return go != null && ComponentCacheBank.Instance.Destroy(go);
	}

	internal static void InternalEntityDestroy(BaseEntity entity)
	{
		if (entity == null || entity.gameObject == null)
		{
			return;
		}

		ComponentCacheBank.Instance.Remove<MonoBehaviour>(entity.gameObject);
	}

	public class ComponentCacheBank : Dictionary<GameObject, List<MonoBehaviour>>
	{
		public static ComponentCacheBank Instance { get; } = new();

		public T Add<T>(GameObject go) where T : MonoBehaviour
		{
			if (!this.TryGetValue(go, out var monos))
			{
				this[go] = monos = new();
			}

			var mono = go.AddComponent<T>();

			monos.Add(mono);

			return mono;
		}

		public MonoBehaviour Add(GameObject go, Type type)
		{
			if (!this.TryGetValue(go, out var monos))
			{
				this[go] = monos = new();
			}

			var mono = (MonoBehaviour)go.AddComponent(type);

			monos.Add(mono);

			return mono;
		}

		public T Get<T>(GameObject go) where T : MonoBehaviour
		{
			if (!this.TryGetValue(go, out var monos))
			{
				this[go] = monos = new();
			}

			var existent = monos.FirstOrDefault(x => x is T);

			if (existent != null)
			{
				return existent as T;
			}

			if (!go.TryGetComponent(out existent))
			{
				return default;
			}

			monos.Add(existent);

			return existent as T;
		}

		public MonoBehaviour Get(GameObject go, Type type)
		{
			if (!this.TryGetValue(go, out var monos))
			{
				this[go] = monos = new();
			}

			var existent = monos.FirstOrDefault(x => x.GetType() == type);

			if (existent != null)
			{
				return existent;
			}

			if (!go.TryGetComponent(out existent))
			{
				return default;
			}

			monos.Add(existent);

			return existent;
		}

		public bool Remove<T>(GameObject go, bool destroy = true) where T : MonoBehaviour
		{
			if (!this.TryGetValue(go, out var monos))
			{
				return false;
			}

			var removed = monos.RemoveAll(x =>
			{
				if (x is not T)
				{
					return false;
				}

				if (destroy)
				{
					GameObject.DestroyImmediate(x);
				}

				return true;
			});

			return removed > 0 && base.Remove(go);
		}

		public bool Remove(GameObject go, Type type, bool destroy = true)
		{
			if (!this.TryGetValue(go, out var monos))
			{
				return false;
			}

			var removed = monos.RemoveAll(x =>
			{
				if (x.GetType() != type)
				{
					return false;
				}

				if (destroy)
				{
					GameObject.DestroyImmediate(x);
				}

				return true;
			});

			return removed > 0 && base.Remove(go);
		}

		public bool Destroy(GameObject go)
		{
			if (!this.TryGetValue(go, out var monos))
			{
				GameObject.Destroy(go);
				return false;
			}

			monos.Clear();
			Remove<MonoBehaviour>(go, false);
			GameObject.Destroy(go);
			return true;
		}
	}
}
