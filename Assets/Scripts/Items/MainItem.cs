using UnityEngine;


[RequireComponent(typeof(Rigidbody))]
public class MainItem : MonoBehaviour
{
    public string itemId = "item"; // zones check this ("water", "wine", ...)
    public Sprite thoughtSprite; // what shows in the thought bubble when you walk past it
    public GameObject thoughtPrefab; // optional: an (animated) prefab instead of a plain sprite

    [Tooltip(
        "Stays put (doesn't fall) until it's picked up and dropped. Use for items sitting in cupboards or on shelves.")]
    public bool startKinematic;

    public bool HasBeenPickedUp { get; private set; }

    public bool IsHeld { get; private set; }

    private Rigidbody rb;
    private Collider[] colliders;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        colliders = GetComponentsInChildren<Collider>();
        if (startKinematic)
        {
            rb.isKinematic = true;
        }
    }

    public void PickUp(Transform holdPoint)
    {
        IsHeld = true;
        HasBeenPickedUp = true;
        rb.isKinematic = true;
        foreach (Collider c in colliders)
        {
            c.enabled = false;
        }

        transform.SetParent(holdPoint);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    public void Drop(Vector3 worldPosition)
    {
        IsHeld = false;
        transform.SetParent(null);
        transform.position = worldPosition;
        foreach (Collider c in colliders)
        {
            c.enabled = true;
        }

        rb.isKinematic = false;
    }

    public void Consume()
    {
        Destroy(gameObject);
    }
}