using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LayerRewardUI : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private TMP_Text currencyText;
    [SerializeField] private Transform rewardRoot;
    [SerializeField] private LayerRewardItem rewardItemPrefab;
    [SerializeField] private string titleFormat = "第{0}層 報酬";
    [SerializeField] private string bossTitleFormat = "第{0}層 ボス報酬";
    [SerializeField] private string hpFormat = "HP {0}/{1}";
    [SerializeField] private string currencyFormat = "通貨 {0}";

    private readonly List<LayerRewardItem> items = new List<LayerRewardItem>();
    private Action<LayerRewardEntry, Action> onClaim;
    private Action onAllClaimed;
    private bool claimInProgress;
    private PlayerData player;

    public void Show(LayerDefinition definition, PlayerData player, Action<LayerRewardEntry, Action> onClaimReward, Action onAllClaimed)
    {
        this.player = player;
        this.onClaim = onClaimReward;
        this.onAllClaimed = onAllClaimed;
        claimInProgress = false;

        ClearItems();

        if (titleText != null && definition != null)
        {
            string format = definition.isBoss ? bossTitleFormat : titleFormat;
            titleText.text = string.Format(format, definition.layer);
        }

        UpdateStatus();

        if (rewardRoot == null || rewardItemPrefab == null)
        {
            Debug.LogWarning("LayerRewardUI: missing rewardRoot or rewardItemPrefab");
            SetVisible(false);
            onAllClaimed?.Invoke();
            return;
        }

        Transform spawnRoot = ResolveRewardRoot();
        if (spawnRoot == null)
        {
            Debug.LogWarning("LayerRewardUI: reward spawn root is not set");
            SetVisible(false);
            onAllClaimed?.Invoke();
            return;
        }

        PrepareTemplate();

        if (definition != null && definition.rewards != null)
        {
            foreach (var reward in definition.rewards)
            {
                if (reward == null)
                {
                    continue;
                }

                LayerRewardItem item = Instantiate(rewardItemPrefab, spawnRoot);
                if (item == null)
                {
                    continue;
                }

                item.gameObject.SetActive(true);
                item.Setup(reward, HandleItemClicked, HandleSkipClicked);
                items.Add(item);
            }
        }

        SetVisible(true);
        SetItemsInteractable(true);

        if (items.Count == 0)
        {
            SetVisible(false);
            onAllClaimed?.Invoke();
        }
    }

    public void Refresh()
    {
        UpdateStatus();
    }

    public void Hide()
    {
        SetVisible(false);
    }

    public void SetVisible(bool visible)
    {
        if (root != null)
        {
            root.SetActive(visible);
        }
        else
        {
            gameObject.SetActive(visible);
        }
    }

    private void HandleItemClicked(LayerRewardEntry reward, LayerRewardItem item)
    {
        if (claimInProgress)
        {
            return;
        }

        claimInProgress = true;
        SetItemsInteractable(false);

        if (onClaim != null)
        {
            onClaim(reward, () => CompleteClaim(item));
            return;
        }

        CompleteClaim(item);
    }

    private void HandleSkipClicked(LayerRewardItem item)
    {
        if (claimInProgress)
        {
            return;
        }

        CompleteClaim(item);
    }

    private void CompleteClaim(LayerRewardItem item)
    {
        if (item != null)
        {
            items.Remove(item);
            Destroy(item.gameObject);
        }

        claimInProgress = false;

        if (items.Count == 0)
        {
            SetVisible(false);
            onAllClaimed?.Invoke();
            return;
        }

        SetItemsInteractable(true);
    }

    private void UpdateStatus()
    {
        if (player == null)
        {
            return;
        }

        if (hpText != null)
        {
            hpText.text = string.Format(hpFormat, player.currentHP, player.maxHP);
        }

        if (currencyText != null)
        {
            currencyText.text = string.Format(currencyFormat, player.currency);
        }
    }

    private void SetItemsInteractable(bool interactable)
    {
        foreach (var item in items)
        {
            if (item != null)
            {
                item.SetInteractable(interactable);
            }
        }
    }

    private void ClearItems()
    {
        foreach (var item in items)
        {
            if (item != null)
            {
                Destroy(item.gameObject);
            }
        }

        items.Clear();
    }

    private void PrepareTemplate()
    {
        if (rewardItemPrefab == null)
        {
            return;
        }

        var scene = rewardItemPrefab.gameObject.scene;
        bool isSceneObject = scene.IsValid() && scene.isLoaded;
        if (isSceneObject)
        {
            rewardItemPrefab.gameObject.SetActive(false);
        }
    }

    private Transform ResolveRewardRoot()
    {
        if (rewardRoot == null)
        {
            if (rewardItemPrefab != null && rewardItemPrefab.transform.parent != null)
            {
                return rewardItemPrefab.transform.parent;
            }

            return null;
        }

        ScrollRect scrollRect = rewardRoot.GetComponent<ScrollRect>();
        if (scrollRect != null && scrollRect.content != null)
        {
            return scrollRect.content;
        }

        return rewardRoot;
    }
}
