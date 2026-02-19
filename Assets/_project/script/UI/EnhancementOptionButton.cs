using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EnhancementOptionButton : MonoBehaviour, IPointerEnterHandler, ISelectHandler
{
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text label;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text effectText;
    [SerializeField] private GameObject cursor;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = new Color(1f, 0.85f, 0.3f);
    [SerializeField] private RectTransform targetRect;
    [SerializeField] private LayoutElement layoutElement;
    [SerializeField] private bool autoWidth;
    [SerializeField] private Vector2 padding = new Vector2(28f, 16f);
    [SerializeField] private Vector2 minSize = new Vector2(240f, 56f);

    private Action hoverAction;

    public void Bind(string text, Action onClick)
    {
        Bind(text, null, onClick, null);
    }

    public void Bind(string text, Action onClick, Action onHover)
    {
        Bind(text, null, onClick, onHover);
    }

    public void Bind(string title, string effect, Action onClick)
    {
        Bind(title, effect, onClick, null);
    }

    public void Bind(string title, string effect, Action onClick, Action onHover)
    {
        if (titleText != null)
        {
            titleText.text = title;
        }
        else if (label != null)
        {
            label.text = title;
        }

        if (effectText != null)
        {
            effectText.text = effect;
        }

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            if (onClick != null)
            {
                button.onClick.AddListener(() => onClick());
            }
        }

        hoverAction = onHover;
        UpdateSize();
    }

    public void SetActive(bool active)
    {
        gameObject.SetActive(active);
    }

    public void SetSelected(bool selected)
    {
        if (label != null)
        {
            label.color = selected ? selectedColor : normalColor;
        }

        if (titleText != null)
        {
            titleText.color = selected ? selectedColor : normalColor;
        }

        if (effectText != null)
        {
            effectText.color = selected ? selectedColor : normalColor;
        }

        if (cursor != null)
        {
            cursor.SetActive(selected);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hoverAction != null)
        {
            hoverAction();
        }
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (hoverAction != null)
        {
            hoverAction();
        }
    }

    private void Awake()
    {
        if (targetRect == null)
        {
            targetRect = GetComponent<RectTransform>();
        }

        if (layoutElement == null)
        {
            layoutElement = GetComponent<LayoutElement>();
        }
    }

    private void OnValidate()
    {
        UpdateSize();
    }

    private void UpdateSize()
    {
        float titleHeight = GetPreferredHeight(titleText != null ? titleText : label);
        float effectHeight = GetPreferredHeight(effectText);
        float bodyHeight = Mathf.Max(effectHeight, 0f);
        float height = titleHeight + bodyHeight + padding.y;
        height = Mathf.Max(height, minSize.y);

        float width = minSize.x;
        if (autoWidth)
        {
            float titleWidth = GetPreferredWidth(titleText != null ? titleText : label);
            float effectWidth = GetPreferredWidth(effectText);
            float maxWidth = Mathf.Max(titleWidth, effectWidth);
            width = Mathf.Max(maxWidth + padding.x, minSize.x);
        }

        if (layoutElement != null)
        {
            layoutElement.preferredHeight = height;
            if (autoWidth)
            {
                layoutElement.preferredWidth = width;
                layoutElement.flexibleWidth = 0f;
            }
            else
            {
                layoutElement.preferredWidth = -1f;
                layoutElement.flexibleWidth = 1f;
            }
            return;
        }

        if (targetRect == null)
        {
            return;
        }

        if (autoWidth)
        {
            targetRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        }

        targetRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
    }

    private static float GetPreferredHeight(TMP_Text text)
    {
        if (text == null)
        {
            return 0f;
        }

        text.ForceMeshUpdate();
        return text.preferredHeight;
    }

    private static float GetPreferredWidth(TMP_Text text)
    {
        if (text == null)
        {
            return 0f;
        }

        text.ForceMeshUpdate();
        return text.preferredWidth;
    }
}
