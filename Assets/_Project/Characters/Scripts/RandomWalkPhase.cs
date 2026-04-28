using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class RandomWalkPhase : MonoBehaviour
{
    [Header("Animator State")]
    [SerializeField] private string stateName = "Base Layer.Walk";
    [SerializeField] private int layer = 0;

    [Header("Randomization")]
    [SerializeField] private bool randomizePhase = true;
    [SerializeField] private bool randomizeSpeed = true;
    [SerializeField] private float minSpeed = 0.96f;
    [SerializeField] private float maxSpeed = 1.04f;

    private Animator animator;
    private int stateHash;

    private IEnumerator Start()
    {
        animator = GetComponent<Animator>();
        stateHash = Animator.StringToHash(stateName);

        yield return null;

        if (randomizeSpeed)
            animator.speed = Random.Range(minSpeed, maxSpeed);

        if (randomizePhase)
            animator.Play(stateHash, layer, Random.Range(0f, 1f));
    }
}