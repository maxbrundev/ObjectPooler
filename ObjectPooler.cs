using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utils.Extensions;

namespace Pooling
{
	public class ObjectPooler : MonoBehaviour
	{
		[Space, Header("POOLABLE OBJECTS")] 
		[SerializeField] private List<APoolableObject> poolableObjects = null;

		private Dictionary<APoolableObject, Queue<APoolableObject>> poolDictionary = new Dictionary<APoolableObject, Queue<APoolableObject>>();

		private Dictionary<APoolableObject, List<Tuple<float, APoolableObject>>> pooledObjectsWithLifeTime = new Dictionary<APoolableObject, List<Tuple<float, APoolableObject>>>();

		private Dictionary<APoolableObject, Transform> poolableParents = new Dictionary<APoolableObject, Transform>();

		private static ObjectPooler instance = null;

		public static ObjectPooler Instance
		{
			get
			{
				if (instance == null)
				{
					var go = new GameObject("ObjectPooler");
					go.AddComponent<ObjectPooler>();

					instance = go.GetComponent<ObjectPooler>();
				}

				return instance;
			}
		}

		// Use this for initialization
		void Awake()
		{
			if (instance != null && instance != this)
			{
				Destroy(this.gameObject);
				return;
			}

			instance = this;

			InitPoolDictionary();
		}

		void Update()
		{
			CheckPooledObjectsLifeTime();
		}

		void OnDestroy()
		{
			ClearAll();
		}

		private void InitPoolDictionary()
		{
			if (poolableObjects == null)
				return;

			foreach (APoolableObject poolableObject in poolableObjects)
			{
				if (poolDictionary.ContainsKey(poolableObject))
					continue;

				Queue<APoolableObject> gameObjectQueue = new Queue<APoolableObject>();

				GenerateKeyParentAndSet(poolableObject);

				var obj = Instantiate(poolableObject);

				obj.transform.parent = poolableParents[poolableObject];

				obj.SetActive(false);

				gameObjectQueue.Enqueue(obj);
				
				poolDictionary.Add(poolableObject, gameObjectQueue);
			}
		}

		public void Warm<T>(T poolable, int size) where T : APoolableObject
		{
			poolableObjects ??= new List<APoolableObject>();

			if (!poolableObjects.Contains(poolable))
				AddObjectToPool(poolable);

			for (int i = 0; i < size - 1; i++)
			{
				var obj = Instantiate(poolable);

				obj.transform.SetParent(poolableParents[poolable]);
				obj.SetActive(false);
				poolDictionary[poolable].Enqueue(obj);
			}
		}

		public void AddObjectToPool<T>(T poolable) where T : APoolableObject
		{
			poolableObjects ??= new List<APoolableObject>();

			if (poolableObjects.Contains(poolable) || poolDictionary.ContainsKey(poolable))
				return;

			poolableObjects.Add(poolable);

			Queue<APoolableObject> gameObjectQueue = new Queue<APoolableObject>();

			GenerateKeyParentAndSet(poolable);

			var obj = Instantiate(poolable);

			obj.transform.parent = poolableParents[poolable];

			obj.SetActive(false);

			gameObjectQueue.Enqueue(obj);

			poolDictionary.Add(poolable, gameObjectQueue);
		}

		public void Resize<T>(T poolable, int newSize) where T : APoolableObject
		{
			if (!poolDictionary.ContainsKey(poolable))
				return;

			if (poolDictionary[poolable].Count == newSize)
				return;

			bool isBigger = poolDictionary[poolable].Count < newSize;

			int diff = poolDictionary[poolable].Count - newSize;

			diff = Math.Abs(diff);

			int count = 0;

			while (count != diff)
			{
				if (isBigger)
				{
					var obj = Instantiate(poolable);

					obj.transform.parent = poolableParents[poolable];

					obj.SetActive(false);

					poolDictionary[poolable].Enqueue(obj);
				}
				else
				{
					APoolableObject spawnedObject = poolDictionary[poolable].Dequeue();

					if(pooledObjectsWithLifeTime.TryGetValue(poolable, out var poolableList))
					{
						for (int i = 0; i < poolableList.Count; i++)
						{
							if (poolableList[i].Item2 == spawnedObject)
							{
								poolableList.RemoveAt(i);
								break;
							}
						}
					}

					Destroy(spawnedObject.gameObject);
				}

				count++;
			}
		}

