using DG.Tweening;
using TMPro;
using UnityEngine;

public class DamagePopup : MonoBehaviour
{
    [SerializeField] private TMP_Text textMesh;

    private Sequence sequence;

    public void Play(int damageAmount, Color color, float fontSize, float floatDistance, float duration)
    {
        if (textMesh == null)
        {
            textMesh = GetComponent<TMP_Text>();
        }

        if (textMesh == null)
        {
            Debug.LogWarning("DamagePopup: missing TMP_Text");
            return;
        }

        textMesh.text = damageAmount.ToString();
        textMesh.color = new Color(color.r, color.g, color.b, 1f);
        textMesh.alpha = 1f;

        if (fontSize > 0f)
        {
            textMesh.fontSize = fontSize;
        }

        Vector3 startPos = transform.position;
        if (sequence != null)
        {
            sequence.Kill();
        }

        sequence = DOTween.Sequence();
        sequence.SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        sequence.Append(transform.DOMoveY(startPos.y + floatDistance, duration).SetEase(Ease.Linear));
        sequence.Join(DOTween.To(() => textMesh.alpha, x => textMesh.alpha = x, 0f, duration));
        sequence.OnComplete(() => Destroy(gameObject));
    }

    public void PlayText(string text, Color color, float fontSize, float floatDistance, float duration)
    {
        if (textMesh == null)
        {
            textMesh = GetComponent<TMP_Text>();
        }

        if (textMesh == null)
        {
            Debug.LogWarning("DamagePopup: missing TMP_Text");
            return;
        }

        textMesh.text = text;
        textMesh.color = new Color(color.r, color.g, color.b, 1f);
        textMesh.alpha = 1f;

        if (fontSize > 0f)
        {
            textMesh.fontSize = fontSize;
        }

        Vector3 startPos = transform.position;
        if (sequence != null)
        {
            sequence.Kill();
        }

        sequence = DOTween.Sequence();
        sequence.SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        sequence.Append(transform.DOMoveY(startPos.y + floatDistance, duration).SetEase(Ease.Linear));
        sequence.Join(DOTween.To(() => textMesh.alpha, x => textMesh.alpha = x, 0f, duration));
        sequence.OnComplete(() => Destroy(gameObject));
    }
    public void PlayPreview(int damageAmount, Color color, float fontSize, float duration)
    {
        if (textMesh == null)
        {
            textMesh = GetComponent<TMP_Text>();
        }

        if (textMesh == null)
        {
            Debug.LogWarning("DamagePopup: missing TMP_Text");
            return;
        }

        textMesh.text = damageAmount.ToString();
        textMesh.color = new Color(color.r, color.g, color.b, 1f);
        textMesh.alpha = 1f;

        if (fontSize > 0f)
        {
            textMesh.fontSize = fontSize;
        }

        if (sequence != null)
        {
            sequence.Kill();
        }

        if (duration <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        sequence = DOTween.Sequence();
        sequence.SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        sequence.AppendInterval(duration);
        sequence.OnComplete(() => Destroy(gameObject));
    }

    public void PlayImpact(int damageAmount, Color color, float fontSize, Vector3 fromPosition, Vector3 toPosition, float impactDuration, float scale, float floatDistance, float floatDuration)
    {
        if (textMesh == null)
        {
            textMesh = GetComponent<TMP_Text>();
        }

        if (textMesh == null)
        {
            Debug.LogWarning("DamagePopup: missing TMP_Text");
            return;
        }

        textMesh.text = damageAmount.ToString();
        textMesh.color = new Color(color.r, color.g, color.b, 1f);
        textMesh.alpha = 1f;

        if (fontSize > 0f)
        {
            textMesh.fontSize = fontSize;
        }

        transform.position = fromPosition;
        transform.localScale = Vector3.one;

        if (sequence != null)
        {
            sequence.Kill();
        }

        if (impactDuration <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        sequence = DOTween.Sequence();
        sequence.SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        sequence.Append(transform.DOMove(toPosition, impactDuration).SetEase(Ease.OutQuad));
        sequence.Join(transform.DOScale(scale, impactDuration).SetEase(Ease.OutBack));

        float resolvedFloatDuration = Mathf.Max(0f, floatDuration);
        if (resolvedFloatDuration > 0f)
        {
            sequence.Append(transform.DOMoveY(toPosition.y + floatDistance, resolvedFloatDuration).SetEase(Ease.Linear));
            sequence.Join(transform.DOScale(1f, resolvedFloatDuration).SetEase(Ease.OutQuad));
            sequence.Join(DOTween.To(() => textMesh.alpha, x => textMesh.alpha = x, 0f, resolvedFloatDuration));
        }
        else
        {
            sequence.Append(DOTween.To(() => textMesh.alpha, x => textMesh.alpha = x, 0f, 0.05f));
        }
        sequence.OnComplete(() => Destroy(gameObject));
    }

    private void OnDisable()
    {
        if (sequence != null)
        {
            sequence.Kill();
        }
    }

    private void OnDestroy()
    {
        if (sequence != null)
        {
            sequence.Kill();
        }
    }
}
