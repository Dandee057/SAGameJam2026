using UnityEngine;
using UnityEngine.UI;
public class PourMinigame : Minigame
{
    [Header("UI")]
    [Tooltip("The rotating pivot. Its own Image is hidden; the jug sprites are drawn inside it. Put its pivot where the jug should hinge.")]
    public RectTransform jug;
    [Tooltip("The falling water stream image.")]
    public Image waterStream;
    public GameObject hint;

    [Header("Sprites (drag your PNGs in here)")]
    public Sprite uprightSprite;
    public Sprite tippedSprite;
    public Sprite streamSprite;
    [Tooltip("Splash droplets, flicked between to look animated (e.g. Splash_A, Splash_B).")]
    public Sprite[] splashFrames;
    [Tooltip("Foam under the splash (e.g. Foam_A, Foam_B).")]
    public Sprite[] foamFrames;
    [Tooltip("Puddle left where the water lands. Grows as you pour and stays after.")]
    public Sprite puddleSprite;

    [Header("Image sizes (drag to rescale - works live in Play mode too)")]
    [Range(0.1f, 3f)] public float uprightJugSize = 1f;
    [Range(0.1f, 3f)] public float tippedJugSize = 1f;
    [Tooltip("Makes the stream thicker/thinner (its length is always automatic).")]
    [Range(0.1f, 3f)] public float streamThickness = 1f;
    [Range(0.1f, 3f)] public float splashSize = 1f;
    [Range(0.1f, 3f)] public float foamSize = 1f;
    [Tooltip("Size of the puddle when the jug is fully poured (it starts small and grows to this).")]
    [Range(0.1f, 5f)] public float puddleSize = 2f;
    [Tooltip("How big the puddle is the moment pouring starts, as a fraction of its final size.")]
    [Range(0f, 1f)] public float puddleStartFraction = 0.25f;
    [Tooltip("Tints the puddle. The grey art takes this colour (white = no change).")]
    public Color puddleColour = new Color(0.45f, 0.75f, 1f, 0.9f);

    [Header("Layout")]
    [Tooltip("How many degrees the tipped artwork is already tilted. Stops the swap from 'jumping'.")]
    public float tippedArtAngle = 35f;
    [Tooltip("Where the spout is on the tipped sprite (0,0 = bottom-left, 1,1 = top-right).")]
    public Vector2 spoutOnTippedSprite = new Vector2(0.01f, 0.74f);
    [Tooltip("On = the Spout point always follows the slider above (tweak it live in Play mode). Off = place the Spout object by hand.")]
    public bool spoutFollowsSlider = true;
    [Tooltip("Height where the water lands, as a fraction of the minigame panel (0 = bottom, 1 = top).")]
    [Range(0f, 1f)] public float splashHeight = 0.15f;
    [Tooltip("Where the thin TOP tip of the stream art is, across its width (0 = left, 1 = right). This point is pinned to the spout.")]
    [Range(0f, 1f)] public float streamTipX = 0.81f;
    [Tooltip("Where the BOTTOM of the stream art is centred, across its width. The splash lands under this point.")]
    [Range(0f, 1f)] public float streamBottomX = 0.54f;
    [Tooltip("Width of the stream. 0 = keep whatever width the WaterStream object has.")]
    public float streamWidth = 100f;
    [Tooltip("Seconds per splash frame. Lower = faster flicker.")]
    public float splashFrameTime = 0.09f;
    [Tooltip("How quickly the stream shoots down when pouring starts (seconds).")]
    public float streamGrowTime = 0.12f;

    [Header("Optional overrides (leave empty = created automatically)")]
    public Image uprightJug;
    public Image tippedJug;
    public RectTransform spout;
    public RectTransform splashPoint;
    public Image splash;
    public Image foam;
    public Image puddle;

