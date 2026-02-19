using System.Collections.Generic;
using UnityEngine;

public class EnhancementOption
{
    public LineData plusLine;
    public LineData minusLine;
    public int plusInsertIndex = -1;
    public int minusInsertIndex = -1;

    public int TotalScore => (plusLine != null ? plusLine.valueScore : 0) + (minusLine != null ? minusLine.valueScore : 0);

    public string GetSummary()
    {
        string plusName = plusLine != null ? plusLine.GetDisplayText() : "None";
        string minusName = minusLine != null ? minusLine.GetDisplayText() : "None";
        return plusName + " / " + minusName + " (score " + TotalScore + ")";
    }
}

public static class CardEnhancementSystem
{
    private const string EnhancementPlusPool = "enhancement_plus";
    private const string EnhancementMinusPool = "enhancement_minus";
    private const string EnhancementPlusRarePool = "enhancement_plus_rare";
    private const string EnhancementMinusRarePool = "enhancement_minus_rare";
    private static readonly HashSet<string> SupportedLineIds = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
    {
        "attack_normal",
        "self_dmg",
        "buff_atk",
        "debuff_def",
        "skip_next_line",
        "stop_execution",
        "restart_card",
        "damage_scale_by_line_count",
        "damage_scale_by_line_number",
        "conditional_power_boost",
        "damage_scale_by_missing_hp",
        "damage_scale_by_enemy_current_hp",
        "damage_sum_all_numerics",
        "global_damage_modifier",
        "shield_scale_by_cards_played",
        "perm_max_hp_reduction",
        "negate_curse_and_heal",
        "self_damage_scale_by_line_number",
        "amplify_following_curse",
        "heal_normal",
        "repeat_previous_line",
        "repeat_last_numeric",
        "probabilistic_redirect",
        "transfer_debuff",
        "destroy_adjacent_lines",
        "copy_to_another_card",
        "pi",
        "hallucination"
    };
    private static readonly HashSet<string> UnsupportedLineIds = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
    {
    };
    private static readonly List<System.Func<LineData>> PlusPool = new List<System.Func<LineData>>
    {
        () => CreateBuffLine("atkBuff", 5, 30, "猛攻", "+5", "攻撃力+5"),
        () => CreateDebuffLine("EnemyDefenseDown", 3, 20, "防御減", "-3", "敵の防御を下げる"),
        () => CreateAttackLine(4, 25, "追撃", "4", "追加で4ダメージ")
    };

    private static readonly List<System.Func<LineData>> MinusPool = new List<System.Func<LineData>>
    {
        () => CreateCurseLine(3, -20, "自傷", "3", "自分に3ダメージ"),
        () => CreateBuffLine("atkBuff", -3, -15, "衰弱", "-3", "攻撃力-3"),
        () => CreateDebuffLine("EnemyDefenseDown", -2, -15, "防御増", "+2", "敵の防御が上がる"),
        () => CreateCurseLine(5, -30, "自傷", "5", "自分に5ダメージ")
    };

    public static List<EnhancementOption> GenerateOptions(int count)
    {
        return GenerateOptions(count, 1, 1);
    }

    public static List<EnhancementOption> GenerateOptions(int count, int currentLayer, int maxLayer)
    {
        float progress = GetLayerProgress(currentLayer, maxLayer);
        return GenerateOptionsInternal(count, progress, 0f);
    }

    public static List<EnhancementOption> GenerateOptions(int count, int currentLayer, int maxLayer, float progressOffset, bool isBoss, float bossProgressBoost)
    {
        return GenerateOptions(count, currentLayer, maxLayer, progressOffset, isBoss, bossProgressBoost, 0f);
    }

    public static List<EnhancementOption> GenerateOptions(int count, int currentLayer, int maxLayer, float progressOffset, bool isBoss, float bossProgressBoost, float rareChance)
    {
        float progress = GetLayerProgress(currentLayer, maxLayer);
        progress += progressOffset;
        if (isBoss)
        {
            progress += bossProgressBoost;
        }
        progress = Mathf.Clamp01(progress);

        if (currentLayer < 5)
        {
            rareChance = 0f;
        }

        return GenerateOptionsInternal(count, progress, Mathf.Clamp01(rareChance));
    }

