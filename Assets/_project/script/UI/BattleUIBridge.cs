using System;
using System.Collections.Generic;
using UnityEngine;

public class BattleUIBridge : MonoBehaviour
{
    [Header("Legacy UI")]
    [SerializeField] private GameObject battleUIRoot;
    [SerializeField] private CardSelectionUI cardSelectionUI;
    [SerializeField] private PostBattleRewardUI postBattleRewardUI;
    [SerializeField] private PostBattleEnhancementUI postBattleEnhancementUI;
    [SerializeField] private LayerRewardUI layerRewardUI;
    [SerializeField] private LineCalibrationUI lineCalibrationUI;
    [SerializeField] private LineRemoveUI lineRemoveUI;
    [SerializeField] private ShopUI shopUI;
    [SerializeField] private RunResultUI runResultUI;

    public void BindLegacy(
        GameObject battleRoot,
        CardSelectionUI cardSelection,
        PostBattleRewardUI postBattleReward,
        PostBattleEnhancementUI postBattleEnhancement,
        LayerRewardUI layerReward,
        LineCalibrationUI lineCalibration,
        LineRemoveUI lineRemove,
        ShopUI shop,
        RunResultUI runResult)
    {
        battleUIRoot = battleRoot;
        cardSelectionUI = cardSelection;
        postBattleRewardUI = postBattleReward;
        postBattleEnhancementUI = postBattleEnhancement;
        layerRewardUI = layerReward;
        lineCalibrationUI = lineCalibration;
        lineRemoveUI = lineRemove;
        shopUI = shop;
        runResultUI = runResult;
    }

    public void SetBattleVisible(bool visible)
    {
        if (battleUIRoot != null)
        {
            battleUIRoot.SetActive(visible);
        }
    }

    public void HideCardSelection()
    {
        if (cardSelectionUI != null)
        {
            cardSelectionUI.Hide();
        }
    }

    public void ShowCardSelection(IReadOnlyList<CardData> hand, Action<int> onSelected)
    {
        if (cardSelectionUI != null)
        {
            cardSelectionUI.Show(hand, onSelected);
        }
    }

    public void HidePostBattleReward()
    {
        if (postBattleRewardUI != null)
        {
            postBattleRewardUI.Hide();
        }
    }

    public void ShowPostBattleReward(List<RewardOption> options, Action<RewardOption> onSelected)
    {
        if (postBattleRewardUI != null)
        {
            postBattleRewardUI.Show(options, onSelected);
        }
    }

    public void HidePostBattleEnhancement()
    {
        if (postBattleEnhancementUI != null)
        {
            postBattleEnhancementUI.Hide();
        }
    }

    public void ShowPostBattleEnhancement(CardData card, List<EnhancementOption> options, Action<EnhancementOption> onSelected)
    {
        if (postBattleEnhancementUI != null)
        {
            postBattleEnhancementUI.Show(card, options, onSelected);
        }
    }

    public void HideLayerReward()
    {
        if (layerRewardUI != null)
        {
            layerRewardUI.Hide();
        }
    }

    public void ShowLayerReward(LayerDefinition definition, PlayerData player, Action<LayerRewardEntry, Action> onClaimReward, Action onAllClaimed)
    {
        if (layerRewardUI != null)
        {
            layerRewardUI.Show(definition, player, onClaimReward, onAllClaimed);
        }
    }

    public void HideLineCalibration()
    {
        if (lineCalibrationUI != null)
        {
            lineCalibrationUI.Hide();
        }
    }

    public void ShowLineCalibration(CardData card, Action onComplete)
    {
        if (lineCalibrationUI != null)
        {
            lineCalibrationUI.Show(card, onComplete);
        }
    }

    public void HideLineRemove()
    {
        if (lineRemoveUI != null)
        {
            lineRemoveUI.Hide();
        }
    }

    public void ShowLineRemove(CardData card, Action onComplete)
    {
        if (lineRemoveUI != null)
        {
            lineRemoveUI.Show(card, onComplete);
        }
    }

    public void HideShop()
    {
        if (shopUI != null)
        {
            shopUI.Hide();
        }
    }

    public void ShowShop(PlayerData player, int itemCost, int resetCost, int healAmount, Action<ShopItemType, int, Action<bool>> onPurchase, Func<int, bool> onReset, Action onClosed)
    {
        if (shopUI != null)
        {
            shopUI.Show(player, itemCost, resetCost, healAmount, onPurchase, onReset, onClosed);
        }
    }

    public void HideRunResult()
    {
        if (runResultUI != null)
        {
            runResultUI.Hide();
        }
    }

    public void ShowRunResult(bool cleared, int layerReached, int maxLayer, int baseScore, int totalScore, int totalTurns, CardData finalCard)
    {
        if (runResultUI != null)
        {
            runResultUI.Show(cleared, layerReached, maxLayer, baseScore, totalScore, totalTurns, finalCard);
        }
    }

    public void HideAllOverlays()
    {
        HideCardSelection();
        HidePostBattleReward();
        HidePostBattleEnhancement();
        HideLayerReward();
        HideLineCalibration();
        HideLineRemove();
        HideShop();
        HideRunResult();
    }
}
