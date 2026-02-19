using System;
using System.Collections.Generic;

[Serializable]
public class BattleContext
{
    public Dictionary<string, int> buffs = new Dictionary<string, int>();
    public float nextLineMultiplier = 1f;
    public float baseDamageMultiplier = 1f;
    public int curseMultiplier = 1;
    public int globalDamageAmp = 0;
    public int playerStrength = 0;
    public int enemyStrength = 0;
    public int cardsPlayedThisCombat = 0;
    public List<PendingLineTransform> pendingLineTransforms = new List<PendingLineTransform>();

    public void AddBuff(string key, int value)
    {
        if (string.IsNullOrEmpty(key))
        {
            return;
        }

        if (buffs.ContainsKey(key))
        {
            buffs[key] += value;
        }
        else
        {
            buffs.Add(key, value);
        }
    }

    public int GetBuff(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return 0;
        }

        return buffs.TryGetValue(key, out var value) ? value : 0;
    }

    public float ConsumeNextLineMultiplier()
    {
        float multiplier = nextLineMultiplier;
        nextLineMultiplier = 1f; // Reset after consumption
        return multiplier;
    }

    public float PeekNextLineMultiplier()
    {
        return nextLineMultiplier;
    }

    public void QueueLineTransform(CardData card, int lineIndex, LineData replacement)
    {
        if (card == null || replacement == null)
        {
            return;
        }

        if (pendingLineTransforms == null)
        {
            pendingLineTransforms = new List<PendingLineTransform>();
        }

        pendingLineTransforms.Add(new PendingLineTransform
        {
            card = card,
            lineIndex = lineIndex,
            replacement = replacement
        });
    }

    public void ResetForTurn()
    {
        buffs.Clear();
        nextLineMultiplier = 1f;
        baseDamageMultiplier = 1f;
        curseMultiplier = 1;
        // globalDamageAmp, playerStrength, enemyStrength persist across turns
    }

    public void ResetForCombat()
    {
        buffs.Clear();
        nextLineMultiplier = 1f;
        baseDamageMultiplier = 1f;
        curseMultiplier = 1;
        globalDamageAmp = 0;
        playerStrength = 0;
        enemyStrength = 0;
        cardsPlayedThisCombat = 0;
        if (pendingLineTransforms != null)
        {
            pendingLineTransforms.Clear();
        }
    }
}

[Serializable]
public class PendingLineTransform
{
    public CardData card;
    public int lineIndex;
    public LineData replacement;
}
