using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PostBattleEnhancementUI : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text previewTitleText;
    [SerializeField] private CardView previewCardView;
    [SerializeField] private bool previewShowDescription;
    [SerializeField] private EnhancementOptionButton[] optionButtons;

    private Action<EnhancementOption> onSelect;
    private List<EnhancementOption> currentOptions;
    private CardData currentCard;
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

    public void Show(CardData card, List<EnhancementOption> options, Action<EnhancementOption> onSelected)
    {
        currentCard = card;
        currentOptions = options ?? new List<EnhancementOption>();
        onSelect = onSelected;
        selectedIndex = 0;
        visible = true;

        EnsurePreviewView();
        if (previewCardView != null)
        {
            previewCardView.Clear();
        }

        int baseLineCount = card != null && card.lines != null ? card.lines.Count : 0;
        foreach (EnhancementOption option in currentOptions)
        {
            EnsureInsertIndices(option, baseLineCount);
        }

        int buttonCount = optionButtons != null ? optionButtons.Length : 0;
        visibleOptionCount = Mathf.Min(buttonCount, currentOptions.Count);

        if (titleText != null)
        {
            titleText.text = card != null ? "構文付加: " + card.name : "構文付加";
        }

        if (previewTitleText != null)
        {
            previewTitleText.text = card != null ? card.name : string.Empty;
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
                    EnhancementOption option = currentOptions[i];
                    string title = BuildOptionTitle(option);
                    string effect = BuildOptionEffectText(option);

                    button.SetActive(true);
                    int index = i;
                    button.Bind(title, effect, () => SelectOption(option), () => HoverOption(index));
                    button.SetSelected(i == selectedIndex);
                }
                else
                {
                    button.SetActive(false);
                }
            }
        }

        UpdatePreview();
        SetRootActive(true);
    }

    public void Hide()
    {
        visible = false;
        SetRootActive(false);
    }

    private void SelectOption(EnhancementOption option)
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
        UpdatePreview();
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
        UpdatePreview();
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

    private void UpdatePreview()
    {
        if (currentCard == null || currentOptions == null)
        {
            if (previewCardView != null)
            {
                previewCardView.Clear();
            }
            return;
        }

        EnsurePreviewView();
        if (previewCardView == null)
        {
            return;
        }

        int optionCount = currentOptions.Count;
        if (optionCount <= 0)
        {
            previewCardView.Clear();
            return;
        }

        selectedIndex = Mathf.Clamp(selectedIndex, 0, optionCount - 1);
        EnhancementOption option = currentOptions[selectedIndex];
        CardData previewCard = BuildPreviewCard(currentCard, option);
        Dictionary<LineData, LineCategory> categories = BuildPreviewCategories(previewCard, option);
        previewCardView.RenderCard(previewCard, categories, previewShowDescription);
    }

    private static string BuildOptionTitle(EnhancementOption option)
    {
        if (option == null)
        {
            return "なし";
        }

        string plus = option.plusLine != null ? option.plusLine.GetDisplayText() : "なし";
        string minus = option.minusLine != null ? option.minusLine.GetDisplayText() : "なし";
        return plus + "/" + minus;
    }

    private static string BuildOptionEffectText(EnhancementOption option)
    {
        if (option == null)
        {
            return "なし";
        }

        string plus = BuildLineDescription(option.plusLine);
        string minus = BuildLineDescription(option.minusLine);
        return "良い効果: " + plus + "\n悪い効果: " + minus;
    }

    private static string BuildLineDescription(LineData line)
    {
        if (line == null)
        {
            return "なし";
        }

        if (!string.IsNullOrEmpty(line.description))
        {
            return line.description;
        }

        return line.GetDisplayText();
    }

    private static CardData BuildPreviewCard(CardData baseCard, EnhancementOption option)
    {
        if (baseCard == null)
        {
            return null;
        }

        CardData clone = new CardData
        {
            uid = baseCard.uid,
            name = baseCard.name,
            baseCost = baseCard.baseCost,
            lines = new List<LineData>()
        };

        if (baseCard.lines != null)
        {
            foreach (LineData line in baseCard.lines)
            {
                clone.lines.Add(CloneLine(line));
            }
        }

        CardEnhancementSystem.ApplyOption(clone, option);
        return clone;
    }

    private static LineData CloneLine(LineData source)
    {
        if (source == null)
        {
            return null;
        }

        LineData clone = new LineData
        {
            effectId = source.effectId,
            lineID = source.lineID,
            displayName = source.displayName,
            displayPhrase = source.displayPhrase,
            displayValueText = source.displayValueText,
            description = source.description,
            type = source.type,
            valueScore = source.valueScore,
            paramsInt = new List<LineParamInt>(),
            paramsStr = new List<LineParamStr>()
        };

        if (source.paramsInt != null)
        {
            foreach (LineParamInt param in source.paramsInt)
            {
                clone.paramsInt.Add(new LineParamInt { key = param.key, value = param.value });
            }
        }

        if (source.paramsStr != null)
        {
            foreach (LineParamStr param in source.paramsStr)
            {
                clone.paramsStr.Add(new LineParamStr { key = param.key, value = param.value });
            }
        }

        return clone;
    }

    private static void EnsureInsertIndices(EnhancementOption option, int baseLineCount)
    {
        if (option == null)
        {
            return;
        }

        if (option.plusInsertIndex < 0)
        {
            option.plusInsertIndex = UnityEngine.Random.Range(0, baseLineCount + 1);
        }

        if (option.minusInsertIndex < 0)
        {
            option.minusInsertIndex = UnityEngine.Random.Range(0, baseLineCount + 1);
        }
    }

    private static Dictionary<LineData, LineCategory> BuildPreviewCategories(CardData previewCard, EnhancementOption option)
    {
        Dictionary<LineData, LineCategory> categories = new Dictionary<LineData, LineCategory>();
        if (previewCard == null || previewCard.lines == null)
        {
            return categories;
        }

        foreach (LineData line in previewCard.lines)
        {
            bool isAdded = option != null && (ReferenceEquals(line, option.plusLine) || ReferenceEquals(line, option.minusLine));
            categories[line] = GetCategoryByScore(line != null ? line.valueScore : 0, isAdded);
        }

        return categories;
    }

    private static LineCategory GetCategoryByScore(int valueScore, bool added)
    {
        if (valueScore > 0)
        {
            return added ? LineCategory.AddedGood : LineCategory.ExistingGood;
        }

        if (valueScore < 0)
        {
            return added ? LineCategory.AddedBad : LineCategory.ExistingBad;
        }

        return added ? LineCategory.AddedNeutral : LineCategory.ExistingNeutral;
    }

    private void EnsurePreviewView()
    {
        if (previewCardView != null)
        {
            return;
        }

        Transform searchRoot = root != null ? root.transform : transform;
        previewCardView = searchRoot.GetComponentInChildren<CardView>(true);
    }
}
