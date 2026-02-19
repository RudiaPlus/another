using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopUI : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private TMP_Text currencyText;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private TMP_Text resetLabelText;
    [SerializeField] private Transform itemRoot;
    [SerializeField] private ShopItemView itemPrefab;
    [SerializeField] private string title = "ショップ";
    [SerializeField] private string hpFormat = "HP {0}/{1}";
    [SerializeField] private string currencyFormat = "通貨 {0}";
    [SerializeField] private string resetLabelFormat = "商品リセット ({0})";
    [SerializeField] private int itemCost = 10;
    [SerializeField] private int resetCost = 5;
    [SerializeField] private int healAmount = 50;

    private readonly Dictionary<ShopItemType, bool> soldOut = new Dictionary<ShopItemType, bool>();
    [SerializeField] private List<ShopItemType> itemOrder = new List<ShopItemType>();
    private readonly List<ShopItemView> activeItems = new List<ShopItemView>();

    private PlayerData player;
    private Action<ShopItemType, int, Action<bool>> onPurchase;
    private Func<int, bool> onReset;
    private Action onClosed;
    private bool isBusy;
    private int currentItemCost;
    private int currentResetCost;
    private int currentHealAmount;

    private void Awake()
    {
        SetVisible(false);
        EnsureTemplateHidden();
    }

    public void Show(PlayerData player, int itemCost, int resetCost, int healAmount, Action<ShopItemType, int, Action<bool>> onPurchase, Func<int, bool> onReset, Action onClosed)
    {
        this.player = player;
        this.onPurchase = onPurchase;
        this.onReset = onReset;
        this.onClosed = onClosed;
        isBusy = false;
        currentItemCost = itemCost > 0 ? itemCost : this.itemCost;
        currentResetCost = resetCost > 0 ? resetCost : this.resetCost;
        currentHealAmount = healAmount > 0 ? healAmount : this.healAmount;

        if (titleText != null)
        {
            titleText.text = title;
        }

        BuildDefaultItemOrder();
        ResetSoldOut();
        BindButtons();
        RefreshStatus();
        RefreshItems();

        SetVisible(true);
    }

    public void Hide()
    {
        SetVisible(false);
    }

    public void Refresh()
    {
        RefreshStatus();
        RefreshItems();
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

    private void BindButtons()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(Close);
        }

        if (resetButton != null)
        {
            resetButton.onClick.RemoveAllListeners();
            resetButton.onClick.AddListener(HandleReset);
        }
    }

    private void RefreshStatus()
    {
        if (player != null)
        {
            if (hpText != null)
            {
                hpText.text = string.Format(hpFormat, player.currentHP, player.maxHP);
            }

            if (currencyText != null)
            {
                currencyText.text = string.Format(currencyFormat, player.currency);
            }
        }

        if (resetLabelText != null)
        {
            resetLabelText.text = string.Format(resetLabelFormat, currentResetCost);
        }
    }

    private void RefreshItems()
    {
        EnsureItemsBuilt();
        for (int i = 0; i < activeItems.Count; i++)
        {
            ShopItemView view = activeItems[i];
            if (view == null || i >= itemOrder.Count)
            {
                continue;
            }

            ShopItemType type = itemOrder[i];
            bool isSoldOut = soldOut.TryGetValue(type, out bool value) && value;
            view.Setup(type, GetTitle(type), GetDescription(type), currentItemCost, HandleItemClick);
            view.SetSoldOut(isSoldOut);
            view.SetInteractable(!isBusy && !isSoldOut && CanAfford(currentItemCost));
        }

        if (resetButton != null)
        {
            resetButton.interactable = !isBusy && CanAfford(currentResetCost);
        }
    }

    private void HandleItemClick(ShopItemType type)
    {
        if (isBusy)
        {
            return;
        }

        if (!CanAfford(currentItemCost))
        {
            return;
        }

        isBusy = true;
        RefreshItems();

        // Hide shop for complex interactions (everything except simple Heal)
        bool hideShop = type != ShopItemType.Heal;
        if (hideShop)
        {
            SetVisible(false);
        }

        if (onPurchase != null)
        {
            onPurchase(type, currentItemCost, success =>
            {
                if (success)
                {
                    soldOut[type] = true;
                }
                isBusy = false;
                
                if (hideShop)
                {
                    SetVisible(true);
                }

                RefreshStatus();
                RefreshItems();
            });
            return;
        }

        isBusy = false;
        RefreshItems();
    }

    private void HandleReset()
    {
        if (isBusy)
        {
            return;
        }

        if (!CanAfford(resetCost))
        {
            return;
        }

        if (onReset != null && onReset(currentResetCost))
        {
            ResetSoldOut();
            RefreshStatus();
            RefreshItems();
        }
    }

    private void ResetSoldOut()
    {
        soldOut.Clear();
        foreach (var type in itemOrder)
        {
            soldOut[type] = false;
        }
    }

    private void EnsureTemplateHidden()
    {
        if (itemPrefab == null)
        {
            return;
        }

        if (itemRoot != null && itemPrefab.transform.parent == itemRoot)
        {
            itemPrefab.gameObject.SetActive(false);
        }
    }

    private bool CanAfford(int cost)
    {
        return player != null && player.currency >= cost;
    }

    private void Close()
    {
        SetVisible(false);
        onClosed?.Invoke();
    }

    private void BuildDefaultItemOrder()
    {
        if (itemOrder.Count > 0)
        {
            return;
        }

        itemOrder.Add(ShopItemType.Heal);
        itemOrder.Add(ShopItemType.Enhancement);
        itemOrder.Add(ShopItemType.LineSwap);
        itemOrder.Add(ShopItemType.LineRemove);
    }

    private void EnsureItemsBuilt()
    {
        Transform spawnRoot = ResolveItemRoot();
        if (spawnRoot == null || itemPrefab == null)
        {
            return;
        }

        if (activeItems.Count > 0)
        {
            return;
        }

        EnsureTemplateHidden(spawnRoot);

        for (int i = 0; i < itemOrder.Count; i++)
        {
            ShopItemView view = Instantiate(itemPrefab, spawnRoot);
            if (view == null)
            {
                continue;
            }

            view.gameObject.SetActive(true);
            activeItems.Add(view);
        }
    }

    private void EnsureTemplateHidden(Transform spawnRoot)
    {
        if (itemPrefab == null || spawnRoot == null)
        {
            return;
        }

        if (itemPrefab.transform.parent == spawnRoot)
        {
            itemPrefab.gameObject.SetActive(false);
        }
    }

    private Transform ResolveItemRoot()
    {
        if (itemRoot == null)
        {
            if (itemPrefab != null && itemPrefab.transform.parent != null)
            {
                return itemPrefab.transform.parent;
            }

            return null;
        }

        ScrollRect scrollRect = itemRoot.GetComponent<ScrollRect>();
        if (scrollRect != null && scrollRect.content != null)
        {
            return scrollRect.content;
        }

        return itemRoot;
    }

    private static string GetTitle(ShopItemType type)
    {
        switch (type)
        {
            case ShopItemType.Heal:
                return "回復";
            case ShopItemType.Enhancement:
                return "構文付加";
            case ShopItemType.LineSwap:
                return "文校正";
            case ShopItemType.LineRemove:
                return "文削除";
            default:
                return "商品";
        }
    }

    private string GetDescription(ShopItemType type)
    {
        switch (type)
        {
            case ShopItemType.Heal:
                return "HPを" + currentHealAmount + "回復します。";
            case ShopItemType.Enhancement:
                return "構文を強化します。";
            case ShopItemType.LineSwap:
                return "構文の行を2つ入れ替えます。";
            case ShopItemType.LineRemove:
                return "構文から効果を1つ削除します。";
            default:
                return string.Empty;
        }
    }
}
