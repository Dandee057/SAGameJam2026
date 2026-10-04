using System.Collections;
using UnityEngine;

public class GuestThoughts : MonoBehaviour
{
    [System.Serializable]
    public class Hint
    {
        [Tooltip("Icon shown in the bubble, e.g. anchovies with a cross through it.")]
        public Sprite icon;

        [Tooltip("Higher = picked more often.")]
        public float weight = 1f;

        [Tooltip("Untick to stop this hint from appearing (e.g. once the player has already solved it).")]
        public bool enabled = true;
    }

    [Header("References")] public ThoughtBubble bubble;

    [Header("Hints")] public Hint[] hints;

    [Header("Timing (seconds)")] public Vector2 timeBetweenThoughts = new(6f, 14f);
    public Vector2 thoughtDuration = new(2.5f, 4f);

    [Tooltip("Delay before the first thought, so guests don't all start at once.")]
    public Vector2 initialDelay = new(1f, 8f);

    [Header("Optional")] [Tooltip("If set, only think while the player is within this distance. 0 = always think.")]
    public float onlyNearPlayerDistance = 0f;

    public Transform player;

    private int lastIndex = -1;
    private Coroutine routine;

    private void OnEnable()
    {
        routine = StartCoroutine(ThinkLoop());
    }

    private void OnDisable()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
        }

        if (bubble != null)
        {
            bubble.Hide();
        }
    }

    private IEnumerator ThinkLoop()
    {
        yield return new WaitForSeconds(Random.Range(initialDelay.x, initialDelay.y));

        while (true)
        {
            // Wait until the player is close enough (if that option is on)
            while (!PlayerInRange())
            {
                yield return null;
            }

            int index = PickHint();
            if (index >= 0)
            {
                bubble.Show(hints[index].icon);
                yield return new WaitForSeconds(Random.Range(thoughtDuration.x, thoughtDuration.y));
                bubble.Hide();
            }

            yield return new WaitForSeconds(Random.Range(timeBetweenThoughts.x, timeBetweenThoughts.y));
        }
    }

    private bool PlayerInRange()
    {
        if (onlyNearPlayerDistance <= 0f || player == null)
        {
            return true;
        }

        return (player.position - transform.position).sqrMagnitude
               <= onlyNearPlayerDistance * onlyNearPlayerDistance;
    }

    // Weighted random pick that avoids repeating the previous hint when possible
    private int PickHint()
    {
        if (hints == null || hints.Length == 0)
        {
            return -1;
        }

        float total = 0f;
        for (int i = 0; i < hints.Length; i++)
        {
            if (!IsPickable(i))
            {
                continue;
            }

            total += hints[i].weight;
        }

        if (total <= 0f)
        {
            return -1;
        }

        float roll = Random.value * total;
        for (int i = 0; i < hints.Length; i++)
        {
            if (!IsPickable(i))
            {
                continue;
            }

            roll -= hints[i].weight;
            if (roll <= 0f)
            {
                lastIndex = i;
                return i;
            }
        }

        return -1;
    }

    private bool IsPickable(int i)
    {
        Hint h = hints[i];
        if (!h.enabled || h.icon == null || h.weight <= 0f)
        {
            return false;
        }

        // Skip the last hint unless it's the only option
        if (i == lastIndex && CountPickable() > 1)
        {
            return false;
        }

        return true;
    }

    private int CountPickable()
    {
        int count = 0;
        foreach (Hint h in hints)
        {
            if (h.enabled && h.icon != null && h.weight > 0f)
            {
                count++;
            }
        }

        return count;
    }

    // Call these from your puzzle logic
    public void SetHintEnabled(int index, bool value)
    {
        if (index >= 0 && index < hints.Length)
        {
            hints[index].enabled = value;
        }
    }

    // Force a thought right now (e.g. when the player does something nearby)
    public void ThinkNow(Sprite icon, float duration = 3f)
    {
        StartCoroutine(ThinkOnce(icon, duration));
    }

    private IEnumerator ThinkOnce(Sprite icon, float duration)
    {
        bubble.Show(icon);
        yield return new WaitForSeconds(duration);
        bubble.Hide();
    }
}