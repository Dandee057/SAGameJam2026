using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// The rat. Runs to the cheese, stands by it for a moment, eats it (the cheese disappears), changes sprite,
/// then hands over to an item version of itself (a normal pickup prefab) and removes itself.
///
/// Prefab: NavMeshAgent + this script + a sprite child. The item prefab is a separate pickup prefab; give it
/// the same art as 'finishedSprite' and the hand-over is invisible.
/// If the rat can't use the NavMesh (not on it / no path), it falls back to walking in a straight line.
public class RatEater : MonoBehaviour
{
    [Header("Movement")]
    public float runSpeed = 3.5f;
    [Tooltip("How close it gets to the cheese before it stops and starts eating.")]
    public float standOffDistance = 0.5f;
    [Tooltip("Safety net: gives up running after this long and just starts eating.")]
    public float maxRunTime = 10f;

    [Header("Eating")]
    public float eatTime = 2f;

    [Header("Becoming an item")]
    [Tooltip("Optional: the rat's sprite changes to this once the cheese is gone.")]
    public Sprite finishedSprite;
    [Tooltip("How long the new sprite shows before the item version replaces the rat.")]
    public float transformDelay = 0.4f;
    [Tooltip("The pickup version of the rat (a normal item prefab).")]
    public GameObject itemPrefab;
    public float itemSpawnHeight = 0.1f;

    [Header("Visuals")]
    public SpriteRenderer spriteRenderer;   // auto-found in children if empty
    public bool artFacesRight = true;

    private NavMeshAgent agent;
    private Transform cam;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (Camera.main != null)
        {
            cam = Camera.main.transform;
        }
    }

    /// Called by CheeseBait right after spawning.
    public void GoEat(GameObject cheese)
    {
        StartCoroutine(Run(cheese));
    }

    private IEnumerator Run(GameObject cheese)
    {
        yield return RunTo(cheese);

        // stand by the cheese, facing it
        if (cheese != null)
        {
            Face(cheese.transform.position - transform.position);
        }

        yield return new WaitForSeconds(eatTime);

        if (cheese != null)
        {
            Destroy(cheese);
        }

        if (finishedSprite != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = finishedSprite;
        }

        yield return new WaitForSeconds(transformDelay);

        if (itemPrefab == null)
        {
            Debug.LogWarning("RatEater: 'itemPrefab' isn't assigned, so the rat stays a rat.", this);
            yield break;
        }

        Instantiate(itemPrefab, transform.position + Vector3.up * itemSpawnHeight, transform.rotation);
        Destroy(gameObject);
    }

    private IEnumerator RunTo(GameObject target)
    {
        if (target == null)
        {
            yield break;
        }

        Vector3 goal = target.transform.position;
        bool useAgent = TrySnapToNavMesh();

        if (useAgent)
        {
            agent.speed = runSpeed;
            agent.updateRotation = false;
            agent.stoppingDistance = 0f;
            agent.SetDestination(goal);
        }

        float elapsed = 0f;
        while (elapsed < maxRunTime && FlatDistance(goal) > standOffDistance)
        {
            if (useAgent)
            {
                if (!agent.pathPending && agent.pathStatus == NavMeshPathStatus.PathInvalid)
                {
                    // no route to the cheese: switch to walking straight at it
                    agent.enabled = false;
                    useAgent = false;
                }
                else
                {
                    Face(agent.desiredVelocity);
                }
            }

            if (!useAgent)
            {
                Vector3 flatGoal = new(goal.x, transform.position.y, goal.z);
                Vector3 dir = flatGoal - transform.position;
                transform.position = Vector3.MoveTowards(transform.position, flatGoal, runSpeed * Time.deltaTime);
                Face(dir);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (useAgent && agent.isOnNavMesh)
        {
            agent.ResetPath();
        }
    }

    private bool TrySnapToNavMesh()
    {
        if (agent == null || !agent.enabled)
        {
            return false;
        }

        if (agent.isOnNavMesh)
        {
            return true;
        }

        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
            return agent.isOnNavMesh;
        }

        return false;
    }

    private float FlatDistance(Vector3 point)
    {
        Vector3 d = point - transform.position;
        d.y = 0f;
        return d.magnitude;
    }

    /// Flip the sprite to face a world direction, relative to the camera (left/right only).
    private void Face(Vector3 worldDir)
    {
        if (spriteRenderer == null || cam == null)
        {
            return;
        }

        float screenX = Vector3.Dot(worldDir, cam.right);
        if (Mathf.Abs(screenX) > 0.05f)
        {
            bool goingLeft = screenX < 0f;
            spriteRenderer.flipX = artFacesRight ? goingLeft : !goingLeft;
        }
    }
}
