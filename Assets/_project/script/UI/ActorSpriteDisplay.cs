using UnityEngine;
using UnityEngine.UI;

public class ActorSpriteDisplay : MonoBehaviour
{
    [SerializeField] private Image image;
    [SerializeField] private SpriteRenderer spriteRenderer;

    public void SetSprite(Sprite sprite)
    {
        if (image != null)
        {
            image.sprite = sprite;
            image.enabled = sprite != null;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = sprite;
            spriteRenderer.enabled = sprite != null;
        }
    }

    public void SetVisible(bool visible)
    {
        if (image != null)
        {
            image.enabled = visible && image.sprite != null;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = visible && spriteRenderer.sprite != null;
        }
    }
}
