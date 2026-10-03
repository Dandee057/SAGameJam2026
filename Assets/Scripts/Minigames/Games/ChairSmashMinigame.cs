using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Smash a chair with a bat. Weak spots appear on the chair one at a time - click each one to whack it.
///
/// The bat follows the mouse. On click:
///   - the hand sprite swaps from "raised" to "whack" (then back),
///   - a swing trail flashes and fades,
///   - on a HIT: an impact star pops at the click point, splinters fly off, the chair shakes
///     and the game freezes for a split second (hit-stop) so it feels crunchy.
/// Hit enough spots and the chair breaks with a big burst of splinters.
///
/// SETUP: drag the sprites into the "Sprites" section. The trail, impact and splinter images are created
/// automatically. The chair / weak spots / hand objects are the same as before.
/// </summary>
public class ChairSmashMinigame : Minigame
{
    [Header("Chair")] public RectTransform chair;
    public Image chairImage;
    public Sprite brokenChairSprite; // optional: shown when it breaks
    [Tooltip("Size of the broken chair compared to the normal one. Lower = smaller.")]
    [Range(0.1f, 2f)] public float brokenChairSize = 1f;
    [Tooltip("Nudge the broken chair up/down/left/right (pixels) if it doesn't line up.")]
    public Vector2 brokenChairOffset = Vector2.zero;
    public float shakeAmount = 14f;
    public float shakeDuration = 0.15f;

    [Header("Weak spots")] public RectTransform[] spots; // each should have an Image
    public int spotsToHit = 3; // clamped to the number of spots
    public Sprite crackSprite; // optional: a hit spot turns into this

    [Header("Bat hand")]
    [Tooltip("The Image that shows the hands + bat. Its sprite is swapped between the two poses below.")]
    public RectTransform hand;
    [Tooltip("Offset from the cursor. With the default hand pivot the bat head lands right on the cursor, so 0,0 is usually right.")]
    public Vector2 gripOffset = Vector2.zero;
    [Tooltip("Show the normal mouse cursor (recommended: it's your aim point).")]
    public bool hideSystemCursor = false;

    [Header("Sprites (drag your PNGs in here)")]
    [Tooltip("Bat raised, ready to swing (Bashing_3).")]
    public Sprite raisedSprite;
    [Tooltip("Bat mid-whack (Bashing_1).")]
    public Sprite whackSprite;
    [Tooltip("Swing trail, drawn on the same canvas as the whack frame (Bashing_6).")]
    public Sprite trailSprite;
    [Tooltip("Impact stars - one is picked at random per hit (Bashing_5_6, _7, _8).")]
    public Sprite[] impactSprites;
    [Tooltip("Wood splinters that fly off (Bashing_5_0 to _5).")]
    public Sprite[] splinterSprites;

    [Header("Hand layout")]
    [Tooltip("Size of the hand image. Keep the 2:1 shape of the artwork.")]
    public Vector2 handSize = new Vector2(1100f, 556f);
    [Tooltip("Point on the hand artwork that sits on the cursor (0-1). Default = the bat head in the whack frame.")]
    public Vector2 handPivot = new Vector2(0.31f, 0.6f);

    [Header("Whack feel")]
    [Tooltip("How long the whack pose is held before going back to raised.")]
    public float whackHoldTime = 0.14f;
    [Tooltip("Minimum time between swings.")]
    public float swingCooldown = 0.18f;
    [Tooltip("Extra rotation punch on each whack (degrees).")]
    public float whackKick = 8f;
    [Tooltip("Freeze the game this long on a hit for extra crunch. 0 = off.")]
    public float hitStop = 0.05f;

    [Header("Trail")]
    public float trailFadeTime = 0.18f;

    [Header("Impact star")]
    [Range(0.1f, 3f)] public float impactSize = 0.6f;
    public float impactTime = 0.16f;
    [Tooltip("Also show a smaller star when you miss.")]
    public bool impactOnMiss = true;

    [Header("Splinters")]
    [Range(0.1f, 3f)] public float splinterSize = 0.5f;
    public int splintersPerHit = 4;
    public int splintersOnBreak = 14;
    public float splinterSpeed = 900f;
    public float splinterGravity = 2600f;
    public float splinterSpin = 720f;
    public float splinterLife = 0.7f;

    [Header("Timing")] public float breakDelay = 0.35f; // pause after the last hit, before the chair breaks
    public float finishDelay = 0.9f; // pause after the break, before the minigame closes

    private enum State { Playing, Breaking, Finishing }

    private class Splinter
    {
        public RectTransform rt;
        public Image img;
        public Vector2 vel;
        public float spin, life, maxLife;
    }

    private State state = State.Playing;
    private readonly List<int> order = new();
    private readonly List<Splinter> splinters = new();
    private int step;
    private Camera uiCam;
    private RectTransform handParent, fxLayer;
    private Image handImage, trail, impact;
    private Vector3 chairBasePos;

    private float shakeTimer, timer, whackTimer, cooldown, trailTimer, impactTimer, impactScale, kick;
    private float hitStopTimer;

    // ---------------------------------------------------------------- setup

