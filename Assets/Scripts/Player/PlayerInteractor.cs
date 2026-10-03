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
        if (Busy || player.IsStunned)
        {
            if (thoughtBubble)
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

        if (!Held)
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, itemRadius, hits, interactMask,
                QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                MainItem item = hits[i].GetComponentInParent<MainItem>();
                if (!item || item.IsHeld)
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


        if (candidateItem && candidateZone)
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
        if (!thoughtBubble)
        {
            return;
        }

        if (candidateZone)
        {
            thoughtBubble.Show(candidateZone.thoughtSprite, candidateZone.thoughtPrefab);
        }
        else if (candidateItem)
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
        if (candidateZone)
        {
            MainItem item = Held;
            Busy = true;
            candidateZone.Interact(item, this, consumed =>
            {
                Busy = false;
                if (consumed && item)
                {
                    item.Consume();
                    Held = null;
                }
            });
            return;
        }

        if (!Held)
        {
            if (!candidateItem)
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