    [Header("Sounds")]
    [Tooltip("Sloshing on every key press. One is picked at random (Jug_Slosh_1/2).")]
    public AudioClip[] mashSounds;
    [Tooltip("Looping water stream while it pours (Pour_Loop). Fades in/out automatically.")]
    public AudioClip pourLoop;
    [Tooltip("Splash when the water first hits the floor (Water_Splash).")]
    public AudioClip splashSound;
    [Range(0f, 1f)] public float sfxVolume = 1f;
    [Tooltip("How fast the pour loop fades in/out (volume per second).")]
    public float pourFadeSpeed = 6f;

    [Header("Input")] public KeyCode mashKey = KeyCode.A;

    [Header("Tuning (degrees)")] public float kickPerPress = 12f;
    public float uprightPull = 50f;
    public float drag = 0.8f;
    public float maxAngle = 110f;
    [Tooltip("Tilt past this and the jug starts pouring.")]
    public float pourAngle = 50f;
    [Tooltip("How fast the jug empties while pouring (fraction per second at full tilt).")]
    public float fillRate = 0.35f;
    [Tooltip("Even a slight pour drains at least this fraction of fillRate.")]
    [Range(0f, 1f)] public float minPourStrength = 0.3f;
    public float timeLimit = 0f;
    public float finishDelay = 0.4f;
    public float tiltDirection = 1f;

    private float angle, angularVel, progress, elapsed, finishTimer;
    private float splashTimer, streamGrow;
    private int splashIndex;
    private bool done;
    private bool puddlePlaced;
    private AudioSource pourSource;
    private bool wasPouring;

    protected override void OnBegin()
    {
        BuildVisuals();
        if (pourSource == null) pourSource = Sfx.MakeLoop(gameObject, pourLoop);
        if (hint) hint.SetActive(true);
        Apply(false);
    }

    private void Update()
    {
        if (done)
        {
            UpdateSound(false);
            finishTimer -= Time.deltaTime;
            if (finishTimer <= 0f) Finish(true);
            return;
        }

        float dt = Time.deltaTime;
        elapsed += dt;

        if (Input.GetKeyDown(mashKey))
        {
            angularVel += kickPerPress;
            Sfx.Play(mashSounds, sfxVolume * 0.6f, 0.12f);
            if (hint) hint.SetActive(false);
        }
        
        angularVel -= uprightPull * dt;
        angularVel *= Mathf.Exp(-drag * dt);
        angle += angularVel * dt;

        if (angle <= 0f) { angle = 0f; if (angularVel < 0f) angularVel = 0f; }
        if (angle >= maxAngle) { angle = maxAngle; if (angularVel > 0f) angularVel = 0f; }

        bool pouring = angle >= pourAngle && progress < 1f;
        if (pouring)
        {
            float tilt = Mathf.InverseLerp(pourAngle, maxAngle, angle);
            float strength = Mathf.Lerp(minPourStrength, 1f, tilt);
            progress = Mathf.Clamp01(progress + strength * fillRate * dt);
        }

        Apply(pouring);
        UpdateSound(pouring);

        if (progress >= 1f)
        {
            done = true;
            finishTimer = finishDelay;
            Apply(false);
        }
        else if (timeLimit > 0f && elapsed >= timeLimit)
        {
            Finish(false);
        }
    }

   

    private void UpdateSound(bool pouring)
    {
        if (pouring && !wasPouring) Sfx.Play(splashSound, sfxVolume * 0.8f);
        wasPouring = pouring;

        if (pourSource == null) return;
        if (!pourSource.isPlaying) pourSource.Play();
        float target = pouring ? sfxVolume : 0f;
        pourSource.volume = Mathf.MoveTowards(pourSource.volume, target, pourFadeSpeed * Time.unscaledDeltaTime);
    }
    

