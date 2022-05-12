using System;

using UnityEngine;

[Serializable]
public class PoolableObject
{
    public string     m_key;
    public float      m_lifeTime;
    public int        m_size;
   
    public GameObject m_prefab;

    private Transform m_parent;

    public bool m_canRecyleWithoutLifeTime;

    public void SetParent(Transform p_parent)
    {
        m_parent = p_parent;
    }

    public Transform GetParent()
    {
        return m_parent;
    }
}
