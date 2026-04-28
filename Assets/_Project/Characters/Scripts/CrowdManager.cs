using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class CrowdManager : MonoBehaviour
{
    [Header("Pedestrian Prefabs")]
    public List<GameObject> pedestrianPrefabs = new();

    [Header("Marker Roots")]
    public Transform spawnPointsRoot;
    public Transform targetPointsRoot;

    [Header("Spawn Settings")]
    public int maxActiveAgents = 5;
    public float spawnIntervalMin = 1f;
    public float spawnIntervalMax = 2f;
    public float sampleRadius = 2f;

    private readonly List<GameObject> activeAgents = new();
    private readonly Dictionary<GameObject, GameObject> instanceToPrefab = new();
    private readonly HashSet<GameObject> activePrefabs = new();

    private Transform[] spawnPoints;
    private Transform[] targetPoints;

    void Start()
    {
        spawnPoints = GetChildren(spawnPointsRoot);
        targetPoints = GetChildren(targetPointsRoot);

        if (maxActiveAgents > pedestrianPrefabs.Count)
            maxActiveAgents = pedestrianPrefabs.Count;

        StartCoroutine(SpawnLoop());
    }

    IEnumerator SpawnLoop()
    {
        while (true)
        {
            CleanupDeadEntries();

            if (activeAgents.Count < maxActiveAgents)
                TrySpawnUnique();

            yield return new WaitForSeconds(Random.Range(spawnIntervalMin, spawnIntervalMax));
        }
    }

    void TrySpawnUnique()
    {
        if (spawnPoints.Length == 0 || targetPoints.Length == 0 || pedestrianPrefabs.Count == 0)
            return;

        List<GameObject> availablePrefabs = new();

        for (int i = 0; i < pedestrianPrefabs.Count; i++)
        {
            GameObject prefab = pedestrianPrefabs[i];
            if (prefab != null && !activePrefabs.Contains(prefab))
                availablePrefabs.Add(prefab);
        }

        if (availablePrefabs.Count == 0)
            return;

        GameObject chosenPrefab = availablePrefabs[Random.Range(0, availablePrefabs.Count)];
        Transform spawnMarker = spawnPoints[Random.Range(0, spawnPoints.Length)];
        Transform targetMarker = targetPoints[Random.Range(0, targetPoints.Length)];

        if (!TryGetNavMeshPosition(spawnMarker.position, out Vector3 spawnPos))
            return;

        if (!TryGetNavMeshPosition(targetMarker.position, out Vector3 targetPos))
            return;

        GameObject instance = Instantiate(chosenPrefab, spawnPos, spawnMarker.rotation);

        NavMeshAgent agent = instance.GetComponent<NavMeshAgent>();
        if (agent == null || !agent.isOnNavMesh)
        {
            Destroy(instance);
            return;
        }

        CrowdWalker walker = instance.GetComponent<CrowdWalker>();
        if (walker == null)
            walker = instance.AddComponent<CrowdWalker>();

        walker.Initialize(targetPos, this);

        activeAgents.Add(instance);
        instanceToPrefab[instance] = chosenPrefab;
        activePrefabs.Add(chosenPrefab);
    }

    bool TryGetNavMeshPosition(Vector3 source, out Vector3 result)
    {
        if (NavMesh.SamplePosition(source, out NavMeshHit hit, sampleRadius, NavMesh.AllAreas))
        {
            result = hit.position;
            return true;
        }

        result = Vector3.zero;
        return false;
    }

    Transform[] GetChildren(Transform root)
    {
        if (root == null) return new Transform[0];

        Transform[] items = new Transform[root.childCount];
        for (int i = 0; i < root.childCount; i++)
            items[i] = root.GetChild(i);

        return items;
    }

    void CleanupDeadEntries()
    {
        for (int i = activeAgents.Count - 1; i >= 0; i--)
        {
            GameObject instance = activeAgents[i];

            if (instance != null)
                continue;

            activeAgents.RemoveAt(i);

            if (instanceToPrefab.ContainsKey(instance))
            {
                GameObject prefab = instanceToPrefab[instance];
                instanceToPrefab.Remove(instance);

                if (prefab != null)
                    activePrefabs.Remove(prefab);
            }
        }
    }

    public void NotifyAgentDestroyed(GameObject instance)
    {
        activeAgents.Remove(instance);

        if (instanceToPrefab.TryGetValue(instance, out GameObject prefab))
        {
            instanceToPrefab.Remove(instance);

            if (prefab != null)
                activePrefabs.Remove(prefab);
        }
    }
}