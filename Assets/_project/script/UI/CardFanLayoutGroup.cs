using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class CardFanLayoutGroup : LayoutGroup
{
    [SerializeField] private float radius = 420f;
    [SerializeField] private float maxAngle = 40f;
    [SerializeField] private float anglePerCard = 8f;
    [SerializeField] private float verticalOffset = 0f;
    [SerializeField] private float rotationFactor = 1f;
    [SerializeField] private bool alignToBottom = true;

    public override void CalculateLayoutInputHorizontal()
    {
        base.CalculateLayoutInputHorizontal();
    }

    public override void CalculateLayoutInputVertical()
    {
    }

    public override void SetLayoutHorizontal()
    {
        Arrange();
    }

    public override void SetLayoutVertical()
    {
        Arrange();
    }

    private void Arrange()
    {
        int count = rectChildren.Count;
        if (count == 0)
        {
            return;
        }

        float totalAngle = Mathf.Min(maxAngle, anglePerCard * Mathf.Max(0, count - 1));
        float startAngle = -totalAngle * 0.5f;
        float step = count > 1 ? totalAngle / (count - 1) : 0f;

        for (int i = 0; i < count; i++)
        {
            RectTransform child = rectChildren[i];
            float angle = startAngle + step * i;
            float rad = angle * Mathf.Deg2Rad;
            float x = Mathf.Sin(rad) * radius;
            float y = (1f - Mathf.Cos(rad)) * radius + verticalOffset;
            if (!alignToBottom)
            {
                y = -y;
            }

            child.anchoredPosition = new Vector2(x, y);
            child.localRotation = Quaternion.Euler(0f, 0f, -angle * rotationFactor);
        }
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        SetDirty();
    }

    private void SetDirty()
    {
        if (!IsActive())
        {
            return;
        }

        LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
    }
#endif
}