    private void Apply(bool pouring)
    {
        ApplySizes();
        bool tipped = angle >= pourAngle;

        if (uprightJug) uprightJug.gameObject.SetActive(!tipped);
        if (tippedJug) tippedJug.gameObject.SetActive(tipped);

        if (jug)
        {
            float visual = tipped ? angle - tippedArtAngle : angle;
            jug.localRotation = Quaternion.Euler(0f, 0f, -visual * tiltDirection);
        }

        streamGrow = pouring
            ? Mathf.MoveTowards(streamGrow, 1f, Time.deltaTime / Mathf.Max(0.01f, streamGrowTime))
            : 0f;

        if (waterStream)
        {
            waterStream.gameObject.SetActive(pouring);
            if (pouring) PlaceStream();
        }

        UpdatePuddle(pouring && streamGrow >= 0.99f);

        bool splashing = pouring && streamGrow >= 0.99f;
        if (splash) splash.gameObject.SetActive(splashing);
        if (foam) foam.gameObject.SetActive(splashing);
        if (!splashing) return;

        Vector3 landing = LandingPoint();
        if (splash) splash.rectTransform.position = landing;
        if (foam) foam.rectTransform.position = landing;

        splashTimer -= Time.deltaTime;
        if (splashTimer <= 0f)
        {
            splashTimer = splashFrameTime;
            splashIndex++;
            if (splash && splashFrames != null && splashFrames.Length > 0)
                splash.sprite = splashFrames[splashIndex % splashFrames.Length];
            if (foam && foamFrames != null && foamFrames.Length > 0)
                foam.sprite = foamFrames[splashIndex % foamFrames.Length];
        }
    }

  
    private void ApplySizes()
    {
        if (uprightJug) uprightJug.rectTransform.localScale = Vector3.one * uprightJugSize;
        if (tippedJug) tippedJug.rectTransform.localScale = Vector3.one * tippedJugSize;
        if (waterStream)
        {
            waterStream.rectTransform.localScale = new Vector3(streamThickness, 1f, 1f);
            waterStream.rectTransform.pivot = new Vector2(streamTipX, 1f);
        }
        if (splash) splash.rectTransform.localScale = Vector3.one * splashSize;
        if (foam) foam.rectTransform.localScale = Vector3.one * foamSize;
        ApplyPuddleScale();

        if (spoutFollowsSlider && spout && tippedJug && spout.parent == tippedJug.transform)
        {
            spout.anchorMin = spout.anchorMax = spoutOnTippedSprite;
            spout.anchoredPosition = Vector2.zero;
        }
    }

  
    private void UpdatePuddle(bool waterLanding)
    {
        if (puddle == null) return;
        if (waterLanding && !puddlePlaced)
        {
            puddle.rectTransform.position = LandingPoint();
            puddlePlaced = true;
        }
        puddle.gameObject.SetActive(puddlePlaced);
        ApplyPuddleScale();
    }

    private void ApplyPuddleScale()
    {
        if (puddle == null) return;
        float grow = Mathf.Lerp(puddleStartFraction, 1f, progress);
        puddle.color = puddleColour;
        puddle.rectTransform.localScale = Vector3.one * (puddleSize * grow);
    }

   
    private void OnValidate() => ApplySizes();

 
    private void PlaceStream()
    {
        RectTransform s = waterStream.rectTransform;
        if (spout) s.position = spout.position;
        s.rotation = Quaternion.identity;

        if (splashPoint)
        {
            float worldDrop = s.position.y - splashPoint.position.y;
            float scaleY = Mathf.Max(0.0001f, s.lossyScale.y);
            float height = Mathf.Max(0f, worldDrop / scaleY) * streamGrow;
            s.sizeDelta = new Vector2(s.sizeDelta.x, height);
        }
    }

    private Vector3 LandingPoint()
    {
        Vector3 top = spout ? spout.position : waterStream.rectTransform.position;
        float y = splashPoint ? splashPoint.position.y : top.y;
        float x = top.x;
        if (waterStream)
        {
           
            RectTransform s = waterStream.rectTransform;
            x += (streamBottomX - streamTipX) * s.rect.width * s.lossyScale.x;
        }
        return new Vector3(x, y, top.z);
    }

 

