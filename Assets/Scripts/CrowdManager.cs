using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrowdManager : MonoBehaviour
{
    public GameObject cylinderPrefab;
    public int numberOfCylinders = 60;
    public float radius = 16.5f;
    public float startAngle = 0f;
    public float minAngleIncrement = 1f;
    public float maxAngleIncrement = 10f;
    public Transform center;
    void Start()
    {
        for (int i = 0; i < numberOfCylinders; i++)
        {
            float angleIncrement = 0.5f + Random.Range(minAngleIncrement, maxAngleIncrement);
            SpawnCylinderOnEdge(startAngle + i * angleIncrement);
            //yield return null;
        }
        //StartCoroutine(SpawnCylinders());
    }

    IEnumerator SpawnCylinders()
    {
        for (int i = 0; i < numberOfCylinders; i++)
        {
            float angleIncrement = Random.Range(minAngleIncrement, maxAngleIncrement);
            SpawnCylinderOnEdge(startAngle + i * angleIncrement);
            yield return null;
        }
    }

    void SpawnCylinderOnEdge(float angle)
    {
        Vector2 randomPoint = Quaternion.Euler(0, 0, angle) * Vector2.up * radius;
        Vector3 spawnPosition = new Vector3(randomPoint.x, center.position.y, randomPoint.y);

        if (!CheckOverlap(spawnPosition))
        {
            Instantiate(cylinderPrefab, spawnPosition, Quaternion.identity);
        }
    }

    bool CheckOverlap(Vector3 position)
    {
        Collider[] colliders = Physics.OverlapSphere(position, 0.5f); // Adjust the radius as needed

        foreach (Collider collider in colliders)
        {
            if (collider.CompareTag("Spectator") && collider != null)
            {
                return true; // Overlapping
            }
        }

        return false; // Not overlapping
    }
}
