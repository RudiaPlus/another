using System;
using System.Collections.Generic;
using UnityEngine;


// カード実行効果=Context(文脈)
[Serializable]
public class BattleContext
{
    public Dictionary<string, int> buffs = new Dictionary<string, int>{};
    public void AddBuff(string key, int value)
    {
        if (buffs.ContainsKey(key)) buffs[key] += value;
        else buffs.Add(key, value);
    }
    public int GetBuff(string key)
    {
        return buffs.ContainsKey(key) ? buffs[key] : 0;
    }

    public void Clear() => buffs.Clear();
}

public class CardProcessor: MonoBehaviour
{
    [SerializeField] private BattleManager battleManager;
    [SerializeField] private DamagePopupManager damagePopupManager;
    [SerializeField] private LineDisplayer lineDisplayer;

    public void ExcuteCard(CardData card, EnemyData target, PlayerData player)
    {
        BattleContext context = new BattleContext();
        Debug.Log($"カード発動: {card.name}");

        foreach (var line in card.lines)
        {
            ProcessLine(line, context, target, player);
            lineDisplayer.DisplayLine(line, target, player);
            yield return new WaitForSeconds(0.5f);
        }
    }

    private void ProcessLine(LineData line, BattleContext context, EnemyData target, PlayerData player)
    {
        switch (line.type)
        {
            case EffectType.Effect:
                if (line.lineID == "attack_normal")
                {
                    int baseDMG = line.paramsInt["power"];
                    int finalDMG = baseDMG + context.GetBuff("atkBuff") + context.GetBuff("EnemyDefenseDown");
                    target.currentHP -= finalDMG;
                    damagePopupManager.ShowDamage(target.transform.position, finalDMG);
                    Debug.Log($"{target.name}に{finalDMG}のダメージ");
                }
                break;

            case EffectType.Buff:
                // 次行に値を足す
                foreach(var kvp in line.paramsInt) 
                {
                    context.AddBuff(kvp.Key, kvp.Value);
                    Debug.Log($"{kvp.Key}が{kvp.Value}増加");
                }
                break;

            case EffectType.Debuff:
                context.AddBuff("EnemyDefenseDown", line.valueScore);
                Debug.Log($"{target.name}の防御が{line.valueScore}減少");
                break;

            case EffectType.Curse:
                // プレイヤーへの呪い効果
                if (line.lineID == "self_dmg")
                {
                    int selfDMG = line.paramsInt["power"];
                    player.currentHP -= selfDMG;
                    damagePopupManager.ShowDamage(player.transform.position, selfDMG, Color.red);
                    Debug.Log($"プレイヤーに{selfDMG}の自己ダメージ");
                }
                Debug.Log($"{target.name}に呪い効果を付与");
                break;
        }
    }

}

public class AnotherTextSystem
{
    private Dictionary<string, string[]> namePrefix = new Dictionary<string, string[]>
    {
        {"atk", new string[] {"強撃", "猛攻", "激突"}},
        {"def", new string[] {"鉄壁", "堅牢", "金剛"}},
        {"curse", new string[] {"吐血", "呪詛", "禍根", "災厄"}}
    };

    public void AnotherText(CardData card)
    {
        // Value+50相当
        LineData buffLine = new LineData();
        buffLine.type = EffectType.Buff;
        buffLine.lineID = "buff_atk";
        buffLine.paramsInt.Add("atkBuff", 10);
        buffLine.valueScore = 50;
        buffLine.displayName = "強撃 +10";
        //Value-40相当
        LineData curseLine = new LineData();
        curseLine.type = EffectType.Curse;
        curseLine.lineID = "self_dmg";
        curseLine.paramsInt.Add("power", 5);
        curseLine.valueScore = -40;
        curseLine.displayName = "吐血 -5";

        card.lines.Add(buffLine);
        card.lines.Add(curseLine);
    }
}