    private static List<EnhancementOption> GenerateOptionsInternal(int count, float progress, float rareChance)
    {
        List<EnhancementOption> options = new List<EnhancementOption>();
        if (count <= 0)
        {
            return options;
        }

        for (int i = 0; i < count; i++)
        {
            // 1. Determine rarity for the pair
            bool useRare = rareChance > 0f && Random.value < rareChance;

            LineData plusLine = null;
            bool plusIsRare = false;

            // 2. Try to create Plus line
            if (useRare)
            {
                if (TryCreateLineFromLibrary(true, progress, -1f, true, out plusLine))
                {
                    plusIsRare = true;
                }
                else
                {
                    // Rare pool empty or failed, fallback to normal
                    useRare = false; 
                }
            }

            if (plusLine == null)
            {
                if (!TryCreateLineFromLibrary(true, progress, -1f, false, out plusLine))
                {
                    plusLine = CreateLineFromFallback(true, progress, -1f);
                }
            }

            // 3. Try to create Minus line (matching rarity)
            float targetAbs = plusLine != null ? Mathf.Abs(plusLine.valueScore) : -1f;
            if (targetAbs <= 0f)
            {
                targetAbs = -1f;
            }

            LineData minusLine = null;
            if (useRare) // useRare is true only if plus succeeded as rare
            {
                if (!TryCreateLineFromLibrary(false, progress, targetAbs, true, out minusLine))
                {
                    // Rare minus failed? This shouldn't happen ideally if pools are balanced, 
                    // but if it does, we must fallback to normal.
                }
            }

            if (minusLine == null)
            {
                if (!TryCreateLineFromLibrary(false, progress, targetAbs, false, out minusLine))
                {
                    minusLine = CreateLineFromFallback(false, progress, targetAbs);
                }
            }

            EnhancementOption option = new EnhancementOption
            {
                plusLine = plusLine,
                minusLine = minusLine
            };
            options.Add(option);
        }

        return options;
    }

    private static bool TryCreateLineFromLibrary(bool positive, float progress, float targetAbs, float rareChance, out LineData line)
    {
        bool useRare = rareChance > 0f && Random.value < rareChance;
        if (useRare && TryCreateLineFromLibrary(positive, progress, targetAbs, true, out line))
        {
            return true;
        }

        return TryCreateLineFromLibrary(positive, progress, targetAbs, false, out line);
    }

    private static bool TryCreateLineFromLibrary(bool positive, float progress, float targetAbs, bool useRare, out LineData line)
    {
        line = null;

        List<EffectDefinition> pool = GetEffectPool(positive, useRare);
        if (pool.Count == 0)
        {
            return false;
        }

        EffectDefinition definition = PickWeightedDefinition(pool, progress, targetAbs);
        if (definition == null)
        {
            return false;
        }

        line = definition.ToLineData();
        return line != null;
    }

    private static List<EffectDefinition> GetEffectPool(bool positive, bool useRare)
    {
        string poolName = positive
            ? (useRare ? EnhancementPlusRarePool : EnhancementPlusPool)
            : (useRare ? EnhancementMinusRarePool : EnhancementMinusPool);
        List<EffectDefinition> pool = FilterSupported(EffectLibrary.GetByPool(poolName));
        List<EffectDefinition> signedPool = FilterBySign(pool, positive);
        if (signedPool.Count > 0)
        {
            return signedPool;
        }

        if (pool.Count > 0)
        {
            return pool;
        }

        return useRare ? new List<EffectDefinition>() : FilterSupported(EffectLibrary.GetByScoreSign(positive));
    }

    private static List<EffectDefinition> FilterBySign(List<EffectDefinition> source, bool positive)
    {
        List<EffectDefinition> filtered = new List<EffectDefinition>();
        if (source == null)
        {
            return filtered;
        }

        foreach (EffectDefinition definition in source)
        {
            if (definition == null)
            {
                continue;
            }

            if (positive && definition.valueScore > 0)
            {
                filtered.Add(definition);
            }
            else if (!positive && definition.valueScore < 0)
            {
                filtered.Add(definition);
            }
        }

        return filtered;
    }

    private static LineData CreateLineFromFallback(bool positive, float progress, float targetAbs)
    {
        List<System.Func<LineData>> pool = positive ? PlusPool : MinusPool;
        if (pool == null || pool.Count == 0)
        {
            return null;
        }

        List<LineData> candidates = new List<LineData>();
        foreach (var creator in pool)
        {
            if (creator == null)
            {
                continue;
            }

            LineData line = creator.Invoke();
            if (line != null)
            {
                candidates.Add(line);
            }
        }

        return PickWeightedLine(candidates, progress, targetAbs);
    }

