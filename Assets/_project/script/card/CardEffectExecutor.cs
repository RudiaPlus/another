using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public static class CardEffectExecutor
{
    private static readonly HashSet<string> TransformSupportedLineIds = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
    {
        "attack_normal",
        "self_dmg",
        "buff_atk",
        "debuff_def",
        "skip_next_line",
        "stop_execution",
        "restart_card",
        "damage_scale_by_line_count",
        "damage_scale_by_line_number",
        "conditional_power_boost",
        "damage_scale_by_missing_hp",
        "damage_scale_by_enemy_current_hp",
        "damage_scale_by_total_char_count",
        "damage_sum_all_numerics",
        "global_damage_modifier",
        "shield_scale_by_cards_played",
        "perm_max_hp_reduction",
        "negate_curse_and_heal",
        "self_damage_scale_by_line_number",
        "amplify_following_curse",
        "heal_normal",
        "heal_scale_by_negative_lines",
        "conditional_boost_low_damage",
        "skip_and_shield_by_value",
        "heal_if_prev_self_dmg",
        "randomize_next_line_value",
        "shield_scale_by_missing_hp",
        "shield_normal",
        "repeat_previous_line",
        "repeat_last_numeric",
        "probabilistic_redirect",
        "transfer_debuff",
        "destroy_adjacent_lines",
        "copy_to_another_card",
        "pi",
        "hallucination",
        "heavy_pressure",
        "misfire",
        "gravity_sink",
        "waste_shields",
        "corrosion",
        "safety_measure",
        "decay"
    };
    public static IEnumerator ExecuteCard(CardExecutionContext context)
    {
        int executionDepth = 0;
        const int maxDepth = 99;

        while (context.CurrentIndex < context.Lines.Count)
        {
            executionDepth++;
            if (executionDepth > maxDepth)
            {
                Debug.LogError("Card execution exceeded max depth, breaking loop.");
                break;
            }

            if (context.RestartRequested)
            {
                context.RestartRequested = false;
                context.CurrentIndex = 0;
            }

            if (context.CurrentIndex >= context.Lines.Count) break;

            if (context.ShouldSkipNext)
            {
                if(context.GetCurrentLine() != null)
                    Debug.Log($"Skipping line {context.CurrentIndex}: {context.GetCurrentLine().displayName}");
                context.ShouldSkipNext = false;
                context.CurrentIndex++;
                continue;
            }

            LineData currentLine = context.GetCurrentLine();
            if (currentLine != null)
            {
                yield return context.CoroutineRunner.StartCoroutine(ApplyLineLogic(currentLine, context));
            }

            if (context.ShouldStop)
            {
                Debug.Log("Stopping card execution.");
                break;
            }

            // We increment the index here, but restart logic will reset it.
            context.CurrentIndex++;
        }
    }

    private static IEnumerator ApplyLineLogic(LineData line, CardExecutionContext context)
    {
        bool ownerIsPlayer = context.IsPlayerTurn;
        bool targetIsPlayer = !ownerIsPlayer;
        bool redirectAttack = context.RedirectNextAttack &&
            (IsDamageLine(line) || (line.lineID == "repeat_last_numeric" && context.HasLastNumeric && !context.LastNumericWasHeal));
        if (redirectAttack)
        {
            targetIsPlayer = ownerIsPlayer;
            context.RedirectNextAttack = false;
        }
        Vector3 targetPosition = targetIsPlayer == ownerIsPlayer ? context.OwnerPosition : context.TargetPosition;
        
        if (context.LineDisplayer != null)
        {
            context.LineDisplayer.DisplayLine(line, context.Enemy, context.Player, ownerIsPlayer);
        }

        // --- Preview Logic ---
        int previewAmount = 0;
        DamageElement previewElement = DamageElement.None;
        bool previewBoosted = false;
        bool showPreview = context.DamagePopupManager != null && TryGetPreviewValue(line, context, out previewAmount, out previewElement, out previewBoosted);
        float previewDuration = 0f;

        if (showPreview)
        {
            int previewDisplayAmount = previewAmount;
            if (IsPreviewDamageLine(line, context))
            {
                previewDisplayAmount = GetDisplayedDamage(previewAmount, context.BattleContext);
            }
            context.DamagePopupManager.ShowDamagePreview(targetPosition, previewDisplayAmount, previewBoosted, previewElement);
            previewDuration = context.DamagePopupManager.PreviewDuration;
            if(previewDuration > 0)
                yield return new WaitForSeconds(previewDuration);
        }

        // --- Main Execution ---
        float impactDuration = 0f;
        bool handled = true;
        switch (line.lineID)
        {
            case "perm_strength_up":
                {
                    int amount = line.GetInt("value");
                    if (context.BattleContext != null)
                    {
                        if (ownerIsPlayer)
                        {
                            context.BattleContext.playerStrength += amount;
                            Debug.Log($"Player Strength increased by {amount} (Total: {context.BattleContext.playerStrength})");
                        }
                        else
                        {
                            context.BattleContext.enemyStrength += amount;
                            Debug.Log($"Enemy Strength increased by {amount} (Total: {context.BattleContext.enemyStrength})");
                        }
                    }
                }
                break;

            case "skip_next_line": context.ShouldSkipNext = true; break;
            case "stop_execution": context.ShouldStop = true; break;
            case "restart_card":
                {
                    if (context.RestartCardConsumed)
                    {
                        break;
                    }

                    context.RestartCardConsumed = true;
                    context.RestartRequested = true;
                }
                break;
            case "repeat_previous_line":
                {
                    int previousIndex = context.CurrentIndex - 1;
                    if (previousIndex >= 0 && previousIndex < context.Lines.Count)
                    {
                        LineData previousLine = context.Lines[previousIndex];
                        if (previousLine != null && previousLine.lineID != "repeat_previous_line")
                        {
                            int originalIndex = context.CurrentIndex;
                            context.CurrentIndex = previousIndex;
                            if (context.CoroutineRunner != null)
                            {
                                yield return context.CoroutineRunner.StartCoroutine(ApplyLineLogic(previousLine, context));
                            }
                            else
                            {
                                yield return ApplyLineLogic(previousLine, context);
                            }
                            context.CurrentIndex = originalIndex;
                        }
                    }
                }
                break;
            case "repeat_last_numeric":
                {
                    if (context.HasLastNumeric)
                    {
                        int percentage = line.GetInt("value", 100);
                        int repeatAmount = Mathf.CeilToInt(context.LastNumericAmount * percentage / 100f);
                        if (context.BattleContext != null)
                        {
                            repeatAmount = Mathf.CeilToInt(repeatAmount * context.BattleContext.ConsumeNextLineMultiplier());
                        }
                        if (repeatAmount > 0)
                        {
                            bool lastTargetIsPlayer = context.LastNumericTargetIsPlayer;
                            bool lastOwnerIsPlayer = context.LastNumericOwnerIsPlayer;
                            if (!context.LastNumericWasHeal && redirectAttack)
                            {
                                lastTargetIsPlayer = targetIsPlayer;
                                lastOwnerIsPlayer = ownerIsPlayer;
                            }
                            Vector3 repeatPosition = lastTargetIsPlayer == lastOwnerIsPlayer ? context.OwnerPosition : context.TargetPosition;
                            if (context.LastNumericWasHeal)
                            {
                                ApplyHeal(repeatAmount, context.Enemy, context.Player, lastTargetIsPlayer, context.OnStatsChanged);
                                RecordNumericChange(context, repeatAmount, true, lastTargetIsPlayer, lastOwnerIsPlayer, DamageElement.None);
                                if (context.DamagePopupManager != null)
                                {
                                    context.DamagePopupManager.ShowHeal(repeatPosition, repeatAmount);
                                    impactDuration = context.DamagePopupManager.ImpactDuration;
                                }
                            }
                            else
                            {
                                ApplyDamage(repeatAmount, context.Enemy, context.Player, lastTargetIsPlayer, lastOwnerIsPlayer, context.BattleContext, context.OnStatsChanged, context.OnDamageDealt, context.OnFinalBossDefeated, context.EnemyImmortal);
                                RecordNumericChange(context, repeatAmount, false, lastTargetIsPlayer, lastOwnerIsPlayer, context.LastNumericElement);
                                if (context.DamagePopupManager != null)
                                {
                                    int displayDamage = GetDisplayedDamage(repeatAmount, context.BattleContext);
                                    context.DamagePopupManager.ShowDamageImpact(repeatPosition, displayDamage, true, context.LastNumericElement);
                                    impactDuration = context.DamagePopupManager.ImpactDuration;
                                }
                            }
                        }
                    }
                }
                break;
            case "copy_to_another_card":
            case "pi":
                {
                    if (context.Card != null && context.Card.lines != null && context.Card.lines.Count > 0)
                    {
                        int sourceIndex = Random.Range(0, context.Card.lines.Count);
                        LineData sourceLine = context.Card.lines[sourceIndex];
                        LineData clone = CloneLineData(sourceLine);
                        if (clone != null)
                        {
                            int insertIndex = Random.Range(0, context.Card.lines.Count + 1);
                            context.Card.lines.Insert(insertIndex, clone);
                            if (insertIndex <= context.CurrentIndex)
                            {
                                context.CurrentIndex++;
                            }
                            if (context.OnStatsChanged != null) context.OnStatsChanged.Invoke();
                        }
                    }
                }
                break;
            case "hallucination":
                {
                    // Create a random effect line from the library
                    // Use a dummy original line just to get a valid random line
                    LineData randomEffect = CreateRandomReplacementLine(null);
                    
                    if (randomEffect != null)
                    {
                        // Prevent infinite recursion if it somehow picks hallucination again
                        // (CreateRandomReplacementLine already excludes hallucination by default logic below, but double check)
                        if (randomEffect.lineID != "hallucination")
                        {
                            Debug.Log($"Hallucination triggered: Executing {randomEffect.displayName}");
                            if (context.CoroutineRunner != null)
                            {
                                yield return context.CoroutineRunner.StartCoroutine(ApplyLineLogic(randomEffect, context));
                            }
                            else
                            {
                                yield return ApplyLineLogic(randomEffect, context);
                            }
                        }
                    }
                }
                break;
            case "probabilistic_redirect":
                {
                    int chance = line.GetInt("chance", line.GetInt("value", 0));
                    if (chance > 0)
                    {
                        int roll = Random.Range(0, 100);
                        if (roll < chance)
                        {
                            context.RedirectNextAttack = true;
                        }
                    }
                }
                break;
            case "self_dmg":
                {
                    int selfDamage = line.GetInt("value");
                    if (context.BattleContext != null)
                    {
                        selfDamage *= context.BattleContext.curseMultiplier;
                    }
                    DamageElement selfDmgElement = ParseDamageElement(line.GetStr("dmgType"));
                    ApplySelfDamage(selfDamage, context.Enemy, context.Player, ownerIsPlayer, context.BattleContext, context.OnStatsChanged, context.OnFinalBossDefeated, context.EnemyImmortal);
                    RecordNumericChange(context, selfDamage, false, ownerIsPlayer, ownerIsPlayer, selfDmgElement);
                    if (context.DamagePopupManager != null)
                    {
                        int displayDamage = GetDisplayedDamage(selfDamage, context.BattleContext);
                        context.DamagePopupManager.ShowDamageImpact(context.OwnerPosition, displayDamage, false, selfDmgElement);
                        impactDuration = context.DamagePopupManager.ImpactDuration;
                    }
                }
                break;

            case "buff_atk":
            case "debuff_def":
                {
                    string targetStat = line.GetStr("targetStat");
                    int baseAmount = line.GetInt("value");
                    float multiplier = (context.BattleContext != null) ? context.BattleContext.ConsumeNextLineMultiplier() : 1f;
                    int amount = Mathf.CeilToInt(baseAmount * multiplier);
                    
                    if (!string.IsNullOrEmpty(targetStat) && amount != 0 && context.BattleContext != null)
                    {
                        context.BattleContext.AddBuff(targetStat, amount);
                        Debug.Log($"Applied Buff/Debuff: {targetStat} by {amount}");
                    }
                }
                break;

            // --- New Scaling Effects ---
            case "damage_scale_by_enemy_current_hp":
                {
                    int percentage = line.GetInt("value");
                    int currentHP = targetIsPlayer ? (context.Player != null ? context.Player.currentHP : 0) : (context.Enemy != null ? context.Enemy.currentHP : 0);
                    int baseDamage = Mathf.CeilToInt(currentHP * percentage / 100f);
                    int strengthBonus = GetStrengthBonus(context.BattleContext, ownerIsPlayer);
                    float multiplier = (context.BattleContext != null) ? context.BattleContext.ConsumeNextLineMultiplier() : 1f;
                    int damage = Mathf.CeilToInt((baseDamage + strengthBonus) * multiplier);
                    
                    ApplyDamage(damage, context.Enemy, context.Player, targetIsPlayer, ownerIsPlayer, context.BattleContext, context.OnStatsChanged, context.OnDamageDealt, context.OnFinalBossDefeated, context.EnemyImmortal);
                    RecordNumericChange(context, damage, false, targetIsPlayer, ownerIsPlayer, DamageElement.None);
                    if (context.DamagePopupManager != null)
                    {
                        int displayDamage = GetDisplayedDamage(damage, context.BattleContext);
                        context.DamagePopupManager.ShowDamageImpact(targetPosition, displayDamage, true, DamageElement.None);
                        impactDuration = context.DamagePopupManager.ImpactDuration;
                    }
                }
                break;
            case "transfer_debuff":
                {
                    if (context.BattleContext != null && context.BattleContext.buffs != null && context.BattleContext.buffs.Count > 0 && context.OpponentContext != null)
                    {
                        string selectedKey = null;
                        int selectedValue = 0;
                        foreach (var entry in context.BattleContext.buffs)
                        {
                            if (entry.Value < selectedValue)
                            {
                                selectedValue = entry.Value;
                                selectedKey = entry.Key;
                            }
                        }

                        if (!string.IsNullOrEmpty(selectedKey))
                        {
                            context.BattleContext.buffs.Remove(selectedKey);
                            context.OpponentContext.AddBuff(selectedKey, selectedValue);
                            Debug.Log($"Transferred debuff: {selectedKey} {selectedValue} -> target");
                            if (context.OnStatsChanged != null)
                            {
                                context.OnStatsChanged.Invoke();
                            }
                        }
                    }
                }
                break;

            case "self_damage_scale_by_line_number":
                {
                    int multiplier = line.GetInt("value", 1);
                    int selfDamage = multiplier * (context.CurrentIndex + 1); // 1-indexed
                    if (context.BattleContext != null)
                    {
                        selfDamage *= context.BattleContext.curseMultiplier;
                    }
                    ApplySelfDamage(selfDamage, context.Enemy, context.Player, ownerIsPlayer, context.BattleContext, context.OnStatsChanged, context.OnFinalBossDefeated, context.EnemyImmortal);
                    RecordNumericChange(context, selfDamage, false, ownerIsPlayer, ownerIsPlayer, DamageElement.None);
                    if (context.DamagePopupManager != null)
                    {
                        int displayDamage = GetDisplayedDamage(selfDamage, context.BattleContext);
                        context.DamagePopupManager.ShowDamageImpact(context.OwnerPosition, displayDamage, false, DamageElement.None);
                        impactDuration = context.DamagePopupManager.ImpactDuration;
                    }
                }
                break;

            case "heal_normal":
                {
                    int healAmount = line.GetInt("value");
                    if (context.BattleContext != null)
                    {
                        healAmount = Mathf.CeilToInt(healAmount * context.BattleContext.ConsumeNextLineMultiplier());
                    }
                    ApplyHeal(healAmount, context.Enemy, context.Player, ownerIsPlayer, context.OnStatsChanged);
                    RecordNumericChange(context, healAmount, true, ownerIsPlayer, ownerIsPlayer, DamageElement.None);
                    Debug.Log($"Healed player for {healAmount}");
                    if (context.DamagePopupManager != null && healAmount > 0)
                    {
                        context.DamagePopupManager.ShowHeal(context.OwnerPosition, healAmount);
                        impactDuration = context.DamagePopupManager.ImpactDuration;
                    }
                }
                break;

            case "amplify_following_curse":
                {
                    int multiplier = line.GetInt("value", 5);
                    if (context.BattleContext != null)
                    {
                        context.BattleContext.curseMultiplier *= multiplier;
                        Debug.Log($"Curse multiplier increased to {context.BattleContext.curseMultiplier}x");
                    }
                }
                break;

            case "heal_scale_by_negative_lines":
                {
                    int multiplier = line.GetInt("value", 3);
                    int negativeCount = 0;
                    if (context.Lines != null)
                    {
                        foreach (var l in context.Lines)
                        {
                            if (l.valueScore < 0)
                            {
                                negativeCount++;
                            }
                        }
                    }
                    int healAmount = negativeCount * multiplier;
                    
                    if (context.BattleContext != null)
                    {
                         healAmount = Mathf.CeilToInt(healAmount * context.BattleContext.ConsumeNextLineMultiplier());
                    }

                    ApplyHeal(healAmount, context.Enemy, context.Player, ownerIsPlayer, context.OnStatsChanged);
                    RecordNumericChange(context, healAmount, true, ownerIsPlayer, ownerIsPlayer, DamageElement.None);
                    if (context.DamagePopupManager != null && healAmount > 0)
                    {
                        context.DamagePopupManager.ShowHeal(context.OwnerPosition, healAmount);
                        impactDuration = context.DamagePopupManager.ImpactDuration;
                    }
                }
                break;

            case "conditional_boost_low_damage":
                {
                    if (context.CurrentIndex + 1 < context.Lines.Count)
                    {
                        LineData nextLine = context.Lines[context.CurrentIndex + 1];
                        int baseVal = nextLine.GetInt("baseValue", nextLine.GetInt("value"));
                        int threshold = line.GetInt("threshold", 10);
                        
                        // Condition: Is Attack AND Base value <= Threshold
                        if (IsAttackLine(nextLine) && baseVal <= threshold && baseVal > 0)
                        {
                            int boost = line.GetInt("bonusValue", 20);
                            // Note: Adding to generic atkBuff for now, which lasts for the turn.
                            // Ideally should be a one-time next-line bonus.
                            if (context.BattleContext != null)
                            {
                                context.BattleContext.AddBuff("atkBuff", boost);
                                Debug.Log($"Conditional Boost: Added {boost} to atkBuff (low damage detected)");
                            }
                        }
                    }
                }
                break;

            case "shield_scale_by_missing_hp":
                {
                    if (context.Player != null)
                    {
                        int percentage = line.GetInt("value");
                        int missingHp = context.Player.maxHP - context.Player.currentHP;
                        int shieldAmount = Mathf.CeilToInt(missingHp * percentage / 100f);
                        
                        if (context.BattleContext != null)
                        {
                            shieldAmount = Mathf.CeilToInt(shieldAmount * context.BattleContext.ConsumeNextLineMultiplier());
                        }

                        if (ownerIsPlayer && context.Player != null)
                        {
                            context.Player.shield += shieldAmount;
                        }
                        else if (!ownerIsPlayer && context.Enemy != null)
                        {
                            context.Enemy.shield += shieldAmount;
                        }

                        if (context.OnStatsChanged != null) context.OnStatsChanged.Invoke();
                        
                        if (context.DamagePopupManager != null && shieldAmount > 0)
                        {
                            context.DamagePopupManager.ShowShield(context.OwnerPosition, shieldAmount);
                            impactDuration = context.DamagePopupManager.ImpactDuration;
                        }
                    }
                }
                break;

            case "shield_normal":
                {
                    int shieldAmount = line.GetInt("value");
                    if (context.BattleContext != null)
                    {
                        shieldAmount = Mathf.CeilToInt(shieldAmount * context.BattleContext.ConsumeNextLineMultiplier());
                    }

                    if (ownerIsPlayer && context.Player != null)
                    {
                        context.Player.shield += shieldAmount;
                    }
                    else if (!ownerIsPlayer && context.Enemy != null)
                    {
                        context.Enemy.shield += shieldAmount;
                    }

                    if (context.OnStatsChanged != null)
                    {
                        context.OnStatsChanged.Invoke();
                    }

                    if (context.DamagePopupManager != null && shieldAmount > 0)
                    {
                        context.DamagePopupManager.ShowShield(context.OwnerPosition, shieldAmount);
                        impactDuration = context.DamagePopupManager.ImpactDuration;
                    }
                }
                break;

            case "damage_scale_by_line_count":
                {
                    float multiplier = line.GetFloat("value", 1f);
                    int lineCount = context.Lines != null ? context.Lines.Count : 0;
                    int baseDamage = Mathf.CeilToInt(multiplier * lineCount);
                    int strengthBonus = GetStrengthBonus(context.BattleContext, ownerIsPlayer);
                    int damage = baseDamage + strengthBonus;
                    if (context.BattleContext != null)
                    {
                        damage = Mathf.CeilToInt(damage * context.BattleContext.ConsumeNextLineMultiplier());
                    }
                    ApplyDamage(damage, context.Enemy, context.Player, targetIsPlayer, ownerIsPlayer, context.BattleContext, context.OnStatsChanged, context.OnDamageDealt, context.OnFinalBossDefeated, context.EnemyImmortal);
                    RecordNumericChange(context, damage, false, targetIsPlayer, ownerIsPlayer, DamageElement.None);
                    if (context.DamagePopupManager != null)
                    {
                        int displayDamage = GetDisplayedDamage(damage, context.BattleContext);
                        context.DamagePopupManager.ShowDamageImpact(targetPosition, displayDamage, true, DamageElement.None);
                        impactDuration = context.DamagePopupManager.ImpactDuration;
                    }
                }
                break;

            case "damage_scale_by_line_number":
                {
                    int multiplier = line.GetInt("value", 1);
                    int baseDamage = multiplier * (context.CurrentIndex + 1);
                    int strengthBonus = GetStrengthBonus(context.BattleContext, ownerIsPlayer);
                    int damage = baseDamage + strengthBonus;
                    if (context.BattleContext != null)
                    {
                        damage = Mathf.CeilToInt(damage * context.BattleContext.ConsumeNextLineMultiplier());
                    }
                    ApplyDamage(damage, context.Enemy, context.Player, targetIsPlayer, ownerIsPlayer, context.BattleContext, context.OnStatsChanged, context.OnDamageDealt, context.OnFinalBossDefeated, context.EnemyImmortal);
                    RecordNumericChange(context, damage, false, targetIsPlayer, ownerIsPlayer, DamageElement.None);
                    if (context.DamagePopupManager != null)
                    {
                        int displayDamage = GetDisplayedDamage(damage, context.BattleContext);
                        context.DamagePopupManager.ShowDamageImpact(targetPosition, displayDamage, true, DamageElement.None);
                        impactDuration = context.DamagePopupManager.ImpactDuration;
                    }
                }
                break;

            case "damage_scale_by_total_char_count":
                {
                    int percentage = line.GetInt("value", 50);
                    int totalChars = 0;
                    if (context.Card != null && !string.IsNullOrEmpty(context.Card.name))
                    {
                        totalChars += context.Card.name.Length;
                    }
                    if (context.Lines != null)
                    {
                        foreach (var l in context.Lines)
                        {
                            if (!string.IsNullOrEmpty(l.displayName)) totalChars += l.displayName.Length;
                            if (!string.IsNullOrEmpty(l.description)) totalChars += l.description.Length;
                        }
                    }
                    int baseDamage = Mathf.CeilToInt(totalChars * percentage / 100f);
                    int strengthBonus = GetStrengthBonus(context.BattleContext, ownerIsPlayer);
                    int damage = baseDamage + strengthBonus;
                    if (context.BattleContext != null)
                    {
                        damage = Mathf.CeilToInt(damage * context.BattleContext.ConsumeNextLineMultiplier());
                    }
                    ApplyDamage(damage, context.Enemy, context.Player, targetIsPlayer, ownerIsPlayer, context.BattleContext, context.OnStatsChanged, context.OnDamageDealt, context.OnFinalBossDefeated, context.EnemyImmortal);
                    RecordNumericChange(context, damage, false, targetIsPlayer, ownerIsPlayer, DamageElement.None);
                    if (context.DamagePopupManager != null)
                    {
                        int displayDamage = GetDisplayedDamage(damage, context.BattleContext);
                        context.DamagePopupManager.ShowDamageImpact(targetPosition, displayDamage, true, DamageElement.None);
                        impactDuration = context.DamagePopupManager.ImpactDuration;
                    }
                }
                break;

            case "skip_and_shield_by_value":
                {
                    context.ShouldSkipNext = true;
                    if (context.CurrentIndex + 1 < context.Lines.Count)
                    {
                        LineData nextLine = context.Lines[context.CurrentIndex + 1];
                        int value = nextLine.GetInt("value", nextLine.GetInt("baseValue"));
                        if (value > 0)
                        {
                            int shieldAmount = value;
                            if (context.BattleContext != null)
                            {
                                shieldAmount = Mathf.CeilToInt(shieldAmount * context.BattleContext.ConsumeNextLineMultiplier());
                            }
                            if (ownerIsPlayer && context.Player != null)
                            {
                                context.Player.shield += shieldAmount;
                            }
                            else if (!ownerIsPlayer && context.Enemy != null)
                            {
                                context.Enemy.shield += shieldAmount;
                            }
                            if (context.OnStatsChanged != null) context.OnStatsChanged.Invoke();
                            if (context.DamagePopupManager != null)
                            {
                                context.DamagePopupManager.ShowShield(context.OwnerPosition, shieldAmount);
                                impactDuration = context.DamagePopupManager.ImpactDuration;
                            }
                        }
                    }
                }
                break;

            case "heal_if_prev_self_dmg":
                {
                    int prevIndex = context.CurrentIndex - 1;
                    if (prevIndex >= 0 && prevIndex < context.Lines.Count)
                    {
                        LineData prevLine = context.Lines[prevIndex];
                        if (prevLine != null &&
                            (prevLine.lineID == "self_dmg" || prevLine.lineID.Contains("self_damage")))
                        {
                            // Try to get value. For scaling effects, this might be inaccurate without re-calculation,
                            // but re-calculation is complex. Using static value for now.
                            // Better approach: check context.HasLastNumeric etc? 
                            // But previous line might have been skipped or failed.
                            // Let's rely on LastNumeric if it matches the previous line execution.
                            // But LastNumeric stores the *result*, which includes buffs/multipliers.
                            // The request says "recover 150% of that damage amount". 
                            // Using LastNumericAmount is closest to "that damage amount".
                            
                            int healAmount = 0;
                            // Check if last numeric change was self damage
                            // Self damage: Target == Owner, !WasHeal, Element usually not None (or check ID)
                            if (context.HasLastNumeric && !context.LastNumericWasHeal && 
                                context.LastNumericTargetIsPlayer == context.LastNumericOwnerIsPlayer)
                            {
                                healAmount = Mathf.CeilToInt(context.LastNumericAmount * 1.5f);
                            }
                            else
                            {
                                // Fallback: Calculate from params (only works for fixed value)
                                int val = prevLine.GetInt("value");
                                healAmount = Mathf.CeilToInt(val * 1.5f);
                            }

                            if (healAmount > 0)
                            {
                                if (context.BattleContext != null)
                                {
                                    healAmount = Mathf.CeilToInt(healAmount * context.BattleContext.ConsumeNextLineMultiplier());
                                }
                                ApplyHeal(healAmount, context.Enemy, context.Player, ownerIsPlayer, context.OnStatsChanged);
                                RecordNumericChange(context, healAmount, true, ownerIsPlayer, ownerIsPlayer, DamageElement.None);
                                if (context.DamagePopupManager != null)
                                {
                                    context.DamagePopupManager.ShowHeal(context.OwnerPosition, healAmount);
                                    impactDuration = context.DamagePopupManager.ImpactDuration;
                                }
                            }
                        }
                    }
                }
                break;

            case "randomize_next_line_value":
                {
                    if (context.BattleContext != null)
                    {
                        // Random multiplier between 0 and 2. 
                        float multiplier = Random.Range(0f, 2.01f);
                        context.BattleContext.nextLineMultiplier = multiplier;
                        Debug.Log($"Chaos: Next line multiplier set to {multiplier:F2}x");
                    }
                }
                break;

            case "heavy_pressure":
                {
                    if (Random.value < 0.5f)
                    {
                        context.ShouldSkipNext = true;
                        Debug.Log("Heavy Pressure: Next line will be skipped.");
                        if (Random.value < 0.5f && context.CurrentIndex + 2 < context.Lines.Count)
                        {
                            // 25% overall chance to skip an extra line (50% skip-next * 50% extra).
                            context.CurrentIndex++; // Skip one now, the loop will skip the second.
                            Debug.Log("Heavy Pressure: EXTRA! Second line will also be skipped.");
                        }
                    }
                }
                break;

            case "misfire":
                {
                    if (Random.value < 0.5f)
                    {
                        context.RedirectNextAttack = true;
                        // Force redirection to Player specifically if possible
                        // RedirectNextAttack logic in ApplyLineLogic swaps target. 
                        // If player is owner, target becomes player.
                        Debug.Log("Misfire: Next attack target might be redirected to self!");
                    }
                }
                break;

            case "gravity_sink":
                {
                    // Swaps this line with the next line permanently in the card
                    if (context.Card != null && context.Card.lines != null)
                    {
                        int currentIndex = context.CurrentIndex;
                        int nextIndex = currentIndex + 1;
                        if (nextIndex < context.Card.lines.Count)
                        {
                            LineData temp = context.Card.lines[currentIndex];
                            context.Card.lines[currentIndex] = context.Card.lines[nextIndex];
                            context.Card.lines[nextIndex] = temp;
                            Debug.Log("Gravity: Line swapped with the next one.");
                            if (context.OnStatsChanged != null) context.OnStatsChanged.Invoke();
                        }
                    }
                }
                break;

            case "waste_shields":
                {
                    if (ownerIsPlayer && context.Player != null)
                    {
                        context.Player.shield = 0;
                    }
                    else if (!ownerIsPlayer && context.Enemy != null)
                    {
                        context.Enemy.shield = 0;
                    }
                    if (context.OnStatsChanged != null) context.OnStatsChanged.Invoke();
                    Debug.Log("Waste: All shields lost!");
                }
                break;

            case "corrosion":
                {
                    if (context.BattleContext != null)
                    {
                        context.BattleContext.curseMultiplier *= 3;
                        Debug.Log("Corrosion: Self-damage tripled for the rest of the turn.");
                    }
                }
                break;

            case "safety_measure":
                {
                    if (context.BattleContext != null)
                    {
                        // Persistent reduction of 90% from this line onwards
                        context.BattleContext.baseDamageMultiplier *= 0.1f;
                        Debug.Log("Safety Measure: Damage from this line onwards reduced by 90%.");
                    }
                }
                break;

            case "decay":
                {
                    if (context.BattleContext != null)
                    {
                        context.BattleContext.nextLineMultiplier = 0.5f;
                        Debug.Log("Decay: Next line damage reduced by 50%.");
                    }
                }
                break;

            case "conditional_power_boost":
                {
                    int requiredLines = line.GetInt("requiredLines");
                    if (context.Lines.Count >= requiredLines)
                    {
                        int boost = line.GetInt("boostMultiplier");
                        context.BattleContext.nextLineMultiplier = boost;
                        Debug.Log($"Next line power boosted by {boost}x");
                    }
                }
                break;

            case "damage_scale_by_missing_hp":
                {
                    if (context.Player != null)
                    {
                        int percentage = line.GetInt("value");
                        int missingHp = context.Player.maxHP - context.Player.currentHP;
                        int baseDamage = missingHp * percentage / 100;
                        int strengthBonus = GetStrengthBonus(context.BattleContext, ownerIsPlayer);
                        int damage = baseDamage + strengthBonus;
                        if (context.BattleContext != null)
                        {
                            damage = Mathf.CeilToInt(damage * context.BattleContext.ConsumeNextLineMultiplier());
                        }
                        ApplyDamage(damage, context.Enemy, context.Player, targetIsPlayer, ownerIsPlayer, context.BattleContext, context.OnStatsChanged, context.OnDamageDealt, context.OnFinalBossDefeated, context.EnemyImmortal);
                        RecordNumericChange(context, damage, false, targetIsPlayer, ownerIsPlayer, DamageElement.None);
                        if (context.DamagePopupManager != null)
                        {
                            int displayDamage = GetDisplayedDamage(damage, context.BattleContext);
                            context.DamagePopupManager.ShowDamageImpact(targetPosition, displayDamage, true, DamageElement.None);
                            impactDuration = context.DamagePopupManager.ImpactDuration;
                        }
                    }
                }
                break;
            
            case "damage_sum_all_numerics":
                {
                    int totalDamage = 0;
                    foreach (var l in context.Lines)
                    {
                        if(l.paramsInt != null)
                        {
                            foreach (var p in l.paramsInt)
                            {
                                totalDamage += Mathf.Abs(p.value);
                            }
                        }
                    }

                    int baseDamage = totalDamage;
                    int strengthBonus = GetStrengthBonus(context.BattleContext, ownerIsPlayer);
                    int damage = baseDamage + strengthBonus;
                    if (context.BattleContext != null)
                    {
                        damage = Mathf.CeilToInt(damage * context.BattleContext.ConsumeNextLineMultiplier());
                    }
                    ApplyDamage(damage, context.Enemy, context.Player, targetIsPlayer, ownerIsPlayer, context.BattleContext, context.OnStatsChanged, context.OnDamageDealt, context.OnFinalBossDefeated, context.EnemyImmortal);
                    if (context.DamagePopupManager != null)
                    {
                        int displayDamage = GetDisplayedDamage(damage, context.BattleContext);
                        context.DamagePopupManager.ShowDamageImpact(context.TargetPosition, displayDamage, true, DamageElement.None);
                        impactDuration = context.DamagePopupManager.ImpactDuration;
                    }
                }
                break;
            
            case "global_damage_modifier":
                {
                    int amp = line.GetInt("value");
                    if (context.BattleContext != null)
                    {
                        context.BattleContext.globalDamageAmp += amp;
                    }
                    if (context.OpponentContext != null)
                    {
                        context.OpponentContext.globalDamageAmp += amp;
                    }
                    Debug.Log($"Global damage amp applied: +{amp}% to both sides.");

                    int shieldPercent = line.GetInt("shieldPercent");
                    if (shieldPercent > 0)
                    {
                        int maxHP = ownerIsPlayer && context.Player != null
                            ? context.Player.maxHP
                            : (!ownerIsPlayer && context.Enemy != null ? context.Enemy.maxHP : 0);
                        if (maxHP > 0)
                        {
                            int shieldAmount = Mathf.CeilToInt(maxHP * shieldPercent / 100f);
                            if (ownerIsPlayer && context.Player != null)
                            {
                                context.Player.shield += shieldAmount;
                            }
                            else if (!ownerIsPlayer && context.Enemy != null)
                            {
                                context.Enemy.shield += shieldAmount;
                            }
                            if (context.OnStatsChanged != null)
                            {
                                context.OnStatsChanged.Invoke();
                            }
                            if (context.DamagePopupManager != null && shieldAmount > 0)
                            {
                                context.DamagePopupManager.ShowShield(context.OwnerPosition, shieldAmount);
                            }
                        }
                    }
                }
                break;

            case "shield_scale_by_cards_played":
                {
                    if (context.BattleContext != null)
                    {
                        int multiplier = line.GetInt("value");
                        // The count includes the current card, so we subtract 1 if we want cards played *before* this one.
                        // Let's assume the intent is "cards played so far in this combat".
                        int cardsPlayed = context.BattleContext.cardsPlayedThisCombat;
                        int shieldAmount = multiplier * cardsPlayed;
                        shieldAmount = Mathf.CeilToInt(shieldAmount * context.BattleContext.ConsumeNextLineMultiplier()); // Apply boost
                        if (ownerIsPlayer && context.Player != null)
                        {
                            context.Player.shield += shieldAmount;
                            Debug.Log($"Player gained {shieldAmount} shield.");
                        }
                        else if (!ownerIsPlayer && context.Enemy != null)
                        {
                            context.Enemy.shield += shieldAmount;
                            Debug.Log($"Enemy gained {shieldAmount} shield.");
                        }
                        if (shieldAmount > 0)
                        {
                            if (context.OnStatsChanged != null)
                            {
                                context.OnStatsChanged.Invoke();
                            }
                            if (context.DamagePopupManager != null)
                            {
                                context.DamagePopupManager.ShowShield(context.OwnerPosition, shieldAmount);
                            }
                        }
                    }
                }
                break;

            case "perm_max_hp_reduction":
                {
                    if (context.Player != null)
                    {
                        int percentage = line.GetInt("value");
                        int newMaxHP = Mathf.CeilToInt(context.Player.maxHP * (1 - percentage / 100f));
                        context.Player.maxHP = Mathf.Max(1, newMaxHP); // Ensure max HP doesn't go below 1
                        context.Player.currentHP = Mathf.Min(context.Player.currentHP, context.Player.maxHP);
                        Debug.Log($"Player max HP reduced to {context.Player.maxHP}");
                    }
                }
                break;

            case "negate_curse_and_heal":
                {
                    if (context.CurrentIndex + 1 < context.Lines.Count)
                    {
                        LineData nextLine = context.Lines[context.CurrentIndex + 1];
                        if (nextLine.valueScore < 0)
                        {
                            context.ShouldSkipNext = true;
                            int healAmount = line.GetInt("value");
                            ApplyHeal(healAmount, context.Enemy, context.Player, ownerIsPlayer, context.OnStatsChanged);
                            RecordNumericChange(context, healAmount, true, ownerIsPlayer, ownerIsPlayer, DamageElement.None);
                            Debug.Log($"Negated curse and healed for {healAmount}");
                            if (context.DamagePopupManager != null && healAmount > 0)
                            {
                                context.DamagePopupManager.ShowHeal(context.OwnerPosition, healAmount);
                            }
                        }
                    }
                }
                break;
            case "destroy_adjacent_lines":
                {
                    int currentIndex = context.CurrentIndex;
                    int prevIndex = currentIndex - 1;
                    int nextIndex = currentIndex + 1;
                    if (nextIndex >= 0 && nextIndex < context.Lines.Count)
                    {
                        context.Lines.RemoveAt(nextIndex);
                    }
                    if (currentIndex >= 0 && currentIndex < context.Lines.Count)
                    {
                        context.Lines.RemoveAt(currentIndex);
                    }
                    if (prevIndex >= 0 && prevIndex < context.Lines.Count)
                    {
                        context.Lines.RemoveAt(prevIndex);
                    }
                    if (context.Lines.Count > 0)
                    {
                        context.CurrentIndex = Mathf.Clamp(context.CurrentIndex - 2, -1, context.Lines.Count - 1);
                    }
                    else
                    {
                        context.CurrentIndex = -1;
                    }
                    if (context.OnStatsChanged != null) context.OnStatsChanged.Invoke();
                }
                break;

            default:
                handled = false;
                break;
        }

        if (!handled)
        {
            if (TryResolveAttack(line, context.BattleContext, ownerIsPlayer, out int finalDamage, out DamageElement element, out bool boosted))
            {
                ApplyDamage(finalDamage, context.Enemy, context.Player, targetIsPlayer, ownerIsPlayer, context.BattleContext, context.OnStatsChanged, context.OnDamageDealt, context.OnFinalBossDefeated, context.EnemyImmortal);
                RecordNumericChange(context, finalDamage, false, targetIsPlayer, ownerIsPlayer, element);
                if (context.DamagePopupManager != null)
                {
                    int displayDamage = GetDisplayedDamage(finalDamage, context.BattleContext);
                    context.DamagePopupManager.ShowDamageImpact(targetPosition, displayDamage, boosted, element);
                    impactDuration = context.DamagePopupManager.ImpactDuration;
                }
            }
            else
            {
                Debug.LogWarning($"Unhandled lineID '{line.lineID}'. No action taken.");
            }
        }

        // --- Delay Logic ---
        if (impactDuration > 0)
            yield return new WaitForSeconds(impactDuration);
        
        float remainingDelay = context.LineDelay - previewDuration - impactDuration;
        if (remainingDelay > 0f)
        {
            yield return new WaitForSeconds(remainingDelay);
        }
    }

    private static bool IsDamageLine(LineData line)
    {
        if (line == null) return false;
        if (IsAttackLine(line)) return true;
        switch (line.lineID)
        {
            case "damage_scale_by_line_count":
            case "damage_scale_by_line_number":
            case "damage_scale_by_missing_hp":
            case "damage_scale_by_enemy_current_hp":
            case "damage_scale_by_total_char_count":
            case "damage_sum_all_numerics":
                return true;
            default:
                return false;
        }
    }

    private static bool IsPreviewDamageLine(LineData line, CardExecutionContext context)
    {
        if (line == null || context == null)
        {
            return false;
        }

        if (IsDamageLine(line))
        {
            return true;
        }

        if (line.lineID == "repeat_last_numeric")
        {
            return context.HasLastNumeric && !context.LastNumericWasHeal;
        }

        if (line.lineID == "repeat_previous_line")
        {
            int previousIndex = context.CurrentIndex - 1;
            if (previousIndex >= 0 && previousIndex < context.Lines.Count)
            {
                LineData previousLine = context.Lines[previousIndex];
                return IsDamageLine(previousLine);
            }
        }

        return false;
    }

    private static int GetDisplayedDamage(int amount, BattleContext context)
    {
        if (context == null)
        {
            return amount;
        }

        int amp = context.globalDamageAmp + context.GetBuff("globalDamageAmp");
        if (amp == 0)
        {
            return amount;
        }

        return Mathf.CeilToInt(amount * (1 + amp / 100f));
    }

    private static int GetStrengthBonus(BattleContext context, bool ownerIsPlayer)
    {
        if (context == null)
        {
            return 0;
        }

        return ownerIsPlayer ? context.playerStrength : context.enemyStrength;
    }

    private static bool GetPreviewTargetIsPlayer(CardExecutionContext context, LineData line)
    {
        if (context == null)
        {
            return false;
        }

        bool targetIsPlayer = !context.IsPlayerTurn;
        bool redirectPreview = context.RedirectNextAttack &&
            (IsDamageLine(line) || (line != null && line.lineID == "repeat_last_numeric" && context.HasLastNumeric && !context.LastNumericWasHeal));
        if (redirectPreview)
        {
            targetIsPlayer = context.IsPlayerTurn;
        }

        return targetIsPlayer;
    }

    private static void RecordNumericChange(CardExecutionContext context, int amount, bool isHeal, bool targetIsPlayer, bool ownerIsPlayer, DamageElement element)
    {
        if (context == null || amount <= 0)
        {
            return;
        }

        context.HasLastNumeric = true;
        context.LastNumericAmount = amount;
        context.LastNumericWasHeal = isHeal;
        context.LastNumericTargetIsPlayer = targetIsPlayer;
        context.LastNumericOwnerIsPlayer = ownerIsPlayer;
        context.LastNumericElement = element;
    }

    private static LineData CloneLineData(LineData source)
    {
        if (source == null)
        {
            return null;
        }

        LineData clone = new LineData
        {
            effectId = source.effectId,
            lineID = source.lineID,
            displayName = source.displayName,
            displayPhrase = source.displayPhrase,
            displayValueText = source.displayValueText,
            description = source.description,
            type = source.type,
            valueScore = source.valueScore,
            paramsInt = new List<LineParamInt>(),
            paramsStr = new List<LineParamStr>()
        };

        if (source.paramsInt != null)
        {
            foreach (LineParamInt param in source.paramsInt)
            {
                if (param != null)
                {
                    clone.paramsInt.Add(new LineParamInt { key = param.key, value = param.value });
                }
            }
        }

        if (source.paramsStr != null)
        {
            foreach (LineParamStr param in source.paramsStr)
            {
                if (param != null)
                {
                    clone.paramsStr.Add(new LineParamStr { key = param.key, value = param.value });
                }
            }
        }

        return clone;
    }

    private static LineData CreateRandomReplacementLine(LineData original)
    {
        if (!EffectLibrary.IsLoaded)
        {
            return null;
        }

        IReadOnlyList<EffectDefinition> allEffects = EffectLibrary.GetAll();
        if (allEffects == null || allEffects.Count == 0)
        {
            return null;
        }

        List<EffectDefinition> candidates = new List<EffectDefinition>();
        for (int i = 0; i < allEffects.Count; i++)
        {
            EffectDefinition definition = allEffects[i];
            if (!IsSupportedEffectDefinition(definition))
            {
                continue;
            }

            if (definition.lineID != null && definition.lineID.Equals("hallucination", System.StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (original != null)
            {
                if (!string.IsNullOrEmpty(original.effectId) &&
                    string.Equals(definition.id, original.effectId, System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (string.IsNullOrEmpty(original.effectId) && !string.IsNullOrEmpty(original.lineID) &&
                    string.Equals(definition.lineID, original.lineID, System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
            }

            candidates.Add(definition);
        }

        if (candidates.Count == 0)
        {
            return null;
        }

        EffectDefinition selected = candidates[Random.Range(0, candidates.Count)];
        return selected != null ? selected.ToLineData() : null;
    }

    private static bool IsSupportedEffectDefinition(EffectDefinition definition)
    {
        if (definition == null || string.IsNullOrEmpty(definition.lineID))
        {
            return false;
        }

        if (TransformSupportedLineIds.Contains(definition.lineID))
        {
            return true;
        }

        if (!string.IsNullOrEmpty(definition.type) && definition.type.Equals("Effect", System.StringComparison.OrdinalIgnoreCase))
        {
            return HasIntParam(definition, "baseValue") || HasIntParam(definition, "value");
        }

        return false;
    }

    private static bool HasIntParam(EffectDefinition definition, string key)
    {
        if (definition == null || definition.paramsInt == null)
        {
            return false;
        }

        foreach (LineParamInt param in definition.paramsInt)
        {
            if (param != null && param.key == key)
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsAttackLine(LineData line)
    {
        if (line == null) return false;
        if (IsNonAttackLineId(line.lineID))
        {
            return false;
        }

        if (line.GetEffectType() == EffectType.Effect || line.lineID == "attack_normal")
        {
             return line.GetInt("baseValue", line.GetInt("value")) != 0;
        }
        return false;
    }

    private static bool IsNonAttackLineId(string lineId)
    {
        if (string.IsNullOrEmpty(lineId))
        {
            return false;
        }

        switch (lineId)
        {
            case "heal_normal":
            case "negate_curse_and_heal":
            case "global_damage_modifier":
            case "shield_scale_by_cards_played":
            case "perm_max_hp_reduction":
            case "conditional_power_boost":
            case "heal_scale_by_negative_lines":
            case "conditional_boost_low_damage":
            case "shield_scale_by_missing_hp":
            case "shield_normal":
            case "skip_and_shield_by_value":
            case "heal_if_prev_self_dmg":
            case "randomize_next_line_value":
            case "repeat_previous_line":
            case "repeat_last_numeric":
            case "probabilistic_redirect":
            case "transfer_debuff":
            case "destroy_adjacent_lines":
            case "copy_to_another_card":
            case "pi":
            case "heavy_pressure":
            case "misfire":
            case "gravity_sink":
            case "waste_shields":
            case "corrosion":
            case "safety_measure":
            case "decay":
            case "hallucination":
            case "skip_next_line":
            case "stop_execution":
            case "restart_card":
                return true;
            default:
                return false;
        }
    }

    public static bool TryResolveAttack(LineData line, BattleContext context, bool isPlayer, out int finalDamage, out DamageElement element, out bool boosted, bool consumeMultiplier = true)
    {
        finalDamage = 0;
        element = DamageElement.None;
        boosted = false;

        if (!IsAttackLine(line)) return false;

        int baseDamage = line.GetInt("baseValue", line.GetInt("value"));
        int bonus = 0;
        float multiplier = 1f;
        if (context != null)
        {
            bonus += context.GetBuff("atkBuff");
            bonus += context.GetBuff("EnemyDefenseDown");
            
            // Add Strength
            if (isPlayer)
            {
                bonus += context.playerStrength;
            }
            else
            {
                bonus += context.enemyStrength;
            }

            multiplier = consumeMultiplier ? context.ConsumeNextLineMultiplier() : context.PeekNextLineMultiplier();
            multiplier *= context.baseDamageMultiplier;
        }
        finalDamage = Mathf.Max(0, Mathf.CeilToInt((baseDamage + bonus) * multiplier));
        boosted = bonus > 0 || multiplier > 1.01f;
        element = ParseDamageElement(line.GetStr("dmgType"));
        return true;
    }

    public static bool TryGetPreviewValue(LineData line, BattleContext context, out int amount, out DamageElement element, out bool boosted)
    {
        amount = 0;
        element = DamageElement.None;
        boosted = false;

        if (line == null) return false;

        if (TryResolveAttack(line, context, true, out int finalDamage, out element, out boosted, false))
        {
            amount = finalDamage;
            return true;
        }

        if (line.lineID == "buff_atk" || line.lineID == "debuff_def")
        {
            amount = line.GetInt("value");
            boosted = amount > 0;
            return true;
        }

        return false;
    }

    public static bool TryGetPreviewValue(LineData line, CardExecutionContext context, out int amount, out DamageElement element, out bool boosted)
    {
        amount = 0;
        element = DamageElement.None;
        boosted = false;

        if (line == null || context == null)
        {
            return false;
        }

        switch (line.lineID)
        {
            case "repeat_previous_line":
            {
                int previousIndex = context.CurrentIndex - 1;
                if (previousIndex >= 0 && previousIndex < context.Lines.Count)
                {
                    LineData previousLine = context.Lines[previousIndex];
                    if (previousLine != null && previousLine.lineID != "repeat_previous_line")
                    {
                        int originalIndex = context.CurrentIndex;
                        context.CurrentIndex = previousIndex;
                        bool result = TryGetPreviewValue(previousLine, context, out amount, out element, out boosted);
                        context.CurrentIndex = originalIndex;
                        return result;
                    }
                }
                return false;
            }
            case "repeat_last_numeric":
            {
                if (!context.HasLastNumeric || context.LastNumericWasHeal)
                {
                    return false;
                }
                int percentage = line.GetInt("value", 100);
                amount = Mathf.CeilToInt(context.LastNumericAmount * percentage / 100f);
                element = context.LastNumericElement;
                boosted = amount > 0;
                return amount != 0;
            }
            case "damage_scale_by_line_count":
            {
                float multiplier = line.GetFloat("value", 1f);
                int lineCount = context.Lines != null ? context.Lines.Count : 0;
                amount = Mathf.CeilToInt(multiplier * lineCount);
                amount += GetStrengthBonus(context.BattleContext, context.IsPlayerTurn);
                boosted = amount > 0;
                return true;
            }
            case "damage_scale_by_line_number":
            {
                int multiplier = line.GetInt("value", 1);
                amount = multiplier * (context.CurrentIndex + 1);
                amount += GetStrengthBonus(context.BattleContext, context.IsPlayerTurn);
                boosted = amount > 0;
                return true;
            }
            case "damage_scale_by_missing_hp":
            {
                if (context.Player == null)
                {
                    return false;
                }
                int percentage = line.GetInt("value");
                int missingHp = context.Player.maxHP - context.Player.currentHP;
                amount = missingHp * percentage / 100;
                amount += GetStrengthBonus(context.BattleContext, context.IsPlayerTurn);
                boosted = amount > 0;
                return true;
            }
            case "damage_sum_all_numerics":
            {
                int total = 0;
                if (context.Lines != null)
                {
                    foreach (LineData lineData in context.Lines)
                    {
                        if (lineData != null && lineData.paramsInt != null)
                        {
                            foreach (LineParamInt param in lineData.paramsInt)
                            {
                                if (param != null)
                                {
                                    total += param.value;
                                }
                            }
                        }
                    }
                }
                amount = total + GetStrengthBonus(context.BattleContext, context.IsPlayerTurn);
                boosted = amount > 0;
                return true;
            }
            case "damage_scale_by_enemy_current_hp":
            {
                bool targetIsPlayer = !context.IsPlayerTurn;
                int percentage = line.GetInt("value");
                int currentHP = targetIsPlayer
                    ? (context.Player != null ? context.Player.currentHP : 0)
                    : (context.Enemy != null ? context.Enemy.currentHP : 0);
                amount = Mathf.CeilToInt(currentHP * percentage / 100f);
                amount += GetStrengthBonus(context.BattleContext, context.IsPlayerTurn);
                boosted = amount > 0;
                return true;
            }
            case "damage_scale_by_total_char_count":
            {
                int percentage = line.GetInt("value", 50);
                int totalChars = 0;
                if (context.Card != null && !string.IsNullOrEmpty(context.Card.name))
                {
                    totalChars += context.Card.name.Length;
                }
                if (context.Lines != null)
                {
                    foreach (var l in context.Lines)
                    {
                        if (!string.IsNullOrEmpty(l.displayName)) totalChars += l.displayName.Length;
                        if (!string.IsNullOrEmpty(l.description)) totalChars += l.description.Length;
                    }
                }
                amount = Mathf.CeilToInt(totalChars * percentage / 100f);
                amount += GetStrengthBonus(context.BattleContext, context.IsPlayerTurn);
                boosted = amount > 0;
                return true;
            }
        }

        if (TryResolveAttack(line, context.BattleContext, context.IsPlayerTurn, out int finalDamage, out element, out boosted, false))
        {
            amount = finalDamage;
            return true;
        }

        return TryGetPreviewValue(line, context.BattleContext, out amount, out element, out boosted);
    }

    public static void ApplyDamage(int amount, EnemyData enemy, PlayerData player, bool targetIsPlayer, bool ownerIsPlayer, BattleContext context, System.Action onStatsChanged, System.Action<int> onDamageDealt, System.Action onFinalBossDefeated, bool enemyImmortal)
    {
        if (context != null)
        {
            int amp = context.globalDamageAmp + context.GetBuff("globalDamageAmp");
            if (amp != 0)
            {
                amount = Mathf.CeilToInt(amount * (1 + amp / 100f));
            }
        }

        if (targetIsPlayer)
        {
            if (player != null)
            {
                int damageToShield = Mathf.Min(player.shield, amount);
                player.shield -= damageToShield;
                int remainingDamage = amount - damageToShield;
                player.currentHP -= remainingDamage;
            }
        }
        else
        {
            if (enemy != null)
            {
                int damageToShield = Mathf.Min(enemy.shield, amount);
                enemy.shield -= damageToShield;
                int remainingDamage = amount - damageToShield;
                enemy.currentHP -= remainingDamage;

                if (ownerIsPlayer && amount > 0 && onDamageDealt != null)
                {
                    onDamageDealt.Invoke(amount);
                }

                if (enemyImmortal && enemy.currentHP <= 0)
                {
                    enemy.currentHP = 1;
                    if (onFinalBossDefeated != null)
                    {
                        onFinalBossDefeated.Invoke();
                    }
                }
            }
        }
        if (onStatsChanged != null)
        {
            onStatsChanged.Invoke();
        }
    }

    public static void ApplySelfDamage(int amount, EnemyData enemy, PlayerData player, bool ownerIsPlayer, BattleContext context, System.Action onStatsChanged, System.Action onFinalBossDefeated, bool enemyImmortal)
    {
        if (context != null)
        {
            int amp = context.globalDamageAmp + context.GetBuff("globalDamageAmp");
            if (amp != 0)
            {
                amount = Mathf.CeilToInt(amount * (1 + amp / 100f));
            }
        }

        if (ownerIsPlayer)
        {
            if (player != null)
            {
                player.currentHP -= amount;
            }
        }
        else
        {
            if (enemy != null)
            {
                enemy.currentHP -= amount;
                if (enemyImmortal && enemy.currentHP <= 0)
                {
                    enemy.currentHP = 1;
                    if (onFinalBossDefeated != null)
                    {
                        onFinalBossDefeated.Invoke();
                    }
                }
            }
        }
        if (onStatsChanged != null)
        {
            onStatsChanged.Invoke();
        }
    }

    public static void ApplyHeal(int amount, EnemyData enemy, PlayerData player, bool ownerIsPlayer, System.Action onStatsChanged)
    {
        if (amount <= 0)
        {
            return;
        }

        if (ownerIsPlayer)
        {
            if (player != null)
            {
                player.currentHP = Mathf.Min(player.currentHP + amount, player.maxHP);
            }
        }
        else
        {
            if (enemy != null)
            {
                enemy.currentHP = Mathf.Min(enemy.currentHP + amount, enemy.maxHP);
            }
        }

        if (onStatsChanged != null)
        {
            onStatsChanged.Invoke();
        }
    }

    public static DamageElement ParseDamageElement(string dmgType)
    {
        if (string.IsNullOrEmpty(dmgType)) return DamageElement.None;
        switch (dmgType.ToLowerInvariant())
        {
            case "true_damage": case "true": case "physical": case "normal": return DamageElement.None;
            case "fire": return DamageElement.Fire;
            case "ice": return DamageElement.Ice;
            case "lightning": return DamageElement.Lightning;
            case "poison": return DamageElement.Poison;
            case "dark": return DamageElement.Dark;
            default: return DamageElement.None;
        }
    }

    private static string SafeNameTarget(EnemyData enemy, PlayerData player, bool targetIsPlayer)
    {
        if (targetIsPlayer)
        {
            return player == null ? "Player" : player.name;
        }
        return enemy == null ? "Enemy" : enemy.name;
    }

}
