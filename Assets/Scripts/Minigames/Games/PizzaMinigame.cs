using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PizzaMinigame : Minigame
{
    [Header("Pizza")] public RectTransform pizzaArea;
    public bool circularPizza = true;

    [Header("Piles (each child = one draggable piece)")]
    public RectTransform pineapplePile;

    public RectTransform anchovyPile;

    [Header("Rules")] [Tooltip("Total pieces (any mix) that must be on the pizza before Done unlocks.")]
    public int minPiecesOnPizza = 5;

    [Header("Outcomes")] public Vignette pineappleVignette;
    public Vignette anchovyVignette;
    public Vignette bothVignette;

    [Header("UI")] public Button doneButton;

    private class Piece
    {
        public RectTransform rt;
        public bool pineapple;
        public bool placed;
        public Transform homeParent;
        public Vector2 homePos;
        public int homeSibling;
    }

    private readonly List<Piece> pieces = new();
    private Piece dragging;
    private Vector3 grabOffset;
    private Camera uiCam;
    private Vignette currentResult;

    protected override void OnBegin()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvas = canvas.rootCanvas;
            uiCam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        }

        AddPieces(pineapplePile, true);
        AddPieces(anchovyPile, false);

        if (doneButton != null)
        {
            doneButton.onClick.AddListener(OnDone);
        }

        Refresh();
    }

    private void AddPieces(RectTransform pile, bool pineapple)
    {
        if (pile == null)
        {
            return;
        }

        foreach (RectTransform child in pile)
        {
            pieces.Add(new Piece
            {
                rt = child,
                pineapple = pineapple,
                homeParent = pile,
                homePos = child.anchoredPosition,
                homeSibling = child.GetSiblingIndex()
            });
        }
    }

    private void Update()
    {
        Vector2 mouse = Input.mousePosition;

        if (Input.GetMouseButtonDown(0))
        {
            TryGrab(mouse);
        }

        if (dragging != null)
        {
            MoveDragged(mouse);
            if (Input.GetMouseButtonUp(0))
            {
                Drop();
            }
        }
    }

    // ---------- dragging ----------

    private void TryGrab(Vector2 mouse)
    {
        Piece best = null;
        foreach (Piece p in pieces)
        {
            if (!RectTransformUtility.RectangleContainsScreenPoint(p.rt, mouse, uiCam))
            {
                continue;
            }

            if (best == null || p.rt.GetSiblingIndex() > best.rt.GetSiblingIndex())
            {
                best = p;
            }
        }

        if (best == null)
        {
            return;
        }

        dragging = best;
        dragging.placed = false; // lifting it off the pizza un-places it
        dragging.rt.SetParent(transform, true); // top layer of the minigame so it draws over everything
        dragging.rt.SetAsLastSibling();

        RectTransformUtility.ScreenPointToWorldPointInRectangle(dragging.rt, mouse, uiCam, out Vector3 wp);
        grabOffset = dragging.rt.position - wp;
        Refresh();
    }

    private void MoveDragged(Vector2 mouse)
    {
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(dragging.rt, mouse, uiCam, out Vector3 wp))
        {
            dragging.rt.position = wp + grabOffset;
        }
    }

    private void Drop()
    {
        Vector3 local = pizzaArea.InverseTransformPoint(dragging.rt.position);

        if (OnPizza(local))
        {
            dragging.rt.SetParent(pizzaArea, true); // stays where you dropped it
            dragging.rt.SetAsLastSibling();
            dragging.placed = true;
        }
        else
        {
            dragging.rt.SetParent(dragging.homeParent, false);
            dragging.rt.anchoredPosition = dragging.homePos;
            dragging.rt.SetSiblingIndex(dragging.homeSibling);
            dragging.placed = false;
        }

        dragging = null;
        Refresh();
    }

    private bool OnPizza(Vector3 local)
    {
        Rect r = pizzaArea.rect;
        if (circularPizza)
        {
            return ((Vector2)local - r.center).magnitude <= Mathf.Min(r.width, r.height) * 0.5f;
        }

        return r.Contains(local);
    }

    // ---------- result ----------

    private void Refresh()
    {
        int pine = 0, anch = 0;
        foreach (Piece p in pieces)
        {
            if (!p.placed)
            {
                continue;
            }

            if (p.pineapple)
            {
                pine++;
            }
            else
            {
                anch++;
            }
        }

        bool enoughOnPizza = pine + anch >= Mathf.Max(1, minPiecesOnPizza);

        if (!enoughOnPizza)
        {
            currentResult = null;
        }
        else if (pine > 0 && anch > 0)
        {
            currentResult = bothVignette;
        }
        else if (pine > 0)
        {
            currentResult = pineappleVignette;
        }
        else
        {
            currentResult = anchovyVignette;
        }

        if (doneButton != null)
        {
            doneButton.interactable = currentResult != null;
        }
    }

    private void OnDone()
    {
        if (currentResult == null || dragging != null)
        {
            return;
        }

        Finish(true, currentResult);
    }
}