using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EffectDefinition
{
    public string id;
    public string pool;
    public string lineID;
    public string displayName;
    public string displayPhrase;
    public string displayValueText;
    public string description;
    public string type;
    public int valueScore;
    public List<LineParamInt> paramsInt = new List<LineParamInt>();
    public List<LineParamStr> paramsStr = new List<LineParamStr>();

    public LineData ToLineData(LineData overrides = null)
    {
        LineData line = new LineData
        {
            effectId = id,
            lineID = lineID,
            displayName = displayName,
            displayPhrase = displayPhrase,
            displayValueText = displayValueText,
            description = description,
            type = type,
            valueScore = valueScore,
            paramsInt = CloneParams(paramsInt),
            paramsStr = CloneParams(paramsStr)
        };

        if (overrides != null)
        {
            ApplyOverrides(line, overrides);
        }

        return line;
    }

    public void ApplyDefaults(LineData line)
    {
        if (line == null)
        {
            return;
        }

        line.effectId = id;

        if (string.IsNullOrEmpty(line.lineID))
        {
            line.lineID = lineID;
        }

        if (string.IsNullOrEmpty(line.displayName))
        {
            line.displayName = displayName;
        }

        if (string.IsNullOrEmpty(line.displayPhrase))
        {
            line.displayPhrase = displayPhrase;
        }

        if (string.IsNullOrEmpty(line.displayValueText))
        {
            line.displayValueText = displayValueText;
        }

        if (string.IsNullOrEmpty(line.description))
        {
            line.description = description;
        }

        if (string.IsNullOrEmpty(line.type))
        {
            line.type = type;
        }

        if (line.valueScore == 0 && valueScore != 0)
        {
            line.valueScore = valueScore;
        }

        List<LineParamInt> overridesInt = line.paramsInt ?? new List<LineParamInt>();
        List<LineParamStr> overridesStr = line.paramsStr ?? new List<LineParamStr>();

        line.paramsInt = CloneParams(paramsInt);
        line.paramsStr = CloneParams(paramsStr);

        ApplyOverrides(line.paramsInt, overridesInt);
        ApplyOverrides(line.paramsStr, overridesStr);
    }

    private static void ApplyOverrides(LineData target, LineData overrides)
    {
        if (target == null || overrides == null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(overrides.lineID))
        {
            target.lineID = overrides.lineID;
        }

        if (!string.IsNullOrEmpty(overrides.displayName))
        {
            target.displayName = overrides.displayName;
        }

        if (!string.IsNullOrEmpty(overrides.displayPhrase))
        {
            target.displayPhrase = overrides.displayPhrase;
        }

        if (!string.IsNullOrEmpty(overrides.displayValueText))
        {
            target.displayValueText = overrides.displayValueText;
        }

        if (!string.IsNullOrEmpty(overrides.description))
        {
            target.description = overrides.description;
        }

        if (!string.IsNullOrEmpty(overrides.type))
        {
            target.type = overrides.type;
        }

        if (overrides.valueScore != 0)
        {
            target.valueScore = overrides.valueScore;
        }

        ApplyOverrides(target.paramsInt, overrides.paramsInt);
        ApplyOverrides(target.paramsStr, overrides.paramsStr);
    }

    private static List<LineParamInt> CloneParams(List<LineParamInt> source)
    {
        List<LineParamInt> clone = new List<LineParamInt>();
        if (source == null)
        {
            return clone;
        }

        foreach (LineParamInt param in source)
        {
            if (param == null)
            {
                continue;
            }

            clone.Add(new LineParamInt { key = param.key, value = param.value });
        }

        return clone;
    }

    private static List<LineParamStr> CloneParams(List<LineParamStr> source)
    {
        List<LineParamStr> clone = new List<LineParamStr>();
        if (source == null)
        {
            return clone;
        }

        foreach (LineParamStr param in source)
        {
            if (param == null)
            {
                continue;
            }

            clone.Add(new LineParamStr { key = param.key, value = param.value });
        }

        return clone;
    }

    private static void ApplyOverrides(List<LineParamInt> target, List<LineParamInt> overrides)
    {
        if (target == null || overrides == null)
        {
            return;
        }

        foreach (LineParamInt param in overrides)
        {
            if (param == null || string.IsNullOrEmpty(param.key))
            {
                continue;
            }

            int index = target.FindIndex(item => item != null && item.key == param.key);
            if (index >= 0)
            {
                target[index] = new LineParamInt { key = param.key, value = param.value };
            }
            else
            {
                target.Add(new LineParamInt { key = param.key, value = param.value });
            }
        }
    }

    private static void ApplyOverrides(List<LineParamStr> target, List<LineParamStr> overrides)
    {
        if (target == null || overrides == null)
        {
            return;
        }

        foreach (LineParamStr param in overrides)
        {
            if (param == null || string.IsNullOrEmpty(param.key))
            {
                continue;
            }

            int index = target.FindIndex(item => item != null && item.key == param.key);
            if (index >= 0)
            {
                target[index] = new LineParamStr { key = param.key, value = param.value };
            }
            else
            {
                target.Add(new LineParamStr { key = param.key, value = param.value });
            }
        }
    }
}