    private void BuildVisuals()
    {
        if (jug == null) { Debug.LogError("PourMinigame: 'Jug' isn't assigned.", this); return; }

       
        Image own = jug.GetComponent<Image>();
        if (own && (uprightSprite || tippedSprite)) own.enabled = false;

        
        if (uprightJug == null && uprightSprite) { uprightJug = MakeImage("UprightJug", jug, uprightSprite); SitOnBase(uprightJug, 1f); }
        if (tippedJug == null && tippedSprite) { tippedJug = MakeImage("TippedJug", jug, tippedSprite); SitOnBase(tippedJug, 1f); }

        if (spout == null && tippedJug)
        {
            spout = MakeEmpty("Spout", tippedJug.rectTransform);
            spout.anchorMin = spout.anchorMax = spoutOnTippedSprite;
            spout.anchoredPosition = Vector2.zero;
        }

        RectTransform root = transform as RectTransform;

        if (waterStream)
        {
            if (streamSprite) waterStream.sprite = streamSprite;
            waterStream.preserveAspect = false;
            waterStream.raycastTarget = false;
            RectTransform s = waterStream.rectTransform;
            s.pivot = new Vector2(streamTipX, 1f);
            if (streamWidth > 0f) s.sizeDelta = new Vector2(streamWidth, s.sizeDelta.y);
        }

        if (splashPoint == null && root)
        {
            splashPoint = MakeEmpty("SplashPoint", root);
            splashPoint.anchorMin = splashPoint.anchorMax = new Vector2(0.5f, splashHeight);
            splashPoint.anchoredPosition = Vector2.zero;
        }

        if (puddle == null && puddleSprite)
        {
            puddle = MakeImage("Puddle", transform, puddleSprite);
            puddle.rectTransform.sizeDelta = puddleSprite.rect.size * 0.8f;
            puddle.preserveAspect = true;
            if (splashPoint) puddle.rectTransform.position = splashPoint.position;
        }
        if (puddle && Application.isPlaying) puddle.gameObject.SetActive(false);   

        if (foam == null && foamFrames != null && foamFrames.Length > 0)
        { foam = MakeImage("Foam", transform, foamFrames[0]); NativeScaled(foam); }
        if (splash == null && splashFrames != null && splashFrames.Length > 0)
        { splash = MakeImage("Splash", transform, splashFrames[0]); NativeScaled(splash); }

        ApplySizes();

  
        if (waterStream)
        {
            if (puddle && puddle.transform.parent == waterStream.transform.parent)
                puddle.transform.SetSiblingIndex(waterStream.transform.GetSiblingIndex()); 
            int i = waterStream.transform.GetSiblingIndex();
            if (foam && foam.transform.parent == waterStream.transform.parent) foam.transform.SetSiblingIndex(i + 1);
            if (splash && splash.transform.parent == waterStream.transform.parent) splash.transform.SetSiblingIndex(i + 2);
        }
    }

   
    [ContextMenu("Build Visuals In Editor")]
    private void BuildVisualsInEditor()
    {
        BuildVisuals();
        if (uprightJug) uprightJug.gameObject.SetActive(true);
        if (tippedJug) tippedJug.gameObject.SetActive(true);
        if (splash) splash.gameObject.SetActive(true);
        if (foam) foam.gameObject.SetActive(true);
        if (puddle) puddle.gameObject.SetActive(true);
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    private static Image MakeImage(string name, Transform parent, Sprite sprite)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = false;
        return img;
    }

    private static RectTransform MakeEmpty(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

  
    private void SitOnBase(Image img, float scale)
    {
        if (img.sprite == null) return;
        Rect r = jug.rect;
        float aspect = img.sprite.rect.width / img.sprite.rect.height;
        float h = r.height, w = h * aspect;
        if (w > r.width) { w = r.width; h = w / aspect; }

        RectTransform t = img.rectTransform;
        t.anchorMin = t.anchorMax = new Vector2(0.5f, 0f);
        t.pivot = new Vector2(0.5f, 0f);
        t.anchoredPosition = Vector2.zero;
        t.sizeDelta = new Vector2(w, h) * scale;
        img.preserveAspect = true;
    }

    private void NativeScaled(Image img)
    {
        if (img.sprite == null) return;
        img.rectTransform.sizeDelta = img.sprite.rect.size * 0.8f;
        img.rectTransform.pivot = new Vector2(0.5f, 0.2f);  
        img.preserveAspect = true;
    }
}
