using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LineRemoveUI : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Transform lineRoot;
    [SerializeField] private LineSelectionItem lineItemPrefab;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button skipButton;
    [SerializeField] private string titleFormat = "行削除: {0}";
    [SerializeField] private CardView previewCardView;

    private readonly List<LineSelectionItem> items = new List<LineSelectionItem>();
    private CardData currentCard;
    private int selectedIndex = -1;
    private Action onComplete;

    private void Awake()
    {
        SetVisible(false);
        EnsureTemplateHidden();
    }

    public void Show(CardData card, Action onComplete)
    {
        currentCard = card;
        this.onComplete = onComplete;
        selectedIndex = -1;
        ClearItems();

        if (titleText != null)
        {
            string name = card != null ? card.name : "Card";
            titleText.text = string.Format(titleFormat, name);
        }

        if (previewCardView != null)
        {
            previewCardView.Clear();
            if (card != null)
            {
                previewCardView.RenderCard(card);
            }
        }

        Transform spawnRoot = ResolveLineRoot();
        if (spawnRoot == null || lineItemPrefab == null || card == null || card.lines == null)
        {
            SetVisible(false);
            onComplete?.Invoke();
            return;
        }

        EnsureTemplateHidden();

        for (int i = 0; i < card.lines.Count; i++)
        {
            LineSelectionItem item = Instantiate(lineItemPrefab, spawnRoot);
            if (item == null)
            {
                continue;
            }

            item.gameObject.SetActive(true);
            item.Setup(card.lines[i], i, HandleLineSelected);
            items.Add(item);
        }

        UpdateConfirmState();
        SetVisible(true);
    }

    public void Hide()
    {
        SetVisible(false);
    }

    private void HandleLineSelected(LineSelectionItem item)
    {
        if (item == null || currentCard == null || currentCard.lines == null)
        {
            return;
        }

        selectedIndex = item.Index;
        RefreshSelection();
        UpdateConfirmState();
    }

    public void ConfirmRemove()
    {
        if (currentCard == null || currentCard.lines == null || selectedIndex < 0 || selectedIndex >= currentCard.lines.Count)
        {
            return;
        }

        currentCard.lines.RemoveAt(selectedIndex);
        if (previewCardView != null)
        {
            previewCardView.Clear();
            previewCardView.RenderCard(currentCard);
        }

        Close();
    }

    public void Skip()
    {
        Close();
    }

    private void Close()
    {
        SetVisible(false);
        onComplete?.Invoke();
    }

    private void UpdateConfirmState()
    {
        if (confirmButton != null)
        {
            confirmButton.interactable = selectedIndex >= 0;
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(ConfirmRemove);
        }

        if (skipButton != null)
        {
            skipButton.onClick.RemoveAllListeners();
            skipButton.onClick.AddListener(Skip);
        }
    }

    private void RefreshSelection()
    {
        for (int i = 0; i < items.Count; i++)
        {
            LineSelectionItem item = items[i];
            if (item == null)
            {
                continue;
            }

            item.SetSelected(item.Index == selectedIndex);
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

    private void EnsureTemplateHidden()
    {
        if (lineItemPrefab == null)
        {
            return;
        }

        Transform spawnRoot = ResolveLineRoot();
        if (spawnRoot != null && lineItemPrefab.transform.parent == spawnRoot)
        {
            lineItemPrefab.gameObject.SetActive(false);
        }
    }

    private Transform ResolveLineRoot()
    {
        if (lineRoot == null)
        {
            if (lineItemPrefab != null && lineItemPrefab.transform.parent != null)
            {
                return lineItemPrefab.transform.parent;
            }

            return null;
        }

        ScrollRect scrollRect = lineRoot.GetComponent<ScrollRect>();
        if (scrollRect != null && scrollRect.content != null)
        {
            return scrollRect.content;
        }

        return lineRoot;
    }

    private void SetVisible(bool visible)
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
}
