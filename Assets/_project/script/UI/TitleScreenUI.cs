using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TitleScreenUI : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private Button startButton;
    [SerializeField] private Button aboutButton;
    [SerializeField] private Button aboutCloseButton;
    [SerializeField] private GameObject aboutRoot;
    [SerializeField] private TMP_Text aboutText;
    [SerializeField] private string howToPlayTitle = "ゲームの遊び方";
    [SerializeField, TextArea(6, 14)] private string howToPlayBody = "構文を選んで順に実行します。\n強化で効果が追加されるたびに、マイナス効果も混ざります。\nできるだけ良い構文を作って進行しましょう。";
    [SerializeField] private string creditsTitle = "クレジット・ライセンス";
    [SerializeField, TextArea(6, 14)] private string creditsBody = "Unity / TextMeshPro / DOTween\nその他の素材は各ライセンスに準拠";

    private BattleManager battleManager;
    private RunResultUI runResultUI;

    public void Initialize(BattleManager manager, RunResultUI resultUI)
    {
        battleManager = manager;
        runResultUI = resultUI;
        if (runResultUI != null)
        {
            runResultUI.SetBackToTitleAction(Show);
        }
    }

    public void Show()
    {
        if (battleManager != null)
        {
            battleManager.ResetRun();
            battleManager.SetBattleUIVisibleExternal(false);
        }
        if (runResultUI != null)
        {
            runResultUI.Hide();
        }
        SetVisible(true);
        ShowAbout(false);
    }

    public void Hide()
    {
        SetVisible(false);
    }

    private void Awake()
    {
        BindButtons();
        UpdateAboutText();
        ShowAbout(false);
    }

    private void BindButtons()
    {
        if (startButton != null)
        {
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(HandleStart);
        }

        if (aboutButton != null)
        {
            aboutButton.onClick.RemoveAllListeners();
            aboutButton.onClick.AddListener(() => ShowAbout(true));
        }

        if (aboutCloseButton != null)
        {
            aboutCloseButton.onClick.RemoveAllListeners();
            aboutCloseButton.onClick.AddListener(() => ShowAbout(false));
        }
    }

    private void HandleStart()
    {
        Hide();
        if (battleManager != null)
        {
            battleManager.StartBattle();
        }
    }

    private void ShowAbout(bool visible)
    {
        if (aboutRoot != null)
        {
            aboutRoot.SetActive(visible);
        }
    }

    private void UpdateAboutText()
    {
        if (aboutText == null)
        {
            return;
        }

        aboutText.text =
            howToPlayTitle + "\n" +
            howToPlayBody + "\n\n" +
            creditsTitle + "\n" +
            creditsBody;
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
}
