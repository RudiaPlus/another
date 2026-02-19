using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardSelectionItem : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private CardView cardView;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private GameObject cursor;
    [SerializeField] private Image background;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = new Color(1f, 0.85f, 0.3f);

    public void Bind(CardData card, Action onClick)
    {
        if (nameText != null)
        {
            nameText.text = card != null ? card.name : string.Empty;
        }

        if (costText != null)
        {
            costText.text = card != null ? card.baseCost.ToString() : string.Empty;
        }

        if (cardView != null)
        {
            cardView.RenderCard(card);
        }

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            if (onClick != null)
            {
                button.onClick.AddListener(() => onClick());
            }
        }
    }

    public void SetSelected(bool selected)
    {
        if (cursor != null)
        {
            cursor.SetActive(selected);
        }

        if (background != null)
        {
            background.color = selected ? selectedColor : normalColor;
        }
    }

    public void SetActive(bool active)
    {
        gameObject.SetActive(active);
    }
}
