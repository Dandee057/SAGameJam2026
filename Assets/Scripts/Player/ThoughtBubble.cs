using System.Collections.Generic;
using UnityEngine;


public class ThoughtBubble : MonoBehaviour
{
    public Transform visual;
    public SpriteRenderer bubbleRenderer;
    public Transform contentSlot;
    public SpriteRenderer iconRenderer;

    [Header("Look")] public float iconMaxSize = 0.6f;
    public int sortingOrder = 100;

    [Header("Animation")] public float stiffness = 400f;
    public float damping = 14f;
    public float bobAmount = 0.05f;
    public float bobSpeed = 3f;

    private object current;
    private GameObject activeInstance;
    private readonly Dictionary<GameObject, GameObject> instances = new();

    private float target, scale, scaleVel, bobTime;

    private void Awake()
    {
        bubbleRenderer.sortingOrder = sortingOrder;
        iconRenderer.sortingOrder = sortingOrder + 1;
        iconRenderer.gameObject.SetActive(false);
        Apply(0f);
    }


    public void Show(Sprite sprite, GameObject prefab = null)
    {
        object key = prefab != null ? (object)prefab : sprite;
        if (key == null)
        {
            Hide();
            return;
        }

        if (!ReferenceEquals(key, current))
        {
            SetContent(sprite, prefab);
            current = key;
            scale = Mathf.Min(scale, 0.4f);
        }

        target = 1f;
    }

    public void Hide()
    {
        target = 0f;
    }

    private void SetContent(Sprite sprite, GameObject prefab)
    {
        if (activeInstance)
        {
            activeInstance.SetActive(false);
            activeInstance = null;
        }

        iconRenderer.gameObject.SetActive(false);

        if (prefab)
        {
            if (!instances.TryGetValue(prefab, out GameObject inst))
            {
                inst = Instantiate(prefab, contentSlot);
                inst.transform.localPosition = Vector3.zero;
                inst.transform.localRotation = Quaternion.identity;
                foreach (Renderer r in inst.GetComponentsInChildren<Renderer>())
                {
                    r.sortingOrder = sortingOrder + 1;
                }

                instances[prefab] = inst;
            }

            inst.SetActive(true);
            activeInstance = inst;
        }
        else
        {
            iconRenderer.sprite = sprite;
            float size = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
            iconRenderer.transform.localScale = Vector3.one * (iconMaxSize / Mathf.Max(size, 0.0001f));
            iconRenderer.gameObject.SetActive(true);
        }
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        scaleVel += (target - scale) * stiffness * dt;
        scaleVel *= Mathf.Exp(-damping * dt);
        scale += scaleVel * dt;

        bobTime += dt;
        Apply(Mathf.Max(scale, 0f));
    }

    private void Apply(float s)
    {
        bool visible = s > 0.01f || target > 0f;
        if (visual.gameObject.activeSelf != visible)
        {
            visual.gameObject.SetActive(visible);
        }

        if (!visible)
        {
            return;
        }

        visual.localScale = Vector3.one * s;
        visual.localPosition = new Vector3(0f, Mathf.Sin(bobTime * bobSpeed) * bobAmount, 0f);
    }
}