using System;
using System.Collections.Generic;

public enum LayerRewardType
{
    MaxHpUp,
    Currency,
    Enhancement,
    CardAdd,
    LineSwap
}

[Serializable]
public class LayerRewardEntry
{
    public LayerRewardType type;
    public int amount;
    public string title;
    public string description;
    public string actionLabel;
}

[Serializable]
public class LayerDefinition
{
    public int layer;
    public bool isBoss;
    public string bossName;
    public float effectProgressOffset;
    public string enemyDeckPath;
    public List<LayerRewardEntry> rewards = new List<LayerRewardEntry>();
}

[System.Serializable]
public class LayerDefinitionList
{
    public List<LayerDefinition> layers = new List<LayerDefinition>();
}