    protected override void OnBegin()
    {
        if (chair == null || hand == null || spots == null || spots.Length == 0)
        {
            Debug.LogError("ChairSmashMinigame: 'chair', 'hand' and at least one entry in 'spots' must be assigned.", this);
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
        BuildVisuals();

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
        for (int i = 0; i < count; i++) order.Add(all[i]);
        spots[order[0]].gameObject.SetActive(true);

        if (hideSystemCursor) Cursor.visible = false;
    }

    private void BuildVisuals()
    {
        handImage = hand.GetComponent<Image>();
        if (handImage != null)
        {
            if (raisedSprite) handImage.sprite = raisedSprite;
            handImage.preserveAspect = true;
            handImage.raycastTarget = false;
        }
        hand.pivot = handPivot;
        hand.sizeDelta = handSize;
        hand.localRotation = Quaternion.identity;

        // trail: same canvas/size as the hand art, so it lines up with the whack frame
        if (trailSprite)
        {
            trail = MakeImage("Trail", hand, trailSprite);
            RectTransform t = trail.rectTransform;
            t.anchorMin = Vector2.zero; t.anchorMax = Vector2.one;
            t.offsetMin = t.offsetMax = Vector2.zero;
            trail.preserveAspect = true;
            trail.transform.SetAsFirstSibling();   // behind the hand art
            SetAlpha(trail, 0f);
        }

        // effects layer on top of the chair, below the hand
        fxLayer = new GameObject("FX", typeof(RectTransform)).GetComponent<RectTransform>();
        fxLayer.SetParent(handParent, false);
        fxLayer.anchorMin = Vector2.zero; fxLayer.anchorMax = Vector2.one;
        fxLayer.offsetMin = fxLayer.offsetMax = Vector2.zero;
        fxLayer.SetSiblingIndex(hand.GetSiblingIndex());   // just before the hand

        if (impactSprites != null && impactSprites.Length > 0)
        {
            impact = MakeImage("Impact", fxLayer, impactSprites[0]);
            impact.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (hideSystemCursor) Cursor.visible = true;
        Time.timeScale = 1f;   // never leave the game frozen by hit-stop
    }

    // ---------------------------------------------------------------- loop

    private void Update()
    {
        if (hand == null) return;

        // hit-stop: freeze briefly (uses real time so it always ends)
        if (hitStopTimer > 0f)
        {
            hitStopTimer -= Time.unscaledDeltaTime;
            if (hitStopTimer <= 0f) Time.timeScale = 1f;
        }

        float dt = Time.deltaTime;
        Vector2 mouse = Input.mousePosition;
        FollowMouse(mouse);

        cooldown -= dt;
        UpdateWhack(dt);
        UpdateTrail(dt);
        UpdateImpact(dt);
        UpdateSplinters(dt);
        UpdateShake(dt);

        if (state == State.Playing)
        {
            if (cooldown <= 0f && Input.GetMouseButtonDown(0)) Whack(mouse);
            return;
        }

        timer -= dt;
        if (timer > 0f) return;

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

    private void FollowMouse(Vector2 mouse)
    {
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(handParent, mouse, uiCam, out Vector2 lp))
            hand.localPosition = new Vector3(lp.x + gripOffset.x, lp.y + gripOffset.y, 0f);
    }

    // ---------------------------------------------------------------- whack

    private void Whack(Vector2 mouse)
    {
        cooldown = swingCooldown;
        whackTimer = whackHoldTime;
        kick = whackKick;
        if (handImage && whackSprite) handImage.sprite = whackSprite;

        if (trail) { trailTimer = trailFadeTime; SetAlpha(trail, 1f); }

        // was it on the current weak spot?
        RectTransform spot = spots[order[step]];
        bool hit = RectTransformUtility.RectangleContainsScreenPoint(spot, mouse, uiCam);

        if (!hit)
        {
            shakeTimer = shakeDuration * 0.4f;   // thud, no progress
            if (impactOnMiss) ShowImpact(mouse, 0.5f);
            return;
        }

        shakeTimer = shakeDuration;
        ShowImpact(mouse, 1f);
        SpawnSplinters(mouse, splintersPerHit, 1f);
        if (hitStop > 0f) { Time.timeScale = 0.02f; hitStopTimer = hitStop; }

        Image img = spot.GetComponent<Image>();
        if (img != null && crackSprite != null) img.sprite = crackSprite;

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

    private void UpdateWhack(float dt)
    {
        if (whackTimer > 0f)
        {
            whackTimer -= dt;
            if (whackTimer <= 0f && handImage && raisedSprite) handImage.sprite = raisedSprite;
        }
        kick = Mathf.MoveTowards(kick, 0f, whackKick * dt / Mathf.Max(0.01f, whackHoldTime));
        hand.localRotation = Quaternion.Euler(0f, 0f, kick);
    }

    private void UpdateTrail(float dt)
    {
        if (trail == null || trailTimer <= 0f) return;
        trailTimer -= dt;
        SetAlpha(trail, Mathf.Clamp01(trailTimer / Mathf.Max(0.01f, trailFadeTime)));
    }

    // ---------------------------------------------------------------- impact star

    private void ShowImpact(Vector2 screenPos, float sizeMul)
    {
        if (impact == null || impactSprites == null || impactSprites.Length == 0) return;
        impact.sprite = impactSprites[Random.Range(0, impactSprites.Length)];
        impact.rectTransform.sizeDelta = impact.sprite.rect.size;
        impact.rectTransform.position = ScreenToWorld(screenPos);
        impact.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
        impact.gameObject.SetActive(true);
        impactScale = impactSize * sizeMul;
        impactTimer = impactTime;
        SetAlpha(impact, 1f);
    }

    private void UpdateImpact(float dt)
    {
        if (impact == null || impactTimer <= 0f) return;
        impactTimer -= dt;
        float k = 1f - Mathf.Clamp01(impactTimer / Mathf.Max(0.01f, impactTime));   // 0 -> 1
        // pop big fast, then shrink + fade
        float s = k < 0.3f ? Mathf.Lerp(0.4f, 1.15f, k / 0.3f) : Mathf.Lerp(1.15f, 0.8f, (k - 0.3f) / 0.7f);
        impact.rectTransform.localScale = Vector3.one * (impactScale * s);
        SetAlpha(impact, k < 0.5f ? 1f : Mathf.Lerp(1f, 0f, (k - 0.5f) / 0.5f));
        if (impactTimer <= 0f) impact.gameObject.SetActive(false);
    }

    // ---------------------------------------------------------------- splinters

    private void SpawnSplinters(Vector2 screenPos, int count, float speedMul)
    {
        if (splinterSprites == null || splinterSprites.Length == 0 || fxLayer == null) return;
        Vector3 world = ScreenToWorld(screenPos);

        for (int i = 0; i < count; i++)
        {
            Sprite sp = splinterSprites[Random.Range(0, splinterSprites.Length)];
            Image img = MakeImage("Splinter", fxLayer, sp);
            img.rectTransform.sizeDelta = sp.rect.size * splinterSize;
            img.rectTransform.position = world;
            img.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

            // mostly upwards and outwards, like they burst off the wood
            float ang = Random.Range(20f, 160f) * Mathf.Deg2Rad;
            float spd = splinterSpeed * speedMul * Random.Range(0.6f, 1.2f);
            splinters.Add(new Splinter
            {
                rt = img.rectTransform,
                img = img,
                vel = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * spd,
                spin = Random.Range(-splinterSpin, splinterSpin),
                life = splinterLife * Random.Range(0.8f, 1.2f),
            });
            splinters[splinters.Count - 1].maxLife = splinters[splinters.Count - 1].life;
        }
    }

    private void UpdateSplinters(float dt)
    {
        for (int i = splinters.Count - 1; i >= 0; i--)
        {
            Splinter s = splinters[i];
            s.life -= dt;
            if (s.life <= 0f || s.rt == null)
            {
                if (s.rt) Destroy(s.rt.gameObject);
                splinters.RemoveAt(i);
                continue;
            }
            s.vel.y -= splinterGravity * dt;
            s.rt.anchoredPosition += s.vel * dt;
            s.rt.localRotation = Quaternion.Euler(0f, 0f, s.rt.localEulerAngles.z + s.spin * dt);
            float fade = Mathf.Clamp01(s.life / (s.maxLife * 0.35f));   // fade out over the last third
            SetAlpha(s.img, fade);
        }
    }

    // ---------------------------------------------------------------- break + shake

    private void BreakChair()
    {
        if (brokenChairSprite != null)
        {
            chairImage.sprite = brokenChairSprite;
            chairImage.preserveAspect = true;   // don't stretch the broken art into the old chair's box
            chair.localScale = Vector3.one * brokenChairSize;
            chairBasePos += new Vector3(brokenChairOffset.x, brokenChairOffset.y, 0f);
            foreach (RectTransform s in spots) s.gameObject.SetActive(false);
        }

        // big burst from the middle of the chair
        Vector2 centre = RectTransformUtility.WorldToScreenPoint(uiCam, chair.position);
        SpawnSplinters(centre, splintersOnBreak, 1.4f);
        ShowImpact(centre, 1.6f);
        shakeTimer = shakeDuration * 2f;
    }

    private void UpdateShake(float dt)
    {
        if (shakeTimer > 0f)
        {
            shakeTimer -= dt;
            float strength = Mathf.Clamp01(shakeTimer / shakeDuration);
            Vector2 offset = Random.insideUnitCircle * shakeAmount * strength;
            chair.localPosition = chairBasePos + new Vector3(offset.x, offset.y, 0f);
        }
        else
        {
            chair.localPosition = chairBasePos;
        }
    }

    // ---------------------------------------------------------------- helpers

    private Vector3 ScreenToWorld(Vector2 screen)
    {
        RectTransformUtility.ScreenPointToWorldPointInRectangle(handParent, screen, uiCam, out Vector3 w);
        return w;
    }

    private static Image MakeImage(string name, Transform parent, Sprite sprite)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = false;
        img.preserveAspect = true;
        return img;
    }

    private static void SetAlpha(Image img, float a)
    {
        if (img == null) return;
        Color c = img.color; c.a = a; img.color = c;
    }
}
