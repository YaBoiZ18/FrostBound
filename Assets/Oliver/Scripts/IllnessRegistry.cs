using System;
using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;

public class IllnessRegistry : MonoBehaviour
{
    public static IllnessRegistry IR { get; private set; }

    [Header("Config")]
    public IllnessCatalogue catalogue;
    //public int worldSeed = 12345; -> if we want to make it deterministic

    //in-memory store
    [Serializable] public class Record { public string npcId; public string illnessKey; public string status; } // status: "sick","cured","dead"
    Dictionary<string, Record> _map = new();
    System.Random _rng;

    const string PREFS_KEY = "IllnessRegistry.v1";

    void Awake()
    {
        if (IR && IR != this) { Destroy(gameObject); return; }
        IR = this;
        DontDestroyOnLoad(gameObject);

        _rng = new System.Random();
        Load();
    }

    //assign if missing; otherwise return existing
    public string AssignIfMissing(string npcId)
    {
        if (string.IsNullOrEmpty(npcId)) return "";

        if (_map.TryGetValue(npcId, out var rec))
        {
            //if already has an illness and not cured/dead, keep it
            if (!string.IsNullOrEmpty(rec.illnessKey) && rec.status == "sick")
                return rec.illnessKey;

            //if cured/dead, keep existing key (or you can clear it) — up to your design
            return rec.illnessKey;
        }

        //create new record with a random illness
        string key = catalogue ? catalogue.Draw(_rng) : "";
        var r = new Record { npcId = npcId, illnessKey = key, status = "sick" };
        _map[npcId] = r;
        Save();
        return key;
    }

    public string GetIllness(string npcId)
        => _map.TryGetValue(npcId, out var r) ? r.illnessKey : "";

    public string GetStatus(string npcId)
        => _map.TryGetValue(npcId, out var r) ? r.status : "";

    public void SetStatus(string npcId, string status)
    {
        if (!_map.TryGetValue(npcId, out var r)) return;
        r.status = status; // "sick" | "cured" | "dead"
        Save();
    }

    // ---------------- Save/Load ----------------

    [Serializable] class SaveBlob { public List<Record> items = new(); }

    public void Save()
    {
        var blob = new SaveBlob { items = new List<Record>(_map.Values) };
        var json = JsonUtility.ToJson(blob);
        PlayerPrefs.SetString(PREFS_KEY, json);
        PlayerPrefs.Save();
    }

    public void Load()
    {
        _map.Clear();
        if (!PlayerPrefs.HasKey(PREFS_KEY)) return;

        var json = PlayerPrefs.GetString(PREFS_KEY, "{}");
        var blob = JsonUtility.FromJson<SaveBlob>(json) ?? new SaveBlob();
        foreach (var r in blob.items)
            if (!string.IsNullOrEmpty(r.npcId))
                _map[r.npcId] = r;
    }

    void OnApplicationQuit() => Save();

    // ---------------- YarnSpinner bridge ----------------

    //function: npc_illness("<id>") -> returns the illness key for that npc
    [YarnFunction("npc_illness")]
    public static string Yarn_NpcIllness(string npcId) => IR ? IR.GetIllness(npcId) : "";

    //commands to mutate state from Yarn:
    [YarnCommand("cure_npc")] public static void Yarn_Cure(string npcId) { if (IR != null) IR.SetStatus(npcId, "cured"); }
    [YarnCommand("kill_npc")] public static void Yarn_Kill(string npcId) { if (IR != null) IR.SetStatus(npcId, "dead"); }


    // ---------------- Debugging ----------------

#if UNITY_EDITOR
    public void DebugSetIllness(string npcId, string newKey)
    {
        if (string.IsNullOrEmpty(npcId) || string.IsNullOrEmpty(newKey)) return;
        if (!_map.TryGetValue(npcId, out var r))
        {
            r = new Record { npcId = npcId, illnessKey = newKey, status = "sick" };
            _map[npcId] = r;
        }
        else
        {
            r.illnessKey = newKey;
            if (r.status != "dead") r.status = "sick";
        }
        Save();
    }
#endif


}