using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Animator))]
public class RandomizeAnimatorStart : MonoBehaviour
{
    [SerializeField] private string stateName = "Base Layer.IdleTalk";
    [SerializeField] private int layer = 0;
    [SerializeField] private float minSpeed = 0.95f;
    [SerializeField] private float maxSpeed = 1.05f;

    IEnumerator Start()
    {
        Animator animator = GetComponent<Animator>();
        yield return null;

        animator.speed = Random.Range(minSpeed, maxSpeed);
        animator.Play(stateName, layer, Random.Range(0f, 1f));
    }
}