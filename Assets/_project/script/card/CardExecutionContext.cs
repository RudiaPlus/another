using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Holds the state and context for a single card's execution process.
/// This allows for complex, stateful execution flows (e.g., skipping lines, stopping).
/// </summary>
public class CardExecutionContext
{
    public readonly List<LineData> Lines;
    public readonly BattleContext BattleContext;
    public readonly CardData Card;
    public readonly BattleContext OpponentContext;
    public readonly EnemyData Enemy;
    public readonly PlayerData Player;
    public readonly bool IsPlayerTurn;
    public readonly DamagePopupManager DamagePopupManager;
    public readonly LineDisplayer LineDisplayer;
    public readonly System.Action OnStatsChanged;
    public readonly System.Action<int> OnDamageDealt;
    public readonly System.Action OnFinalBossDefeated;
    public readonly bool EnemyImmortal;
    public readonly Vector3 OwnerPosition;
    public readonly Vector3 TargetPosition; // For VFX
    public readonly MonoBehaviour CoroutineRunner; // To start nested coroutines
    public readonly float LineDelay; // For pacing

    public int CurrentIndex { get; set; }
    public bool ShouldStop { get; set; }
    public bool ShouldSkipNext { get; set; }
    public bool RestartRequested { get; set; }
    public bool RestartCardConsumed { get; set; }
    public bool RedirectNextAttack { get; set; }
    public bool HasLastNumeric { get; set; }
    public int LastNumericAmount { get; set; }
    public bool LastNumericWasHeal { get; set; }
    public bool LastNumericTargetIsPlayer { get; set; }
    public bool LastNumericOwnerIsPlayer { get; set; }
    public DamageElement LastNumericElement { get; set; }

    public CardExecutionContext(List<LineData> lines, BattleContext battleContext, BattleContext opponentContext, CardData card, EnemyData enemy, PlayerData player, bool isPlayerTurn, DamagePopupManager damagePopupManager, LineDisplayer lineDisplayer, System.Action onStatsChanged, System.Action<int> onDamageDealt, System.Action onFinalBossDefeated, bool enemyImmortal, Vector3 ownerPosition, Vector3 targetPosition, MonoBehaviour coroutineRunner, float lineDelay)
    {
        Lines = lines;
        BattleContext = battleContext;
        OpponentContext = opponentContext;
        Card = card;
        Enemy = enemy;
        Player = player;
        IsPlayerTurn = isPlayerTurn;
        DamagePopupManager = damagePopupManager;
        LineDisplayer = lineDisplayer;
        OnStatsChanged = onStatsChanged;
        OnDamageDealt = onDamageDealt;
        OnFinalBossDefeated = onFinalBossDefeated;
        EnemyImmortal = enemyImmortal;
        OwnerPosition = ownerPosition;
        TargetPosition = targetPosition;
        CoroutineRunner = coroutineRunner;
        LineDelay = lineDelay;
        
        CurrentIndex = 0;
        ShouldStop = false;
        ShouldSkipNext = false;
        RestartRequested = false;
        RestartCardConsumed = false;
        RedirectNextAttack = false;
        HasLastNumeric = false;
        LastNumericAmount = 0;
        LastNumericWasHeal = false;
        LastNumericTargetIsPlayer = false;
        LastNumericOwnerIsPlayer = false;
        LastNumericElement = DamageElement.None;
    }

    public LineData GetCurrentLine()
    {
        if (CurrentIndex >= 0 && CurrentIndex < Lines.Count)
        {
            return Lines[CurrentIndex];
        }
        return null;
    }
}
