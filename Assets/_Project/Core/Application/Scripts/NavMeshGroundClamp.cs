using UnityEngine;
using UnityEngine.AI;

/**
 *
 * Keeps the rig standing on the NavMesh (where the crowd NPCs walk) rather than on the
 * invisible ProBuilder "Teleportation Nav" colliders, which do not line up with the visible
 * floor. Without this the local player stands off the ground and reads as the wrong height
 * next to the NPCs - and, because the rig drives the networked avatar, remote players see it.
 *
 * The Teleportation Nav "Open Area" is a single 181x41 m polyshape spanning both the footpath
 * and the plaza. It cannot follow the plaza's slope, so its error is uneven and no rigid
 * transform offset can fix it (measured: footpath ~0.0 m, plaza 0.07-0.46 m below the NavMesh).
 * Clamping at runtime sidesteps that instead of re-authoring the geometry.
 *
 * Only snaps when a NavMesh point is within maxSnapDistance, because the NavMesh covers
 * MAIN PLAZA only - the Teleportation Nav stairs are NOT baked into it. On the stairs the
 * nearest NavMesh point is the plaza ~0.97 m below, so an unconditional snap would drop the
 * player through the steps. maxSnapDistance must therefore sit between the worst plaza error
 * (~0.46) and the stair drop (~0.97).
 *
 **/

public class NavMeshGroundClamp : MonoBehaviour
{
    [Tooltip("Only snap when a NavMesh point is within this distance of the rig. Must exceed the " +
             "worst plaza error (~0.46 m) but stay below the drop from un-baked geometry such as " +
             "the stairs (~0.97 m), or the player sinks through them.")]
    public float maxSnapDistance = 0.5f;

    [Tooltip("Only ever move the rig down onto the NavMesh, never lift it up. Off by default: the " +
             "plaza teleport surface sits BELOW the NavMesh, so the rig must be lifted onto it.")]
    public bool downwardOnly = false;

    Vector3 lastClampedFrom;
    bool hasClamped;

    void LateUpdate()
    {
        var position = transform.position;

        // Only re-sample when the rig has actually moved; locomotion here is teleport-based
        // so this is idle most frames.
        if (hasClamped && position == lastClampedFrom) return;

        if (!NavMesh.SamplePosition(position, out var hit, maxSnapDistance, NavMesh.AllAreas)) return;

        var delta = hit.position.y - position.y;
        if (downwardOnly && delta > 0f) return;

        position.y = hit.position.y;
        transform.position = position;
        lastClampedFrom = position;
        hasClamped = true;
    }
}
