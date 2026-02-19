using DG.Tweening;
using UnityEngine;

public class DamagePopupManager : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private DamagePopup popupPrefab;
    [SerializeField] private Transform popupRoot;
    [SerializeField] private bool useScreenSpace;
    [SerializeField] private Camera worldCamera;

    [Header("Motion")]
    [SerializeField] private float floatDistance = 1.2f;
    [SerializeField] private float duration = 1.0f;

    [Header("Screen Space Scale")]
    [SerializeField] private float screenSpaceMultiplier = 100f;
    [SerializeField] private Vector2 referenceResolution = new Vector2(1920f, 1080f);

    [Header("Preview")]
    [SerializeField] private Vector3 previewOffset = new Vector3(0.8f, 0.3f, 0f);
    [SerializeField] private float previewDuration = 0.45f;

    [Header("Impact")]
    [SerializeField] private Vector3 impactFromOffset = new Vector3(0.9f, 0.1f, 0f);
    [SerializeField] private Vector3 impactToOffset = new Vector3(0.1f, 0.1f, 0f);
    [SerializeField] private float impactDuration = 0.35f;
    [SerializeField] private float impactScale = 1.3f;

    [Header("Size")]
    [SerializeField] private float normalFontSize = 36f;
    [SerializeField] private float boostedFontSize = 48f;
    [SerializeField] private float elementFontSize = 42f;

    [Header("Color")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color boostedColor = new Color(1f, 0.85f, 0.2f);
    [SerializeField] private Color previewColor = new Color(0.75f, 0.75f, 0.75f);
    [SerializeField] private Color healColor = new Color(0.4f, 0.9f, 0.45f);
    [SerializeField] private Color shieldColor = new Color(0.45f, 0.75f, 1f);
    [SerializeField] private Color fireColor = new Color(1f, 0.45f, 0.2f);
    [SerializeField] private Color iceColor = new Color(0.5f, 0.85f, 1f);
    [SerializeField] private Color lightningColor = new Color(1f, 0.95f, 0.3f);
    [SerializeField] private Color poisonColor = new Color(0.4f, 0.9f, 0.4f);
    [SerializeField] private Color darkColor = new Color(0.7f, 0.4f, 0.9f);

    public float PreviewDuration => previewDuration;
    public float ImpactDuration => impactDuration;

    public void ShowDamage(Vector3 position, int amount)
    {
        ShowDamage(position, amount, false, DamageElement.None);
    }

    public void ShowDamage(Vector3 position, int amount, Color color)
    {
        Spawn(position, amount, color, normalFontSize);
    }

    public void ShowDamage(Vector3 position, int amount, bool boosted, DamageElement element = DamageElement.None)
    {
        Color color = GetColor(boosted, element);
        float fontSize = GetFontSize(boosted, element);
        Spawn(position, amount, color, fontSize);
    }

    public void ShowDamagePreview(Vector3 targetPosition, int amount, bool boosted, DamageElement element = DamageElement.None)
    {
        ShowDamagePreview(targetPosition, amount, boosted, previewDuration, element);
    }

    public void ShowDamagePreview(Vector3 targetPosition, int amount, bool boosted, float durationOverride, DamageElement element = DamageElement.None)
    {
        if (popupPrefab == null)
        {
            Debug.LogWarning("DamagePopupManager: popupPrefab is missing");
            return;
        }

        Color color = previewColor;
        float fontSize = GetFontSize(boosted, element);
        Vector3 spawnPos = ResolvePosition(targetPosition, previewOffset);
        DamagePopup popup = Instantiate(popupPrefab, spawnPos, Quaternion.identity, popupRoot);
        popup.transform.localScale = Vector3.one;
        float duration = durationOverride > 0f ? durationOverride : previewDuration;
        popup.PlayPreview(amount, color, fontSize, duration);
    }

    public void ShowDamageImpact(Vector3 targetPosition, int amount, bool boosted, DamageElement element = DamageElement.None)
    {
        if (popupPrefab == null)
        {
            Debug.LogWarning("DamagePopupManager: popupPrefab is missing");
            return;
        }

        Color color = GetColor(boosted, element);
        float fontSize = GetFontSize(boosted, element);
        Vector3 fromPos = ResolvePosition(targetPosition, impactFromOffset);
        Vector3 toPos = ResolvePosition(targetPosition, impactToOffset);
        DamagePopup popup = Instantiate(popupPrefab, fromPos, Quaternion.identity, popupRoot);
        popup.transform.localScale = Vector3.one;
        popup.PlayImpact(amount, color, fontSize, fromPos, toPos, impactDuration, impactScale, GetFloatDistance(), duration);
    }

    public void ShowHeal(Vector3 position, int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        SpawnText(position, "+" + amount, healColor, normalFontSize);
    }

    public void ShowShield(Vector3 position, int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        SpawnText(position, "+" + amount, shieldColor, normalFontSize);
    }

    public void ClearPopups()
    {
        if (popupRoot != null)
        {
            for (int i = popupRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = popupRoot.GetChild(i);
                if (child != null)
                {
                    DOTween.Kill(child, true);
                    Destroy(child.gameObject);
                }
            }
            return;
        }

        DamagePopup[] popups = FindObjectsOfType<DamagePopup>();
        for (int i = 0; i < popups.Length; i++)
        {
            if (popups[i] != null)
            {
                DOTween.Kill(popups[i].transform, true);
                Destroy(popups[i].gameObject);
            }
        }
    }

    private void Spawn(Vector3 position, int amount, Color color, float fontSize)
    {
        if (popupPrefab == null)
        {
            Debug.LogWarning("DamagePopupManager: popupPrefab is missing");
            return;
        }

        Vector3 spawnPos = ResolvePosition(position);
        DamagePopup popup = Instantiate(popupPrefab, spawnPos, Quaternion.identity, popupRoot);
        popup.transform.localScale = Vector3.one;
        popup.Play(amount, color, fontSize, GetFloatDistance(), duration);
    }

    private void SpawnText(Vector3 position, string text, Color color, float fontSize)
    {
        if (popupPrefab == null)
        {
            Debug.LogWarning("DamagePopupManager: popupPrefab is missing");
            return;
        }

        Vector3 spawnPos = ResolvePosition(position);
        DamagePopup popup = Instantiate(popupPrefab, spawnPos, Quaternion.identity, popupRoot);
        popup.transform.localScale = Vector3.one;
        popup.PlayText(text, color, fontSize, GetFloatDistance(), duration);
    }

    private float GetFloatDistance()
    {
        return useScreenSpace ? floatDistance * screenSpaceMultiplier * GetScreenSpaceScale() : floatDistance;
    }

    private Vector3 ResolvePosition(Vector3 worldPosition, Vector3 offset)
    {
        if (!useScreenSpace)
        {
            return worldPosition + offset;
        }

        Camera cam = worldCamera != null ? worldCamera : Camera.main;
        if (cam == null)
        {
            return worldPosition + offset;
        }

        Vector3 screenPos = cam.WorldToScreenPoint(worldPosition);
        return screenPos + offset * screenSpaceMultiplier * GetScreenSpaceScale();
    }

    private Vector3 ResolvePosition(Vector3 worldPosition)
    {
        if (!useScreenSpace)
        {
            return worldPosition;
        }

        Camera cam = worldCamera != null ? worldCamera : Camera.main;
        if (cam == null)
        {
            return worldPosition;
        }

        return cam.WorldToScreenPoint(worldPosition);
    }

    private float GetScreenSpaceScale()
    {
        if (!useScreenSpace || referenceResolution.x <= 0f || referenceResolution.y <= 0f)
        {
            return 1f;
        }

        float scaleX = Screen.width / referenceResolution.x;
        float scaleY = Screen.height / referenceResolution.y;
        return Mathf.Min(scaleX, scaleY);
    }

    private Color GetColor(bool boosted, DamageElement element)
    {
        if (element != DamageElement.None)
        {
            return GetElementColor(element);
        }

        return boosted ? boostedColor : normalColor;
    }

    private float GetFontSize(bool boosted, DamageElement element)
    {
        if (boosted)
        {
            return boostedFontSize;
        }

        if (element != DamageElement.None)
        {
            return elementFontSize;
        }

        return normalFontSize;
    }

    private Color GetElementColor(DamageElement element)
    {
        switch (element)
        {
            case DamageElement.Fire:
                return fireColor;
            case DamageElement.Ice:
                return iceColor;
            case DamageElement.Lightning:
                return lightningColor;
            case DamageElement.Poison:
                return poisonColor;
            case DamageElement.Dark:
                return darkColor;
            default:
                return normalColor;
        }
    }
}
