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

    [Header("Debug")] [SerializeField] [Tooltip("Read-only: what the interactor is thinking right now.")]
    private string debugState;

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

        if (thoughtBubble == null)
        {
            Debug.LogWarning(
                "PlayerInteractor: no ThoughtBubble found on the player or its children - no thoughts will show.",
                this);
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
        if (Busy && !MinigameManager.IsRunning)
        {
            Debug.LogWarning("PlayerInteractor: Busy was stuck with no minigame running - resetting.", this);
            Busy = false;
        }

        if (Busy || player.IsStunned)
        {
            debugState = Busy ? "BUSY (minigame open)" : "STUNNED";
            if (thoughtBubble != null)
            {
                thoughtBubble.Hide();
            }

            return;
        }

        Scan();
        UpdateBubble();

        debugState =
            $"held={(Held != null ? Held.name : "-")}  item={(candidateItem != null ? candidateItem.name : "-")}  zone={(candidateZone != null ? candidateZone.name : "-")}";

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

        if (candidateItem != null && candidateZone != null)
        {
            if (!candidateZone.toggleable && bestZone <= bestItem)
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
            thoughtBubble.Show(candidateZone.CurrentThoughtSprite);
        }
        else if (candidateItem != null)
        {
            thoughtBubble.Show(candidateItem.thoughtSprite);
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