using TMPro;
using UnityEngine;
using UnityEngine.UI;
using unityroom.Api;

public class RunResultUI : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text baseScoreText;
    [SerializeField] private TMP_Text totalScoreText;
    [SerializeField] private TMP_Text turnCountText;
    [SerializeField] private CardView finalCardView;
    [SerializeField] private Button backToTitleButton;
    [SerializeField] private string clearTitle = "クリア";
    [SerializeField] private string gameOverTitle = "ゲームオーバー";
    [SerializeField] private string progressFormat = "進行: {0}/{1}";
    [SerializeField] private string scoreFormat = "スコア: {0}";
    [SerializeField] private string baseScoreFormat = "基本スコア: {0}";
    [SerializeField] private string totalScoreFormat = "合計スコア(クリアボーナス込み): {0}";
    [SerializeField] private string turnCountFormat = "合計ターン数: {0}";
    private System.Action backToTitleAction;

    public void Show(bool cleared, int layerReached, int maxLayer, int baseScore, int totalScore, int totalTurns, CardData finalCard)
    {
        if (titleText != null)
        {
            titleText.text = cleared ? clearTitle : gameOverTitle;
        }

        if (progressText != null)
        {
            progressText.text = string.Format(progressFormat, layerReached, maxLayer);
        }

        if (baseScoreText != null)
        {
            baseScoreText.text = string.Format(baseScoreFormat, baseScore);
        }

        if (totalScoreText != null)
        {
            totalScoreText.text = string.Format(totalScoreFormat, totalScore);
        }
        else if (scoreText != null)
        {
            scoreText.text = string.Format(scoreFormat, totalScore);
        }

        if (turnCountText != null)
        {
            turnCountText.text = string.Format(turnCountFormat, totalTurns);
        }

        if (finalCardView != null)
        {
            finalCardView.Clear();
            finalCardView.RenderCard(finalCard);
        }

        UnityroomApiClient.Instance.SendScore(1, totalScore, ScoreboardWriteMode.HighScoreDesc);

        SetVisible(true);
    }

    public void SetBackToTitleAction(System.Action action)
    {
        backToTitleAction = action;
        BindBackButton();
    }

    public void Hide()
    {
        SetVisible(false);
    }

    private void SetVisible(bool visible)
    {
        if (root != null)
        {
            root.SetActive(visible);
        }
        else
        {
            gameObject.SetActive(visible);
        }
    }

    private void Awake()
    {
        BindBackButton();
    }

    private void BindBackButton()
    {
        if (backToTitleButton == null)
        {
            return;
        }

        backToTitleButton.onClick.RemoveAllListeners();
        backToTitleButton.onClick.AddListener(HandleBackToTitle);
    }

    private void HandleBackToTitle()
    {
        if (backToTitleAction != null)
        {
            backToTitleAction.Invoke();
        }
    }
}
