using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LineCalibrationUI : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Transform lineRoot;
    [SerializeField] private LineSelectionItem lineItemPrefab;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button skipButton;
    [SerializeField] private string titleFormat = "行校正: {0}";
    [SerializeField] private CardView previewCardView;

    private readonly List<LineSelectionItem> items = new List<LineSelectionItem>();
    private readonly List<int> selectedIndices = new List<int>();
    private CardData currentCard;
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
        selectedIndices.Clear();
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
            LineData line = card.lines[i];
            LineSelectionItem item = Instantiate(lineItemPrefab, spawnRoot);
            if (item == null)
            {
                continue;
            }

            item.gameObject.SetActive(true);
            item.Setup(line, i, HandleLineSelected);
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

        int index = item.Index;
        if (selectedIndices.Contains(index))
        {
            selectedIndices.Remove(index);
        }
        else
        {
            if (selectedIndices.Count >= 2)
            {
                selectedIndices.RemoveAt(0);
            }
            selectedIndices.Add(index);
        }

        RefreshSelection();
        UpdateConfirmState();
    }

    public void ConfirmSwap()
    {
        if (currentCard == null || currentCard.lines == null || selectedIndices.Count != 2)
        {
            return;
        }

        int indexA = selectedIndices[0];
        int indexB = selectedIndices[1];
        if (indexA == indexB)
        {
            return;
        }

        LineData temp = currentCard.lines[indexA];
        currentCard.lines[indexA] = currentCard.lines[indexB];
        currentCard.lines[indexB] = temp;

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
            confirmButton.interactable = selectedIndices.Count == 2;
        }

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(ConfirmSwap);
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

            item.SetSelected(selectedIndices.Contains(item.Index));
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
