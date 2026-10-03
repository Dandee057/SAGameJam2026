using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

/// A waiter who wanders: pick a random spot on the NavMesh, walk there, pause, repeat.
/// If the player bumps into them they get knocked back and stunned.
///
/// Setup: a GameObject with a NavMeshAgent + this script (+ a billboard sprite child).
/// The player must be tagged "Player" (or drag it into 'player'). The floor needs a baked NavMesh.
[RequireComponent(typeof(NavMeshAgent))]
public class WaiterWander : MonoBehaviour
{
    public static readonly List<WaiterWander> All = new();

    [Header("Wandering")] [Tooltip("Centre of the area to wander in. Defaults to where the waiter starts.")]
    public Transform wanderCenter;

    public float wanderRadius = 8f;
    public float minTravelDistance = 2f; // ignore picks closer than this
    public float minWait = 0.5f;
    public float maxWait = 2f;
    public float arriveDistance = 0.3f;

    [Header("Bumping the player")] public PlayerController player; // auto-found by the "Player" tag if empty
    public float contactRadius = 0.8f; // flat distance at which the waiter "hits" the player
    public float maxHeightDifference = 1.5f; // so jumping over/past them doesn't count
    public float knockbackSpeed = 9f; // with the player's knockbackDecel (20) that's about 2 units of shove
    public float stunDuration = 2f;
    public float hitCooldown = 1f; // grace period AFTER the stun ends
    public float pauseAfterHit = 1f; // waiter stops for a moment, too
    public UnityEvent onHitPlayer; // hook up sounds / a dizzy effect

    [Header("Visuals")]
    public SpriteRenderer spriteRenderer; // auto-found in children if empty; flipped to face the walking direction

    [Tooltip("Tick if your waiter art faces RIGHT by default. Untick if it faces left.")]
    public bool artFacesRight = true;

    private NavMeshAgent agent;
    private Vector3 center;
    private bool waiting = true;
    private float waitTimer;
    private float nextHitTime;
    private bool frozen;
    private Transform cam;

    private void OnEnable()
    {
        All.Add(this);
    }

    private void OnDisable()
    {
        All.Remove(this);
    }

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false; // the sprite billboards, so no need to spin the root

        center = wanderCenter != null ? wanderCenter.position : transform.position;
        waitTimer = Random.Range(minWait, maxWait);
        if (Camera.main != null)
        {
            cam = Camera.main.transform;
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (player == null)
        {
            GameObject go = GameObject.FindWithTag("Player");
            if (go != null)
            {
                player = go.GetComponent<PlayerController>();
            }
        }

        if (player == null)
        {
            Debug.LogError("WaiterWander: no player found. Tag the player 'Player' or assign 'player'.", this);
        }
    }

    /// Called by GameFlow when the sabotage phase ends.
    public void Freeze()
    {
        frozen = true;
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
        }
    }

    private void Update()
    {
        if (frozen)
        {
            return;
        }

        CheckPlayerContact();

        if (!agent.isOnNavMesh)
        {
            return;
        }

        if (waiting)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
            {
                PickDestination();
            }
        }
        else if (!agent.pathPending &&
                 (agent.remainingDistance <= arriveDistance || agent.pathStatus == NavMeshPathStatus.PathInvalid))
        {
            StartWaiting(Random.Range(minWait, maxWait));
        }

        UpdateFacing();
    }

    private void StartWaiting(float seconds)
    {
        waiting = true;
        waitTimer = seconds;
        agent.ResetPath();
    }

    private void PickDestination()
    {
        for (int i = 0; i < 12; i++)
        {
            Vector3 p = center + Random.insideUnitSphere * wanderRadius;
            p.y = center.y;

            if (!NavMesh.SamplePosition(p, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                continue;
            }

            if ((hit.position - transform.position).sqrMagnitude < minTravelDistance * minTravelDistance)
            {
                continue;
            }

            agent.SetDestination(hit.position);
            waiting = false;
            return;
        }

        // nothing suitable found - wait and try again
        waitTimer = Random.Range(minWait, maxWait);
    }

    private void CheckPlayerContact()
    {
        if (player == null)
        {
            return;
        }

        if (player.inputLocked || player.IsStunned || player.IsHidden)
        {
            return; // in a minigame / already stunned / hiding
        }

        if (Time.time < nextHitTime)
        {
            return;
        }

        Vector3 d = player.transform.position - transform.position;
        if (Mathf.Abs(d.y) > maxHeightDifference)
        {
            return;
        }

        d.y = 0f;
        if (d.sqrMagnitude > contactRadius * contactRadius)
        {
            return;
        }

        // push the player directly away from the waiter
        Vector3 dir = d.sqrMagnitude > 0.0001f ? d.normalized : transform.forward;
        player.Knockback(dir, knockbackSpeed, stunDuration);

        nextHitTime = Time.time + stunDuration + hitCooldown;
        if (agent != null && agent.isOnNavMesh)
        {
            StartWaiting(pauseAfterHit);
        }

        onHitPlayer?.Invoke();
    }

    private void UpdateFacing()
    {
        if (spriteRenderer == null || cam == null)
        {
            return;
        }

        // which way is she heading on SCREEN? (left/right relative to the camera)
        float screenX = Vector3.Dot(agent.desiredVelocity, cam.right);
        if (Mathf.Abs(screenX) > 0.1f)
        {
            bool goingLeft = screenX < 0f;
            spriteRenderer.flipX = artFacesRight ? goingLeft : !goingLeft;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Vector3 c = Application.isPlaying ? center : wanderCenter != null ? wanderCenter.position : transform.position;
        UnityEditor.Handles.color = new Color(1f, 0.6f, 0.1f, 0.9f);
        UnityEditor.Handles.DrawWireDisc(c, Vector3.up, wanderRadius);

        UnityEditor.Handles.color = new Color(1f, 0.2f, 0.2f, 0.9f);
        UnityEditor.Handles.DrawWireDisc(transform.position, Vector3.up, contactRadius);
    }
#endif
}