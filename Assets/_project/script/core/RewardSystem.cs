using System.Collections.Generic;
using UnityEngine;

public enum RewardType
{
    Enhancement,
    Heal,
    SwapLine,
    RemoveLine
}

[System.Serializable]
public class RewardOption
{
    public RewardType type;
    public string title;
    public string description;
    public int amount;
}

public static class RewardSystem
{
    public static List<RewardOption> GenerateOptions(int count, int healAmount)
    {
        List<RewardOption> options = new List<RewardOption>
        {
            new RewardOption
            {
                type = RewardType.Enhancement,
                title = "構文付加",
                description = "構文にプラス/マイナス効果を追加する"
            },
            new RewardOption
            {
                type = RewardType.Heal,
                title = "回復",
                description = "HPを" + healAmount + "回復する",
                amount = healAmount
            },
            new RewardOption
            {
                type = RewardType.SwapLine,
                title = "行入れ替え",
                description = "構文内の行を2つ入れ替える"
            },
            new RewardOption
            {
                type = RewardType.RemoveLine,
                title = "行削除",
                description = "構文から行を1つ捨てる"
            }
        };

        if (count <= 0 || count >= options.Count)
        {
            return options;
        }

        Shuffle(options);
        return options.GetRange(0, count);
    }

    private static void Shuffle(List<RewardOption> options)
    {
        for (int i = 0; i < options.Count; i++)
        {
            int swapIndex = Random.Range(i, options.Count);
            RewardOption temp = options[i];
            options[i] = options[swapIndex];
            options[swapIndex] = temp;
        }
    }
}
