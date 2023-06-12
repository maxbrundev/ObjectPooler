using System;

using UnityEngine;

namespace Pooling
{
	[Serializable]
	public abstract class APoolableObject : MonoBehaviour, IPoolable
	{
		[SerializeField] public float lifeTime;

		[SerializeField] public bool canRecyleWithoutLifeTime;

		public abstract void Pool();
	}
}