using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CluckmechCoop
{
    internal static class ThemeBranchMaps
    {
        internal static void Install(地图管理器 manager)
        {
            if (manager == null || !ThemeBranchRuntime.Enabled && Plugin.Role != 2 || manager.GetComponent<ThemeBranchMapSet>() != null) return;
            var pages = manager.地图数据.SelectMany(x => x.随机场景列表)
                .SelectMany(x => x.场景小层列表).SelectMany(x => x.地图页列表).Where(x => x != null).Distinct().ToList();
            var template = pages.FirstOrDefault(x => HasKind(x, 地图节点数据模版.节点类型枚举.随机地图));
            var fourth = pages.FirstOrDefault(x => x.所属场景 == "绿洲");
            if (template == null || fourth == null) return;
            var set = manager.gameObject.AddComponent<ThemeBranchMapSet>();
            set.Manager = manager;
            set.OriginalFourth = fourth;
            set.NativePages.AddRange(pages);
            var staging = new GameObject("Theme branch map staging");
            staging.SetActive(false);
            try
            {
                for (int theme = 1; theme <= 2; theme++)
                    set.BranchPages.Add(BuildBranch(set, template, staging.transform, theme));
                set.Fourth = BuildFourth(set, fourth, staging.transform);
            }
            finally { UnityEngine.Object.Destroy(staging); }
            foreach (var scene in manager.地图数据.SelectMany(x => x.随机场景列表))
                foreach (var group in scene.场景小层列表)
                    if (group.地图页列表.Contains(fourth) && !group.地图页列表.Contains(set.Fourth)) group.地图页列表.Add(set.Fourth);
            foreach (var page in set.BranchPages.Concat(new[] { set.Fourth })) page.gameObject.SetActive(true);
        }

        internal static void Prepare(地图管理器 manager)
        {
            var set = manager?.GetComponent<ThemeBranchMapSet>();
            var summary = Plugin.Summary ?? UnityEngine.Object.FindObjectOfType<总结脚本>();
            var map = summary?.地图存档;
            if (set == null || map == null) return;
            var state = ThemeBranchRuntime.State;
            if (Plugin.Role != 2)
            {
                for (int i = 0; i < map.地图页列表.Count; i++)
                {
                    bool fourth = map.地图页列表[i] == set.OriginalFourth.地图页名 || map.地图页列表[i] == ThemeBranchCatalog.FourthPage;
                    if (!fourth) continue;
                    if (!state.FourthPageChosen)
                    {
                        state.FourthPageChosen = true;
                        state.FourthPageEnabled = map.地图页列表[i] == ThemeBranchCatalog.FourthPage;
                    }
                    if (state.Used && (i > map.当前地图页位置 || i == map.当前地图页位置 && map.是否初始化地图))
                    {
                        state.FourthPageEnabled = false;
                        map.地图页列表[i] = set.OriginalFourth.地图页名;
                        if (i < map.地图页信息列表.Count)
                        {
                            var info = map.地图页信息列表[i];
                            info.地图页名 = set.OriginalFourth.地图页名;
                            info.节点列表 = Nodes(set.OriginalFourth).Select(x => x.节点名).ToList();
                        }
                        if (i == map.当前地图页位置) map.当前地图页 = set.OriginalFourth.地图页名;
                    }
                }
            }
            if (state.Active)
            {
                map.当前地图页 = ThemeBranchCatalog.Page(state.Theme);
                var page = set.BranchPages[state.Theme - 1];
                page.所属场景 = map.当前场景名;
                ConfigureBranch(set, page, state.Theme);
                RefreshLabels(page, state.Theme, state.Eldritch, state.Service);
            }
            foreach (var page in set.BranchPages.Concat(new[] { set.Fourth }))
            {
                InjectPage(page, manager, summary);
                page.gameObject.SetActive(page.地图页名 == map.当前地图页);
            }
        }

        internal static void ApplyNodeData(地图页 page)
        {
            var state = ThemeBranchRuntime.State;
            if (!state.Active || page == null || page.地图页名 != ThemeBranchCatalog.Page(state.Theme)) return;
            foreach (var entry in page.节点作用列表)
            {
                int step = Step(state.Theme, entry.节点名);
                if (step < 0) continue;
                if (step == 0) entry.作用 = 地图管理器.节点作用.起点;
                else if (step == 1 || step == 3) entry.作用 = 地图管理器.节点作用.随机场景;
                else if (step == 5) entry.作用 = 地图管理器.节点作用.随机场景_boss;
                else if (step == 4)
                {
                    entry.作用 = state.Service == 0 ? 地图管理器.节点作用.商店 : 地图管理器.节点作用.工匠;
                    entry.节点场景id = state.Service == 0 ? page.商店_场景id : page.工匠_场景id;
                }
                else
                {
                    entry.作用 = 地图管理器.节点作用.事件;
                    entry.节点场景id = page.事件_场景id;
                }
            }
        }

        private static 地图页 BuildBranch(ThemeBranchMapSet set, 地图页 source, Transform staging, int theme)
        {
            var nodeSource = Nodes(source).First();
            var page = ClonePage(set, source, staging, ThemeBranchCatalog.Page(theme));
            foreach (var node in Nodes(page)) UnityEngine.Object.DestroyImmediate(node.gameObject);
            page.地图页内节点列表 = new List<地图节点>();
            var sourcePoints = Nodes(source).Select(x => Position(x.transform)).ToArray();
            float minX = sourcePoints.Min(x => x.x), maxX = sourcePoints.Max(x => x.x);
            float minY = sourcePoints.Min(x => x.y), maxY = sourcePoints.Max(x => x.y);
            bool horizontal = maxX - minX >= maxY - minY;
            float span = Mathf.Max(maxX - minX, maxY - minY);
            float cross = Mathf.Max(Mathf.Min(maxX - minX, maxY - minY), span * .22f);
            var center = new Vector3((minX + maxX) * .5f, (minY + maxY) * .5f, sourcePoints[0].z);
            for (int step = 0; step <= 6; step++)
            {
                var node = CloneNode(set, nodeSource, page, ThemeBranchCatalog.Node(theme, step), step);
                float along = span * (step / 6f - .5f);
                float across = step == 0 || step == 6 ? 0 : (step % 2 == 0 ? cross * .22f : -cross * .22f);
                Place(node.transform, center + (horizontal ? new Vector3(along, across, 0) : new Vector3(across, -along, 0)));
                node.是否起点 = step == 0;
                page.地图页内节点列表.Add(node);
            }
            for (int step = 0; step < 6; step++) Link(page.地图页内节点列表[step], page.地图页内节点列表[step + 1]);
            var exit = CloneNode(set, nodeSource, page, ThemeBranchCatalog.ExitNode, 7);
            Place(exit.transform, center + (horizontal ? new Vector3(0, cross * .5f, 0) : new Vector3(cross * .5f, 0, 0)));
            page.地图页内节点列表.Add(exit);
            for (int step = 1; step <= 4; step++) Link(page.地图页内节点列表[step], exit);
            ConfigureBranch(set, page, theme);
            return page;
        }

        private static 地图页 BuildFourth(ThemeBranchMapSet set, 地图页 source, Transform staging)
        {
            var page = ClonePage(set, source, staging, ThemeBranchCatalog.FourthPage);
            var nodes = Nodes(page);
            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                node.节点名 = ThemeBranchCatalog.FourthPage + ":" + i;
                node.gameObject.name = node.节点名;
                node.地图页信息 = PageInfo(page);
                InjectNode(node, page, set.Manager);
            }
            foreach (var node in nodes)
                foreach (var link in node.上端信息列表.Concat(node.下端信息列表))
                    if (link.地图节点 != null) { link.节点名 = link.地图节点.节点名; link.节点物体 = link.地图节点.gameObject; }
            var supply = nodes.First(x => x.地图节点id数据?.节点类型 == 地图节点数据模版.节点类型枚举.补给);
            var shop = nodes.First(x => x.地图节点id数据?.节点类型 == 地图节点数据模版.节点类型枚举.商店);
            var tree = CloneNode(set, supply, page, ThemeBranchCatalog.FourthPage + ":tree", nodes.Count);
            tree.地图节点id数据.节点类型 = 地图节点数据模版.节点类型枚举.事件;
            tree.地图节点id数据.地图id列表 = new List<int> { page.事件_场景id };
            var between = (Position(supply.transform) + Position(shop.transform)) * .5f;
            var direction = Position(shop.transform) - Position(supply.transform);
            var offset = new Vector3(-direction.y, direction.x, 0).normalized * Mathf.Max(direction.magnitude * .45f, 1f);
            Place(tree.transform, between + offset);
            Link(supply, tree); Link(tree, shop);
            page.地图页内节点列表 = Nodes(page);
            AddLabel(tree.transform, "树洞");
            return page;
        }

        private static 地图页 ClonePage(ThemeBranchMapSet set, 地图页 source, Transform staging, string name)
        {
            var root = UnityEngine.Object.Instantiate(source.gameObject, staging, false);
            root.SetActive(false);
            root.transform.SetParent(source.transform.parent, false);
            root.name = name;
            var page = root.GetComponent<地图页>();
            page.地图页名 = name;
            page.事件_地图节点初始化 = new UnityEvent();
            page.事件_地图节点加载 = new UnityEvent();
            page.节点作用列表 = new List<地图管理器.节点作用类>();
            page.地图页内节点列表 = Nodes(page);
            InjectPage(page, set.Manager, Plugin.Summary ?? UnityEngine.Object.FindObjectOfType<总结脚本>());
            return page;
        }

        private static 地图节点 CloneNode(ThemeBranchMapSet set, 地图节点 source, 地图页 page, string name, int position)
        {
            var root = UnityEngine.Object.Instantiate(source.gameObject, page.transform, false);
            root.name = name;
            root.SetActive(true);
            var node = root.GetComponent<地图节点>();
            node.节点名 = name;
            node.地图页信息 = PageInfo(page);
            node.上端信息列表 = new List<地图节点.节点信息类>();
            node.下端信息列表 = new List<地图节点.节点信息类>();
            node.是否起点 = node.是否终点 = node.是否地图页终点 = node.是否走过 = node.是否可走 = node.是否所在位置 = false;
            node.下一地图页 = null;
            var data = ScriptableObject.CreateInstance<地图节点数据模版>();
            data.地图id列表 = new List<int> { 0 };
            node.地图节点id数据 = data;
            set.NodeData.Add(data);
            InjectNode(node, page, set.Manager);
            return node;
        }

        private static void ConfigureBranch(ThemeBranchMapSet set, 地图页 page, int theme)
        {
            var area = page.所属场景;
            var ordinary = SceneIds(set, area, 地图节点数据模版.节点类型枚举.随机地图);
            var bosses = SceneIds(set, area, 地图节点数据模版.节点类型枚举.随机地图_boss);
            foreach (var node in Nodes(page))
            {
                int step = Step(theme, node.节点名);
                if (step < 0) continue;
                var data = node.地图节点id数据;
                data.节点类型 = step == 0 ? 地图节点数据模版.节点类型枚举.起点 : step == 1 || step == 3 ? 地图节点数据模版.节点类型枚举.随机地图 :
                    step == 5 ? 地图节点数据模版.节点类型枚举.随机地图_boss : step == 4 ? 地图节点数据模版.节点类型枚举.商店 : 地图节点数据模版.节点类型枚举.事件;
                data.地图id列表 = step == 1 || step == 3 ? new List<int>(ordinary) : step == 5 ? new List<int>(bosses) : new List<int> { 0 };
            }
        }

        private static List<int> SceneIds(ThemeBranchMapSet set, string area, 地图节点数据模版.节点类型枚举 kind)
        {
            var finalIds = new HashSet<int>(Nodes(set.OriginalFourth).SelectMany(x => x.地图节点id数据?.地图id列表 ?? new List<int>()));
            var pages = set.NativePages.Where(x => x.所属场景 == area).ToList();
            var ids = pages.SelectMany(Nodes).Where(x => x.地图节点id数据?.节点类型 == kind).SelectMany(x => x.地图节点id数据.地图id列表)
                .Where(x => x > 0 && !finalIds.Contains(x)).Distinct().ToList();
            if (ids.Count == 0)
                ids = set.NativePages.Where(x => x.所属场景 == "焦香火山" || x.所属场景 == "废弃研究所").SelectMany(Nodes)
                    .Where(x => x.地图节点id数据?.节点类型 == kind).SelectMany(x => x.地图节点id数据.地图id列表)
                    .Where(x => x > 0 && !finalIds.Contains(x)).Distinct().ToList();
            if (ids.Count == 0) throw new InvalidOperationException("主题支线缺少原版战斗场景模板：" + kind);
            return ids;
        }

        private static void RefreshLabels(地图页 page, int theme, bool[] eldritch, int service)
        {
            string[] names = theme == 1 ? new[] { "古堡", "城门外庭", "管家的待办清单", "破败宴会厅", service == 0 ? "商店" : "工匠", "伯爵大厅", "返回主线" } :
                new[] { "深海", "裂谷观景廊", "值守员的最后一班", "环流泵房", service == 0 ? "商店" : "工匠", "沉渊巨章", "返回主线" };
            string[] variants = theme == 1 ? new[] { "无主之影", "席外之客", "倒悬王庭" } : new[] { "回望之井", "深海之肺", "白渊来客" };
            foreach (var node in Nodes(page))
            {
                int step = Step(theme, node.节点名);
                string text = step == 7 ? "返回主线" : step >= 0 ? names[step] : null;
                int battle = step == 1 ? 0 : step == 3 ? 1 : step == 5 ? 2 : -1;
                if (battle >= 0 && eldritch != null && eldritch.Length > battle && eldritch[battle]) text = "异化·" + variants[battle];
                if (text != null) AddLabel(node.transform, text);
            }
        }

        private static void AddLabel(Transform parent, string text)
        {
            var previous = parent.Find("Theme node label");
            if (previous != null) { previous.GetComponent<Text>().text = text; return; }
            if (NativeUi.Font == null) NativeUi.Capture(parent);
            var root = new GameObject("Theme node label", typeof(RectTransform), typeof(Text));
            var rect = root.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 0f);
            rect.pivot = new Vector2(.5f, 1f);
            rect.anchoredPosition = new Vector2(0, -10);
            rect.sizeDelta = new Vector2(240, 44);
            var label = root.GetComponent<Text>();
            label.font = NativeUi.Font; label.fontSize = 22; label.text = text;
            label.alignment = TextAnchor.MiddleCenter; label.color = new Color(1f, .87f, .55f); label.raycastTarget = false;
        }

        private static void InjectPage(地图页 page, 地图管理器 manager, 总结脚本 summary)
        {
            AccessTools.Field(typeof(地图页), "管理器").SetValue(page, manager);
            AccessTools.Field(typeof(地图页), "总结").SetValue(page, summary);
            AccessTools.Field(typeof(地图页), "大地图场景库").SetValue(page, UnityEngine.Object.FindObjectOfType<数据库_大地图场景>());
            AccessTools.Field(typeof(地图页), "关卡奖励库").SetValue(page, UnityEngine.Object.FindObjectOfType<数据库_关卡奖励>());
            AccessTools.Field(typeof(地图页), "关卡挑战库").SetValue(page, UnityEngine.Object.FindObjectOfType<数据库_关卡挑战>());
        }

        private static void InjectNode(地图节点 node, 地图页 page, 地图管理器 manager)
        {
            AccessTools.Field(typeof(地图节点), "地图页").SetValue(node, page);
            AccessTools.Field(typeof(地图节点), "地图管理器").SetValue(node, manager);
            AccessTools.Field(typeof(地图节点), "总结").SetValue(node, Plugin.Summary ?? UnityEngine.Object.FindObjectOfType<总结脚本>());
            node.UI引用器 = UnityEngine.Object.FindObjectOfType<UI引用器>();
        }

        private static 地图页.地图页信息类 PageInfo(地图页 page) => new 地图页.地图页信息类 { 地图页名 = page.地图页名, 地图页组件 = page };
        private static Vector3 Position(Transform transform) => transform is RectTransform rect ? rect.anchoredPosition3D : transform.localPosition;
        private static void Place(Transform transform, Vector3 position)
        {
            if (transform is RectTransform rect) rect.anchoredPosition3D = position;
            else transform.localPosition = position;
        }
        private static bool HasKind(地图页 page, 地图节点数据模版.节点类型枚举 kind) => Nodes(page).Any(x => x.地图节点id数据?.节点类型 == kind);
        private static List<地图节点> Nodes(地图页 page) => page.GetComponentsInChildren<地图节点>(true).ToList();
        private static int Step(int theme, string node)
        {
            if (node == ThemeBranchCatalog.ExitNode) return 7;
            for (int i = 0; i <= 6; i++) if (node == ThemeBranchCatalog.Node(theme, i)) return i;
            return -1;
        }
        private static void Link(地图节点 from, 地图节点 to)
        {
            from.上端信息列表.Add(Info(to));
            to.下端信息列表.Add(Info(from));
        }
        private static 地图节点.节点信息类 Info(地图节点 node) => new 地图节点.节点信息类 { 节点名 = node.节点名, 地图节点 = node, 节点物体 = node.gameObject };
    }

    internal sealed class ThemeBranchMapSet : MonoBehaviour
    {
        internal 地图管理器 Manager;
        internal 地图页 OriginalFourth, Fourth;
        internal readonly List<地图页> NativePages = new List<地图页>();
        internal readonly List<地图页> BranchPages = new List<地图页>();
        internal readonly List<地图节点数据模版> NodeData = new List<地图节点数据模版>();
        private void OnDestroy()
        {
            foreach (var data in NodeData) if (data != null) UnityEngine.Object.Destroy(data);
        }
    }
}
