using UnityEngine;

public class AlwaysOnTopUI : MonoBehaviour
{
    [Tooltip("Higher = drawn on top. Minigame canvases default to 0, so anything above that works.")]
    public int sortingOrder = 500;

    private void Awake()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
        }

        canvas.overrideSorting = true;
        canvas.sortingOrder = sortingOrder;
    }
}