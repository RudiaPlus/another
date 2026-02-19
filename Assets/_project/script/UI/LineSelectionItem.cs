using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LineSelectionItem : MonoBehaviour
{
    [SerializeField] private TMP_Text lineText;
    [SerializeField] private Button selectButton;
    [SerializeField] private Image selectionHighlight;
    [SerializeField] private LineColorTheme colorTheme;
    [SerializeField] private bool useLineColors = true;
    [SerializeField] private Color defaultLineColor = Color.white;
    [SerializeField] private int lineFontSize = 36;
    [SerializeField] private int descriptionFontSize = 24;
    [SerializeField] private bool useSelectionColor = false;
    [SerializeField] private Color selectedColor = new Color(1f, 0.85f, 0.2f);
    [SerializeField] private bool useHighlightColor = true;
    [SerializeField] private Color highlightColor = new Color(1f, 1f, 0.6f, 0.2f);

    public LineData Line { get; private set; }
    public int Index { get; private set; }

    private Action<LineSelectionItem> onSelected;
    private Color baseColor = Color.white;

    public void Setup(LineData line, int index, Action<LineSelectionItem> onSelected)
    {
        Line = line;
        Index = index;
        this.onSelected = onSelected;

        if (lineText != null)
        {
            string text = line != null ? line.GetDisplayText() : "None";
            text = (index + 1) + ". " + text;
            if (line != null && !string.IsNullOrEmpty(line.description))
            {
                if (descriptionFontSize > 0)
                {
                    text += "\n<size=" + descriptionFontSize + ">" + line.description + "</size>";
                }
                else
                {
                    text += "\n" + line.description;
                }
            }
            lineText.text = text;
            if (lineFontSize > 0)
            {
                lineText.fontSize = lineFontSize;
            }
        }

        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(HandleClick);
        }

        if (selectionHighlight != null)
        {
            if (useHighlightColor)
            {
                selectionHighlight.color = highlightColor;
            }
            selectionHighlight.raycastTarget = false;
        }

        baseColor = ResolveColor(line);
        if (lineText != null)
        {
            lineText.color = baseColor;
        }

        SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        if (selectionHighlight != null)
        {
            selectionHighlight.enabled = selected;
        }

        if (lineText != null && useSelectionColor)
        {
            lineText.color = selected ? selectedColor : baseColor;
        }
    }

    private void HandleClick()
    {
        onSelected?.Invoke(this);
    }

    private Color ResolveColor(LineData line)
    {
        if (!useLineColors)
        {
            return defaultLineColor;
        }

        if (line == null)
        {
            return defaultLineColor;
        }

        LineCategory category = GetCategoryByScore(line.valueScore);
        if (colorTheme != null)
        {
            return colorTheme.GetColor(category);
        }

        return category == LineCategory.ExistingGood
            ? new Color(0.2f, 0.7f, 0.2f)
            : category == LineCategory.ExistingBad
                ? new Color(0.85f, 0.35f, 0.35f)
                : defaultLineColor;
    }

    private static LineCategory GetCategoryByScore(int valueScore)
    {
        if (valueScore > 0)
        {
            return LineCategory.ExistingGood;
        }

        if (valueScore < 0)
        {
            return LineCategory.ExistingBad;
        }

        return LineCategory.ExistingNeutral;
    }
}
