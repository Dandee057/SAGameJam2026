using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class WaiterVision : MonoBehaviour
{
    [Header("Vision")] public float viewDistance = 6f;
    [Range(10f, 180f)] public float viewAngle = 90f;
    public float eyeHeight = 1f;

    [Tooltip("Layers that block her sight (walls, big furniture). Leave on Nothing and nothing blocks the view.")]
    public LayerMask obstacleMask;

    [Header("Looking around")] public float turnSpeed = 8f;

    [Tooltip("While standing still she sweeps her gaze this many degrees either side.")]
    public float scanAngle = 45f;

    public float scanSpeed = 1.5f;

    [Header("Spotting the player")]
    [Tooltip("She keeps staring this long after she loses sight of you, before carrying on.")]
    public float lingerTime = 1f;

    [Tooltip("Degrees per second she can turn to follow you while alerted. Lower = easier to slip out of her cone.")]
    public float trackTurnSpeed = 90f;

    [Header("Suspicion (fraction of the bar per second)")] [Tooltip("Just being in view.")]
    public float visibleRate = 0.1f;

    [Tooltip("In view while a minigame is open.")]
    public float shadyRate = 0.6f;

    [Header("Cone visual")] public bool showCone = true;

    [Tooltip("Optional. A transparent unlit material. Empty = uses Sprites/Default.")]
    public Material coneMaterial;

    public Color calmColour = new(1f, 0.9f, 0.3f, 0.25f);
    public Color alertColour = new(1f, 0.2f, 0.2f, 0.4f);
    public int coneRays = 24;
    public float coneHeight = 0.03f;

    [Tooltip("If the cone hides under the floor sprite raise this; if it covers the characters lower it.")]
    public int coneSortingOrder = 0;

    [Header("Alert icon")] [Tooltip("Optional '!' object above her head, shown while she can see you.")]
    public GameObject alertIcon;

    [Header("References")] public PlayerController player;

    private NavMeshAgent agent;
    private WaiterWander wander;
    private GameFlow gameFlow;
    private Vector3 baseFacing = Vector3.right;
    private Vector3 facing = Vector3.right;
    private float scanBlend;

    private float alertTimer;
    private Vector3 lastSeenPos;

    private Transform coneTransform;
    private MeshRenderer coneRenderer;
    private Mesh coneMesh;
    private Vector3[] verts;
    private Color[] colours;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        wander = GetComponent<WaiterWander>();

        Vector3 f = transform.forward;
        f.y = 0f;
        if (f.sqrMagnitude > 0.01f)
        {
            baseFacing = f.normalized;
        }

        facing = baseFacing;
        BuildCone();
    }

    private void Start()
    {
        gameFlow = FindAnyObjectByType<GameFlow>();

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
            Debug.LogError("WaiterVision: no player found. Tag the player 'Player' or assign 'player'.", this);
        }
    }

    private void Update()
    {
        bool active = gameFlow == null || gameFlow.Phase == GamePhase.Sabotage;

        bool sees = active && CanSeePlayer();

        if (sees)
        {
            alertTimer = lingerTime;
            lastSeenPos = player.transform.position;
        }
        else
        {
            alertTimer = active ? Mathf.Max(0f, alertTimer - Time.deltaTime) : 0f;
        }

        if (wander != null)
        {
            wander.Holding = alertTimer > 0f;
        }

        UpdateFacing();

        if (sees && SuspicionMeter.Instance != null)
        {
            float rate = MinigameManager.IsRunning ? shadyRate : visibleRate;
            if (rate > 0f)
            {
                SuspicionMeter.Instance.ReportSeen(rate);
            }
        }

        if (alertIcon != null)
        {
            alertIcon.SetActive(sees);
        }

        UpdateCone(active, sees);
    }


    private void UpdateFacing()
    {
        bool alerted = alertTimer > 0f;

        Vector3 v = agent.desiredVelocity;
        v.y = 0f;
        bool moving = !alerted && v.sqrMagnitude > 0.04f;

        if (alerted)
        {
            Vector3 to = lastSeenPos - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude > 0.01f)
            {
                Vector3 turned = Vector3.RotateTowards(baseFacing, to.normalized,
                    trackTurnSpeed * Mathf.Deg2Rad * Time.deltaTime, 0f);
                if (turned.sqrMagnitude > 0.0001f)
                {
                    baseFacing = turned.normalized;
                }
            }
        }
        else if (moving)
        {
            Vector3 target = v.normalized;
            Vector3 blended = Vector3.Slerp(baseFacing, target, 1f - Mathf.Exp(-turnSpeed * Time.deltaTime));
            if (blended.sqrMagnitude > 0.0001f)
            {
                baseFacing = blended.normalized;
            }
        }

        float scanTarget = moving || alerted ? 0f : 1f;
        scanBlend = Mathf.MoveTowards(scanBlend, scanTarget, Time.deltaTime * (alerted ? 4f : 2f));
        float offset = Mathf.Sin(Time.time * scanSpeed) * scanAngle * scanBlend;
        facing = Quaternion.Euler(0f, offset, 0f) * baseFacing;
    }

    private bool CanSeePlayer()
    {
        if (player == null || player.IsHidden)
        {
            return false;
        }

        Vector3 to = player.transform.position - transform.position;
        to.y = 0f;
        float dist = to.magnitude;
        if (dist > viewDistance)
        {
            return false;
        }

        if (dist > 0.01f && Vector3.Angle(facing, to) > viewAngle * 0.5f)
        {
            return false;
        }

        if (obstacleMask.value != 0)
        {
            Vector3 eye = transform.position + Vector3.up * eyeHeight;
            Vector3 target = player.transform.position + Vector3.up * 0.5f;
            Vector3 dir = target - eye;
            if (Physics.Raycast(eye, dir.normalized, dir.magnitude, obstacleMask, QueryTriggerInteraction.Ignore))
            {
                return false;
            }
        }

        return true;
    }


    private void BuildCone()
    {
        GameObject go = new("VisionCone");
        coneTransform = go.transform;
        coneTransform.SetParent(transform, false);

        MeshFilter filter = go.AddComponent<MeshFilter>();
        coneRenderer = go.AddComponent<MeshRenderer>();
        coneRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        coneRenderer.receiveShadows = false;
        coneRenderer.sortingOrder = coneSortingOrder;

        Material m = coneMaterial;
        if (m == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                m = new Material(shader);
            }
        }

        coneRenderer.sharedMaterial = m;

        int rays = Mathf.Max(2, coneRays);
        verts = new Vector3[rays + 2];
        colours = new Color[rays + 2];
        int[] tris = new int[rays * 3];
        for (int i = 0; i < rays; i++)
        {
            tris[i * 3] = 0;
            tris[i * 3 + 1] = i + 1;
            tris[i * 3 + 2] = i + 2;
        }

        coneMesh = new Mesh { name = "VisionCone" };
        coneMesh.MarkDynamic();
        coneMesh.vertices = verts;
        coneMesh.colors = colours;
        coneMesh.triangles = tris;
        filter.sharedMesh = coneMesh;
    }

    private void UpdateCone(bool active, bool sees)
    {
        bool visible = showCone && active;
        if (coneRenderer.enabled != visible)
        {
            coneRenderer.enabled = visible;
        }

        if (!visible)
        {
            return;
        }

        int rays = verts.Length - 2;
        float floorY = transform.position.y + coneHeight;
        Vector3 eye = transform.position + Vector3.up * eyeHeight;
        Color c = sees ? alertColour : calmColour;

        verts[0] = coneTransform.InverseTransformPoint(new Vector3(transform.position.x, floorY, transform.position.z));
        colours[0] = c;

        for (int i = 0; i <= rays; i++)
        {
            float angle = Mathf.Lerp(-viewAngle * 0.5f, viewAngle * 0.5f, i / (float)rays);
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * facing;

            float length = viewDistance;
            if (obstacleMask.value != 0 &&
                Physics.Raycast(eye, dir, out RaycastHit hit, viewDistance, obstacleMask,
                    QueryTriggerInteraction.Ignore))
            {
                length = hit.distance;
            }

            Vector3 world = transform.position + dir * length;
            world.y = floorY;
            verts[i + 1] = coneTransform.InverseTransformPoint(world);
            colours[i + 1] = c;
        }

        coneMesh.vertices = verts;
        coneMesh.colors = colours;
        coneMesh.RecalculateBounds();
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 f = Application.isPlaying ? facing : transform.forward;
        f.y = 0f;
        if (f.sqrMagnitude < 0.01f)
        {
            f = Vector3.right;
        }

        f.Normalize();
        Gizmos.color = new Color(1f, 0.9f, 0.3f);
        Vector3 origin = transform.position + Vector3.up * 0.05f;
        Gizmos.DrawLine(origin, origin + Quaternion.Euler(0f, -viewAngle * 0.5f, 0f) * f * viewDistance);
        Gizmos.DrawLine(origin, origin + Quaternion.Euler(0f, viewAngle * 0.5f, 0f) * f * viewDistance);
    }
}