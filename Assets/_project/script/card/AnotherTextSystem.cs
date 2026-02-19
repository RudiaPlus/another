using System.Collections.Generic;

public class AnotherTextSystem
{
    public void AnotherText(CardData card)
    {
        if (card == null)
        {
            return;
        }

        if (card.lines == null)
        {
            card.lines = new List<LineData>();
        }

        LineData buffLine = new LineData
        {
            lineID = "buff_atk",
            displayPhrase = "強化",
            displayValueText = "+10",
            displayName = "強化+10",
            description = "攻撃力+10",
            valueScore = 50
        };
        buffLine.SetEffectType(EffectType.Buff);
        buffLine.paramsInt.Add(new LineParamInt { key = "value", value = 10 });
        buffLine.paramsStr.Add(new LineParamStr { key = "targetStat", value = "atkBuff" });

        LineData curseLine = new LineData
        {
            lineID = "self_dmg",
            displayPhrase = "呪詛",
            displayValueText = "-5",
            displayName = "呪詛-5",
            description = "自分に5ダメージ",
            valueScore = -40
        };
        curseLine.SetEffectType(EffectType.Curse);
        curseLine.paramsInt.Add(new LineParamInt { key = "value", value = 5 });
        curseLine.paramsStr.Add(new LineParamStr { key = "dmgType", value = "normal" });

        card.lines.Add(buffLine);
        card.lines.Add(curseLine);
    }
}
