using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class NPCState : MonoBehaviour
{
    //this script is for storing NPC-specific states such as what illnes they have

    [Tooltip("Unique identifier for this NPC. Leave empty to auto-generate in editor.")]
    public string npcId;

    [ReadOnly] public string illnessKey;
    [ReadOnly] public string status; // "sick" | "cured" | "dead"
    //[ReadOnlyInInspector] public string illnessKey;
    //[ReadOnlyInInspector] public string status; // "sick" | "cured" | "dead"

    void OnValidate()
    {
        //auto-generate a GUID in editor
        if (string.IsNullOrEmpty(npcId))
            npcId = System.Guid.NewGuid().ToString();
    }

    void Awake()
    {
        //ask the registry to assign (or return existing) illness
        if (IllnessRegistry.IR != null)
        {
            illnessKey = IllnessRegistry.IR.AssignIfMissing(npcId);
            status = IllnessRegistry.IR.GetStatus(npcId);
        }
    }

}
