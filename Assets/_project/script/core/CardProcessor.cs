using System;
using System.Collections;
using UnityEngine;

public class CardProcessor : MonoBehaviour
{
    [SerializeField] private BattleManager battleManager;
    [SerializeField] private DamagePopupManager damagePopupManager;
    [SerializeField] private LineDisplayer lineDisplayer;
    [SerializeField] private Transform enemyAnchor;
    [SerializeField] private Transform playerAnchor;
    [SerializeField] private float lineDelaySeconds = 1.0f;

    private void Awake()
    {
        if (lineDisplayer == null)
        {
            lineDisplayer = FindLineDisplayer();
        }

        if (damagePopupManager == null)
        {
            damagePopupManager = FindObjectOfType<DamagePopupManager>();
        }

        if (battleManager == null)
        {
            battleManager = FindObjectOfType<BattleManager>();
        }
    }

    public IEnumerator ExcuteCard(CardData card, EnemyData target, PlayerData player, BattleContext battleContext, BattleContext opponentContext)
    {
        return ExecuteCardInternal(card, target, player, false, true, battleContext, opponentContext);
    }

    public IEnumerator ExcuteCardOnPlayer(CardData card, PlayerData targetPlayer, EnemyData enemy, BattleContext battleContext, BattleContext opponentContext)
    {
        return ExecuteCardInternal(card, enemy, targetPlayer, true, false, battleContext, opponentContext);
    }

    private IEnumerator ExecuteCardInternal(CardData card, EnemyData enemy, PlayerData player, bool targetIsPlayer, bool ownerIsPlayer, BattleContext battleContext, BattleContext opponentContext)
    {
        if (card == null || card.lines == null)
        {
            Debug.LogWarning("CardProcessor: card or card lines are null");
            yield break;
        }

        Debug.Log("Execute card: " + card.name);

        if (battleContext != null)
        {
            battleContext.cardsPlayedThisCombat++;
        }

        // Create the execution context
        System.Action onStatsChanged = battleManager != null ? battleManager.RefreshViews : null;
        System.Action<int> onDamageDealt = battleManager != null ? battleManager.AddScore : null;
        System.Action onFinalBossDefeated = battleManager != null ? battleManager.NotifyFinalBossDefeated : null;
        bool enemyImmortal = battleManager != null && battleManager.IsEnemyImmortal();
        var executionContext = new CardExecutionContext(
            card.lines,
            battleContext,
            opponentContext,
            card,
            enemy,
            player,
            ownerIsPlayer, // IsPlayerTurn is equivalent to ownerIsPlayer
            damagePopupManager,
            lineDisplayer,
            onStatsChanged,
            onDamageDealt,
            onFinalBossDefeated,
            enemyImmortal,
            GetOwnerPosition(ownerIsPlayer),
            GetTargetPosition(targetIsPlayer),
            this, // CoroutineRunner
            lineDelaySeconds
        );

        // Delegate the entire card execution to the executor
        yield return StartCoroutine(CardEffectExecutor.ExecuteCard(executionContext));
    }

        private static string SafeNameTarget(EnemyData enemy, PlayerData player, bool targetIsPlayer)
    {
        if (targetIsPlayer)
        {
            return player == null ? "Player" : player.name;
        }

        return enemy == null ? "Enemy" : enemy.name;
    }

    public void SetAnchors(Transform enemy, Transform player)
    {
        enemyAnchor = enemy;
        playerAnchor = player;
    }

    private Vector3 GetEnemyPosition()
    {
        return enemyAnchor != null ? enemyAnchor.position : Vector3.zero;
    }

    private Vector3 GetPlayerPosition()
    {
        return playerAnchor != null ? playerAnchor.position : Vector3.zero;
    }

    private Vector3 GetTargetPosition(bool targetIsPlayer)
    {
        return targetIsPlayer ? GetPlayerPosition() : GetEnemyPosition();
    }

    private Vector3 GetOwnerPosition(bool ownerIsPlayer)
    {
        return ownerIsPlayer ? GetPlayerPosition() : GetEnemyPosition();
    }


    private static LineDisplayer FindLineDisplayer()
    {
        LineDisplayer[] candidates = Resources.FindObjectsOfTypeAll<LineDisplayer>();
        foreach (var candidate in candidates)
        {
            if (candidate != null && candidate.gameObject.scene.isLoaded)
            {
                return candidate;
            }
        }

        return null;
    }
}
