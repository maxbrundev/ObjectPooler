using System;
using System.Collections.Generic;

using UnityEngine;

public class ObjectPooler : MonoBehaviour
{
    public static ObjectPooler m_instance;

    [Space, Header("POOLABLE OBJECTS")]
    [SerializeField] private List<PoolableObject> m_poolableObjectList; // List of PoolableObject

    private Dictionary<string, Queue<GameObject>> m_poolDictionary = new Dictionary<string, Queue<GameObject>>(); // PoolableObject key and Queue of prefab to spawn

    [SerializeField] public Dictionary<PoolableObject, List<Tuple<float, GameObject>>> m_pooledObjectsWithLifeTime = new Dictionary<PoolableObject, List<Tuple<float, GameObject>>>();

    private float m_currentTime = 0.0f;

    void Awake()
    {
        m_instance = this;
    }

    // Use this for initialization
    void Start()
    {
        InitPoolDictionary();
    }

    void Update()
    {
        m_currentTime += Time.deltaTime;

        CheckPooledObjectsLifeTime();
    }

    void OnDestroy()
    {
        m_poolableObjectList.Clear();
        m_poolDictionary.Clear();
        m_poolableObjectList.Clear();
    }

    private void InitPoolDictionary()
    {
        foreach (PoolableObject poolableObject in m_poolableObjectList)
        {
            Queue<GameObject> gameObjectQueue = new Queue<GameObject>();

            GameObject parent = new GameObject("[" + poolableObject.m_key + "]");

            parent.transform.SetParent(transform);

            poolableObject.SetParent(parent.transform);

            for (int i = 0; i < poolableObject.m_size; i++)
            {
                GameObject obj = Instantiate(poolableObject.m_prefab);
                //obj.transform.SetParent(parent.transform);
                obj.transform.SetParent(poolableObject.GetParent());

                obj.SetActive(false);

                gameObjectQueue.Enqueue(obj);
            }

            m_poolDictionary.Add(poolableObject.m_key, gameObjectQueue);
        }
    }

    public void AddObjectToPool(PoolableObject p_poolableObject)
    {
        if(m_poolableObjectList.Contains(p_poolableObject))
            return;

        m_poolableObjectList.Add(p_poolableObject);

        Queue<GameObject> gameObjectQueue = new Queue<GameObject>();

        GameObject parent = new GameObject("[" + p_poolableObject.m_key + "]");

        parent.transform.SetParent(transform);

        p_poolableObject.SetParent(parent.transform);

        for (int i = 0; i < p_poolableObject.m_size; i++)
        {
            GameObject obj = Instantiate(p_poolableObject.m_prefab);
            //obj.transform.SetParent(parent.transform);
            obj.transform.SetParent(p_poolableObject.GetParent());

            obj.SetActive(false);

            gameObjectQueue.Enqueue(obj);
        }

        m_poolDictionary.Add(p_poolableObject.m_key, gameObjectQueue);
    }

    public GameObject SpawnObjectFromPool(string p_key, Vector3 p_position, Quaternion p_rotation)
    {
        if (!m_poolDictionary.ContainsKey(p_key))
        {
            Debug.LogWarning($"Pool with Key {p_key} doesn't exist.");
            return null;
        }

        GameObject spawnedObject = m_poolDictionary[p_key].Dequeue();

        PoolableObject currentPoolableObject = GetPoolableObject(p_key);

        if (spawnedObject.activeSelf && !currentPoolableObject.m_canRecyleWithoutLifeTime)
        {
            GameObject obj = Instantiate(currentPoolableObject.m_prefab, p_position, p_rotation);
            obj.transform.SetParent(currentPoolableObject.GetParent());
            obj.transform.position = p_position;
            obj.transform.rotation = p_rotation;

            m_poolDictionary[p_key].Enqueue(spawnedObject);
            m_poolDictionary[p_key].Enqueue(obj);

            return obj;
        }

        spawnedObject.transform.position = p_position;
        spawnedObject.transform.rotation = p_rotation;
        spawnedObject.SetActive(true);

        m_poolDictionary[p_key].Enqueue(spawnedObject);

        TryAddObjectToLifeTimeDictionary(p_key, spawnedObject);

        return spawnedObject;
    }

    private void TryAddObjectToLifeTimeDictionary(string p_poolableObjectKey, GameObject p_object)
    {
        for (int i = 0; i < m_poolableObjectList.Count; i++)
        {
            if (m_poolableObjectList[i].m_key == p_poolableObjectKey)
            {
                if (m_poolableObjectList[i].m_lifeTime > 0.0f)
                {
                    PoolableObject currentPoolableObject = m_poolableObjectList[i];

                    float currentTime = Time.time;

                    if (m_pooledObjectsWithLifeTime.ContainsKey(currentPoolableObject))
                    {
                        bool isObjectAlreadySet = false;

                        for (int j = 0; j < m_pooledObjectsWithLifeTime[currentPoolableObject].Count; j++)
                        {
                            if (m_pooledObjectsWithLifeTime[currentPoolableObject][j].Item2 == p_object)
                            {
                                isObjectAlreadySet = true;
                                m_pooledObjectsWithLifeTime[currentPoolableObject][j] = new Tuple<float, GameObject>(currentTime, p_object);
                                break;
                            }

                        }

                        if (!isObjectAlreadySet)
                        {
                            m_pooledObjectsWithLifeTime[currentPoolableObject].Add(new Tuple<float, GameObject>(currentTime, p_object));
                        }
                    }
                    else
                    {
                        m_pooledObjectsWithLifeTime.Add(currentPoolableObject, new List<Tuple<float, GameObject>>());
                        m_pooledObjectsWithLifeTime[currentPoolableObject].Add(new Tuple<float, GameObject>(currentTime, p_object));
                    }
                }
            }
        }
    }

    private void CheckPooledObjectsLifeTime()
    {
        foreach (var pooledObjectWithLifeTime in m_pooledObjectsWithLifeTime)
        {
            if (pooledObjectWithLifeTime.Key.m_lifeTime > 0.0f)
            {
                foreach (var tuple in pooledObjectWithLifeTime.Value)
                {
                    if (tuple.Item2 != null)
                    {
                        if (tuple.Item2.activeSelf)
                        {
                            float timer = tuple.Item1 + pooledObjectWithLifeTime.Key.m_lifeTime;

                            if (m_currentTime > timer)
                            {
                                Debug.Log("Disable");
                                tuple.Item2.SetActive(false);
                            }
                        }
                    }
                }
            }
        }
    }

    private PoolableObject GetPoolableObject(string p_key)
    {
        for (int i = 0; i < m_poolableObjectList.Count; i++)
        {
            if (m_poolableObjectList[i].m_key == p_key)
            {
                return m_poolableObjectList[i];
            }
        }

        Debug.LogWarning($"Poolable Object with Key {p_key} doesn't exist.");

        return null;
    }
}