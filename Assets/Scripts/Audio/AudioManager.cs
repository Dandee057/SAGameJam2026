using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;


public class AudioManager : MonoBehaviour
{
    private static AudioManager instance;
    private static bool quitting;


    public static AudioManager Instance
    {
        get
        {
            if (instance == null) Bootstrap();
            return instance;
        }
        private set => instance = value;
    }

    [System.Serializable]
    public class Sound
    {
        public string name;

        public AudioClip[] clips;

        [Range(0f, 1f)] public float volume = 1f;

        [Range(0f, 0.5f)] public float pitchVariation = 0.05f;
    }

    [Header("Mixer groups")]
    [SerializeField] private AudioMixerGroup musicGroup;
    [SerializeField] private AudioMixerGroup sfxGroup;

    [Header("Sound library")]
    [SerializeField] private Sound[] music;
    [SerializeField] private Sound[] sfx;

    [Header("Settings")]
    [SerializeField] private int sfxVoices = 12;
    [SerializeField] private float defaultMusicFade = 1f;

    private readonly Dictionary<string, Sound> musicLookup = new Dictionary<string, Sound>(System.StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Sound> sfxLookup = new Dictionary<string, Sound>(System.StringComparer.OrdinalIgnoreCase);

    private AudioSource[] musicSources; 
    private int activeMusic;
    private Coroutine musicFade;

    private AudioSource[] sfxSources;
    private int nextSfx;


    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
        quitting = false;
        Application.quitting -= OnQuitting;
        Application.quitting += OnQuitting;
    }

    private static void OnQuitting() => quitting = true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null || quitting) return;
        var prefab = Resources.Load<AudioManager>("AudioManager");
        if (prefab != null)
        {
            Instantiate(prefab).name = "AudioManager";  
            Debug.Log("AudioManager: created (look under DontDestroyOnLoad in the Hierarchy)");
        }
        else
        {
            Debug.LogWarning("AudioManager: no prefab found at Assets/Resources/AudioManager.prefab " +
                             "(it must have the AudioManager component on its root object)");
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);  
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        BuildLookup(music, musicLookup, "music");
        BuildLookup(sfx, sfxLookup, "SFX");

        musicSources = new AudioSource[2];
        for (int i = 0; i < 2; i++)
        {
            musicSources[i] = CreateSource("Music " + i, musicGroup);
            musicSources[i].loop = true;
        }

        sfxSources = new AudioSource[Mathf.Max(1, sfxVoices)];
        for (int i = 0; i < sfxSources.Length; i++)
        {
            sfxSources[i] = CreateSource("SFX " + i, sfxGroup);
        }
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

   
    public void PlaySFX(string soundName)
    {
        if (!sfxLookup.TryGetValue(soundName, out Sound s))
        {
            Debug.LogWarning($"AudioManager: no SFX called \"{soundName}\"");
            return;
        }
        PlayOnFreeSource(PickClip(s), s.volume, s.pitchVariation);
    }

    public void PlaySFX(AudioClip clip, float volume = 1f, float pitchVariation = 0.05f)
    {
        PlayOnFreeSource(clip, volume, pitchVariation);
    }


    public void PlayMusic(string trackName, float fadeTime = -1f)
    {
        if (!musicLookup.TryGetValue(trackName, out Sound s))
        {
            Debug.LogWarning($"AudioManager: no music called \"{trackName}\"");
            return;
        }
        PlayMusic(PickClip(s), s.volume, fadeTime);
    }

    public void PlayMusic(AudioClip clip, float volume = 1f, float fadeTime = -1f)
    {
        if (clip == null) return;
        AudioSource current = musicSources[activeMusic];
        if (current.clip == clip && current.isPlaying) return;   

        if (fadeTime < 0f) fadeTime = defaultMusicFade;

        activeMusic = 1 - activeMusic;
        AudioSource next = musicSources[activeMusic];
        next.clip = clip;
        next.volume = 0f;
        next.Play();

        if (musicFade != null) StopCoroutine(musicFade);
        musicFade = StartCoroutine(Crossfade(current, next, volume, fadeTime));
    }

    public void StopMusic(float fadeTime = -1f)
    {
        if (fadeTime < 0f) fadeTime = defaultMusicFade;
        if (musicFade != null) StopCoroutine(musicFade);
        musicFade = StartCoroutine(Crossfade(musicSources[activeMusic], null, 0f, fadeTime));
    }

    private IEnumerator Crossfade(AudioSource from, AudioSource to, float targetVolume, float duration)
    {
        float fromStart = from != null ? from.volume : 0f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;   
            float k = Mathf.Clamp01(t / duration);
            if (from != null) from.volume = Mathf.Lerp(fromStart, 0f, k);
            if (to != null) to.volume = Mathf.Lerp(0f, targetVolume, k);
            yield return null;
        }
        if (from != null) { from.volume = 0f; from.Stop(); }
        if (to != null) to.volume = targetVolume;
        musicFade = null;
    }

    private void PlayOnFreeSource(AudioClip clip, float volume, float pitchVariation)
    {
        if (clip == null) return;

        AudioSource src = null;
        for (int i = 0; i < sfxSources.Length; i++)
        {
            int idx = (nextSfx + i) % sfxSources.Length;
            if (!sfxSources[idx].isPlaying) { src = sfxSources[idx]; nextSfx = idx + 1; break; }
        }
        if (src == null) { src = sfxSources[nextSfx % sfxSources.Length]; nextSfx++; }

        src.clip = clip;
        src.volume = volume;
        src.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        src.Play();
    }

    private static AudioClip PickClip(Sound s)
    {
        if (s.clips == null || s.clips.Length == 0) return null;
        return s.clips[Random.Range(0, s.clips.Length)];
    }

    private AudioSource CreateSource(string sourceName, AudioMixerGroup group)
    {
        var go = new GameObject(sourceName);
        go.transform.SetParent(transform);
        var src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.outputAudioMixerGroup = group;
        src.spatialBlend = 0f;  
        return src;
    }

    private static void BuildLookup(Sound[] sounds, Dictionary<string, Sound> lookup, string label)
    {
        if (sounds == null) return;
        foreach (var s in sounds)
        {
            if (s == null || string.IsNullOrWhiteSpace(s.name)) continue;
            if (lookup.ContainsKey(s.name))
                Debug.LogWarning($"AudioManager: duplicate {label} name \"{s.name}\"");
            lookup[s.name] = s;
        }
    }
}
