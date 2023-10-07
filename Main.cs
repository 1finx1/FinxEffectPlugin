using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using Rocket.Core.Plugins;
using SDG.Unturned;
using System.Collections.Generic;
using UnityEngine;
using Rocket.API.Collections;
using static System.Net.WebRequestMethods;

namespace SpeedBoostPlugin
{
    public class SpeedBoostPlugin : RocketPlugin<SpeedBoostConfiguration>
    {
        private Dictionary<UnturnedPlayer, Coroutine> speedEffectCoroutines = new Dictionary<UnturnedPlayer, Coroutine>();
        private Dictionary<UnturnedPlayer, Coroutine> jumpBoostCoroutines = new Dictionary<UnturnedPlayer, Coroutine>();
        private Dictionary<UnturnedPlayer, Coroutine> healingEffectCoroutines = new Dictionary<UnturnedPlayer, Coroutine>();


        public override TranslationList DefaultTranslations => new TranslationList
{
    {"effect_jump", "Jump Boost effect has been applied for ({0}) seconds!"},
    {"effect_healing", "You will be healed ({0}) Health every ({1}) seconds for ({2}) seconds!"},
    {"effect_speed", "Speed Boost effect has been applied for ({0}) seconds!"},
    {"effect_jump_finished", "Jump Boost Tea has ended!"},
    {"effect_healing_finished", "Healing Tea has ended!"},
    {"effect_speed_finished", "Speed Booost Tea has ended!"},
    {"effect_healing_remaining", "You have ({0}) seconds remaining for Healing Tea effect!"},
    {"effect_speed_remaining", "You have ({0}) seconds remaining for Speed Boost Tea effect!"},
    {"effect_jump_remaining", "You have ({0}) seconds remaining for Jump Boost Tea effect!"},
};
        protected override void Load()
        {
            UseableConsumeable.onConsumeRequested += HandleItemConsumption;
        }

        protected override void Unload()
        {
            UseableConsumeable.onConsumeRequested -= HandleItemConsumption;
        }

        private void ApplySpeedEffect(UnturnedPlayer player, float speedMultiplier, float duration)
        {
            // Apply the speed effect for the specified duration
            player.Player.movement.sendPluginSpeedMultiplier(speedMultiplier);

            Coroutine coroutine = StartCoroutine(CheckSpeedEffectDuration(player, speedMultiplier, duration));

            // Store the coroutine for later use
            speedEffectCoroutines[player] = coroutine;

            UnturnedChat.Say(player, Translate("effect_speed", duration));
        }

        private System.Collections.IEnumerator CheckSpeedEffectDuration(UnturnedPlayer player, float speedMultiplier, float duration)
        {
            float timer = duration;

            while (timer > 0)
            {
                if (timer <= 10 && timer % 5 == 0)
                {
                    // Send a message when there are 10 seconds left and then every 5 seconds
                    UnturnedChat.Say(player, Translate("effect_speed_remaining", timer));
                }

                yield return new WaitForSeconds(1f);
                timer--;
            }

            // Duration has elapsed, remove the speed effect
            RemoveSpeedEffect(player);
        }

        private void RemoveSpeedEffect(UnturnedPlayer player)
        {
            // Revert the player's speed back to the normal speed
            player.Player.movement.sendPluginSpeedMultiplier(1.0f);

            // Remove the coroutine from the dictionary
            speedEffectCoroutines.Remove(player);

            // Notify the player that the speed effect has ended (optional)
            UnturnedChat.Say(player, Translate("effect_speed_finished"));
        }

        private static readonly float JUMP = 1f;

        private void ApplyJumpBoost(UnturnedPlayer player, float multiplier, float duration)
        {
            // Store the original jump multiplier
            float originalJumpMultiplier = JUMP;

            // Apply the new jump multiplier
            player.Player.movement.sendPluginJumpMultiplier(originalJumpMultiplier * multiplier);

            Coroutine coroutine = StartCoroutine(CheckJumpBoostDuration(player, originalJumpMultiplier, duration));

            // Store the coroutine for later use
            jumpBoostCoroutines[player] = coroutine;

            UnturnedChat.Say(player, Translate("effect_jump", duration));
        }

        private System.Collections.IEnumerator CheckJumpBoostDuration(UnturnedPlayer player, float originalJumpMultiplier, float duration)
        {
            float timer = duration;

            while (timer > 0)
            {
                if (timer <= 10 && timer % 5 == 0)
                {
                    // Send a message when there are 10 seconds left and then every 5 seconds
                    UnturnedChat.Say(player, Translate("effect_jump_remaining", timer));
                }

                yield return new WaitForSeconds(1f);
                timer--;
            }

            // Duration has elapsed, remove the jump boost effect
            RevertJumpBoost(player, originalJumpMultiplier);
        }

