using UnityEngine;

/// Every frame: figure out what the player COULD do, show it as a thought, and do it on E.
///   - Near a zone that fits (right item, or an item-less zone with empty hands) -> bubble shows the zone's thought. E uses it.
///   - Empty hands near an item -> bubble shows the item. E picks it up.
///   - Empty hands near both   -> whichever is closer wins.
///   - Holding an item, no zone nearby -> no bubble. E drops it.
[RequireComponent(typeof(PlayerController))]
public class PlayerInteractor : MonoBehaviour
{
    public KeyCode interactKey = KeyCode.E;
    public LayerMask interactMask; // layer for items

    [Header("Ranges")] public float itemRadius = 1.5f; // how close to an item to "think about it"
    // zones define their own reach - see InteractionZone.reach

    [Header("References")] public Transform holdPoint;
    public ThoughtBubble thoughtBubble; // auto-found on a child if left empty

    [Header("Drop")] public float dropDistance = 0.9f;
    public float dropHeight = 0.5f;

    public MainItem Held { get; private set; }
    public bool Busy { get; private set; } // true while a zone interaction / minigame is running

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
        float bestItem = float.MaxValue;
        float bestZone = float.MaxValue;

        // items (only when hands are empty)
        if (Held == null)
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, itemRadius, hits, interactMask,
                QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                MainItem item = hits[i].GetComponentInParent<MainItem>();
                if (item == null || item.IsHeld)
                {
                    continue;
                }

                float d = (item.transform.position - transform.position).sqrMagnitude;
                if (d < bestItem)
                {
                    bestItem = d;
                    candidateItem = item;
                }
            }
        }

        // zones (item zones when holding the right thing, item-less zones when empty-handed)
        foreach (InteractionZone zone in InteractionZone.All)
        {
            if (!zone.CanUse(Held) || !zone.IsInRange(transform.position))
            {
                continue;
            }

            float d = (zone.transform.position - transform.position).sqrMagnitude;
            if (d < bestZone)
            {
                bestZone = d;
                candidateZone = zone;
            }
        }

        // both available (empty-handed): nearest wins
        if (candidateItem != null && candidateZone != null)
        {
            if (bestZone <= bestItem)
            {
                candidateItem = null;
            }
            else
            {
                candidateZone = null;
            }
        }
    }

    private void UpdateBubble()
    {
        if (thoughtBubble == null)
        {
            return;
        }

        if (candidateZone != null)
        {
            thoughtBubble.Show(candidateZone.thoughtSprite, candidateZone.thoughtPrefab);
        }
        else if (candidateItem != null)
        {
            thoughtBubble.Show(candidateItem.thoughtSprite, candidateItem.thoughtPrefab);
        }
        else
        {
            thoughtBubble.Hide();
        }
    }

    private void Interact()
    {
        if (candidateZone != null)
        {
            MainItem item = Held; // may be null (item-less zone)
            Busy = true;
            candidateZone.Interact(item, this, consumed =>
            {
                Busy = false;
                if (consumed && item != null)
                {
                    item.Consume();
                    Held = null;
                }
            });
            return;
        }

        if (Held == null)
        {
            if (candidateItem == null)
            {
                return;
            }

            Held = candidateItem;
            Held.PickUp(holdPoint);
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