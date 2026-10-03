using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class PlayerInteractor : MonoBehaviour
{
    public KeyCode interactKey = KeyCode.E;
    public LayerMask interactMask;

    [Header("Ranges")] public float itemRadius = 1.5f;


    [Header("References")] public Transform holdPoint;
    public ThoughtBubble thoughtBubble;

    [Header("Drop")] public float dropDistance = 0.9f;
    public float dropHeight = 0.5f;

    public MainItem Held { get; private set; }
    public bool Busy { get; private set; }

    private PlayerController player;
    private readonly Collider[] hits = new Collider[16];

    private MainItem candidateItem;
    private InteractionZone candidateZone;

    private void Awake()
    {
        player = GetComponent<PlayerController>();
        if (thoughtBubble == null)
        {
            thoughtBubble = GetComponentInChildren<ThoughtBubble>();
        }
    }

    private void OnDisable()
    {
        if (thoughtBubble != null)
        {
            thoughtBubble.Hide();
        }
    }

    private void Update()
    {
        if (Busy)
        {
            if (thoughtBubble != null)
            {
                thoughtBubble.Hide();
            }

            return;
        }

        Scan();
        UpdateBubble();

        if (Input.GetKeyDown(interactKey))
        {
            Interact();
        }
    }

    private void Scan()
    {
        candidateItem = null;
        candidateZone = null;

        if (!Held)
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, itemRadius, hits, interactMask,
                QueryTriggerInteraction.Collide);
            float best = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                MainItem item = hits[i].GetComponentInParent<MainItem>();
                if (!item || item.IsHeld)
                {
                    continue;
                }

                float d = (item.transform.position - transform.position).sqrMagnitude;
                if (d < best)
                {
                    best = d;
                    candidateItem = item;
                }
            }
        }
        else
        {
            float best = float.MaxValue;
            foreach (InteractionZone zone in InteractionZone.All)
            {
                if (!zone.CanUse(Held) || !zone.IsInRange(transform.position))
                {
                    continue;
                }

                float d = (zone.transform.position - transform.position).sqrMagnitude;
                if (d < best)
                {
                    best = d;
                    candidateZone = zone;
                }
            }
        }
    }

    private void UpdateBubble()
    {
        if (thoughtBubble == null)
        {
            return;
        }

        if (candidateItem != null)
        {
            thoughtBubble.Show(candidateItem.thoughtSprite, candidateItem.thoughtPrefab);
        }
        else if (candidateZone != null)
        {
            thoughtBubble.Show(candidateZone.thoughtSprite, candidateZone.thoughtPrefab);
        }
        else
        {
            thoughtBubble.Hide();
        }
    }

    private void Interact()
    {
        if (Held == null)
        {
            if (candidateItem == null)
            {
                return;
            }

            Held = candidateItem;
            Held.PickUp(holdPoint);
        }
        else if (candidateZone != null)
        {
            MainItem item = Held;
            Busy = true;
            candidateZone.Interact(item, this, consumed =>
            {
                Busy = false;
                if (consumed)
                {
                    item.Consume();
                    Held = null;
                }
            });
        }
        else
        {
            Vector3 dropPos = transform.position + player.Facing * dropDistance + Vector3.up * dropHeight;
            Held.Drop(dropPos);
            Held = null;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, itemRadius);
    }
}