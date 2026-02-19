using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PostBattleRewardUI : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private EnhancementOptionButton[] optionButtons;

    private Action<RewardOption> onSelect;
    private List<RewardOption> currentOptions;
    private int selectedIndex;
    private int visibleOptionCount;
    private bool visible;

    private void Awake()
    {
        Hide();
    }

    private void Update()
    {
        if (!visible || visibleOptionCount <= 0)
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
            SelectCurrent();
        }
#endif
    }

    public void Show(List<RewardOption> options, Action<RewardOption> onSelected)
    {
        currentOptions = options ?? new List<RewardOption>();
        onSelect = onSelected;
        selectedIndex = 0;
        visible = true;

        int buttonCount = optionButtons != null ? optionButtons.Length : 0;
        visibleOptionCount = Mathf.Min(buttonCount, currentOptions.Count);

        if (titleText != null)
        {
            titleText.text = "報酬選択";
        }

        if (optionButtons != null)
        {
            for (int i = 0; i < optionButtons.Length; i++)
            {
                EnhancementOptionButton button = optionButtons[i];
                if (button == null)
                {
                    continue;
                }

                if (i < visibleOptionCount)
                {
                    RewardOption option = currentOptions[i];
                    string title = option != null ? option.title : "なし";
                    string description = option != null ? option.description : string.Empty;

                    button.SetActive(true);
                    int index = i;
                    button.Bind(title, description, () => SelectOption(option), () => HoverOption(index));
                    button.SetSelected(i == selectedIndex);
                }
                else
                {
                    button.SetActive(false);
                }
            }
        }

        SetRootActive(true);
    }

    public void Hide()
    {
        visible = false;
        SetRootActive(false);
    }

    private void SelectOption(RewardOption option)
    {
        if (onSelect != null)
        {
            onSelect(option);
        }

        Hide();
    }

    private void MoveSelection(int delta)
    {
        int optionCount = currentOptions != null ? currentOptions.Count : 0;
        if (optionCount <= 0)
        {
            return;
        }

        selectedIndex = (selectedIndex + delta) % optionCount;
        if (selectedIndex < 0)
        {
            selectedIndex += optionCount;
        }

        UpdateSelectionVisuals();
    }

    private void SelectCurrent()
    {
        if (visibleOptionCount <= 0 || currentOptions == null)
        {
            return;
        }

        SelectOption(currentOptions[selectedIndex]);
    }

    private void UpdateSelectionVisuals()
    {
        if (optionButtons == null)
        {
            return;
        }

        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (optionButtons[i] != null)
            {
                optionButtons[i].SetSelected(i == selectedIndex && i < visibleOptionCount);
            }
        }
    }

    private void HoverOption(int index)
    {
        if (!visible || visibleOptionCount <= 0)
        {
            return;
        }

        int clamped = Mathf.Clamp(index, 0, visibleOptionCount - 1);
        if (clamped == selectedIndex)
        {
            return;
        }

        selectedIndex = clamped;
        UpdateSelectionVisuals();
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
