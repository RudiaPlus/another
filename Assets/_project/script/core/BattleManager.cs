using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BattleManager : MonoBehaviour
{
    private const string DefaultLayerDefinitionPath = "Assets/_project/data/json/layer/layers.json";
    private const string EnemyDeckTier06Path = "Assets/_project/data/json/card/enemy_deck_06.json";
    private const string EnemyDeckTier11Path = "Assets/_project/data/json/card/enemy_deck_11.json";

    [SerializeField] private CardProcessor cardProcessor;
    [SerializeField] private DamagePopupManager damagePopupManager;
    [SerializeField] private ActorView playerView;
    [SerializeField] private ActorView enemyView;
    [SerializeField] private Sprite playerSprite;
    [SerializeField] private Sprite enemySprite;
    [SerializeField] private Sprite bossSprite;
    [SerializeField] private bool showActorSprites = true;
    [SerializeField] private ActorSpriteDisplay playerFieldSprite;
    [SerializeField] private ActorSpriteDisplay enemyFieldSprite;
    [SerializeField] private Sprite playerFieldSpriteAsset;
    [SerializeField] private Sprite enemyFieldSpriteAsset;
    [SerializeField] private Sprite bossFieldSpriteAsset;
    [SerializeField] private bool showFieldSprites = true;
    [SerializeField] private GameObject battleUIRoot;
    [SerializeField] private TMP_Text turnText;
    [SerializeField] private string playerTurnLabel = "プレイヤーのターン";
    [SerializeField] private string enemyTurnLabel = "敵のターン";
    [SerializeField] private PostBattleRewardUI postBattleRewardUI;
    [SerializeField] private PostBattleEnhancementUI postBattleEnhancementUI;
    [SerializeField] private LayerRewardUI layerRewardUI;
    [SerializeField] private LineCalibrationUI lineCalibrationUI;
    [SerializeField] private LineRemoveUI lineRemoveUI;
    [SerializeField] private ShopUI shopUI;
    [SerializeField] private RunResultUI runResultUI;
    [SerializeField] private CardSelectionUI cardSelectionUI;
    [SerializeField] private BattleUIBridge uiBridge;
    [SerializeField] private CardView playerLineView;
    [SerializeField] private CardView enemyLineView;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private int startingHandSize = 5;
    [SerializeField] private int drawPerTurn = 1;
    [SerializeField] private int enemyAttackDamage = 5;
    [SerializeField] private float enemyHpMultiplierPerWin = 1.4f;
    [SerializeField] private float turnDelaySeconds = 0.4f;
    [SerializeField] private int enhancementOptionCount = 3;
    [SerializeField] private int rewardOptionCount = 3;
    [SerializeField] private int healRewardAmount = 10;
    [SerializeField] private string deckSavePath = "Assets/_project/data/json/card/player_deck.json";
    [SerializeField] private int startLayer = 1;
    [SerializeField] private int maxLayer = 15;
    [SerializeField] private int bossInterval = 5;
    [SerializeField] private string bossNamePrefix = "Boss";
    [SerializeField] private int layerMaxHpReward = 10;
    [SerializeField] private int layerCurrencyReward = 10;
    [SerializeField] private float bossEnhancementProgressBoost = 0.2f;
    [SerializeField, Range(0f, 1f)] private float normalEnhancementRareChance = 0.05f;
    [SerializeField, Range(0f, 1f)] private float shopEnhancementRareChance = 0.1f;
    [SerializeField, Range(0f, 1f)] private float bossEnhancementRareChance = 0.5f;
    [SerializeField] private bool includeLineSwapReward = true;
    [SerializeField] private float lineSwapRewardChance = 0.3f;
    [SerializeField] private int shopItemCost = 10;
    [SerializeField] private int shopResetCost = 5;
    [SerializeField] private int shopHealAmount = 50;
    [SerializeField] private string layerDefinitionPath = "Assets/_project/data/json/layer/layers.json";
    [SerializeField] private string rewardCardPoolPath = "Assets/_project/data/json/card/reward_cards.json";
    [SerializeField] private string defaultEnemyDeckPath = "Assets/_project/data/json/card/enemy_deck.json";
    [SerializeField] private string playerDeckInitialPath = "Assets/_project/data/json/card/player_deck_initial.json";
    [SerializeField] private List<LayerDefinition> layerDefinitions = new List<LayerDefinition>();

    public PlayerData Player { get; private set; }
    public EnemyData Enemy { get; private set; }

    private readonly List<CardData> drawPile = new List<CardData>();
    private readonly List<CardData> hand = new List<CardData>();
    private readonly List<CardData> discard = new List<CardData>();
    private bool battleRunning;
    private BattleContext playerContext;
    private BattleContext enemyContext;
    private int currentLayer;
    private string baseEnemyName;
    private int baseEnemyMaxHp;
    private int basePlayerMaxHp;
    private int basePlayerCurrency;
    private bool rewardPoolLoaded;
    private readonly List<CardData> rewardCardPool = new List<CardData>();
    private int totalDamageScore;
    private int totalPlayerTurns;
    private int lastEnemyEnhancementLayer;
    private bool lastClearedWasBoss;
    private bool finalBossDefeated;
    private bool shopOpen;
    private List<LineData> accumulatedEnemyEnhancements = new List<LineData>();

    public IReadOnlyList<CardData> Hand => hand;

    private enum EnhancementSource
    {
        NormalReward,
        LayerReward,
        Shop
    }

    private void Awake()
    {
        if (cardProcessor == null)
        {
            cardProcessor = FindCardProcessor();
        }

        if (cardProcessor == null)
        {
            GameObject processorObject = new GameObject("CardProcessor");
            cardProcessor = processorObject.AddComponent<CardProcessor>();
        }

        if (damagePopupManager == null)
        {
            damagePopupManager = FindObjectOfType<DamagePopupManager>();
        }

        SyncLegacyUIBridge();
    }

    public void Initialize(PlayerData player, EnemyData enemy)
    {
        Player = player;
        Enemy = enemy;
        currentLayer = Mathf.Max(1, startLayer);
        if (Player != null)
        {
            Player.currency = Mathf.Max(0, Player.currency);
        }
        baseEnemyName = Enemy != null ? Enemy.name : string.Empty;
        baseEnemyMaxHp = Enemy != null ? Enemy.maxHP : 0;
        basePlayerMaxHp = Player != null ? Player.maxHP : 0;
        basePlayerCurrency = Player != null ? Player.currency : 0;
        totalDamageScore = 0;
        totalPlayerTurns = 0;
        lastEnemyEnhancementLayer = 0;
        lastClearedWasBoss = false;
        finalBossDefeated = false;
        accumulatedEnemyEnhancements = new List<LineData>();
        LoadLayerDefinitions();
        ResetEnemyEnhancementTierIfNeeded();
        ApplyLayerInfo();
        UpdateViews();
    }

    public void SetViews(ActorView player, ActorView enemy)
    {
        playerView = player;
        enemyView = enemy;

        if (cardProcessor == null)
        {
            cardProcessor = FindCardProcessor();
        }

        if (cardProcessor != null)
        {
            Transform enemyAnchor = enemyView != null ? enemyView.PopupAnchor : null;
            Transform playerAnchor = playerView != null ? playerView.PopupAnchor : null;
            cardProcessor.SetAnchors(enemyAnchor, playerAnchor);
        }
    }

    public void SetDeckSavePath(string path)
    {
        if (!string.IsNullOrEmpty(path))
        {
            deckSavePath = path;
        }
    }

    public void SetDefaultEnemyDeckPath(string path)
    {
        if (!string.IsNullOrEmpty(path))
        {
            defaultEnemyDeckPath = path;
        }
    }

    public void SetPlayerDeckInitialPath(string path)
    {
        if (!string.IsNullOrEmpty(path))
        {
            playerDeckInitialPath = path;
        }
    }

    public void SetPostBattleUI(PostBattleEnhancementUI ui)
    {
        postBattleEnhancementUI = ui;
        SyncLegacyUIBridge();
    }

    public void SetPostBattleRewardUI(PostBattleRewardUI ui)
    {
        postBattleRewardUI = ui;
        SyncLegacyUIBridge();
    }

    public void SetLayerRewardUI(LayerRewardUI ui)
    {
        layerRewardUI = ui;
        SyncLegacyUIBridge();
    }

    public void SetLineCalibrationUI(LineCalibrationUI ui)
    {
        lineCalibrationUI = ui;
        SyncLegacyUIBridge();
    }

    public void SetLineRemoveUI(LineRemoveUI ui)
    {
        lineRemoveUI = ui;
        SyncLegacyUIBridge();
    }

    public void SetShopUI(ShopUI ui)
    {
        shopUI = ui;
        SyncLegacyUIBridge();
    }

    public void SetRunResultUI(RunResultUI ui)
    {
        runResultUI = ui;
        SyncLegacyUIBridge();
    }

    public void SetCardSelectionUI(CardSelectionUI ui)
    {
        cardSelectionUI = ui;
        SyncLegacyUIBridge();
    }

    public void SetUIBridge(BattleUIBridge bridge)
    {
        uiBridge = bridge;
        SyncLegacyUIBridge();
    }

    public void SetBattleUIRoot(GameObject root)
    {
        battleUIRoot = root;
        SyncLegacyUIBridge();
    }

    public void SetBattleUIVisibleExternal(bool visible)
    {
        SetBattleUIVisible(visible);
    }

    private void SyncLegacyUIBridge()
    {
        if (uiBridge == null)
        {
            return;
        }

        uiBridge.BindLegacy(
            battleUIRoot,
            cardSelectionUI,
            postBattleRewardUI,
            postBattleEnhancementUI,
            layerRewardUI,
            lineCalibrationUI,
            lineRemoveUI,
            shopUI,
            runResultUI);
    }

    public void ResetRun()
    {
        StopAllCoroutines();
        battleRunning = false;
        shopOpen = false;
        finalBossDefeated = false;
        lastClearedWasBoss = false;
        totalDamageScore = 0;
        totalPlayerTurns = 0;
        lastEnemyEnhancementLayer = 0;
        currentLayer = Mathf.Max(1, startLayer);

        if (accumulatedEnemyEnhancements != null)
        {
            accumulatedEnemyEnhancements.Clear();
        }

        LoadLayerDefinitions();
        if (playerContext != null)
        {
            playerContext.ResetForCombat();
        }
        if (enemyContext != null)
        {
            enemyContext.ResetForCombat();
        }

        ResetPlayerForNewRun();
        ResetEnemyForNewRun();

        drawPile.Clear();
        hand.Clear();
        discard.Clear();

        if (postBattleEnhancementUI != null)
        {
            postBattleEnhancementUI.Hide();
        }
        if (postBattleRewardUI != null)
        {
            postBattleRewardUI.Hide();
        }
        if (layerRewardUI != null)
        {
            layerRewardUI.Hide();
        }
        if (lineCalibrationUI != null)
        {
            lineCalibrationUI.Hide();
        }
        if (lineRemoveUI != null)
        {
            lineRemoveUI.Hide();
        }
        if (shopUI != null)
        {
            shopUI.Hide();
        }
        if (runResultUI != null)
        {
            runResultUI.Hide();
        }
        if (cardSelectionUI != null)
        {
            cardSelectionUI.Hide();
        }
        if (damagePopupManager != null)
        {
            damagePopupManager.ClearPopups();
        }

        ResetEnemyEnhancementTierIfNeeded();
        ApplyLayerInfo();
        ResetEnemyStatsForLayer();
        UpdateViews();
    }

    public List<CardData> GetPlayerDeck()
    {
        if (Player == null || Player.deck == null)
        {
            return new List<CardData>();
        }

        return Player.deck;
    }

    public void StartBattle()
    {
        if (Player == null || Enemy == null)
        {
            Debug.LogWarning("BattleManager: missing player or enemy data");
            return;
        }

        ResetEnemyEnhancementTierIfNeeded();
        ApplyLayerInfo();

        if (playerContext == null)
        {
            playerContext = new BattleContext();
        }
        if (enemyContext == null)
        {
            enemyContext = new BattleContext();
        }
        playerContext.ResetForCombat();
        enemyContext.ResetForCombat();
        if (Player != null)
        {
            Player.shield = 0;
        }
        if (Enemy != null)
        {
            Enemy.shield = 0;
        }

        if (postBattleEnhancementUI != null)
        {
            postBattleEnhancementUI.Hide();
        }

        if (postBattleRewardUI != null)
        {
            postBattleRewardUI.Hide();
        }

        if (layerRewardUI != null)
        {
            layerRewardUI.Hide();
        }

        if (lineCalibrationUI != null)
        {
            lineCalibrationUI.Hide();
        }
        if (lineRemoveUI != null)
        {
            lineRemoveUI.Hide();
        }

        if (shopUI != null)
        {
            shopUI.Hide();
        }

        if (runResultUI != null)
        {
            runResultUI.Hide();
        }

        if (cardSelectionUI != null)
        {
            cardSelectionUI.Hide();
        }

        if (damagePopupManager != null)
        {
            damagePopupManager.ClearPopups();
        }

        SetBattleUIVisible(true);
        SetTurnText(playerTurnLabel);

        finalBossDefeated = false;
        ApplyEnemyEnhancementIfNeeded();
        BuildDrawPile();
        DrawCards(startingHandSize);
        Debug.Log("Battle start: " + Player.name + " vs " + Enemy.name);

        if (!battleRunning)
        {
            StartCoroutine(BattleLoop());
        }
    }

    public void StartPlayerTurn()
    {
        DrawCards(drawPerTurn);
    }

    private void BeginPlayerTurn()
    {
        if (playerContext == null)
        {
            playerContext = new BattleContext();
        }
        playerContext.ResetForTurn();
        totalPlayerTurns++;
        if (Player != null)
        {
            Player.shield = 0;
        }
    }

    private void BeginEnemyTurn()
    {
        if (enemyContext == null)
        {
            enemyContext = new BattleContext();
        }
        enemyContext.ResetForTurn();
        if (Enemy != null)
        {
            Enemy.shield = 0;
        }
    }

    public void PlayCard(int handIndex)
    {
        StartCoroutine(PlayCardRoutine(handIndex));
    }

    private IEnumerator BattleLoop()
    {
        battleRunning = true;
        UpdateViews();

        while (battleRunning)
        {
            if (IsBattleOver())
            {
                break;
            }

            BeginPlayerTurn();
            SetTurnText(playerTurnLabel);
            yield return StartCoroutine(HandlePlayerTurn());
            ApplyPendingLineTransforms(playerContext);
            UpdateViews();

            if (IsBattleOver())
            {
                break;
            }

            if (turnDelaySeconds > 0f)
            {
                yield return new WaitForSeconds(turnDelaySeconds);
            }

            BeginEnemyTurn();
            SetTurnText(enemyTurnLabel);
            yield return StartCoroutine(EnemyAction());
            ApplyPendingLineTransforms(enemyContext);
            UpdateViews();

            if (turnDelaySeconds > 0f)
            {
                yield return new WaitForSeconds(turnDelaySeconds);
            }
        }

        battleRunning = false;
        if ((Enemy != null && Enemy.currentHP <= 0) || (IsFinalLayer() && finalBossDefeated))
        {
            HandleBattleWin();
        }
        else
        {
            HandleBattleLose();
        }
    }

    private IEnumerator PlayCardRoutine(int handIndex)
    {
        if (handIndex < 0 || handIndex >= hand.Count)
        {
            Debug.LogWarning("BattleManager: invalid hand index");
            yield break;
        }

        CardData card = hand[handIndex];
        hand.RemoveAt(handIndex);
        discard.Add(card);

        if (cardProcessor != null)
        {
            yield return StartCoroutine(cardProcessor.ExcuteCard(card, Enemy, Player, playerContext, enemyContext));
        }
    }

    private IEnumerator HandlePlayerTurn()
    {
        if (hand.Count == 0)
        {
            StartPlayerTurn();
            UpdateViews();
        }

        if (hand.Count == 0)
        {
            Debug.Log("BattleManager: no cards to play");
            yield break;
        }

        if (cardSelectionUI == null)
        {
            yield return StartCoroutine(PlayCardRoutine(0));
            yield break;
        }

        bool selectionMade = false;
        int selectedIndex = 0;
        cardSelectionUI.Show(hand, index =>
        {
            selectionMade = true;
            selectedIndex = index;
        });

        while (!selectionMade)
        {
            yield return null;
        }

        cardSelectionUI.Hide();
        yield return StartCoroutine(PlayCardRoutine(selectedIndex));
    }

    private IEnumerator EnemyAction()
    {
        if (Enemy == null || Player == null)
        {
            yield break;
        }

        if (Enemy.actionDeck != null && Enemy.actionDeck.Count > 0 && cardProcessor != null)
        {
            int index = UnityEngine.Random.Range(0, Enemy.actionDeck.Count);
            CardData card = Enemy.actionDeck[index];
            yield return StartCoroutine(cardProcessor.ExcuteCardOnPlayer(card, Player, Enemy, enemyContext, playerContext));
            yield break;
        }

        int finalDamage = enemyAttackDamage;
        if (enemyContext != null && enemyContext.globalDamageAmp != 0)
        {
            finalDamage = Mathf.CeilToInt(finalDamage * (1 + enemyContext.globalDamageAmp / 100f));
        }

        Player.currentHP -= finalDamage;
        if (damagePopupManager != null)
        {
            damagePopupManager.ShowDamage(GetPlayerPopupPosition(), finalDamage, false, DamageElement.None);
        }

        Debug.Log("Enemy attacks for " + finalDamage);
        yield return null;
    }

    private bool IsBattleOver()
    {
        if (Player == null || Enemy == null)
        {
            return true;
        }

        if (Player.currentHP <= 0)
        {
            return true;
        }

        return Enemy.currentHP <= 0;
    }

    private void HandleBattleWin()
    {
        StartCoroutine(HandleBattleWinRoutine());
    }

    private IEnumerator HandleBattleWinRoutine()
    {
        Debug.Log("Battle win");
        yield return new WaitForSeconds(1f);

        if (currentLayer >= maxLayer)
        {
            FinishRun();
            yield break;
        }

        if (layerRewardUI != null)
        {
            ShowLayerRewards();
            yield break;
        }

        if (postBattleRewardUI != null)
        {
            ShowRewardSelection();
            yield break;
        }

        ShowEnhancementReward();
    }

    private void ShowRewardSelection()
    {
        List<RewardOption> options = RewardSystem.GenerateOptions(rewardOptionCount, healRewardAmount);
        if (options == null || options.Count == 0)
        {
            ShowEnhancementReward();
            return;
        }

        if (cardSelectionUI != null)
        {
            cardSelectionUI.Hide();
        }

        if (damagePopupManager != null)
        {
            damagePopupManager.ClearPopups();
        }

        SetBattleUIVisible(false);
        postBattleRewardUI.Show(options, ApplyReward);
    }

    private void ShowLayerRewards()
    {
        if (layerRewardUI == null)
        {
            ShowEnhancementReward();
            return;
        }

        LayerDefinition definition = GetLayerDefinition(currentLayer);
        if (definition == null || definition.rewards == null || definition.rewards.Count == 0)
        {
            AdvanceLayerAndStartBattle();
            return;
        }

        lastClearedWasBoss = definition.isBoss;

        if (cardSelectionUI != null)
        {
            cardSelectionUI.Hide();
        }
        if (damagePopupManager != null)
        {
            damagePopupManager.ClearPopups();
        }
        SetBattleUIVisible(false);

        layerRewardUI.Show(definition, Player, ApplyLayerReward, AdvanceLayerAndStartBattle);
    }

    private void ApplyReward(RewardOption option)
    {
        if (option == null)
        {
            StartNextBattle();
            return;
        }

        switch (option.type)
        {
            case RewardType.Enhancement:
                ShowEnhancementReward();
                return;
            case RewardType.Heal:
                ApplyHealReward(option.amount);
                break;
            case RewardType.SwapLine:
                ApplySwapLineReward();
                break;
            case RewardType.RemoveLine:
                if (lineRemoveUI != null)
                {
                    StartLineRemoveReward(() =>
                    {
                        StartNextBattle();
                    }, false);
                    return;
                }
                ApplyRemoveLineReward();
                break;
        }

        SaveDeck();
        UpdateViews();
        StartNextBattle();
    }

    private void ApplyLayerReward(LayerRewardEntry entry, Action onComplete)
    {
        if (entry == null)
        {
            onComplete?.Invoke();
            return;
        }

        switch (entry.type)
        {
            case LayerRewardType.MaxHpUp:
                ApplyMaxHpReward(entry.amount);
                UpdateViews();
                if (layerRewardUI != null)
                {
                    layerRewardUI.Refresh();
                }
                onComplete?.Invoke();
                return;
            case LayerRewardType.Currency:
                ApplyCurrencyReward(entry.amount);
                if (layerRewardUI != null)
                {
                    layerRewardUI.Refresh();
                }
                onComplete?.Invoke();
                return;
            case LayerRewardType.Enhancement:
                if (layerRewardUI != null)
                {
                    layerRewardUI.SetVisible(false);
                }
                StartEnhancementReward(() =>
                {
                    if (layerRewardUI != null)
                    {
                        layerRewardUI.SetVisible(true);
                        layerRewardUI.Refresh();
                    }
                    onComplete?.Invoke();
                }, EnhancementSource.LayerReward);
                return;
            case LayerRewardType.CardAdd:
                Debug.Log("LayerReward: card add is disabled");
                onComplete?.Invoke();
                return;
            case LayerRewardType.LineSwap:
                StartLineCalibrationReward(() =>
                {
                    if (layerRewardUI != null)
                    {
                        layerRewardUI.Refresh();
                    }
                    onComplete?.Invoke();
                }, true);
                return;
        }

        onComplete?.Invoke();
    }

    private void ApplyHealReward(int amount)
    {
        if (Player == null || amount <= 0)
        {
            return;
        }

        int before = Player.currentHP;
        Player.currentHP = Mathf.Min(Player.currentHP + amount, Player.maxHP);
        int healed = Player.currentHP - before;
        if (damagePopupManager != null && healed > 0)
        {
            damagePopupManager.ShowHeal(GetPlayerPopupPosition(), healed);
        }
    }

    private void ApplySwapLineReward()
    {
        CardData card = ChooseEnhancementCard();
        if (card == null || card.lines == null || card.lines.Count < 2)
        {
            return;
        }

        int indexA = UnityEngine.Random.Range(0, card.lines.Count);
        int indexB = UnityEngine.Random.Range(0, card.lines.Count - 1);
        if (indexB >= indexA)
        {
            indexB++;
        }

        LineData temp = card.lines[indexA];
        card.lines[indexA] = card.lines[indexB];
        card.lines[indexB] = temp;
    }

    private void ApplyRemoveLineReward()
    {
        CardData card = ChooseEnhancementCard();
        if (card == null || card.lines == null || card.lines.Count <= 1)
        {
            return;
        }

        int index = UnityEngine.Random.Range(0, card.lines.Count);
        card.lines.RemoveAt(index);
    }

    private void ApplyMaxHpReward(int amount)
    {
        if (Player == null || amount <= 0)
        {
            return;
        }

        Player.maxHP = Mathf.Max(1, Player.maxHP + amount);
        Player.currentHP = Mathf.Min(Player.currentHP + amount, Player.maxHP);
    }

    private void ApplyCurrencyReward(int amount)
    {
        if (Player == null || amount == 0)
        {
            return;
        }

        Player.currency = Mathf.Max(0, Player.currency + amount);
    }

    private void StartEnhancementReward(Action onComplete)
    {
        StartEnhancementReward(onComplete, EnhancementSource.NormalReward);
    }

    private void StartEnhancementReward(Action onComplete, EnhancementSource source)
    {
        if (postBattleEnhancementUI == null)
        {
            onComplete?.Invoke();
            return;
        }

        CardData card = ChooseEnhancementCard();
        if (card == null)
        {
            onComplete?.Invoke();
            return;
        }

        LayerDefinition definition = GetLayerDefinition(currentLayer);
        bool isBoss = definition != null ? definition.isBoss : IsBossLayer(currentLayer);
        float progressOffset = definition != null ? definition.effectProgressOffset : 0f;
        float rareChance = GetEnhancementRareChance(source, isBoss);
        List<EnhancementOption> options = CardEnhancementSystem.GenerateOptions(
            enhancementOptionCount,
            currentLayer,
            maxLayer,
            progressOffset,
            isBoss,
            bossEnhancementProgressBoost,
            rareChance);
        if (damagePopupManager != null)
        {
            damagePopupManager.ClearPopups();
        }
        SetBattleUIVisible(false);
        postBattleEnhancementUI.Show(card, options, option =>
        {
            CardEnhancementSystem.ApplyOption(card, option);
            SaveDeck();
            UpdateViews();
            if (cardSelectionUI != null)
            {
                cardSelectionUI.Hide();
            }
            if (damagePopupManager != null)
            {
                damagePopupManager.ClearPopups();
            }
            onComplete?.Invoke();
        });
    }

    private void StartLineCalibrationReward(Action onComplete)
    {
        StartLineCalibrationReward(onComplete, false);
    }

    private void StartLineCalibrationReward(Action onComplete, bool restoreLayerReward)
    {
        if (lineCalibrationUI == null)
        {
            onComplete?.Invoke();
            return;
        }

        CardData card = ChooseEnhancementCard();
        if (card == null)
        {
            onComplete?.Invoke();
            return;
        }

        if (cardSelectionUI != null)
        {
            cardSelectionUI.Hide();
        }

        if (damagePopupManager != null)
        {
            damagePopupManager.ClearPopups();
        }

        if (restoreLayerReward && layerRewardUI != null)
        {
            layerRewardUI.SetVisible(false);
        }

        SetBattleUIVisible(false);
        lineCalibrationUI.Show(card, () =>
        {
            SaveDeck();
            UpdateViews();
            if (restoreLayerReward && layerRewardUI != null)
            {
                layerRewardUI.SetVisible(true);
            }
            onComplete?.Invoke();
        });
    }

    private void StartLineRemoveReward(Action onComplete, bool restoreLayerReward)
    {
        if (lineRemoveUI == null)
        {
            onComplete?.Invoke();
            return;
        }

        CardData card = ChooseEnhancementCard();
        if (card == null)
        {
            onComplete?.Invoke();
            return;
        }

        if (cardSelectionUI != null)
        {
            cardSelectionUI.Hide();
        }

        if (damagePopupManager != null)
        {
            damagePopupManager.ClearPopups();
        }

        if (restoreLayerReward && layerRewardUI != null)
        {
            layerRewardUI.SetVisible(false);
        }

        SetBattleUIVisible(false);
        lineRemoveUI.Show(card, () =>
        {
            SaveDeck();
            UpdateViews();
            if (restoreLayerReward && layerRewardUI != null)
            {
                layerRewardUI.SetVisible(true);
            }
            onComplete?.Invoke();
        });
    }

    private float GetEnhancementRareChance(EnhancementSource source, bool isBoss)
    {
        switch (source)
        {
            case EnhancementSource.Shop:
                return Mathf.Clamp01(shopEnhancementRareChance);
            default:
                return Mathf.Clamp01(isBoss ? bossEnhancementRareChance : normalEnhancementRareChance);
        }
    }

    private void ShowEnhancementReward()
    {
        StartEnhancementReward(() =>
        {
            if (cardSelectionUI != null)
            {
                cardSelectionUI.Hide();
            }
            if (damagePopupManager != null)
            {
                damagePopupManager.ClearPopups();
            }
            SetBattleUIVisible(true);
            StartNextBattle();
        });
    }

    private void AdvanceLayerAndStartBattle()
    {
        if (currentLayer > maxLayer)
        {
            FinishRun();
            return;
        }

        if (lastClearedWasBoss && shopUI != null)
        {
            lastClearedWasBoss = false;
            shopOpen = true;
            HideOverlayUIsForShop();
            SetBattleUIVisible(false);
            shopUI.Show(Player, shopItemCost, shopResetCost, shopHealAmount, HandleShopPurchase, HandleShopReset, () =>
            {
                shopOpen = false;
                currentLayer += 1;
                if (currentLayer > maxLayer)
                {
                    FinishRun();
                    return;
                }
                StartNextBattle();
            });
            return;
        }

        currentLayer += 1;
        lastEnemyEnhancementLayer = Mathf.Min(lastEnemyEnhancementLayer, currentLayer - 1);
        if (currentLayer > maxLayer)
        {
            FinishRun();
            return;
        }
        StartNextBattle();
    }

    private void FinishRun()
    {
        battleRunning = false;
        if (cardSelectionUI != null)
        {
            cardSelectionUI.Hide();
        }
        if (postBattleEnhancementUI != null)
        {
            postBattleEnhancementUI.Hide();
        }
        if (postBattleRewardUI != null)
        {
            postBattleRewardUI.Hide();
        }
        if (layerRewardUI != null)
        {
            layerRewardUI.Hide();
        }
        if (lineCalibrationUI != null)
        {
            lineCalibrationUI.Hide();
        }
        if (lineRemoveUI != null)
        {
            lineRemoveUI.Hide();
        }
        if (shopUI != null)
        {
            shopUI.Hide();
        }
        SetBattleUIVisible(false);
        if (runResultUI != null)
        {
            int baseScore = totalDamageScore;
            int bonusScore = CalculateClearBonus();
            int totalScore = baseScore + bonusScore;
            CardData summary = BuildSummaryCard(Player != null ? Player.deck : null, Player != null ? Player.name : "Player");
            runResultUI.Show(true, maxLayer, maxLayer, baseScore, totalScore, totalPlayerTurns, summary);
        }
        Debug.Log("Run complete. Layers cleared: " + maxLayer);
    }

    private void HandleShopPurchase(ShopItemType type, int cost, Action<bool> onComplete)
    {
        if (Player == null)
        {
            onComplete?.Invoke(false);
            return;
        }

        if (Player.currency < cost || cost < 0)
        {
            onComplete?.Invoke(false);
            return;
        }

        Player.currency -= cost;
        HideOverlayUIsForShop();

        switch (type)
        {
            case ShopItemType.Heal:
                ApplyHealReward(shopHealAmount);
                UpdateViews();
                onComplete?.Invoke(true);
                return;
            case ShopItemType.Enhancement:
                if (shopUI != null)
                {
                    shopUI.SetVisible(false);
                }
                StartEnhancementReward(() =>
                {
                    UpdateViews();
                    if (shopUI != null)
                    {
                        shopUI.SetVisible(true);
                    }
                    onComplete?.Invoke(true);
                }, EnhancementSource.Shop);
                return;
            case ShopItemType.LineSwap:
                if (shopUI != null)
                {
                    shopUI.SetVisible(false);
                }
                StartLineCalibrationReward(() =>
                {
                    UpdateViews();
                    if (shopUI != null)
                    {
                        shopUI.SetVisible(true);
                    }
                    onComplete?.Invoke(true);
                });
                return;
            case ShopItemType.LineRemove:
                if (shopUI != null)
                {
                    shopUI.SetVisible(false);
                }
                StartLineRemoveReward(() =>
                {
                    UpdateViews();
                    if (shopUI != null)
                    {
                        shopUI.SetVisible(true);
                    }
                    onComplete?.Invoke(true);
                }, false);
                return;
            default:
                onComplete?.Invoke(false);
                return;
        }
    }

    private bool HandleShopReset(int cost)
    {
        if (Player == null)
        {
            return false;
        }

        if (Player.currency < cost || cost < 0)
        {
            return false;
        }

        Player.currency -= cost;
        UpdateViews();
        return true;
    }

    private void HideOverlayUIsForShop()
    {
        if (postBattleRewardUI != null)
        {
            postBattleRewardUI.Hide();
        }
        if (postBattleEnhancementUI != null)
        {
            postBattleEnhancementUI.Hide();
        }
        if (layerRewardUI != null)
        {
            layerRewardUI.Hide();
        }
        if (lineCalibrationUI != null)
        {
            lineCalibrationUI.Hide();
        }
        if (lineRemoveUI != null)
        {
            lineRemoveUI.Hide();
        }
        if (cardSelectionUI != null)
        {
            cardSelectionUI.Hide();
        }
    }

    private void ApplyLayerInfo()
    {
        if (Enemy == null)
        {
            return;
        }

        LayerDefinition definition = GetLayerDefinition(currentLayer);
        if (definition != null && definition.isBoss && !string.IsNullOrEmpty(definition.bossName))
        {
            Enemy.name = definition.bossName;
        }
        else if (IsBossLayer(currentLayer))
        {
            int bossIndex = Mathf.Max(1, currentLayer / Mathf.Max(1, bossInterval));
            Enemy.name = bossNamePrefix + bossIndex;
        }
        else if (!string.IsNullOrEmpty(baseEnemyName))
        {
            Enemy.name = baseEnemyName;
        }

        bool applied = ApplyEnemyDeckForLayer(definition);
        if (!applied)
        {
            ApplyEnemyDeckFromPath(defaultEnemyDeckPath, false);
        }
    }

    private bool ApplyEnemyDeckForLayer(LayerDefinition definition)
    {
        if (Enemy == null)
        {
            return false;
        }

        int layer = definition != null ? definition.layer : currentLayer;
        string path = ResolveEnemyDeckPath(definition, layer);
        if (string.IsNullOrEmpty(path))
        {
            return false;
        }

        bool isBoss = definition != null ? definition.isBoss : IsBossLayer(layer);
        return ApplyEnemyDeckFromPath(path, isBoss);
    }

    private bool ApplyEnemyDeckFromPath(string path, bool isBoss)
    {
        if (Enemy == null || string.IsNullOrEmpty(path))
        {
            return false;
        }

        CardDataList list = CardJsonIO.LoadCardList(path);
        if (list == null || list.cards == null || list.cards.Count == 0)
        {
            return false;
        }

        List<CardData> deck = new List<CardData>(list.cards);
        CardData primaryCard = deck.Count > 0 ? deck[0] : new CardData { lines = new List<LineData>() };
        if (primaryCard.lines == null)
        {
            primaryCard.lines = new List<LineData>();
        }

        AppendAccumulatedEnemyEnhancements(primaryCard);

        if (isBoss)
        {
            deck = new List<CardData> { primaryCard };
        }

        Enemy.actionDeck = deck;
        return true;
    }

    private void LoadLayerDefinitions()
    {
        string path = string.IsNullOrEmpty(layerDefinitionPath) ? DefaultLayerDefinitionPath : layerDefinitionPath;
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        LayerDefinitionList list = LayerDefinitionIO.Load(path);
        if (list == null || list.layers == null || list.layers.Count == 0)
        {
            return;
        }

        layerDefinitions = list.layers;
    }

    private void ApplyEnemyEnhancementIfNeeded()
    {
        if (Enemy == null)
        {
            return;
        }

        if (currentLayer <= lastEnemyEnhancementLayer)
        {
            return;
        }

        if (Enemy.actionDeck == null || Enemy.actionDeck.Count == 0)
        {
            ApplyEnemyDeckFromPath(defaultEnemyDeckPath, false);
        }

        bool applied = false;
        List<EnemyUpgradeOption> options = EnemyEnhancementSystem.GenerateOptions(1, currentLayer);
        if (options != null && options.Count > 0)
        {
            EnemyUpgradeOption option = options[0];
            if (EnemyEnhancementSystem.ApplyOption(Enemy, option))
            {
                RecordEnemyEnhancement(option);
                applied = true;
            }
        }

        if (applied)
        {
            lastEnemyEnhancementLayer = currentLayer;
            LayerDefinition definition = GetLayerDefinition(currentLayer);
            string deckPath = ResolveEnemyDeckPath(definition, currentLayer);
            bool isBoss = definition != null ? definition.isBoss : IsBossLayer(currentLayer);
            ApplyEnemyDeckFromPath(deckPath, isBoss);
        }
    }

    private void ResetEnemyEnhancementTierIfNeeded()
    {
        if (currentLayer != 6 && currentLayer != 11)
        {
            return;
        }

        if (accumulatedEnemyEnhancements != null)
        {
            accumulatedEnemyEnhancements.Clear();
        }

        lastEnemyEnhancementLayer = Mathf.Min(lastEnemyEnhancementLayer, currentLayer - 1);
    }

    private bool IsBossLayer(int layer)
    {
        if (bossInterval <= 0)
        {
            return false;
        }

        return layer % bossInterval == 0;
    }

    private LayerDefinition GetLayerDefinition(int layer)
    {
        if (layerDefinitions == null)
        {
            layerDefinitions = new List<LayerDefinition>();
        }
        EnsureLayerDefinitionsLoaded();

        if (layerDefinitions.Count > 0)
        {
            foreach (var definition in layerDefinitions)
            {
                if (definition != null && definition.layer == layer)
                {
                    return EnsureDefinitionRewards(definition);
                }
            }
        }

        LayerDefinition created = BuildDefaultLayerDefinition(layer);
        if (created != null)
        {
            layerDefinitions.Add(created);
        }
        return created;
    }

    private void EnsureLayerDefinitionsLoaded()
    {
        if (layerDefinitions != null && layerDefinitions.Count > 0)
        {
            return;
        }

        LoadLayerDefinitions();
    }

    private string ResolveEnemyDeckPath(LayerDefinition definition, int layer)
    {
        if (definition != null && !string.IsNullOrEmpty(definition.enemyDeckPath))
        {
            return definition.enemyDeckPath;
        }

        if (definition != null ? definition.isBoss : IsBossLayer(layer))
        {
            return defaultEnemyDeckPath;
        }

        if (layer >= 11)
        {
            return EnemyDeckTier11Path;
        }

        if (layer >= 6)
        {
            return EnemyDeckTier06Path;
        }

        return defaultEnemyDeckPath;
    }

    private LayerDefinition BuildDefaultLayerDefinition(int layer)
    {
        LayerDefinition definition = new LayerDefinition
        {
            layer = layer,
            isBoss = IsBossLayer(layer),
            bossName = string.Empty
        };

        if (definition.isBoss)
        {
            int bossIndex = Mathf.Max(1, layer / Mathf.Max(1, bossInterval));
            definition.bossName = bossNamePrefix + bossIndex;
        }

        if (layerMaxHpReward != 0)
        {
            definition.rewards.Add(new LayerRewardEntry
            {
                type = LayerRewardType.MaxHpUp,
                amount = layerMaxHpReward,
                title = "最大HP強化",
                description = "最大HPが増加します。",
                actionLabel = "受け取る"
            });
        }

        if (layerCurrencyReward != 0)
        {
            definition.rewards.Add(new LayerRewardEntry
            {
                type = LayerRewardType.Currency,
                amount = layerCurrencyReward,
                title = "資金獲得",
                description = "ショップで使える資金を入手します。",
                actionLabel = "受け取る"
            });
        }

        definition.rewards.Add(new LayerRewardEntry
        {
            type = LayerRewardType.Enhancement,
            amount = 1,
            title = "構文付加",
            description = "構文を強化します。ただし、必ず悪い効果が付いてきます。",
            actionLabel = "選択"
        });

        bool addLineSwap = includeLineSwapReward && (definition.isBoss || UnityEngine.Random.value < Mathf.Clamp01(lineSwapRewardChance));
        if (addLineSwap)
        {
            definition.rewards.Add(new LayerRewardEntry
            {
                type = LayerRewardType.LineSwap,
                amount = 1,
                title = "行校正",
                description = "構文の行を2つ選んで入れ替えます。",
                actionLabel = "選択"
            });
        }

        return definition;
    }

    private LayerDefinition EnsureDefinitionRewards(LayerDefinition definition)
    {
        if (definition == null)
        {
            return null;
        }

        if (definition.rewards == null)
        {
            definition.rewards = new List<LayerRewardEntry>();
        }

        if (definition.rewards.Count == 0)
        {
            LayerDefinition defaults = BuildDefaultLayerDefinition(definition.layer);
            if (defaults != null && defaults.rewards != null && defaults.rewards.Count > 0)
            {
                definition.rewards = new List<LayerRewardEntry>(defaults.rewards);
            }
        }

        return definition;
    }

    private void EnsureRewardPoolLoaded()
    {
        if (rewardPoolLoaded)
        {
            return;
        }

        rewardPoolLoaded = true;
        rewardCardPool.Clear();

        if (string.IsNullOrEmpty(rewardCardPoolPath))
        {
            return;
        }

        CardDataList list = CardJsonIO.LoadCardList(rewardCardPoolPath);
        if (list != null && list.cards != null)
        {
            rewardCardPool.AddRange(list.cards);
        }
    }

    private void AddRewardCard(int amount)
    {
        if (Player == null || Player.deck == null || amount <= 0)
        {
            return;
        }

        EnsureRewardPoolLoaded();
        if (rewardCardPool.Count == 0)
        {
            Debug.LogWarning("BattleManager: reward card pool is empty");
            return;
        }

        for (int i = 0; i < amount; i++)
        {
            CardData source = rewardCardPool[UnityEngine.Random.Range(0, rewardCardPool.Count)];
            if (source == null)
            {
                continue;
            }

            Player.deck.Add(CloneCard(source));
        }
    }

    private static CardData CloneCard(CardData source)
    {
        if (source == null)
        {
            return null;
        }

        string json = JsonUtility.ToJson(source);
        CardData clone = JsonUtility.FromJson<CardData>(json);
        return clone;
    }

    private static LineData CloneLine(LineData source)
    {
        if (source == null)
        {
            return null;
        }

        string json = JsonUtility.ToJson(source);
        LineData clone = JsonUtility.FromJson<LineData>(json);
        if (clone != null)
        {
            if (clone.paramsInt == null)
            {
                clone.paramsInt = new List<LineParamInt>();
            }

            if (clone.paramsStr == null)
            {
                clone.paramsStr = new List<LineParamStr>();
            }
        }

        return clone;
    }

    private void RecordEnemyEnhancement(EnemyUpgradeOption option)
    {
        if (option == null || option.line == null)
        {
            return;
        }

        LineData clone = CloneLine(option.line);
        if (clone != null)
        {
            if (accumulatedEnemyEnhancements == null)
            {
                accumulatedEnemyEnhancements = new List<LineData>();
            }
            accumulatedEnemyEnhancements.Add(clone);
        }
    }

    private void AppendAccumulatedEnemyEnhancements(CardData targetCard)
    {
        if (targetCard == null)
        {
            return;
        }

        if (accumulatedEnemyEnhancements == null || accumulatedEnemyEnhancements.Count == 0)
        {
            return;
        }

        if (targetCard.lines == null)
        {
            targetCard.lines = new List<LineData>();
        }

        foreach (LineData line in accumulatedEnemyEnhancements)
        {
            LineData clone = CloneLine(line);
            if (clone != null)
            {
                targetCard.lines.Add(clone);
            }
        }
    }

    private static CardData BuildSummaryCard(List<CardData> deck, string title)
    {
        CardData summary = new CardData
        {
            name = title,
            lines = new List<LineData>()
        };

        if (deck == null)
        {
            return summary;
        }

        foreach (CardData card in deck)
        {
            if (card == null || card.lines == null)
            {
                continue;
            }

            if (!string.IsNullOrEmpty(card.name))
            {
                summary.lines.Add(new LineData
                {
                    displayName = card.name,
                    displayPhrase = card.name,
                    displayValueText = string.Empty,
                    description = string.Empty,
                    type = EffectType.Effect.ToString(),
                    valueScore = 0
                });
            }

            summary.lines.AddRange(card.lines);
        }

        return summary;
    }

    private void HandleBattleLose()
    {
        Debug.Log("Battle lose");
        if (cardSelectionUI != null)
        {
            cardSelectionUI.Hide();
        }
        if (damagePopupManager != null)
        {
            damagePopupManager.ClearPopups();
        }
        if (layerRewardUI != null)
        {
            layerRewardUI.Hide();
        }
        if (lineCalibrationUI != null)
        {
            lineCalibrationUI.Hide();
        }
        if (shopUI != null)
        {
            shopUI.Hide();
        }
        if (runResultUI != null)
        {
            int baseScore = totalDamageScore;
            CardData summary = BuildSummaryCard(Player != null ? Player.deck : null, Player != null ? Player.name : "Player");
            runResultUI.Show(false, currentLayer, maxLayer, baseScore, baseScore, totalPlayerTurns, summary);
            SetBattleUIVisible(false);
            return;
        }
        SetBattleUIVisible(true);
    }

    private CardData ChooseEnhancementCard()
    {
        List<CardData> deck = GetPlayerDeck();
        if (deck.Count == 0)
        {
            return null;
        }

        int index = UnityEngine.Random.Range(0, deck.Count);
        return deck[index];
    }

    private void SaveDeck()
    {
        if (string.IsNullOrEmpty(deckSavePath) || Player == null || Player.deck == null)
        {
            return;
        }

        CardJsonIO.SaveCardList(deckSavePath, new CardDataList { cards = Player.deck });
    }

    private void ResetPlayerForNewRun()
    {
        if (Player == null)
        {
            return;
        }

        Player.maxHP = basePlayerMaxHp > 0 ? basePlayerMaxHp : Player.maxHP;
        Player.currentHP = Player.maxHP;
        Player.shield = 0;
        Player.currency = basePlayerCurrency;

        List<CardData> deck = LoadInitialPlayerDeck();
        if (deck != null && deck.Count > 0)
        {
            Player.deck = deck;
            SaveDeck();
        }
    }

    private List<CardData> LoadInitialPlayerDeck()
    {
        if (string.IsNullOrEmpty(playerDeckInitialPath))
        {
            return null;
        }

        CardDataList list = CardJsonIO.LoadCardList(playerDeckInitialPath);
        if (list == null || list.cards == null || list.cards.Count == 0)
        {
            return null;
        }

        List<CardData> deck = new List<CardData>();
        foreach (CardData card in list.cards)
        {
            CardData clone = CloneCard(card);
            if (clone != null)
            {
                deck.Add(clone);
            }
        }

        return deck;
    }

    private void ResetEnemyForNewRun()
    {
        if (Enemy == null)
        {
            return;
        }

        Enemy.maxHP = baseEnemyMaxHp > 0 ? baseEnemyMaxHp : Enemy.maxHP;
        Enemy.currentHP = Enemy.maxHP;
        Enemy.shield = 0;
        if (!string.IsNullOrEmpty(baseEnemyName))
        {
            Enemy.name = baseEnemyName;
        }
    }

    private void ResetEnemyStatsForLayer()
    {
        if (Enemy == null)
        {
            return;
        }

        float multiplier = GetEnemyHpMultiplierForLayer(currentLayer);
        int scaledHp = Mathf.CeilToInt(Enemy.maxHP * multiplier);
        Enemy.maxHP = Mathf.Max(1, scaledHp);
        Enemy.currentHP = Enemy.maxHP;
        Enemy.shield = 0;
    }

    private void UpdateViews()
    {
        if (playerView != null && Player != null)
        {
            playerView.Initialize(Player.name, Player.currentHP, Player.maxHP, Player.shield);
        }

        if (enemyView != null && Enemy != null)
        {
            enemyView.Initialize(Enemy.name, Enemy.currentHP, Enemy.maxHP, Enemy.shield);
        }

        UpdateActorSprites();
        UpdateScoreText();
        UpdateLineViews();

        if (shopOpen && shopUI != null)
        {
            shopUI.Refresh();
        }
    }

    private void ApplyPendingLineTransforms(BattleContext context)
    {
        if (context == null || context.pendingLineTransforms == null || context.pendingLineTransforms.Count == 0)
        {
            return;
        }

        foreach (PendingLineTransform transform in context.pendingLineTransforms)
        {
            if (transform == null || transform.card == null || transform.card.lines == null || transform.replacement == null)
            {
                continue;
            }

            if (transform.lineIndex < 0 || transform.lineIndex >= transform.card.lines.Count)
            {
                continue;
            }

            transform.card.lines[transform.lineIndex] = transform.replacement;
        }

        context.pendingLineTransforms.Clear();
    }

    public void RefreshViews()
    {
        UpdateViews();
    }

    public void AddScore(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        int adjusted = ApplyScoreMultiplier(amount);
        totalDamageScore += adjusted;
        UpdateScoreText();
    }

    private int ApplyScoreMultiplier(int amount)
    {
        float multiplier = 1f;
        if (currentLayer >= 5)
        {
            multiplier *= 1.3f;
        }
        if (currentLayer >= 10)
        {
            multiplier *= 1.3f;
        }
        if (currentLayer >= 15)
        {
            multiplier *= 1.3f;
        }

        LayerDefinition definition = GetLayerDefinition(currentLayer);
        bool isBoss = definition != null ? definition.isBoss : IsBossLayer(currentLayer);
        if (isBoss)
        {
            multiplier *= 1.5f;
        }

        return Mathf.CeilToInt(amount * multiplier);
    }

    public void NotifyFinalBossDefeated()
    {
        if (IsFinalLayer())
        {
            finalBossDefeated = true;
        }
    }

    public bool IsEnemyImmortal()
    {
        return false;
    }

    private bool IsFinalLayer()
    {
        return currentLayer >= maxLayer;
    }

    private float GetEnemyHpMultiplierForLayer(int layer)
    {
        if (layer >= 11)
        {
            return 1.5f;
        }
        if (layer >= 6)
        {
            return 1.35f;
        }
        return 1.2f;
    }

    private bool IsCurrentLayerBoss()
    {
        LayerDefinition definition = GetLayerDefinition(currentLayer);
        return definition != null ? definition.isBoss : IsBossLayer(currentLayer);
    }

    private void UpdateScoreText()
    {
        if (scoreText != null)
        {
            scoreText.text = "スコア: " + totalDamageScore;
        }
    }

    private int CalculateClearBonus()
    {
        return Mathf.Max(0, 10000 - (totalPlayerTurns * 100));
    }

    private void UpdateLineViews()
    {
        if (playerLineView != null)
        {
            CardData summary = BuildSummaryCard(Player != null ? Player.deck : null, "Player");
            playerLineView.Clear();
            playerLineView.RenderCard(summary);
        }

        if (enemyLineView != null)
        {
            CardData summary = BuildSummaryCard(Enemy != null ? Enemy.actionDeck : null, "Enemy");
            enemyLineView.Clear();
            enemyLineView.RenderCard(summary);
        }
    }

    private void UpdateActorSprites()
    {
        if (playerView != null)
        {
            playerView.SetPortrait(playerSprite);
            playerView.SetPortraitVisible(showActorSprites);
        }

        if (enemyView != null)
        {
            Sprite sprite = IsCurrentLayerBoss() && bossSprite != null ? bossSprite : enemySprite;
            enemyView.SetPortrait(sprite);
            enemyView.SetPortraitVisible(showActorSprites);
        }

        UpdateFieldSprites();
    }

    private void UpdateFieldSprites()
    {
        if (playerFieldSprite != null)
        {
            playerFieldSprite.SetSprite(playerFieldSpriteAsset);
            playerFieldSprite.SetVisible(showFieldSprites);
        }

        if (enemyFieldSprite != null)
        {
            Sprite sprite = IsCurrentLayerBoss() && bossFieldSpriteAsset != null ? bossFieldSpriteAsset : enemyFieldSpriteAsset;
            enemyFieldSprite.SetSprite(sprite);
            enemyFieldSprite.SetVisible(showFieldSprites);
        }
    }

    private Vector3 GetPlayerPopupPosition()
    {
        return playerView != null ? playerView.PopupAnchor.position : Vector3.zero;
    }

    private void StartNextBattle()
    {
        if (currentLayer > maxLayer)
        {
            FinishRun();
            return;
        }
        ResetEnemyEnhancementTierIfNeeded();
        ApplyLayerInfo();
        ApplyEnemyEnhancementIfNeeded();
        if (Enemy != null)
        {
            float multiplier = GetEnemyHpMultiplierForLayer(currentLayer);
            int scaledHp = Mathf.CeilToInt(Enemy.maxHP * multiplier);
            Enemy.maxHP = Mathf.Max(1, scaledHp);
            Enemy.currentHP = Enemy.maxHP;
            Enemy.shield = 0;
            Debug.Log("Enemy revived. HP: " + Enemy.currentHP);
        }

        if (playerContext == null)
        {
            playerContext = new BattleContext();
        }
        if (enemyContext == null)
        {
            enemyContext = new BattleContext();
        }
        playerContext.ResetForCombat();
        enemyContext.ResetForCombat();
        if (Player != null)
        {
            Player.shield = 0;
        }

        if (damagePopupManager != null)
        {
            damagePopupManager.ClearPopups();
        }
        if (layerRewardUI != null)
        {
            layerRewardUI.Hide();
        }
        if (lineCalibrationUI != null)
        {
            lineCalibrationUI.Hide();
        }
        if (shopUI != null)
        {
            shopUI.Hide();
        }
        if (runResultUI != null)
        {
            runResultUI.Hide();
        }
        finalBossDefeated = false;
        SetBattleUIVisible(true);
        BuildDrawPile();
        DrawCards(startingHandSize);
        UpdateViews();

        if (!battleRunning)
        {
            StartCoroutine(BattleLoop());
        }
    }

    private void SetBattleUIVisible(bool visible)
    {
        if (battleUIRoot != null)
        {
            battleUIRoot.SetActive(visible);
            return;
        }

        if (playerView != null)
        {
            playerView.gameObject.SetActive(visible);
        }

        if (enemyView != null)
        {
            enemyView.gameObject.SetActive(visible);
        }

        if (playerLineView != null)
        {
            playerLineView.gameObject.SetActive(visible);
        }

        if (enemyLineView != null)
        {
            enemyLineView.gameObject.SetActive(visible);
        }

        if (scoreText != null)
        {
            scoreText.gameObject.SetActive(visible);
        }
    }

    private void SetTurnText(string text)
    {
        if (turnText != null)
        {
            turnText.text = text;
        }
    }

    private static CardProcessor FindCardProcessor()
    {
        CardProcessor found = FindObjectOfType<CardProcessor>();
        if (found != null)
        {
            return found;
        }

        CardProcessor[] candidates = Resources.FindObjectsOfTypeAll<CardProcessor>();
        foreach (var candidate in candidates)
        {
            if (candidate != null && candidate.gameObject.scene.isLoaded)
            {
                return candidate;
            }
        }

        return null;
    }

    private void BuildDrawPile()
    {
        drawPile.Clear();
        discard.Clear();
        hand.Clear();

        List<CardData> deck = GetPlayerDeck();
        if (deck.Count == 0)
        {
            return;
        }

        drawPile.AddRange(deck);
        Shuffle(drawPile);
    }

    private void DrawCards(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (drawPile.Count == 0)
            {
                if (discard.Count == 0)
                {
                    return;
                }

                drawPile.AddRange(discard);
                discard.Clear();
                Shuffle(drawPile);
            }

            CardData card = drawPile[0];
            drawPile.RemoveAt(0);
            hand.Add(card);
        }
    }

    private static void Shuffle(List<CardData> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            CardData temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }
}
