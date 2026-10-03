using UnityEngine;


public interface IItemReceiver
{
    bool TryUseItem(MainItem item, PlayerInteractor user);
}


[RequireComponent(typeof(PlayerController))]
public class PlayerInteractor : MonoBehaviour
{
    public KeyCode interactKey = KeyCode.E;
    public float interactRadius = 1.5f;
    public LayerMask interactMask;        
    public Transform holdPoint;              
    public float dropDistance = 0.9f;
    public float dropHeight = 0.5f;

    public MainItem Held { get; private set; }

    PlayerController player;
    readonly Collider[] hits = new Collider[16];

    void Awake() => player = GetComponent<PlayerController>();

    void Update()
    {
        if (!Input.GetKeyDown(interactKey)) return;

        if (Held == null) TryPickUp();
        else UseOrDrop();
    }

    void TryPickUp()
    {
        MainItem best = null;
        float bestDist = float.MaxValue;

        int count = Physics.OverlapSphereNonAlloc(transform.position, interactRadius, hits, interactMask,
                                                  QueryTriggerInteraction.Collide);
        for (int i = 0; i < count; i++)
        {
            var item = hits[i].GetComponentInParent<MainItem>();
            if (item == null || item.IsHeld) continue;

            float d = (item.transform.position - transform.position).sqrMagnitude;
            if (d < bestDist) { bestDist = d; best = item; }
        }

        if (best != null)
        {
            Held = best;
            Held.PickUp(holdPoint);
        }
    }

    void UseOrDrop()
    {
        
        int count = Physics.OverlapSphereNonAlloc(transform.position, interactRadius, hits, interactMask,
                                                  QueryTriggerInteraction.Collide);
        for (int i = 0; i < count; i++)
        {
            var receiver = hits[i].GetComponentInParent<IItemReceiver>();
            if (receiver != null && receiver.TryUseItem(Held, this))
            {
                Held.Consume();
                Held = null;
                return;
            }
        }
        
        Vector3 dropPos = transform.position + player.Facing * dropDistance + Vector3.up * dropHeight;
        Held.Drop(dropPos);
        Held = null;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}
