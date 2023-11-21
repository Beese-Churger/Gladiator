using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DetectAttacker : MonoBehaviour
{
    [SerializeField] PlayerController playerController;

    private void OnTriggerEnter(Collider other)
    {
        PlayerController pc = other.GetComponent<PlayerController>();
        if (playerController.opponentsInAttackRange.Contains(pc))
            return;

        playerController.opponentsInAttackRange.Add(pc);
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerController pc = other.GetComponent<PlayerController>();
        playerController.opponentsInAttackRange.Remove(pc);
    }
}
