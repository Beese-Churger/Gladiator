using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spectator : MonoBehaviour
{
    [SerializeField] CrowdFavour crowdFavour;
    [SerializeField] Animator animator;

    // Threshold for increasing jump frequency
    public float jumpThreshold = 0.8f;

    // Initial jump frequency and maximum jump frequency
    public float initialJumpFrequency = 0.1f;
    public float maxJumpFrequency = 0.2f;

    private void Start()
    {
        crowdFavour = FindObjectOfType<CrowdFavour>();
        StartCoroutine(ContinuousJump());
    }

    private IEnumerator ContinuousJump()
    {
        while (true)
        {
            // Check if the slider is close to either extreme
            float normalizedValue = Mathf.InverseLerp(crowdFavour.advantageSlider.minValue, crowdFavour.advantageSlider.maxValue, crowdFavour.advantageSlider.value);
            if (normalizedValue < jumpThreshold || normalizedValue > (1 - jumpThreshold))
            {
                // Call your JumpFunction for spectators
                Jump();
            }

            // Calculate the next delay based on the slider's position
            float jumpFrequency = Mathf.Lerp(initialJumpFrequency, maxJumpFrequency, Mathf.Abs(normalizedValue - 0.5f) * 2f);

            float nextJumpDelay = 1.0f / (0.5f * jumpFrequency);

            float delay = 0.2f * nextJumpDelay;
            nextJumpDelay += Random.Range(-delay, delay);

            yield return new WaitForSeconds(nextJumpDelay);
        }
    }

    void Jump()
    {
        animator.Play("Jump");
    }
}
