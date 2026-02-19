using System.Collections.Generic;
using UnityEngine;

public class BattleBootstrap : MonoBehaviour
{
    [Header("Deck Load")]
    [SerializeField] private string playerDeckPath = "Assets/_project/data/json/card/player_deck.json";
    [SerializeField] private string fallbackCardPath = "Assets/_project/data/json/card/card_001_fireball.json";
    [SerializeField] private bool autoCreateDeckFile = true;
    [SerializeField] private string playerDeckInitialPath = "Assets/_project/data/json/card/player_deck_initial.json";

    [Header("Player")]
    [SerializeField] private string playerId = "player";
    [SerializeField] private string playerName = "Player";
    [SerializeField] private int playerMaxHP = 50;

    [Header("Enemy")]
    [SerializeField] private string enemyId = "enemy_001";
    [SerializeField] private string enemyName = "Enemy";
    [SerializeField] private int enemyMaxHP = 40;

    [Header("Enemy Deck Load")]
    [SerializeField] private string enemyDeckPath = "Assets/_project/data/json/card/enemy_deck.json";
    [SerializeField] private string fallbackEnemyCardPath = "Assets/_project/data/json/card/enemy_card_basic.json";
    [SerializeField] private bool autoCreateEnemyDeckFile = true;

    [Header("Effect Library")]
    [SerializeField] private string effectLibraryPath = "Assets/_project/data/json/effect/effects.json";
    [SerializeField] private bool loadEffectLibraryOnStart = true;

    [Header("References")]
    [SerializeField] private BattleManager battleManager;
    [SerializeField] private ActorView playerView;
    [SerializeField] private ActorView enemyView;
    [SerializeField] private PostBattleRewardUI postBattleRewardUI;
    [SerializeField] private PostBattleEnhancementUI postBattleEnhancementUI;
    [SerializeField] private CardSelectionUI cardSelectionUI;
    [SerializeField] private LayerRewardUI layerRewardUI;
    [SerializeField] private LineCalibrationUI lineCalibrationUI;
    [SerializeField] private LineRemoveUI lineRemoveUI;
    [SerializeField] private ShopUI shopUI;
    [SerializeField] private RunResultUI runResultUI;
    [SerializeField] private TitleScreenUI titleScreenUI;
    [SerializeField] private BattleUIBridge battleUIBridge;
    [SerializeField] private bool startBattleOnBoot = true;

    private void Start()
    {
        if (loadEffectLibraryOnStart)
        {
            EffectLibrary.LoadFromPath(effectLibraryPath);
        }

        ResetPlayerDeckFromInitial();
        List<CardData> deck = LoadPlayerDeck();
        PlayerData player = BuildPlayer(deck);
        List<CardData> enemyDeck = LoadEnemyDeck();
        EnemyData enemy = BuildEnemy(enemyDeck);

        if (battleManager == null)
        {
            Debug.LogWarning("BattleBootstrap: battleManager is not set");
            return;
        }

        battleManager.Initialize(player, enemy);
        battleManager.SetViews(playerView, enemyView);
        battleManager.SetDeckSavePath(playerDeckPath);
        battleManager.SetDefaultEnemyDeckPath(enemyDeckPath);
        battleManager.SetPlayerDeckInitialPath(playerDeckInitialPath);
        battleManager.SetPostBattleRewardUI(postBattleRewardUI);
        battleManager.SetPostBattleUI(postBattleEnhancementUI);
        battleManager.SetCardSelectionUI(cardSelectionUI);
        battleManager.SetLayerRewardUI(layerRewardUI);
        battleManager.SetLineCalibrationUI(lineCalibrationUI);
        battleManager.SetLineRemoveUI(lineRemoveUI);
        battleManager.SetShopUI(shopUI);
        battleManager.SetRunResultUI(runResultUI);
        battleManager.SetUIBridge(battleUIBridge);
        if (titleScreenUI != null)
        {
            titleScreenUI.Initialize(battleManager, runResultUI);
        }

        if (startBattleOnBoot)
        {
            battleManager.StartBattle();
        }
        else if (titleScreenUI != null)
        {
            battleManager.SetBattleUIVisibleExternal(false);
            titleScreenUI.Show();
        }
    }

    private List<CardData> LoadPlayerDeck()
    {
        CardDataList list = CardJsonIO.LoadCardList(playerDeckPath);
        if (list != null && list.cards != null && list.cards.Count > 0)
        {
            return list.cards;
        }

        List<CardData> fallbackDeck = new List<CardData>();
        if (!string.IsNullOrEmpty(fallbackCardPath))
        {
            CardData fallbackCard = CardJsonIO.LoadCard(fallbackCardPath);
            if (fallbackCard != null)
            {
                fallbackDeck.Add(fallbackCard);
            }
        }

        if (autoCreateDeckFile && fallbackDeck.Count > 0)
        {
            CardJsonIO.SaveCardList(playerDeckPath, new CardDataList { cards = new List<CardData>(fallbackDeck) });
        }

        return fallbackDeck;
    }

    private void ResetPlayerDeckFromInitial()
    {
        if (string.IsNullOrEmpty(playerDeckInitialPath))
        {
            return;
        }

        CardDataList list = CardJsonIO.LoadCardList(playerDeckInitialPath);
        if (list == null || list.cards == null || list.cards.Count == 0)
        {
            Debug.LogWarning("BattleBootstrap: initial deck is missing or empty");
            return;
        }

        CardJsonIO.SaveCardList(playerDeckPath, list);
    }

    private PlayerData BuildPlayer(List<CardData> deck)
    {
        return new PlayerData
        {
            id = playerId,
            name = playerName,
            maxHP = playerMaxHP,
            currentHP = playerMaxHP,
            currency = 0,
            deck = deck
        };
    }

    private EnemyData BuildEnemy(List<CardData> enemyDeck)
    {
        return new EnemyData
        {
            id = enemyId,
            name = enemyName,
            maxHP = enemyMaxHP,
            currentHP = enemyMaxHP,
            actionDeck = enemyDeck
        };
    }

    private List<CardData> LoadEnemyDeck()
    {
        CardDataList list = CardJsonIO.LoadCardList(enemyDeckPath);
        if (list != null && list.cards != null && list.cards.Count > 0)
        {
            return list.cards;
        }

        List<CardData> fallbackDeck = new List<CardData>();
        if (!string.IsNullOrEmpty(fallbackEnemyCardPath))
        {
            CardData fallbackCard = CardJsonIO.LoadCard(fallbackEnemyCardPath);
            if (fallbackCard != null)
            {
                fallbackDeck.Add(fallbackCard);
            }
        }

        if (autoCreateEnemyDeckFile && fallbackDeck.Count > 0)
        {
            CardJsonIO.SaveCardList(enemyDeckPath, new CardDataList { cards = new List<CardData>(fallbackDeck) });
        }

        return fallbackDeck;
    }
}
