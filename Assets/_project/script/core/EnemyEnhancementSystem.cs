using System.Collections.Generic;
using UnityEngine;

public class EnemyUpgradeOption
{
    public LineData line;

    public string GetSummary()
    {
        return line != null ? line.GetDisplayText() : "None";
    }
}

public static class EnemyEnhancementSystem
{
    private static readonly List<System.Func<LineData>> UpgradePoolDefault = new List<System.Func<LineData>>
    {
        () => CreateBuffLine("atkBuff", 2, "強化", " +2", "攻撃力+2"),
        () => CreateAttackLine(2, "追撃", " 2", "追加で2ダメージ")
    };

    private static readonly List<System.Func<LineData>> UpgradePoolLayer6 = new List<System.Func<LineData>>
    {
        () => CreateBuffLine("atkBuff", 3, "強化", " +3", "攻撃力+3"),
        () => CreateShieldLine(4, "防護", " +4", "シールド+4"),
        () => CreateAttackLine(3, "追撃", " +3", "追加で3ダメージ")
    };

    private static readonly List<System.Func<LineData>> UpgradePoolLayer11 = new List<System.Func<LineData>>
    {
        () => CreateBuffLine("atkBuff", 4, "強化", " +5", "攻撃力+5"),
        () => CreateShieldLine(10, "防護", " +10", "シールド+10"),
        () => CreateAttackLine(5, "追撃", " +5", "追加で5ダメージ"),
        () => CreateHealLine(14, "回復", " +14", "HPを14回復")
    };

    public static List<EnemyUpgradeOption> GenerateOptions(int count, int layer)
    {
        List<EnemyUpgradeOption> options = new List<EnemyUpgradeOption>();
        if (count <= 0)
        {
            return options;
        }

        List<System.Func<LineData>> pool = GetPoolForLayer(layer);
        if (pool == null || pool.Count == 0)
        {
            return options;
        }

        for (int i = 0; i < count; i++)
        {
            EnemyUpgradeOption option = new EnemyUpgradeOption
            {
                line = pool[Random.Range(0, pool.Count)].Invoke()
            };
            options.Add(option);
        }

        return options;
    }

    public static bool ApplyOption(EnemyData enemy, EnemyUpgradeOption option)
    {
        if (enemy == null || option == null || option.line == null)
        {
            return false;
        }

        if (enemy.actionDeck == null || enemy.actionDeck.Count == 0)
        {
            return false;
        }

        int index = Random.Range(0, enemy.actionDeck.Count);
        CardData card = enemy.actionDeck[index];
        if (card.lines == null)
        {
            card.lines = new List<LineData>();
        }

        int insertIndex = Random.Range(0, card.lines.Count + 1);
        card.lines.Insert(insertIndex, option.line);
        return true;
    }

    private static LineData CreateBuffLine(string targetStat, int value, string displayPhrase, string displayValueText, string description)
    {
        LineData line = new LineData();
        line.SetEffectType(EffectType.Buff);
        line.lineID = "buff_atk";
        line.displayPhrase = displayPhrase;
        line.displayValueText = displayValueText;
        line.displayName = displayPhrase + displayValueText;
        line.description = description;
        line.paramsInt.Add(new LineParamInt { key = "value", value = value });
        line.paramsStr.Add(new LineParamStr { key = "targetStat", value = targetStat });
        return line;
    }

    private static LineData CreateAttackLine(int baseValue, string displayPhrase, string displayValueText, string description)
    {
        LineData line = new LineData();
        line.SetEffectType(EffectType.Effect);
        line.lineID = "attack_normal";
        line.displayPhrase = displayPhrase;
        line.displayValueText = displayValueText;
        line.displayName = displayPhrase + displayValueText;
        line.description = description;
        line.paramsInt.Add(new LineParamInt { key = "baseValue", value = baseValue });
        line.paramsStr.Add(new LineParamStr { key = "dmgType", value = "normal" });
        return line;
    }

    private static LineData CreateShieldLine(int value, string displayPhrase, string displayValueText, string description)
    {
        LineData line = new LineData();
        line.SetEffectType(EffectType.Buff);
        line.lineID = "shield_normal";
        line.displayPhrase = displayPhrase;
        line.displayValueText = displayValueText;
        line.displayName = displayPhrase + displayValueText;
        line.description = description;
        line.paramsInt.Add(new LineParamInt { key = "value", value = value });
        return line;
    }

    private static LineData CreateHealLine(int value, string displayPhrase, string displayValueText, string description)
    {
        LineData line = new LineData();
        line.SetEffectType(EffectType.Buff);
        line.lineID = "heal_normal";
        line.displayPhrase = displayPhrase;
        line.displayValueText = displayValueText;
        line.displayName = displayPhrase + displayValueText;
        line.description = description;
        line.paramsInt.Add(new LineParamInt { key = "value", value = value });
        return line;
    }

    private static List<System.Func<LineData>> GetPoolForLayer(int layer)
    {
        if (layer >= 11)
        {
            return UpgradePoolLayer11;
        }

        if (layer >= 6)
        {
            return UpgradePoolLayer6;
        }

        return UpgradePoolDefault;
    }
}