    private static EffectDefinition PickWeightedDefinition(List<EffectDefinition> pool, float progress, float targetAbs)
    {
        if (pool == null || pool.Count == 0)
        {
            return null;
        }

        float maxAbs = GetMaxAbsScore(pool);
        if (maxAbs <= 0f)
        {
            return pool[Random.Range(0, pool.Count)];
        }

        float total = 0f;
        for (int i = 0; i < pool.Count; i++)
        {
            EffectDefinition definition = pool[i];
            if (definition == null)
            {
                continue;
            }

            total += ComputeWeight(definition.valueScore, progress, targetAbs, maxAbs);
        }

        if (total <= 0f)
        {
            return pool[Random.Range(0, pool.Count)];
        }

        float roll = Random.Range(0f, total);
        float current = 0f;
        for (int i = 0; i < pool.Count; i++)
        {
            EffectDefinition definition = pool[i];
            if (definition == null)
            {
                continue;
            }

            current += ComputeWeight(definition.valueScore, progress, targetAbs, maxAbs);
            if (roll <= current)
            {
                return definition;
            }
        }

        return pool[Random.Range(0, pool.Count)];
    }

    private static LineData PickWeightedLine(List<LineData> pool, float progress, float targetAbs)
    {
        if (pool == null || pool.Count == 0)
        {
            return null;
        }

        float maxAbs = GetMaxAbsScore(pool);
        if (maxAbs <= 0f)
        {
            return pool[Random.Range(0, pool.Count)];
        }

        float total = 0f;
        for (int i = 0; i < pool.Count; i++)
        {
            LineData line = pool[i];
            if (line == null)
            {
                continue;
            }

            total += ComputeWeight(line.valueScore, progress, targetAbs, maxAbs);
        }

        if (total <= 0f)
        {
            return pool[Random.Range(0, pool.Count)];
        }

        float roll = Random.Range(0f, total);
        float current = 0f;
        for (int i = 0; i < pool.Count; i++)
        {
            LineData line = pool[i];
            if (line == null)
            {
                continue;
            }

            current += ComputeWeight(line.valueScore, progress, targetAbs, maxAbs);
            if (roll <= current)
            {
                return line;
            }
        }

        return pool[Random.Range(0, pool.Count)];
    }

    private static float ComputeWeight(int valueScore, float progress, float targetAbs, float maxAbs)
    {
        float absScore = Mathf.Abs(valueScore);
        float normalized = maxAbs > 0f ? absScore / maxAbs : 0f;

        float favorSmall = Mathf.Lerp(1.2f, 0.4f, normalized);
        float favorLarge = Mathf.Lerp(0.4f, 1.2f, normalized);
        float weight = Mathf.Lerp(favorSmall, favorLarge, progress);

        if (targetAbs >= 0f && maxAbs > 0f)
        {
            float diff = Mathf.Abs(absScore - targetAbs) / maxAbs;
            float closeness = 1f - Mathf.Clamp01(diff);
            float pairBias = Mathf.Lerp(0.6f, 1.6f, closeness);
            weight *= pairBias;
        }

        return Mathf.Max(0.05f, weight);
    }

    private static float GetMaxAbsScore(List<EffectDefinition> pool)
    {
        float maxAbs = 0f;
        foreach (var definition in pool)
        {
            if (definition == null)
            {
                continue;
            }

            float absScore = Mathf.Abs(definition.valueScore);
            if (absScore > maxAbs)
            {
                maxAbs = absScore;
            }
        }

        return maxAbs;
    }

    private static float GetMaxAbsScore(List<LineData> pool)
    {
        float maxAbs = 0f;
        foreach (var line in pool)
        {
            if (line == null)
            {
                continue;
            }

            float absScore = Mathf.Abs(line.valueScore);
            if (absScore > maxAbs)
            {
                maxAbs = absScore;
            }
        }

        return maxAbs;
    }

    private static float GetLayerProgress(int currentLayer, int maxLayer)
    {
        if (maxLayer <= 1)
        {
            return 0f;
        }

        return Mathf.Clamp01((currentLayer - 1f) / (maxLayer - 1f));
    }

    private static List<EffectDefinition> FilterSupported(List<EffectDefinition> source)
    {
        List<EffectDefinition> filtered = new List<EffectDefinition>();
        if (source == null)
        {
            return filtered;
        }

        foreach (EffectDefinition definition in source)
        {
            if (IsSupported(definition))
            {
                filtered.Add(definition);
            }
        }

        return filtered;
    }

    private static bool IsSupported(EffectDefinition definition)
    {
        if (definition == null || string.IsNullOrEmpty(definition.lineID))
        {
            return false;
        }

        if (SupportedLineIds.Contains(definition.lineID))
        {
            return true;
        }

        if (UnsupportedLineIds.Contains(definition.lineID))
        {
            return false;
        }

        if (!string.IsNullOrEmpty(definition.type) && definition.type.Equals("Effect", System.StringComparison.OrdinalIgnoreCase))
        {
            return HasIntParam(definition, "baseValue") || HasIntParam(definition, "value");
        }

        return false;
    }