		public APoolableObject SpawnObjectFromPool<T>(T poolable, Vector3 position, Quaternion rotation) where T : APoolableObject
		{
			if (!poolDictionary.ContainsKey(poolable))
			{
				Debug.LogWarning($"Pool with Key {poolable} doesn't exist.");
				return null;
			}

			APoolableObject poolableInstanceDequeue = poolDictionary[poolable].Dequeue();

			if (poolableInstanceDequeue.gameObject.activeSelf && !poolable.canRecyleWithoutLifeTime)
			{
				var poolableInstance = Instantiate(poolable, position, rotation);

				poolableInstance.transform.SetParent(poolableParents[poolable]);

				poolableInstance.transform.position = position;
				poolableInstance.transform.rotation = rotation;

				poolDictionary[poolable].Enqueue(poolableInstanceDequeue);
				poolDictionary[poolable].Enqueue(poolableInstance);

				TryAddObjectToLifeTimeDictionary(poolable, poolableInstance);

				poolableInstance.Pool();

				return poolableInstance;
			}

			poolableInstanceDequeue.transform.position = position;
			poolableInstanceDequeue.transform.rotation = rotation;
			poolableInstanceDequeue.SetActive(true);

			poolableInstanceDequeue.Pool();

			poolDictionary[poolable].Enqueue(poolableInstanceDequeue);

			TryAddObjectToLifeTimeDictionary(poolable, poolableInstanceDequeue);

			return poolableInstanceDequeue;
		}

		private void TryAddObjectToLifeTimeDictionary<T>(T poolable, APoolableObject poolableInstance) where T : APoolableObject
		{
			if (poolable.lifeTime > 0.0f)
			{
				float currentTime = Time.time;

				if (pooledObjectsWithLifeTime.ContainsKey(poolable))
				{
					bool isObjectAlreadySet = false;

					for (int i = 0; i < pooledObjectsWithLifeTime[poolable].Count; i++)
					{
						if (pooledObjectsWithLifeTime[poolable][i].Item2 == poolableInstance)
						{
							pooledObjectsWithLifeTime[poolable][i] = new Tuple<float, APoolableObject>(currentTime, poolableInstance);
							isObjectAlreadySet = true;
							break;
						}
					}

					if (!isObjectAlreadySet)
					{
						pooledObjectsWithLifeTime[poolable].Add(new Tuple<float, APoolableObject>(currentTime, poolableInstance));
					}
				}
				else
				{
					pooledObjectsWithLifeTime.Add(poolable, new List<Tuple<float, APoolableObject>>());
					pooledObjectsWithLifeTime[poolable].Add(new Tuple<float, APoolableObject>(currentTime, poolableInstance));
				}
			}
		}

		private void CheckPooledObjectsLifeTime()
		{
			foreach (var pooledObjectWithLifeTime in pooledObjectsWithLifeTime)
			{
				foreach (var pair in pooledObjectWithLifeTime.Value)
				{
					if (pair.Item2.gameObject.activeSelf)
					{
						float timer = pair.Item1 + pair.Item2.lifeTime;

						if (Time.time > timer)
						{
							pair.Item2.SetActive(false);
						}
					}
				}
			}
		}

		public void Clear<T>(T poolable) where T : APoolableObject
		{
			if (!poolDictionary.ContainsKey(poolable))
				return;

			while (poolDictionary[poolable].Count > 0)
			{
				APoolableObject spawnedObject = poolDictionary[poolable].Dequeue();

				Destroy(spawnedObject.gameObject);
			}

			poolDictionary[poolable].Clear();
			poolDictionary.Remove(poolable);

			if (pooledObjectsWithLifeTime.ContainsKey(poolable))
			{
				pooledObjectsWithLifeTime[poolable].Clear();
				pooledObjectsWithLifeTime.Remove(poolable);
			}

			Destroy(poolableParents[poolable].gameObject);
		}

		public void ClearAll()
		{
			var poolDictionaryKey = new List<APoolableObject>();

			foreach (var poolable in poolDictionary)
			{
				poolDictionaryKey.Add(poolable.Key);
			}

			foreach (var key in poolDictionaryKey)
			{
				Clear(key);
			}

			poolDictionary.Clear();

			poolableParents.Clear();

			poolableObjects.Clear();

			pooledObjectsWithLifeTime.Clear();
		}

		public void RecycleAll()
		{
			foreach (var poolables in poolableObjects)
			{
				Recycle(poolables);
			}
		}

		public void Recycle<T>(T poolable) where T : APoolableObject
		{
			if(!poolDictionary.ContainsKey(poolable))
				return;

			foreach (var poolableObject in poolDictionary[poolable])
			{
				poolableObject.SetActive(false);
			}

			if (pooledObjectsWithLifeTime.ContainsKey(poolable))
			{
				pooledObjectsWithLifeTime[poolable].Clear();
				pooledObjectsWithLifeTime.Clear();
			}
		}

		private void GenerateKeyParentAndSet<T>(T poolable) where T : APoolableObject
		{
			if(poolableParents.ContainsKey(poolable))
				return;

			GameObject parent = new GameObject("[" + poolable.name + "]");

			parent.transform.parent = transform;

			poolableParents[poolable] = parent.transform;
		}
	}
}