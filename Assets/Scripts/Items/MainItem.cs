using UnityEngine;


[RequireComponent(typeof(Rigidbody))]
public class MainItem : MonoBehaviour
{
    public string itemId = "item";
    public Sprite thoughtSprite;
    public GameObject thoughtPrefab;

    public bool IsHeld { get; private set; }

    private Rigidbody rb;
    private Collider[] colliders;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        colliders = GetComponentsInChildren<Collider>();
    }

    public void PickUp(Transform holdPoint)
    {
        IsHeld = true;
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