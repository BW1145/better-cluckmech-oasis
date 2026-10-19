using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace CluckmechCoop
{
    public sealed class ShopFeatures : MonoBehaviour
    {
        private static ShopFeatures current;
        private static readonly FieldInfo pricesField = AccessTools.Field(typeof(商店脚本), "价格库"), bonusField = AccessTools.Field(typeof(商店脚本), "全局加成");
        internal 商店脚本 Shop;
        internal ShopProgress State => ModuleSettings.LocalProgress.ShopAt(ModuleSettings.NodeKey);
        private Button coupon;
        private Text coupons;
        private RectTransform fourthTray;
        internal static bool Enabled => ModuleSettings.Extra.Shop || ModuleSettings.Extra.Growth;
        internal static ShopFeatures For(商店脚本 shop)
        {
            var feature = shop.GetComponent<ShopFeatures>() ?? shop.gameObject.AddComponent<ShopFeatures>();
            feature.Shop = shop; current = feature; return feature;
        }
        internal void PrepareStock()
        {
            int count = ModuleSettings.Extra.Shop && State.Expanded ? 4 : 3;
            if (Shop.当前商品.Length != count) Array.Resize(ref Shop.当前商品, count);
        }
        internal void StockReady(bool restored)
        {
            var state = State;
            if (Shop.当前商品.Length == 4)
            {
                if (!restored || state.FourthId < 0)
                {
                    var candidates = Plugin.Units.单位列表.Where(x => x.单位等级 <= Shop.商品单位等级 && x.是否商店内出现 && x.是否游戏内出现 && Plugin.Units.是否抽取(x.id) && !Shop.当前商品.Take(3).Any(y => y.x == x.id)).ToList();
                    if (candidates.Count == 0) { Array.Resize(ref Shop.当前商品, 3); Shop.商品背包类.GetComponent<背包类预制体脚本>().更新背包(3); }
                    else
                    {
                        var unit = candidates[UnityEngine.Random.Range(0, candidates.Count)];
                        var prices = (dw单位价格库)pricesField.GetValue(Shop);
                        var bonus = (全局加成)bonusField.GetValue(Shop);
                        state.FourthId = unit.id;
                        state.FourthPrice = Mathf.Max(0, (int)((prices.单位价格[unit.单位等级] + UnityEngine.Random.Range(Shop.价格变化区间.x, Shop.价格变化区间.y)) * (1 + (bonus != null ? bonus.商店价格倍率加成 : 0))));
                    }
                }
                if (Shop.当前商品.Length == 4) Shop.当前商品[3] = new Vector3Int(state.FourthId, state.FourthPrice, 0);
                FitFourCards();
            }
            if (restored)
            {
                var bag = Shop.商品背包类.GetComponent<背包类预制体脚本>();
                for (int i = 0; i < Shop.当前商品.Length; i++) if (state.Sold[i] && bag.单元列表[i] != null) UnityEngine.Object.Destroy(bag.单元列表[i]);
            }
            ModuleSettings.SaveProgress();
        }
        private void FitFourCards()
        {
            var bag = Shop.商品背包类.GetComponent<背包类预制体脚本>();
            var grid = Shop.商品背包类.GetComponentInChildren<GridLayoutGroup>();
            if (grid == null || Shop.当前商品.Length != 4) return;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 4;
            var bounds = ((RectTransform)grid.transform.parent).rect;
            float width = (bounds.width - grid.padding.left - grid.padding.right - grid.spacing.x * 3) / 4;
            grid.cellSize = new Vector2(width, grid.cellSize.y);
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)grid.transform);
            for (int i = 0; i < bag.单元列表.Count; i++)
            {
                var rect = bag.单元列表[i]?.GetComponent<RectTransform>();
                if (rect == null) continue;
                rect.sizeDelta = new Vector2(width, rect.sizeDelta.y);
                var tray = i < 3 ? Shop.transform.Find("商品" + (i + 1)) as RectTransform : fourthTray;
                if (i == 3 && tray == null)
                {
                    var original = Shop.transform.Find("商品3");
                    if (original != null) { fourthTray = UnityEngine.Object.Instantiate(original.gameObject, original.parent).GetComponent<RectTransform>(); fourthTray.name = "商品4"; tray = fourthTray; }
                }
                if (tray != null)
                {
                    Vector3 center = Shop.transform.InverseTransformPoint(rect.TransformPoint(rect.rect.center));
                    tray.anchoredPosition = new Vector2(center.x, tray.anchoredPosition.y);
                }
            }
        }
        internal void BuildCoupon()
        {
            if (!ModuleSettings.Extra.Growth) return;
            NativeUi.Capture(Shop.transform);
            coupon = NativeUi.TransferAction(Shop.transform, "1次工匠刷新  30金币", 0, 0, 320, 66, BuyCoupon);
            var rect = coupon.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1, 0); rect.anchoredPosition = new Vector2(-32, 110);
            coupons = NativeUi.Label(Shop.transform, "", 0, 0, 320, 34, 23);
            var label = coupons.rectTransform; label.anchorMin = label.anchorMax = label.pivot = new Vector2(1, 0); label.anchoredPosition = new Vector2(-32, 185);
            coupon.gameObject.AddComponent<RuleTooltip>().Text = "增加1次工匠刷新，剩余次数本局保留。";
            WalletChanged();
        }
        private void BuyCoupon()
        {
            if (!ModuleSettings.LocalProgress.BuyCoupons(ModuleSettings.NodeKey, Plugin.Summary.当前金钱数)) return;
            Plugin.Summary.获得金钱(-30); ShopRewards.Spent(30); ModuleSettings.SaveProgress(); Plugin.Summary.保存单局存档();
            if (Plugin.Role != 0) Plugin.LocalPlayer?.上传玩家总结数据();
            WalletChanged();
            Plugin.Log("[Growth] Bought 1 artisan reroll for 30 gold");
        }
        internal static void WalletChanged()
        {
            if (current == null || current.coupon == null) return;
            current.coupon.interactable = !current.State.TicketBought && Plugin.Summary.当前金钱数 >= 30;
            current.coupons.text = "剩余工匠刷新次数：" + ModuleSettings.LocalProgress.Coupons;
            current.coupon.GetComponentInChildren<Text>().text = current.State.TicketBought ? "已购买" : "1次工匠刷新  30金币";
        }
        private void OnDestroy() { if (current == this) current = null; }
    }
    [HarmonyPatch(typeof(商店脚本), "Start")]
    internal static class ShopReady
    {
        private static void Postfix(商店脚本 __instance) { if (ShopFeatures.Enabled) ShopFeatures.For(__instance).BuildCoupon(); ShopRewards.Attach(__instance); }
    }
    [HarmonyPatch(typeof(商店脚本), "商店刷新")]
    internal static class PaidShopRefresh
    {
        private static void Prefix(商店脚本 __instance,out int[] __state)
        {
            __state=ModuleSettings.Extra.Shop?new[]{Plugin.Summary.当前金钱数,__instance.刷新费用}:null;
            if (ShopFeatures.Enabled && __instance.刷新费用 > 0 && Plugin.Summary.当前金钱数 >= __instance.刷新费用) ShopFeatures.For(__instance).State.Refreshed();
        }
        private static void Postfix(int[] __state)
        {if(__state!=null && __state[1]>0 && Plugin.Summary.当前金钱数==__state[0]-__state[1]){ShopRewards.Spent(__state[1],true);ShopRewards.Save();}}
    }
    [HarmonyPatch(typeof(商店脚本), "商店商品运算")]
    internal static class NewShopStock
    {
        private static readonly List<模块_基本属性.单位分类枚举> emptyPreferences=new List<模块_基本属性.单位分类枚举>();
        private static void Prefix(商店脚本 __instance) { OtherworldProgress.RestoreNative();if (ShopFeatures.Enabled) ShopFeatures.For(__instance).PrepareStock(); }
        private static void Postfix(商店脚本 __instance) { if (ShopFeatures.Enabled) ShopFeatures.For(__instance).StockReady(false); }
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var weights=AccessTools.Field(typeof(角色天赋数据模板),"商店权重加成列表");
            var code = TranspilerContract.Exact(instructions,"Shop talent preferences",1,
                c=>c.opcode==OpCodes.Ldfld&&Equals(c.operand,weights));
            foreach(var c in code)
                if(c.opcode==OpCodes.Ldfld&&Equals(c.operand,weights))
                {c.opcode=OpCodes.Call;c.operand=AccessTools.Method(typeof(NewShopStock),nameof(Preferences));}
            var range = AccessTools.Method(typeof(UnityEngine.Random), nameof(UnityEngine.Random.Range), new[] { typeof(int), typeof(int) });
            var item = AccessTools.PropertyGetter(typeof(List<int>), "Item");
            var count = AccessTools.PropertyGetter(typeof(List<int>), "Count");
            var setId = AccessTools.PropertySetter(typeof(Vector3Int), "x");
            int draws = 0;
            bool preferred = false, special = false;
            for (int i = 1; i + 2 < code.Count; i++)
            {
                if (!code[i].Calls(range) || !code[i+1].Calls(item) || !code[i+2].Calls(setId)) continue;
                draws++;
                if (draws == 2)
                {
                    var loadShop = new CodeInstruction(OpCodes.Ldarg_0).MoveLabelsFrom(code[i+1]);
                    code[i+1] = new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(NewShopStock), nameof(PreferredOffer)));
                    code.Insert(i+1, loadShop);
                    preferred = true;
                    continue;
                }
                if (draws != 3) continue;
                if (!code[i-1].Calls(count)) throw new InvalidOperationException("Special shop offer count binding changed");
                code[i-1] = new CodeInstruction(OpCodes.Ldarg_0).MoveLabelsFrom(code[i-1]);
                code.Insert(i, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(NewShopStock), nameof(SpecialOfferCount))));
                special = true;
                break;
            }
            if (!preferred || !special) throw new InvalidOperationException("Shop offer selection binding missing");
            return code;
        }
        private static int PreferredOffer(List<int> candidates, int index, 商店脚本 shop)
        {
            if (candidates.Count != 0) return candidates[index];
            shop.当前是否符合角色需要 = false;
            return shop.当前商品[1].x;
        }
        private static List<模块_基本属性.单位分类枚举> Preferences(角色天赋数据模板 talent)
            =>talent==null?emptyPreferences:talent.商店权重加成列表??emptyPreferences;
        private static int SpecialOfferCount(List<int> candidates, 商店脚本 shop)
        {
            // The native preferred-slot replacement leaves its new ID in this pool.
            if (ModuleSettings.Extra.Shop) candidates.Remove(shop.当前商品[1].x);
            return candidates.Count;
        }
    }
    [HarmonyPatch(typeof(商店脚本), "读取储存的商店信息")]
    internal static class RestoreShopStock
    {
        private static void Prefix(商店脚本 __instance) { if (ShopFeatures.Enabled) ShopFeatures.For(__instance).PrepareStock(); }
        private static void Postfix(商店脚本 __instance) { if (ShopFeatures.Enabled) ShopFeatures.For(__instance).StockReady(true); }
    }
    [HarmonyPatch(typeof(商店脚本), "购买")]
    internal static class ShopPurchase
    {
        private static readonly FieldInfo summaryField = AccessTools.Field(typeof(商店脚本), "总结");
        internal sealed class PurchaseState
        {
            internal int Index, Gold, Count, Id, Price;
            internal 总结脚本 Summary;
        }
        private static bool Prefix(商店脚本 __instance, out PurchaseState __state)
        {
            __state = null;
            if (!ShopFeatures.Enabled) return true;
            var bag = __instance.商品背包类.GetComponent<背包类预制体脚本>();
            if (bag.当前已选单元.Count == 0) return true;
            int index = bag.当前已选单元[0];
            var sold = ShopFeatures.For(__instance).State.Sold;
            if (index < 0 || index >= __instance.当前商品.Length || index >= sold.Length || index >= bag.单元列表.Count || bag.单元列表[index] == null || sold[index]) return false;
            var summary = (总结脚本)summaryField.GetValue(__instance);
            var item = __instance.当前商品[index];
            if (summary == null || item.y < 0) return false;
            if (summary.当前金钱数 >= item.y)
                __state = new PurchaseState { Summary = summary, Index = index, Gold = summary.当前金钱数, Count = summary.当前拥有炮台.Count, Id = item.x, Price = item.y };
            return true;
        }
        private static void Postfix(商店脚本 __instance, PurchaseState __state)
        {
            if (__state == null) return;
            var summary = __state.Summary;
            // Native purchase appends the selected machine and then deducts its exact price.
            if (summary.当前金钱数 != __state.Gold - __state.Price || summary.当前拥有炮台.Count != __state.Count + 1 ||
                summary.当前拥有炮台[__state.Count].炮台id != __state.Id) return;
            ShopFeatures.For(__instance).State.Sold[__state.Index] = true; ShopRewards.Spent(__state.Price);
            ModuleSettings.SaveProgress(); summary.保存单局存档();
        }
    }

    [HarmonyPatch(typeof(选择词条流程脚本), "刷新词条")]
    internal static class ArtisanCouponRefresh
    {
        private static bool Prefix(选择词条流程脚本 __instance)
        {
            if (!ModuleSettings.Extra.Growth || Plugin.Summary?.地图存档.是否位于工匠 != true) return true;
            int free = __instance.选择词条.词条剩余刷新次数;
            if (free > 0) return true;
            if (!ModuleSettings.LocalProgress.SpendCoupon(free)) return false;
            __instance.选择词条.词条剩余刷新次数 = 1;
            return true;
        }
        private static void Postfix(选择词条流程脚本 __instance)
        {
            ArtisanCouponUi.Refresh(__instance); ModuleSettings.SaveProgress();
        }
    }
    [HarmonyPatch(typeof(选择词条流程脚本), "初始化")]
    internal static class ArtisanCouponUi
    {
        private static void Postfix(选择词条流程脚本 __instance) => Refresh(__instance);
        internal static void Refresh(选择词条流程脚本 flow)
        {
            if (!ModuleSettings.Extra.Growth || Plugin.Summary?.地图存档.是否位于工匠 != true || flow.选择词条 == null) return;
            int total = Mathf.Max(0, flow.选择词条.词条剩余刷新次数) + ModuleSettings.LocalProgress.Coupons;
            flow.刷新按钮.interactable = total > 0; flow.剩余刷新次数文本.text = total.ToString();
        }
    }
}