    private static bool HasIntParam(EffectDefinition definition, string key)
    {
        if (definition == null || definition.paramsInt == null)
        {
            return false;
        }

        foreach (LineParamInt param in definition.paramsInt)
        {
            if (param != null && param.key == key)
            {
                return true;
            }
        }

        return false;
    }

    public static void ApplyOption(CardData card, EnhancementOption option)
    {
        if (card == null || option == null)
        {
            return;
        }

        if (card.lines == null)
        {
            card.lines = new List<LineData>();
        }

        int baseCount = card.lines.Count;
        bool insertedPlus = false;

        int plusIndex = -1;
        if (option.plusLine != null)
        {
            plusIndex = GetInsertIndex(option.plusInsertIndex, baseCount);
            InsertAt(card.lines, option.plusLine, plusIndex);
            insertedPlus = true;
        }

        if (option.minusLine != null)
        {
            int minusIndex = GetInsertIndex(option.minusInsertIndex, baseCount);
            if (insertedPlus && plusIndex >= 0 && minusIndex >= plusIndex)
            {
                minusIndex++;
            }
            InsertAt(card.lines, option.minusLine, minusIndex);
        }
    }

    private static int GetInsertIndex(int requestedIndex, int baseCount)
    {
        if (baseCount < 0)
        {
            baseCount = 0;
        }

        if (requestedIndex < 0)
        {
            return Random.Range(0, baseCount + 1);
        }

        return Mathf.Clamp(requestedIndex, 0, baseCount);
    }

    private static void InsertAt(List<LineData> lines, LineData line, int index)
    {
        if (lines == null || line == null)
        {
            return;
        }

        int clamped = Mathf.Clamp(index, 0, lines.Count);
        lines.Insert(clamped, line);
    }

    private static LineData CreateBuffLine(string targetStat, int value, int valueScore, string displayPhrase, string displayValueText, string description)
    {
        LineData line = new LineData();
        line.SetEffectType(EffectType.Buff);
        line.lineID = "buff_atk";
        line.displayPhrase = displayPhrase;
        line.displayValueText = displayValueText;
        line.displayName = displayPhrase + displayValueText;
        line.description = description;
        line.valueScore = valueScore;
        line.paramsInt.Add(new LineParamInt { key = "value", value = value });
        line.paramsStr.Add(new LineParamStr { key = "targetStat", value = targetStat });
        return line;
    }

    private static LineData CreateDebuffLine(string targetStat, int value, int valueScore, string displayPhrase, string displayValueText, string description)
    {
        LineData line = new LineData();
        line.SetEffectType(EffectType.Debuff);
        line.lineID = "debuff_def";
        line.displayPhrase = displayPhrase;
        line.displayValueText = displayValueText;
        line.displayName = displayPhrase + displayValueText;
        line.description = description;
        line.valueScore = valueScore;
        line.paramsInt.Add(new LineParamInt { key = "value", value = value });
        line.paramsStr.Add(new LineParamStr { key = "targetStat", value = targetStat });
        return line;
    }

    private static LineData CreateCurseLine(int value, int valueScore, string displayPhrase, string displayValueText, string description)
    {
        LineData line = new LineData();
        line.SetEffectType(EffectType.Curse);
        line.lineID = "self_dmg";
        line.displayPhrase = displayPhrase;
        line.displayValueText = displayValueText;
        line.displayName = displayPhrase + displayValueText;
        line.description = description;
        line.valueScore = valueScore;
        line.paramsInt.Add(new LineParamInt { key = "value", value = value });
        line.paramsStr.Add(new LineParamStr { key = "dmgType", value = "normal" });
        return line;
    }

    private static LineData CreateAttackLine(int baseValue, int valueScore, string displayPhrase, string displayValueText, string description)
    {
        LineData line = new LineData();
        line.SetEffectType(EffectType.Effect);
        line.lineID = "attack_normal";
        line.displayPhrase = displayPhrase;
        line.displayValueText = displayValueText;
        line.displayName = displayPhrase + displayValueText;
        line.description = description;
        line.valueScore = valueScore;
        line.paramsInt.Add(new LineParamInt { key = "baseValue", value = baseValue });
        line.paramsStr.Add(new LineParamStr { key = "dmgType", value = "normal" });
        return line;
    }
}
