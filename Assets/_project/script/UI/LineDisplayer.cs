using DG.Tweening;
using TMPro;
using UnityEngine;

public class LineDisplayer : MonoBehaviour
{
    [Header("Display")]
    [SerializeField] private TMP_Text textMesh;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Color textColor = new Color(0.6f, 0.6f, 0.6f, 1f);
    [SerializeField] private Color enemyAttackColor = new Color(0.85f, 0.35f, 0.35f, 1f);
    [SerializeField] private Color negativeEffectColor = new Color(0.9f, 0.45f, 0.2f, 1f);
    [SerializeField] private float maxAlpha = 0.35f;
    [SerializeField] private float displayScale = 1f;

    [Header("Timing")]
    [SerializeField] private float fadeInSeconds = 0.1f;
    [SerializeField] private float holdSeconds = 0.4f;
    [SerializeField] private float fadeOutSeconds = 0.3f;

    private Sequence sequence;

    private void Awake()
    {
        HideImmediate();
    }

    private void OnEnable()
    {
        HideImmediate();
    }

    public void DisplayLine(LineData line, EnemyData target, PlayerData player, bool ownerIsPlayer)
    {
        if (line == null)
        {
            return;
        }

        if (textMesh == null)
        {
            textMesh = GetComponentInChildren<TMP_Text>();
        }

        if (textMesh == null)
        {
            Debug.LogWarning("LineDisplayer: missing TMP_Text");
            return;
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        string text = line.GetDisplayPhrase();
        textMesh.text = text;
        textMesh.color = ResolveTextColor(line, ownerIsPlayer);
        textMesh.enabled = true;
        transform.localScale = Vector3.one * displayScale;

        if (sequence != null)
        {
            sequence.Kill();
        }

        canvasGroup.alpha = 0f;
        sequence = DOTween.Sequence();
        sequence.Append(canvasGroup.DOFade(maxAlpha, fadeInSeconds));
        sequence.AppendInterval(holdSeconds);
        sequence.Append(canvasGroup.DOFade(0f, fadeOutSeconds));
        sequence.OnComplete(() => HideImmediate());
    }

    private void OnDisable()
    {
        if (sequence != null)
        {
            sequence.Kill();
        }
    }

    private void HideImmediate()
    {
        if (textMesh == null)
        {
            textMesh = GetComponentInChildren<TMP_Text>();
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        canvasGroup.alpha = 0f;
        if (textMesh != null)
        {
            textMesh.enabled = false;
        }
    }

    private Color ResolveTextColor(LineData line, bool ownerIsPlayer)
    {
        if (line != null && line.valueScore < 0)
        {
            return negativeEffectColor;
        }

        if (!ownerIsPlayer)
        {
            return enemyAttackColor;
        }

        return textColor;
    }
}
