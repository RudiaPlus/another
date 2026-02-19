using System;
using System.Collections.Generic;
using System.Globalization;
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
    public string effectId;
    public string lineID;
    public string displayName;
    public string displayPhrase;
    public string displayValueText;
    public string description;
    public string type;
    public int valueScore;

    public List<LineParamInt> paramsInt = new List<LineParamInt>();
    public List<LineParamStr> paramsStr = new List<LineParamStr>();

    public EffectType GetEffectType()
    {
        if (Enum.TryParse(type, true, out EffectType parsedType))
        {
            return parsedType;
        }

        return EffectType.Effect;
    }

    public void SetEffectType(EffectType effectType)
    {
        type = effectType.ToString();
    }

    public bool TryGetInt(string key, out int value)
    {
        if (paramsInt != null)
        {
            foreach (var param in paramsInt)
            {
                if (param.key == key)
                {
                    value = param.value;
                    return true;
                }
            }
        }

        value = 0;
        return false;
    }

    public int GetInt(string key, int defaultValue = 0)
    {
        return TryGetInt(key, out var value) ? value : defaultValue;
    }

    public bool TryGetFloat(string key, out float value)
    {
        if (paramsStr != null)
        {
            foreach (var param in paramsStr)
            {
                if (param.key == key &&
                    float.TryParse(param.value, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                {
                    return true;
                }
            }
        }

        if (paramsInt != null)
        {
            foreach (var param in paramsInt)
            {
                if (param.key == key)
                {
                    value = param.value;
                    return true;
                }
            }
        }

        value = 0f;
        return false;
    }

    public float GetFloat(string key, float defaultValue = 0f)
    {
        return TryGetFloat(key, out var value) ? value : defaultValue;
    }

    public bool TryGetStr(string key, out string value)
    {
        if (paramsStr != null)
        {
            foreach (var param in paramsStr)
            {
                if (param.key == key)
                {
                    value = param.value;
                    return true;
                }
            }
        }

        value = string.Empty;
        return false;
    }

    public string GetStr(string key, string defaultValue = "")
    {
        return TryGetStr(key, out var value) ? value : defaultValue;
    }

    public string GetDisplayText()
    {
        if (!string.IsNullOrEmpty(displayName))
        {
            return displayName;
        }

        if (!string.IsNullOrEmpty(displayPhrase) || !string.IsNullOrEmpty(displayValueText))
        {
            return string.Concat(displayPhrase, displayValueText);
        }

        return lineID;
    }

    public string GetDisplayPhrase()
    {
        if (!string.IsNullOrEmpty(displayPhrase))
        {
            return displayPhrase;
        }

        if (!string.IsNullOrEmpty(displayName))
        {
            return displayName;
        }

        return lineID;
    }
}

[Serializable]
public class LineParamInt
{
    public string key;
    public int value;
}

[Serializable]
public class LineParamStr
{
    public string key;
    public string value;
}

public enum EffectType
{
    Effect,
    Buff,
    Debuff,
    Curse,
    ControlFlow,
    Shield
}

public enum DamageElement
{
    None,
    Fire,
    Ice,
    Lightning,
    Poison,
    Dark
}

[Serializable]
public class EnemyData
{
    public string id;
    public string name;
    public int maxHP;
    public int currentHP;
    public int shield;
    public List<CardData> actionDeck;
}

[Serializable]
public class PlayerData
{
    public string id;
    public string name;
    public int maxHP;
    public int currentHP;
    public int shield;
    public int currency;
    public List<CardData> deck;
}
