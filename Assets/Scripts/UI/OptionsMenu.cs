using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using TMPro;


public class OptionsMenu : MonoBehaviour
{
    [SerializeField] private AudioMixer mixer;

    [Header("Volume sliders")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    [SerializeField] private TMP_Text masterValueText;
    [SerializeField] private TMP_Text musicValueText;
    [SerializeField] private TMP_Text sfxValueText;

    [SerializeField] private Toggle muteToggle;
    [SerializeField] private Toggle fullscreenToggle;

    private const string MasterParam = "MasterVolume";
    private const string MusicParam  = "MusicVolume";
    private const string SfxParam    = "SFXVolume";

    private const string MasterKey = "opt_masterVolume";
    private const string MusicKey  = "opt_musicVolume";
    private const string SfxKey    = "opt_sfxVolume";
    private const string MuteKey   = "opt_mute";

    private const float DefaultVolume = 0.8f;
    private const float MinDb = -80f;

    private float master, music, sfx;
    private bool muted;

    private void Start()
    {
        SetupSlider(masterSlider, v => { master = v; ApplyMaster(); Save(MasterKey, v); UpdateLabel(masterValueText, v); });
        SetupSlider(musicSlider,  v => { music  = v; ApplyVolume(MusicParam, v); Save(MusicKey, v); UpdateLabel(musicValueText, v); });
        SetupSlider(sfxSlider,    v => { sfx    = v; ApplyVolume(SfxParam, v);   Save(SfxKey, v);   UpdateLabel(sfxValueText, v); });

        if (muteToggle != null) muteToggle.onValueChanged.AddListener(SetMute);
        if (fullscreenToggle != null) fullscreenToggle.onValueChanged.AddListener(SetFullscreen);

        LoadAndApply();
    }


    public void SetMute(bool isMuted)
    {
        muted = isMuted;
        ApplyMaster();
        PlayerPrefs.SetInt(MuteKey, muted ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
    }


    private void LoadAndApply()
    {
        master = PlayerPrefs.GetFloat(MasterKey, DefaultVolume);
        music  = PlayerPrefs.GetFloat(MusicKey,  DefaultVolume);
        sfx    = PlayerPrefs.GetFloat(SfxKey,    DefaultVolume);
        muted  = PlayerPrefs.GetInt(MuteKey, 0) == 1;

        if (masterSlider != null) masterSlider.SetValueWithoutNotify(master);
        if (musicSlider  != null) musicSlider.SetValueWithoutNotify(music);
        if (sfxSlider    != null) sfxSlider.SetValueWithoutNotify(sfx);
        if (muteToggle   != null) muteToggle.SetIsOnWithoutNotify(muted);
        if (fullscreenToggle != null) fullscreenToggle.SetIsOnWithoutNotify(Screen.fullScreen);

        UpdateLabel(masterValueText, master);
        UpdateLabel(musicValueText, music);
        UpdateLabel(sfxValueText, sfx);

        ApplyMaster();
        ApplyVolume(MusicParam, music);
        ApplyVolume(SfxParam, sfx);
    }

    private void SetupSlider(Slider slider, UnityEngine.Events.UnityAction<float> onChanged)
    {
        if (slider == null) return;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.onValueChanged.AddListener(onChanged);
    }

    private void ApplyMaster()
    {
        if (mixer == null) return;
        mixer.SetFloat(MasterParam, muted ? MinDb : ToDecibels(master));
    }

    private void ApplyVolume(string param, float linear)
    {
        if (mixer == null) return;
        mixer.SetFloat(param, ToDecibels(linear));
    }


    private static float ToDecibels(float linear)
    {
        return linear <= 0.0001f ? MinDb : Mathf.Log10(linear) * 20f;
    }

    private static void UpdateLabel(TMP_Text label, float value)
    {
        if (label != null) label.text = Mathf.RoundToInt(value * 100f) + "%";
    }

    private static void Save(string key, float value)
    {
        PlayerPrefs.SetFloat(key, value);
        PlayerPrefs.Save();
    }
}
