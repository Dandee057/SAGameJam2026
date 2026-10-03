using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// Smash a chair with a bat. Weak spots appear on the chair one at a time - click each one to smack it.
/// A hand holding a bat follows the mouse and swings on every click (hit or miss). Hit the required
/// number of spots and the chair breaks.
///
/// UI prefab layout (root = full-screen panel with this script):
///   Chair        (Image: the chair art)                       -> 'chair' + 'chairImage'
///     Spot1..N   (child Images of the chair: target marker art, sized as the click area) -> 'spots'
///   Hand         (Image: hand + bat PNG, PIVOT at the grip, drawn last so it's on top)   -> 'hand'
///
/// The vignette comes from the zone itself (one zone per chair), so this prefab can be reused for both chairs.
public class ChairSmashMinigame : Minigame
{
    [Header("Chair")] public RectTransform chair;
    public Image chairImage;
    public Sprite brokenChairSprite; // optional: shown when it breaks
    public float shakeAmount = 14f;
    public float shakeDuration = 0.15f;

    [Header("Weak spots")] public RectTransform[] spots; // each should have an Image
    public int spotsToHit = 3; // clamped to the number of spots
    public Sprite crackSprite; // optional: a hit spot turns into this

    [Header("Bat hand")] public RectTransform hand;

    [Tooltip("Offset from the cursor to the hand's pivot (the grip). Tune so the bat's head ends up near the cursor.")]
    public Vector2 gripOffset = new(120f, -120f);

    [Tooltip("Mirrors the swing direction. Tick/untick if the bat swings the wrong way for your art.")]
    public bool flipSwing = true;

    public float restAngle = 20f; // wind-up pose (bat raised)
    public float swingAngle = -50f; // pose at the moment of impact (bat down)
    public float swingDuration = 0.3f;
    [Range(0.1f, 0.9f)] public float impactAt = 0.35f; // fraction of the swing where the bat lands
    public bool hideSystemCursor = true;

    [Header("Timing")] public float breakDelay = 0.35f; // pause after the last hit, before the chair breaks
    public float finishDelay = 0.9f; // pause after the break, before the minigame closes

    private float Dir => flipSwing ? -1f : 1f;

    private enum State
    {
        Playing,
        Breaking,
        Finishing
    }

    private State state = State.Playing;
    private readonly List<int> order = new();
    private int step;
    private Camera uiCam;
    private RectTransform handParent;
    private Vector3 chairBasePos;

    private bool swinging, impactDone;
    private float swingT;
    private Vector2 clickPos;
    private float shakeTimer, timer;

    protected override void OnBegin()
    {
        if (chair == null || hand == null || spots == null || spots.Length == 0)
        {
            Debug.LogError("ChairSmashMinigame: 'chair', 'hand' and at least one entry in 'spots' must be assigned.",
                this);
            Finish(false);
            return;
        }

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvas = canvas.rootCanvas;
            uiCam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        }

        handParent = (RectTransform)hand.parent;
        chairBasePos = chair.localPosition;
        hand.localRotation = Quaternion.Euler(0f, 0f, restAngle * Dir);

        // pick a random subset of spots, in random order
        List<int> all = new();
        for (int i = 0; i < spots.Length; i++)
        {
            all.Add(i);
            spots[i].gameObject.SetActive(false);
        }

        for (int i = all.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (all[i], all[j]) = (all[j], all[i]);
        }

        int count = Mathf.Clamp(spotsToHit, 1, spots.Length);
        for (int i = 0; i < count; i++)
        {
            order.Add(all[i]);
        }

        spots[order[0]].gameObject.SetActive(true);

        if (hideSystemCursor)
        {
            Cursor.visible = false;
        }
    }

    private void OnDestroy()
    {
        if (hideSystemCursor)
        {
            Cursor.visible = true;
        }
    }

    private void Update()
    {
        if (hand == null)
        {
            return;
        }

        Vector2 mouse = Input.mousePosition;
        FollowMouse(mouse);
        UpdateSwing();
        UpdateShake();

        if (state == State.Playing)
        {
            if (!swinging && Input.GetMouseButtonDown(0))
            {
                StartSwing(mouse);
            }
        }
        else
        {
            timer -= Time.deltaTime;
            if (timer > 0f)
            {
                return;
            }

            if (state == State.Breaking)
            {
                BreakChair();
                state = State.Finishing;
                timer = finishDelay;
            }
            else
            {
                Finish(true);
            }
        }
    }

    // ---------- hand ----------

    private void FollowMouse(Vector2 mouse)
    {
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(handParent, mouse, uiCam, out Vector2 lp))
        {
            hand.localPosition = new Vector3(lp.x + gripOffset.x, lp.y + gripOffset.y, 0f);
        }
    }

    private void StartSwing(Vector2 mouse)
    {
        swinging = true;
        impactDone = false;
        swingT = 0f;
        clickPos = mouse;
    }

    private void UpdateSwing()
    {
        float angle = restAngle * Dir;

        if (swinging)
        {
            swingT += Time.deltaTime / Mathf.Max(0.01f, swingDuration);

            if (!impactDone && swingT >= impactAt)
            {
                impactDone = true;
                ResolveHit();
            }

            // fast wind-in to the impact, slower recovery
            float k = swingT < impactAt
                ? swingT / impactAt
                : 1f - (swingT - impactAt) / (1f - impactAt);
            angle = Mathf.Lerp(restAngle, swingAngle, Mathf.Clamp01(k)) * Dir;

            if (swingT >= 1f)
            {
                swinging = false;
            }
        }

        hand.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    // ---------- hitting ----------

    private void ResolveHit()
    {
        if (state != State.Playing)
        {
            return;
        }

        RectTransform spot = spots[order[step]];
        bool hit = RectTransformUtility.RectangleContainsScreenPoint(spot, clickPos, uiCam);

        if (!hit)
        {
            shakeTimer = shakeDuration * 0.4f; // thud, but no progress
            return;
        }

        shakeTimer = shakeDuration;

        Image img = spot.GetComponent<Image>();
        if (img != null && crackSprite != null)
        {
            img.sprite = crackSprite;
        }

        step++;
        if (step >= order.Count)
        {
            state = State.Breaking;
            timer = breakDelay;
        }
        else
        {
            spots[order[step]].gameObject.SetActive(true);
        }
    }

    private void BreakChair()
    {
        if (brokenChairSprite != null)
        {
            chairImage.sprite = brokenChairSprite;
            foreach (RectTransform s in spots)
            {
                s.gameObject.SetActive(false);
            }
        }

        shakeTimer = shakeDuration * 2f;
    }

    private void UpdateShake()
    {
        if (shakeTimer > 0f)
        {
            shakeTimer -= Time.deltaTime;
            float strength = Mathf.Clamp01(shakeTimer / shakeDuration);
            Vector2 offset = Random.insideUnitCircle * shakeAmount * strength;
            chair.localPosition = chairBasePos + new Vector3(offset.x, offset.y, 0f);
        }
        else
        {
            chair.localPosition = chairBasePos;
        }
    }
}