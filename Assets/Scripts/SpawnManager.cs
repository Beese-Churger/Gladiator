using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public static SpawnManager Instance;

    SpawnPoint[] spawnPoints;

    private void Awake()
    {
        if (!Instance)
            Instance = this;
        else
            Destroy(this);
        spawnPoints = GetComponentsInChildren<SpawnPoint>();
    }

    public Transform GetSpawnPoint(int i)
    {
        return spawnPoints[i-1].transform;
    }
}
