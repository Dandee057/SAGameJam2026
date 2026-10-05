using System;
using UnityEngine;

[Serializable]
public struct VignetteFrame
{
    public Sprite sprite;

    [Tooltip("Seconds this frame is shown. 0 = use the vignette's default.")]
    public float duration;
}

[CreateAssetMenu(menuName = "Date Sabotage/Vignette", fileName = "NewVignette")]
public class Vignette : ScriptableObject
{
    [Header("Playback")]
    [Tooltip("Position in the cutscene. Lower plays first. Independent of the order you did things in.")]
    public int playOrder = 0;

    public VignetteFrame[] frames;
    public float defaultFrameDuration = 1f;

    [Header("Effect")] [Tooltip("Added to the relationship bar while this plays. Negative = bad.")]
    public float relationshipDelta = 10f;

    [Tooltip("Which frame the bar change (and sound) lands on. 0 = first frame, 1 = second...")]
    public int impactFrame = 1;

    public AudioClip sound;

    public float DurationOf(int i)
    {
        float d = frames[i].duration;
        return d > 0f ? d : defaultFrameDuration;
    }
}