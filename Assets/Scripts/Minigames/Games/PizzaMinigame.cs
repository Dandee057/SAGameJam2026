using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// Drag pineapple / anchovy pieces onto the pizza. When you're done, the result depends on what's on it:
///   enough pineapple only -> pineappleVignette (good)
///   enough anchovy only   -> anchovyVignette   (bad)
///   enough of both        -> bothVignette      (super bad)
/// "Enough" = at least 'piecesNeeded' pieces of that topping. The Done button stays greyed out until at
/// least one topping counts (so Done always means something). Pieces can be dragged back off the pizza.
///
/// UI prefab layout (root = full-screen panel with this script):
///   Pizza        (Image)                                 -> 'pizzaArea'
///   PineapplePile (empty RectTransform; each CHILD is one piece Image)  -> 'pineapplePile'
///   AnchovyPile   (same)                                 -> 'anchovyPile'
///   DoneButton   (Button)                                -> 'doneButton'
///   (optional) ready markers, e.g. a tick shown next to a pile once it counts.
/// Don't put a Layout Group on the piles - the pieces are placed by hand and snap back to where they started.
public class PizzaMinigame : Minigame
{
    [Header("Pizza")] public RectTransform pizzaArea;
    public bool circularPizza = true;

    [Header("Piles (each child = one draggable piece)")]
    public RectTransform pineapplePile;

    public RectTransform anchovyPile;

    [Header("Rules")]
    [Tooltip("Pieces of a topping needed for it to count. Set equal to the pile size to require ALL of them.")]
    public int piecesNeeded = 3;

    [Header("Outcomes")] public Vignette pineappleVignette;
    public Vignette anchovyVignette;
    public Vignette bothVignette;

    [Header("UI")] public Button doneButton;
    public GameObject pineappleReadyMarker;
    public GameObject anchovyReadyMarker;

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

        int need = Mathf.Max(1, piecesNeeded);
        bool hasPine = pine >= need;
        bool hasAnch = anch >= need;

        if (hasPine && hasAnch)
        {
            currentResult = bothVignette;
        }
        else if (hasPine)
        {
            currentResult = pineappleVignette;
        }
        else if (hasAnch)
        {
            currentResult = anchovyVignette;
        }
        else
        {
            currentResult = null;
        }

        if (pineappleReadyMarker != null)
        {
            pineappleReadyMarker.SetActive(hasPine);
        }

        if (anchovyReadyMarker != null)
        {
            anchovyReadyMarker.SetActive(hasAnch);
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