        private void RevertJumpBoost(UnturnedPlayer player, float originalJumpMultiplier)
        {
            // Revert the jump multiplier back to its original value
            player.Player.movement.sendPluginJumpMultiplier(originalJumpMultiplier);

            // Remove the coroutine from the dictionary
            jumpBoostCoroutines.Remove(player);

            // Notify the player that the jump boost effect has ended (optional)
            UnturnedChat.Say(player, Translate("effect_jump_finished"));
        }

        private void ApplyHealingEffect(UnturnedPlayer player, float healingAmount, float healingInterval, float duration)
        {
            Coroutine coroutine = StartCoroutine(HealOverTime(player, healingAmount, healingInterval, duration));

            // Store the coroutine for later use
            healingEffectCoroutines[player] = coroutine;

            // Notify the player that the healing effect has been applied (optional)
            UnturnedChat.Say(player, Translate("effect_healing", healingAmount, healingInterval, duration));
        }

        private System.Collections.IEnumerator HealOverTime(UnturnedPlayer player, float healingAmount, float healingInterval, float duration)
        {
            float timer = duration;
            float healTimer = 0f;
            bool sentTenSecondsMessage = false;
            bool sentFiveSecondsMessage = false;

            while (timer > 0)
            {
                float remainingTime = Mathf.Round(timer); // Round the remaining time to the nearest second

                if (remainingTime == 10 && !sentTenSecondsMessage)
                {
                    // Send a message when there are 10 seconds left and mark it as sent
                    UnturnedChat.Say(player, Translate("effect_healing_remaining", remainingTime));
                    sentTenSecondsMessage = true;
                }
                else if (remainingTime == 5 && !sentFiveSecondsMessage)
                {
                    // Send a message when there are 5 seconds left and mark it as sent
                    UnturnedChat.Say(player, Translate("effect_healing_remaining", remainingTime));
                    sentFiveSecondsMessage = true;
                }

                healTimer += Time.deltaTime;

                if (healTimer >= healingInterval)
                {
                    // Convert the float healingAmount to a byte (you may want to use proper rounding logic)
                    byte byteHealingAmount = (byte)Mathf.RoundToInt(healingAmount);

                    // Apply healing amount to the player's health
                    player.Heal(byteHealingAmount);

                    healTimer = 0f; // Reset the heal timer
                }

                yield return null; // Use null to wait for the next frame
                timer -= Time.deltaTime;

                // If the duration has elapsed, remove the healing effect
                if (timer <= 0)
                {
                    RemoveHealingEffect(player);
                }
            }
        }
        

       

        
        private void RemoveHealingEffect(UnturnedPlayer player)
        {
            // Remove the coroutine from the dictionary
            healingEffectCoroutines.Remove(player);

            // Notify the player that the healing effect has ended (optional)
            UnturnedChat.Say(player, Translate("effect_healing_finished"));
        }
        public void HandleItemConsumption(Player instigatingPlayer, ItemConsumeableAsset consumeableAsset, ref bool shouldAllow)
        {
            UnturnedPlayer player = UnturnedPlayer.FromPlayer(instigatingPlayer);

            foreach (var effect in Configuration.Instance.ConsumableEffects)
            {
                if (consumeableAsset.id == effect.ConsumableID)
                {
                    // Check if the player already has an active effect of the same type
                    if (effect.IsSpeedBoost && speedEffectCoroutines.ContainsKey(player))
                    {
                        // Remove the active speed effect and reset the timer
                        RemoveSpeedEffect(player);
                    }
                    else if (effect.IsJumpBoost && jumpBoostCoroutines.ContainsKey(player))
                    {
                        // Remove the active jump boost effect and reset the timer
                        RevertJumpBoost(player, 1.0f); // Revert to the default jump multiplier
                    }
                    else if (effect.IsHealing && healingEffectCoroutines.ContainsKey(player))
                    {
                        // Remove the active healing effect and reset the timer
                        RemoveHealingEffect(player);
                    }

                    // Apply the new effect
                    if (effect.IsSpeedBoost)
                    {
                        ApplySpeedEffect(player, effect.Multiplier, effect.Duration);
                    }
                    else if (effect.IsJumpBoost)
                    {
                        ApplyJumpBoost(player, effect.Multiplier, effect.Duration);
                    }
                    else if (effect.IsHealing)
                    {
                        ApplyHealingEffect(player, effect.HealingAmount, effect.HealingInterval, effect.Duration);
                    }

                    // Prevent the consumable from being consumed (optional)
                    shouldAllow = true;

                    return; // Exit the loop since we found a matching effect
                }
            }
        }

    }
}
