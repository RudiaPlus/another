using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopItemView : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private Button buyButton;
    [SerializeField] private GameObject soldOutRoot;
    [SerializeField] private TMP_Text soldOutText;
    [SerializeField] private string costFormat = "通貨 {0}";
    [SerializeField] private string soldOutLabel = "売り切れ";

    private ShopItemType type;
    private Action<ShopItemType> onClick;

    public void Setup(ShopItemType type, string title, string description, int cost, Action<ShopItemType> onClick)
    {
        this.type = type;
        this.onClick = onClick;

        if (titleText != null)
        {
            titleText.text = title;
        }

        if (descriptionText != null)
        {
            descriptionText.text = description;
        }

        if (costText != null)
        {
            costText.text = string.Format(costFormat, cost);
        }

        if (soldOutText != null)
        {
            soldOutText.text = soldOutLabel;
        }

        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(HandleClick);
        }
    }

    public void SetSoldOut(bool soldOut)
    {
        if (soldOutRoot != null)
        {
            soldOutRoot.SetActive(soldOut);
        }
    }

    public void SetInteractable(bool interactable)
    {
        if (buyButton != null)
        {
            buyButton.interactable = interactable;
        }
    }

    private void HandleClick()
    {
        onClick?.Invoke(type);
    }
}
