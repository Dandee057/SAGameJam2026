using UnityEngine;

/// Tiny helper so any script can fire a sound effect in one line.
/// Sounds go through the AudioManager, so they use the SFX mixer group
/// (the SFX volume slider in Options controls them).
public static class Sfx
{
    /// Plays one random clip from the list. Safe to call with an empty/null list.
    public static void Play(AudioClip[] clips, float volume = 1f, float pitchVariation = 0.08f)
    {
        if (clips == null || clips.Length == 0) return;
        Play(clips[Random.Range(0, clips.Length)], volume, pitchVariation);
    }

    public static void Play(AudioClip clip, float volume = 1f, float pitchVariation = 0.08f)
    {
        if (clip == null) return;
        AudioManager am = AudioManager.Instance;
        if (am != null) am.PlaySFX(clip, volume, pitchVariation);
        else AudioSource.PlayClipAtPoint(clip, Camera.main ? Camera.main.transform.position : Vector3.zero, volume);
    }

    /// Makes a looping AudioSource on the given object (for things like a pouring stream).
    /// It starts silent; set its volume to fade it in and out.
    public static AudioSource MakeLoop(GameObject host, AudioClip clip)
    {
        if (clip == null) return null;
        AudioSource src = host.AddComponent<AudioSource>();
        src.clip = clip;
        src.loop = true;
        src.playOnAwake = false;
        src.spatialBlend = 0f;
        src.volume = 0f;
        AudioManager am = AudioManager.Instance;
        if (am != null) src.outputAudioMixerGroup = am.SfxGroup;
        src.Play();
        return src;
    }
}
