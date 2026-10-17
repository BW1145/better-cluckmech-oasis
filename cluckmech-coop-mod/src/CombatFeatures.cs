using System;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CluckmechCoop
{
    public sealed class CombatFeatures : MonoBehaviour
    {
        internal string Token;
        internal readonly SpeedHolds Holds = new SpeedHolds();
        private bool localHeld, speedWanted, applyingTime;
        private float normalSpeed = 1, lastApplied = 1, energyUntil, pendingEnergy;
        internal float RemainingEnergy => EnergyActive ? energyUntil - Time.time : pendingEnergy;
        private 模块_基本属性 baseStats;
        private 模块_攻击属性 baseAttack;
        internal GameObject EnergyEffect;
        internal bool EnergyActive => baseStats != null && energyUntil > Time.time;
        internal bool IsBase(模块_基本属性 stats) => stats == baseStats;
        internal bool IsBase(模块_攻击属性 stats) => stats == baseAttack;

        internal void Begin(刷怪器 spawner)
        {
            if (Token == null && Plugin.Role != 2) { Token = Guid.NewGuid().ToString("N"); Plugin.Network.PublishSettings(); }
            if (spawner.TryGetComponent<yy引用实例数据库>(out var refs) && refs.基地车 != null) BindBase(refs.基地车.GetComponent<模块_基本属性>());
            normalSpeed = Time.timeScale > .2f ? Time.timeScale : 1;
        }
        internal void BindBase(模块_基本属性 stats)
        {
            baseStats = stats; baseAttack = stats.GetComponent<模块_攻击属性>();
            CombatStatusHud.AttachEnergy(stats);
            if (pendingEnergy > 0) { ApplyEnergy(pendingEnergy); pendingEnergy = 0; }
        }
        private static bool Typing()
        {
            var selected = EventSystem.current?.currentSelectedGameObject;
            return selected != null && selected.GetComponent<InputField>() != null;
        }
        private void Update()
        {
            if (!Plugin.InCombat || !ModuleSettings.InRun) return;
            var keyboard = Keyboard.current;
            if (ModuleSettings.Extra.Speed)
            {
                if (Application.isFocused && !Typing() && keyboard != null && keyboard.leftShiftKey.wasPressedThisFrame && Plugin.Network.SettingsReady)
                    SendHold(!speedWanted);
                ApplySpeed();
            }
            if (ModuleSettings.Extra.Energy && keyboard != null && keyboard.fKey.wasPressedThisFrame && !Typing() && Time.timeScale > 0 && Application.isFocused)
            {
                if (Plugin.Role == 0) StartEnergy();
                else if (Plugin.Network.SettingsReady) Plugin.Network.Send(new SessionMessage { Type = "energy-request", Token = Token });
            }
            if (energyUntil > 0 && Time.time >= energyUntil) ApplyEnergy(0);
        }
        private void SendHold(bool value)
        {
            localHeld = value;
            if (Plugin.Role == 0) HostHold(0, value);
            else Plugin.Network.Send(new SessionMessage { Type = "speed-request", Token = Token, Held = value });
        }
        internal void HostHold(int role, bool value)
        {
            bool previous=Holds.Active;
            Holds.Clear();Holds.Set(role,value);
            if (previous==value) return;
            SetSpeed(Holds.Active);
            Plugin.Network.Broadcast(new SessionMessage { Type = "speed", Token = Token, Held = Holds.Active });
        }
        internal void Synchronize(bool held, float energy)
        {
            SetSpeed(held);
            if (baseStats != null) ApplyEnergy(energy); else pendingEnergy = energy;
        }
        internal void SetSpeed(bool value) { localHeld = speedWanted = value; ApplySpeed(); }
        internal void NativeSpeed(float value)
        {
            if (applyingTime || value <= .2f) return;
            normalSpeed = value;
        }
        private void ApplySpeed()
        {
            if (!Plugin.InCombat) return;
            float current = Time.timeScale;
            if (current <= .2f) { lastApplied = current; return; }
            if (current != lastApplied) normalSpeed = current;
            float value = speedWanted ? 2 : normalSpeed;
            if (current == value) { lastApplied = value; return; }
            applyingTime = true; Time.timeScale = value; applyingTime = false; lastApplied = value;
        }
        internal bool StartEnergy()
        {
            if (!ModuleSettings.Extra.Energy || !Plugin.InCombat || EnergyActive || baseStats == null || baseStats.当前生命值 <= 0 || !ModuleSettings.Progress.UseEnergy()) return false;
            ModuleSettings.SaveProgress(); ApplyEnergy(10);
            Plugin.Network.Broadcast(new SessionMessage { Type = "energy", Token = Token, Delay = 10, EnergyBonusUsed = ModuleSettings.Progress.EnergyBonusUsed });
            Plugin.Log("[Energy] Activated; remaining "+ModuleSettings.Progress.EnergyRemaining+"; duration 10, attack +500, resistance 100%");
            return true;
        }
        internal void ApplyEnergy(float seconds)
        {
            energyUntil = seconds > 0 ? Time.time + seconds : 0;
            if (seconds > 0) ModuleSettings.Progress.EnergyUsed = true;
            baseStats?.运算抗性(); baseAttack?.运算攻击力();
            UpdateEnergyEffect();
            if (seconds == 0 && Plugin.Role == 1) Plugin.Network.Broadcast(new SessionMessage { Type = "energy", Token = Token, Delay = 0, EnergyBonusUsed = ModuleSettings.Progress.EnergyBonusUsed });
        }
        private void UpdateEnergyEffect()
        {
            if (!EnergyActive)
            {
                if (EnergyEffect != null) Destroy(EnergyEffect);
                EnergyEffect = null;
                return;
            }
            if (EnergyEffect != null) return;
            var prefab = Plugin.Spawner?.GetComponent<效果物体引用器>()?.特效.通用提升效果_静音版;
            if (prefab == null) return;
            EnergyEffect = Instantiate(prefab, baseStats.transform);
            EnergyEffect.transform.localPosition = Vector3.zero;
            var sound = EnergyEffect.GetComponent<音效_播放音效>();
            if (sound != null) sound.enabled = false;
            foreach (var particles in EnergyEffect.GetComponentsInChildren<ParticleSystem>())
            {
                particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = particles.main;
                main.loop = true; main.stopAction = ParticleSystemStopAction.None;
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
                particles.Play();
            }
        }
        private void OnApplicationFocus(bool focused) { if (!focused && localHeld) SendHold(false); }
        internal void Disconnected()
        {
            Holds.Clear(); localHeld = false; SetSpeed(false);
        }
        internal void ClearScene()
        {
            Disconnected(); ApplyEnergy(0); Token = null;
            baseStats = null; baseAttack = null; pendingEnergy = 0; normalSpeed = lastApplied = 1;
        }
    }

    internal static class FeatureNetwork
    {
        internal static bool ServerReceive(SessionMessage message, int role)
        {
            if (message.Type != "speed-request" && message.Type != "energy-request" && message.Type != "rabbit-aim") return false;
            var combat = Plugin.Combat;
            if (combat == null || !Plugin.InCombat || message.Token == null || message.Token != combat.Token) return true;
            if (message.Type == "speed-request" && ModuleSettings.Extra.Speed) combat.HostHold(role, message.Held);
            else if (message.Type == "energy-request") combat.StartEnergy();
            else if (message.Type == "rabbit-aim" && ModuleSettings.Extra.Characters) CharacterFeatures.ReceiveAim(role, message);
            return true;
        }
        internal static bool Receive(SessionMessage message)
        {
            if (message.Type != "speed" && message.Type != "energy" && message.Type != "growth") return false;
            if (message.Type == "growth") { GrowthFeatures.Offer(message.Token); return true; }
            if (Plugin.Combat == null || message.Token == null || message.Token != Plugin.Combat.Token || !Plugin.InCombat) return true;
            if (message.Type == "speed" && ModuleSettings.Extra.Speed) Plugin.Combat.SetSpeed(message.Held);
            else if (message.Type == "energy" && ModuleSettings.Extra.Energy && message.EnergyBonusUsed <= ModuleSettings.Progress.EnergyBonus)
            {
                ModuleSettings.Progress.EnergyBonusUsed = message.EnergyBonusUsed;
                Plugin.Combat.ApplyEnergy(message.Delay);
            }
            return true;
        }
    }
    [HarmonyPatch(typeof(设置管理器), "改变游戏时间流速")]
    internal static class NativeSpeedChange
    {
        private static void Postfix(float 数值) { if (ModuleSettings.Extra.Speed) Plugin.Combat.NativeSpeed(数值); }
    }
    [HarmonyPatch(typeof(模块_基本属性), "Start")]
    internal static class BaseReady
    {
        private static void Postfix(模块_基本属性 __instance)
        {
            if (ModuleSettings.Extra.Energy && __instance.单位定位列表.Contains(模块_基本属性.单位定位枚举.基地)) Plugin.Combat.BindBase(__instance);
        }
    }
    [HarmonyPatch(typeof(模块_基本属性), "运算抗性")]
    internal static class EnergyResistance
    {
        private static void Postfix(模块_基本属性 __instance)
        {
            var combat = Plugin.Combat;
            if (combat != null && combat.EnergyActive && combat.IsBase(__instance)) __instance.当前抗性 = 1;
        }
    }
    [HarmonyPatch(typeof(模块_攻击属性), "运算攻击力")]
    internal static class ExtraAttack
    {
        private static void Postfix(模块_攻击属性 __instance)
        {
            var combat = Plugin.Combat;
            if (combat != null && combat.EnergyActive && combat.IsBase(__instance)) __instance.当前攻击力 += 500;
            UnitModifiers.Attack(__instance);
        }
    }
    [HarmonyPatch(typeof(模块_移动属性), "运算移动速度")]
    internal static class ExtraMovement
    {
        private static void Postfix(模块_移动属性 __instance) => UnitModifiers.Move(__instance);
    }
}
