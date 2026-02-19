using UnityEngine;

public class LineColorTheme : MonoBehaviour
{
    [SerializeField] private Color existingGoodColor = new Color(0.2f, 0.7f, 0.2f);
    [SerializeField] private Color existingBadColor = new Color(0.85f, 0.35f, 0.35f);
    [SerializeField] private Color existingNeutralColor = new Color(0.85f, 0.85f, 0.85f);
    [SerializeField] private Color addedGoodColor = new Color(1f, 0.85f, 0.2f);
    [SerializeField] private Color addedBadColor = new Color(0.95f, 0.5f, 0.2f);
    [SerializeField] private Color addedNeutralColor = new Color(0.9f, 0.9f, 0.9f);

    public Color GetColor(LineCategory category)
    {
        switch (category)
        {
            case LineCategory.AddedGood:
                return addedGoodColor;
            case LineCategory.AddedBad:
                return addedBadColor;
            case LineCategory.AddedNeutral:
                return addedNeutralColor;
            case LineCategory.ExistingBad:
                return existingBadColor;
            case LineCategory.ExistingNeutral:
                return existingNeutralColor;
            case LineCategory.ExistingGood:
            default:
                return existingGoodColor;
        }
    }
}
