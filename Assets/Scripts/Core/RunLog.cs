using System.Collections.Generic;


public static class RunLog
{
    public class Entry
    {
        public Vignette vignette;
        public float time;
    }

    public static readonly List<Entry> Entries = new();

    public static void Add(Vignette vignette, float time)
    {
        if (!vignette)
        {
            return;
        }

        Entries.Add(new Entry { vignette = vignette, time = time });
    }

    public static void Clear()
    {
        Entries.Clear();
    }
}