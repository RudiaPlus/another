using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CardData
{
    public string uid;
    public string name;
    public int baseCost;
    public List<LineData> lines;
}

[Serializable]
public class LineData
{
    public string lineID;
    public string displayName;
    public string description;
    public EffectType type;
    public int valueScore;

    public Dictionary<string, int> paramsInt = new Dictionary<string, int>();
    public Dictionary<string, string> paramsStr = new Dictionary<string, string>();
}

public enum EffectType
{
    Effect,
    Buff,
    Debuff,
    Curse
}

[Serializable]
public class EnemyData
{
    public string id;
    public string name;
    public int maxHP;
    public int currentHP;
    public List<CardData> actionDeck;
}

[Serializable]
public class PlayerData
{
    public string id;
    public string name;
    public int maxHP;
    public int currentHP;
}