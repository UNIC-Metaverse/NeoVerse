using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class CrowdWalker : MonoBehaviour
{
    public float arriveThreshold = 0.4f;

    private NavMeshAgent agent;
    private CrowdManager manager;
    private bool initialized;
    private bool finishing;

    public void Initialize(Vector3 destination, CrowdManager crowdManager)
    {
        agent = GetComponent<NavMeshAgent>();
        manager = crowdManager;

        if (!agent.isOnNavMesh)
            return;

        bool success = agent.SetDestination(destination);
        initialized = success;

        if (!success)
            Finish();
    }

    void Update()
    {
        if (!initialized || finishing || agent == null || !agent.isOnNavMesh)
            return;

        if (agent.pathPending)
            return;

        if (agent.pathStatus != NavMeshPathStatus.PathComplete)
        {
            Finish();
            return;
        }

        if (agent.remainingDistance <= Mathf.Max(arriveThreshold, agent.stoppingDistance))
        {
            if (!agent.hasPath || agent.velocity.sqrMagnitude < 0.01f)
                Finish();
        }
    }

    void Finish()
    {
        if (finishing) return;
        finishing = true;

        if (manager != null)
            manager.NotifyAgentDestroyed(gameObject);

        Destroy(gameObject);
    }
}