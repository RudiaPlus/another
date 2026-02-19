using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ActorView : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private TMP_Text shieldText;
    [SerializeField] private Image hpFill;
    [SerializeField] private Image shieldFill;
    [SerializeField] private Image portraitImage;
    [SerializeField] private Color hpTextColor = new Color(0.35f, 0.9f, 0.35f);
    [SerializeField] private Color shieldTextColor = new Color(0.6f, 0.6f, 0.6f);
    [SerializeField] private Transform popupAnchor;

    public Transform PopupAnchor => popupAnchor != null ? popupAnchor : transform;

    public void Initialize(string displayName, int currentHp, int maxHp)
    {
        if (nameText != null)
        {
            nameText.text = displayName;
        }

        UpdateHp(currentHp, maxHp, 0);
    }

    public void Initialize(string displayName, int currentHp, int maxHp, int shield)
    {
        if (nameText != null)
        {
            nameText.text = displayName;
        }

        UpdateHp(currentHp, maxHp, shield);
    }

    public void UpdateHp(int currentHp, int maxHp)
    {
        UpdateHp(currentHp, maxHp, 0);
    }

    public void UpdateHp(int currentHp, int maxHp, int shield)
    {
        if (hpText != null)
        {
            string hpColor = ColorUtility.ToHtmlStringRGB(hpTextColor);
            hpText.text = "<color=#" + hpColor + ">" + currentHp + "</color> / " + maxHp;
        }

        if (shieldText != null)
        {
            if (shield > 0)
            {
                string shieldColor = ColorUtility.ToHtmlStringRGB(shieldTextColor);
                shieldText.text = "+ <color=#" + shieldColor + ">シールド" + shield + "</color>";
                shieldText.gameObject.SetActive(true);
            }
            else
            {
                shieldText.text = string.Empty;
                shieldText.gameObject.SetActive(false);
            }
        }

        float hpRatio = maxHp > 0 ? Mathf.Clamp01((float)currentHp / maxHp) : 0f;
        if (hpFill != null)
        {
            hpFill.fillAmount = hpRatio;
        }

        if (shieldFill != null)
        {
            float total = currentHp + Mathf.Max(0, shield);
            float totalRatio = maxHp > 0 ? Mathf.Clamp01(total / maxHp) : 0f;
            shieldFill.fillAmount = totalRatio;
            shieldFill.enabled = shield > 0;
        }
    }

    public void SetPortrait(Sprite sprite)
    {
        if (portraitImage == null)
        {
            return;
        }

        portraitImage.sprite = sprite;
        portraitImage.enabled = sprite != null;
    }

    public void SetPortraitVisible(bool visible)
    {
        if (portraitImage == null)
        {
            return;
        }

        portraitImage.enabled = visible && portraitImage.sprite != null;
    }
}
