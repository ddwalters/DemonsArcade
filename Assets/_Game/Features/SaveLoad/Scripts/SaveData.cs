using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PlaythroughData
{
    public string id;
    public string characterName;
}

[Serializable]
public class SavedItem
{
    public string definitionId;
    public Vector2Int position;
    public int rotation;
}

[Serializable]
public class SavedContainer
{
    public int slot;
    public List<SavedItem> items = new();
}

[Serializable]
public class SavedWorldObject
{
    public string id;
    public bool collected;
    public bool opened;
    public bool interactable;
    public List<SavedItem> items = new();
}

[Serializable]
public class SaveData
{
    public const int CurrentVersion = 1;
    public int version = CurrentVersion;
    public string id;
    public PlaythroughData playthrough;
    public string savedAtUtc;
    public double playSeconds;
    public string scenePath;
    public Vector3 position;
    public Quaternion rotation;
    public PlayerStatsData stats;
    public int gold;
    public SavedParty party;
    public List<SavedContainer> inventory = new();
    public List<SavedWorldObject> world = new();
}
