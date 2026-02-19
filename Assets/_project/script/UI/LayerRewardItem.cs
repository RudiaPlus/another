using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LayerRewardItem : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text amountText;
    [SerializeField] private TMP_Text actionLabelText;
    [SerializeField] private Button actionButton;
    [SerializeField] private string amountFormat = "x{0}";
    [SerializeField] private Button skipButton;
    [SerializeField] private TMP_Text skipLabelText;
    [SerializeField] private string skipLabel = "スキップ";

    private LayerRewardEntry entry;
    private Action<LayerRewardEntry, LayerRewardItem> onSelected;
    private Action<LayerRewardItem> onSkipped;

    public void Setup(LayerRewardEntry reward, Action<LayerRewardEntry, LayerRewardItem> onSelected, Action<LayerRewardItem> onSkipped)
    {
        entry = reward;
        this.onSelected = onSelected;
        this.onSkipped = onSkipped;

        if (titleText != null)
        {
            titleText.text = BuildTitle(reward);
        }

        if (descriptionText != null)
        {
            descriptionText.text = BuildDescription(reward);
        }

        if (amountText != null)
        {
            amountText.text = reward != null && reward.amount > 0 ? string.Format(amountFormat, reward.amount) : string.Empty;
        }

        if (actionLabelText != null)
        {
            actionLabelText.text = BuildActionLabel(reward);
        }

        if (actionButton != null)
        {
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(HandleClick);
        }

        if (skipLabelText != null)
        {
            skipLabelText.text = skipLabel;
        }

        if (skipButton != null)
        {
            skipButton.onClick.RemoveAllListeners();
            skipButton.onClick.AddListener(HandleSkip);
        }
    }

    public void SetInteractable(bool interactable)
    {
        if (actionButton != null)
        {
            actionButton.interactable = interactable;
        }
    }

    private void HandleClick()
    {
        onSelected?.Invoke(entry, this);
    }

    private void HandleSkip()
    {
        onSkipped?.Invoke(this);
    }

    private static string BuildTitle(LayerRewardEntry reward)
    {
        if (reward == null)
        {
            return "報酬";
        }

        if (!string.IsNullOrEmpty(reward.title))
        {
            return reward.title;
        }

        switch (reward.type)
        {
            case LayerRewardType.MaxHpUp:
                return "最大HP増加";
            case LayerRewardType.Currency:
                return "資金獲得";
            case LayerRewardType.Enhancement:
                return "構文付加";
            case LayerRewardType.CardAdd:
                return "構文追加";
            case LayerRewardType.LineSwap:
                return "行校正";
            default:
                return "報酬";
        }
    }

    private static string BuildDescription(LayerRewardEntry reward)
    {
        if (reward == null)
        {
            return string.Empty;
        }

        if (!string.IsNullOrEmpty(reward.description))
        {
            return reward.description;
        }

        switch (reward.type)
        {
            case LayerRewardType.MaxHpUp:
                return "最大HPが増加します。";
            case LayerRewardType.Currency:
                return "ショップで使える資金を入手します。";
            case LayerRewardType.Enhancement:
                return "ランダムな構文を強化します。";
            case LayerRewardType.CardAdd:
                return "新しい構文を追加します。";
            case LayerRewardType.LineSwap:
                return "構文の行を2つ選んで入れ替えます。";
            default:
                return string.Empty;
        }
    }

    private static string BuildActionLabel(LayerRewardEntry reward)
    {
        if (reward == null)
        {
            return "受け取る";
        }

        if (!string.IsNullOrEmpty(reward.actionLabel))
        {
            return reward.actionLabel;
        }

        if (reward.type == LayerRewardType.Enhancement || reward.type == LayerRewardType.LineSwap)
        {
            return "選択";
        }

        return "受け取る";
    }
}
