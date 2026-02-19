using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CardView : MonoBehaviour
{
    [SerializeField] private Transform linesContainer;
    [SerializeField] private GameObject lineTextPrefab;
    [SerializeField] private LineColorTheme colorTheme;
    [SerializeField] private bool showDescription = true;
    [SerializeField] private int lineFontSize = 36;
    [SerializeField] private int descriptionFontSize = 24;
    [SerializeField] private bool useLineColors = true;
    [SerializeField] private Color defaultLineColor = Color.white;

    public void RenderCard(CardData data)
    {
        RenderCard(data, null, showDescription);
    }

    public void RenderCard(CardData data, Dictionary<LineData, LineCategory> categories, bool showDescriptionOverride)
    {
        RenderCardInternal(data, categories, showDescriptionOverride);
    }

    private void RenderCardInternal(CardData data, Dictionary<LineData, LineCategory> categories, bool showDescriptionOverride)
    {
        if (linesContainer == null)
        {
            return;
        }

        Clear();
        EnsureTemplateHidden();

        if (lineTextPrefab == null)
        {
            return;
        }

        if (data == null || data.lines == null)
        {
            return;
        }

        int lineNumber = 0;
        for (int i = 0; i < data.lines.Count; i++)
        {
            LineData line = data.lines[i];
            GameObject textObj = Instantiate(lineTextPrefab, linesContainer);
            textObj.SetActive(true);
            TextMeshProUGUI textComp = textObj.GetComponent<TextMeshProUGUI>();
            if (textComp == null)
            {
                continue;
            }

            textComp.enabled = true;
            textComp.color = ResolveColor(line, categories);

            string displayText = line != null ? line.GetDisplayText() : "None";
            if (IsTitleLine(line))
            {
                lineNumber = 0;
            }
            else
            {
                lineNumber++;
                displayText = lineNumber + ". " + displayText;
            }
            if (lineFontSize > 0)
            {
                textComp.fontSize = lineFontSize;
            }
            if (showDescriptionOverride && !string.IsNullOrEmpty(line.description))
            {
                if (descriptionFontSize > 0)
                {
                    displayText += "\n<size=" + descriptionFontSize + ">" + line.description + "</size>";
                }
                else
                {
                    displayText += "\n" + line.description;
                }
            }

            textComp.text = displayText;
        }
    }

    public void Clear()
    {
        if (linesContainer == null)
        {
            return;
        }

        foreach (Transform child in linesContainer)
        {
            if (lineTextPrefab != null && child.gameObject == lineTextPrefab)
            {
                continue;
            }

            Destroy(child.gameObject);
        }
    }

    private void EnsureTemplateHidden()
    {
        if (lineTextPrefab == null)
        {
            return;
        }

        if (lineTextPrefab.transform.parent == linesContainer)
        {
            lineTextPrefab.SetActive(false);
        }
    }

    private Color ResolveColor(LineData line, Dictionary<LineData, LineCategory> categories)
    {
        if (!useLineColors)
        {
            return defaultLineColor;
        }

        if (line == null)
        {
            return Color.white;
        }

        if (categories != null && categories.TryGetValue(line, out LineCategory category))
        {
            return colorTheme != null ? colorTheme.GetColor(category) : Color.white;
        }

        if (colorTheme != null)
        {
            return colorTheme.GetColor(GetCategoryByScore(line.valueScore, false));
        }

        return GetColorByType(line.GetEffectType());
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

    private static Color GetColorByType(EffectType type)
    {
        switch (type)
        {
            case EffectType.Buff:
                return new Color(0.2f, 0.7f, 0.2f);
            case EffectType.Debuff:
                return new Color(0.9f, 0.6f, 0.1f);
            case EffectType.Curse:
                return new Color(0.7f, 0.2f, 0.2f);
            default:
                return Color.white;
        }
    }

    private static bool IsTitleLine(LineData line)
    {
        if (line == null)
        {
            return false;
        }

        return string.IsNullOrEmpty(line.lineID) && string.IsNullOrEmpty(line.effectId);
    }
}
