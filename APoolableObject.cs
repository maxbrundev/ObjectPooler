using System;

using UnityEngine;

namespace Pooling
{
	public abstract class APoolableObject : MonoBehaviour, IPoolable
	{
		public float lifeTime;
		public bool canRecyleWithoutLifeTime;

		public event Action<APoolableObject> Pooled;
		public event Action<APoolableObject> Freed;

		private Transform poolParentTransform;

		public virtual void Initialize(Transform parent)
		{
			poolParentTransform = parent;

			transform.SetParent(parent);
		}

		public virtual void Pool()
		{
			Pooled?.Invoke(this);
			Pooled = null;
		}

		public virtual void Free()
		{
			transform.SetParent(poolParentTransform);

			gameObject.SetActive(false);

			Freed?.Invoke(this);
			Freed = null;
		}
	}
}