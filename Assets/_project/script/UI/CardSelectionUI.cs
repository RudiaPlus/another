using System;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class CardSelectionUI : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private Transform contentRoot;
    [SerializeField] private CardSelectionItem itemPrefab;

    private readonly List<CardSelectionItem> items = new List<CardSelectionItem>();
    private IReadOnlyList<CardData> currentHand;
    private Action<int> onSelect;
    private int selectedIndex;
    private bool visible;

    private void Awake()
    {
        Hide();
    }

    private void Update()
    {
        if (!visible || currentHand == null || currentHand.Count == 0)
        {
            return;
        }

#if ENABLE_INPUT_SYSTEM
        var keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame)
        {
            MoveSelection(-1);
        }

        if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame)
        {
            MoveSelection(1);
        }

        if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)
        {
            ConfirmSelection();
        }
#endif
    }

    public void Show(IReadOnlyList<CardData> hand, Action<int> onSelected)
    {
        currentHand = hand;
        onSelect = onSelected;
        selectedIndex = 0;
        visible = true;

        EnsureItems(hand);
        RefreshItems();

        SetRootActive(true);
    }

    public void Hide()
    {
        visible = false;
        SetRootActive(false);
    }

    private void EnsureItems(IReadOnlyList<CardData> hand)
    {
        if (hand == null)
        {
            return;
        }

        if (items.Count == 0 && itemPrefab == null)
        {
            items.AddRange(GetComponentsInChildren<CardSelectionItem>(true));
        }

        if (itemPrefab != null && contentRoot != null)
        {
            while (items.Count < hand.Count)
            {
                CardSelectionItem item = Instantiate(itemPrefab, contentRoot);
                items.Add(item);
            }
        }
    }

    private void RefreshItems()
    {
        if (currentHand == null)
        {
            return;
        }

        for (int i = 0; i < items.Count; i++)
        {
            if (i < currentHand.Count)
            {
                int index = i;
                CardSelectionItem item = items[i];
                item.SetActive(true);
                item.Bind(currentHand[i], () => SelectIndex(index, true));
                item.SetSelected(i == selectedIndex);
            }
            else
            {
                items[i].SetActive(false);
            }
        }
    }

    private void MoveSelection(int delta)
    {
        if (currentHand == null || currentHand.Count == 0)
        {
            return;
        }

        selectedIndex = (selectedIndex + delta) % currentHand.Count;
        if (selectedIndex < 0)
        {
            selectedIndex += currentHand.Count;
        }

        UpdateSelectionVisuals();
    }

    private void ConfirmSelection()
    {
        if (currentHand == null || currentHand.Count == 0)
        {
            return;
        }

        SelectIndex(selectedIndex, true);
    }

    private void SelectIndex(int index, bool confirm)
    {
        if (currentHand == null || currentHand.Count == 0)
        {
            return;
        }

        selectedIndex = Mathf.Clamp(index, 0, currentHand.Count - 1);
        UpdateSelectionVisuals();

        if (confirm && onSelect != null)
        {
            onSelect(selectedIndex);
        }
    }

    private void UpdateSelectionVisuals()
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] != null)
            {
                items[i].SetSelected(i == selectedIndex);
            }
        }
    }

    private void SetRootActive(bool active)
    {
        if (root != null)
        {
            root.SetActive(active);
        }
        else
        {
            gameObject.SetActive(active);
        }
    }
}