[Serializable]
public class EffectDataList
{
    public List<EffectDefinition> effects = new List<EffectDefinition>();
}

public static class EffectLibrary
{
    private static readonly Dictionary<string, EffectDefinition> EffectsById =
        new Dictionary<string, EffectDefinition>(StringComparer.OrdinalIgnoreCase);
    private static readonly List<EffectDefinition> Effects = new List<EffectDefinition>();

    public static bool IsLoaded { get; private set; }

    public static void LoadFromPath(string path)
    {
        EffectDataList list = EffectJsonIO.LoadEffectList(path);
        Load(list);
    }

    public static void Load(EffectDataList list)
    {
        EffectsById.Clear();
        Effects.Clear();

        if (list == null || list.effects == null)
        {
            IsLoaded = false;
            return;
        }

        foreach (EffectDefinition effect in list.effects)
        {
            if (effect == null || string.IsNullOrEmpty(effect.id))
            {
                continue;
            }

            if (!EffectsById.ContainsKey(effect.id))
            {
                EffectsById.Add(effect.id, effect);
                Effects.Add(effect);
            }
        }

        IsLoaded = Effects.Count > 0;
    }

    public static void Clear()
    {
        EffectsById.Clear();
        Effects.Clear();
        IsLoaded = false;
    }

    public static bool TryGet(string id, out EffectDefinition definition)
    {
        if (!string.IsNullOrEmpty(id))
        {
            return EffectsById.TryGetValue(id, out definition);
        }

        definition = null;
        return false;
    }

    public static IReadOnlyList<EffectDefinition> GetAll()
    {
        return Effects;
    }

    public static List<EffectDefinition> GetByPool(string pool)
    {
        List<EffectDefinition> results = new List<EffectDefinition>();
        if (string.IsNullOrEmpty(pool))
        {
            return results;
        }

        foreach (EffectDefinition effect in Effects)
        {
            if (effect != null && string.Equals(effect.pool, pool, StringComparison.OrdinalIgnoreCase))
            {
                results.Add(effect);
            }
        }

        return results;
    }

    public static List<EffectDefinition> GetByScoreSign(bool positive)
    {
        List<EffectDefinition> results = new List<EffectDefinition>();
        foreach (EffectDefinition effect in Effects)
        {
            if (effect == null)
            {
                continue;
            }

            if (positive && effect.valueScore > 0)
            {
                results.Add(effect);
            }
            else if (!positive && effect.valueScore < 0)
            {
                results.Add(effect);
            }
        }

        return results;
    }

    public static LineData CreateLine(string effectId, LineData overrides = null)
    {
        if (!TryGet(effectId, out EffectDefinition definition))
        {
            return null;
        }

        return definition.ToLineData(overrides);
    }

    public static void ApplyDefinition(LineData line)
    {
        if (line == null || string.IsNullOrEmpty(line.effectId))
        {
            return;
        }

        if (TryGet(line.effectId, out EffectDefinition definition))
        {
            definition.ApplyDefaults(line);
        }
    }
}
