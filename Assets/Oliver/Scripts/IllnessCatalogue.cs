using UnityEngine;

[CreateAssetMenu(fileName = "IllnessCatalogue", menuName = "Illness Catalogue")]
public class IllnessCatalogue : ScriptableObject
{
    [System.Serializable]
    public struct Entry
    {
        [Tooltip("String key used in Yarn")]
        public string key;

        [Tooltip("Relative chance of being picked (0 = never).")]
        [Min(0)] public int weight;

        //extras for later
        public string displayName;
        public string yarnPrefixOverride;
        public Sprite icon;
    }

    public Entry[] entries;

    public string Draw(System.Random rng)
    {
        if (entries == null || entries.Length == 0) return "";

        int total = 0;
        for (int i = 0; i < entries.Length; i++)
            total += Mathf.Max(0, entries[i].weight);
        if (total <= 0) return entries[0].key;

        int roll = rng.Next(0, total);
        for (int i = 0; i < entries.Length; i++)
        {
            int w = Mathf.Max(0, entries[i].weight);
            if (roll < w) return ResolvePrefix(entries[i]);
            roll -= w;
        }
        return ResolvePrefix(entries[^1]);
    }

    string ResolvePrefix(Entry e) =>
        string.IsNullOrWhiteSpace(e.yarnPrefixOverride) ? e.key : e.yarnPrefixOverride;

    void OnValidate()
    {
        if (entries == null) return;
        var seen = new System.Collections.Generic.HashSet<string>();
        for (int i = 0; i < entries.Length; i++)
        {
            var k = entries[i].key?.Trim() ?? "";
            if (string.IsNullOrEmpty(k))
                Debug.LogWarning($"[IllnessCatalogue] Entry {i} has an empty key.", this);
            else if (!seen.Add(k))
                Debug.LogWarning($"[IllnessCatalogue] Duplicate key '{k}'. Keys must be unique.", this);
        }
    }
}