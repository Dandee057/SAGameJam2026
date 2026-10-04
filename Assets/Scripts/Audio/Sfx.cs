using UnityEngine;


public static class Sfx
{
    public static void Play(AudioClip[] clips, float volume = 1f, float pitchVariation = 0.08f)
    {
        if (clips == null || clips.Length == 0)
        {
            return;
        }

        Play(clips[Random.Range(0, clips.Length)], volume, pitchVariation);
    }

    public static void Play(AudioClip clip, float volume = 1f, float pitchVariation = 0.08f)
    {
        if (clip == null)
        {
            return;
        }

        AudioManager am = AudioManager.Instance;
        if (am != null)
        {
            am.PlaySFX(clip, volume, pitchVariation);
        }
        else
        {
            AudioSource.PlayClipAtPoint(clip, Camera.main ? Camera.main.transform.position : Vector3.zero, volume);
        }
    }


    public static AudioSource MakeLoop(GameObject host, AudioClip clip)
    {
        if (clip == null)
        {
            return null;
        }

        AudioSource src = host.AddComponent<AudioSource>();
        src.clip = clip;
        src.loop = true;
        src.playOnAwake = false;
        src.spatialBlend = 0f;
        src.volume = 0f;
        AudioManager am = AudioManager.Instance;
        if (am != null)
        {
            src.outputAudioMixerGroup = am.SfxGroup;
        }

        src.Play();
        return src;
    }
}