using System;
using Newtonsoft.Json.Linq;

namespace CluckmechCoop
{
    // The native map owns the active route. This record owns its branch identity and return route.
    internal sealed class ThemeBranchState
    {
        public bool PoolsAdded, Used, Active, FourthPageChosen, FourthPageEnabled;
        public int Theme, Service, Completed, EventChoice = -1, EntranceChoice = -1;
        public bool[] Eldritch = new bool[3];
        public string Token, MainMap, ActiveMap, EntranceNode;

        internal bool Valid
        {
            get
            {
                if (Eldritch == null || Eldritch.Length != 3 || Service < 0 || Service > 1 ||
                    Completed < 0 || Completed > 5 || EventChoice < -1 || EventChoice > 2 ||
                    (FourthPageEnabled && !FourthPageChosen) || EntranceChoice < -1 || EntranceChoice > 1 ||
                    (EntranceChoice >= 0 && string.IsNullOrWhiteSpace(EntranceNode)) ||
                    (EntranceChoice == 0 && !Used)) return false;
                if (!Used) return !Active && Theme == 0 && Completed == 0 && EventChoice == -1 &&
                    Token == null && MainMap == null && ActiveMap == null;
                if (Theme < 1 || Theme > 2 || !Guid.TryParseExact(Token, "N", out _)) return false;
                return ValidMap(MainMap, false) && (!Active || ValidMap(ActiveMap, true, ThemeBranchCatalog.Page(Theme)));
            }
        }

        private static bool ValidMap(string json, bool branch, string expectedPage = null)
        {
            if (string.IsNullOrWhiteSpace(json)) return false;
            try
            {
                var map = JObject.Parse(json);
                if (map["当前地图页"]?.Type != JTokenType.String ||
                    map["当前地图页位置"]?.Type != JTokenType.Integer) return false;
                string page = (string)map["当前地图页"];
                long index = (long)map["当前地图页位置"];
                return index >= 0 && index < int.MaxValue && !string.IsNullOrWhiteSpace(page) &&
                    map["当前节点名"]?.Type == JTokenType.String &&
                    map["地图页列表"] is JArray pages && pages.Count > index &&
                    map["走过的节点"] is JArray && map["节点作用列表"] is JArray &&
                    (expectedPage == null || page == expectedPage) &&
                    (branch ? ThemeBranchCatalog.IsPage(page) : !ThemeBranchCatalog.IsPage(page));
            }
            catch (Newtonsoft.Json.JsonException) { return false; }
        }

        internal bool Begin(string mainMap, string token, int theme, int service, bool[] variants)
        {
            if (Used || !ValidMap(mainMap, false) || !Guid.TryParseExact(token, "N", out _) ||
                theme < 1 || theme > 2 || service < 0 || service > 1 || variants == null || variants.Length != 3) return false;
            MainMap = mainMap; Token = token; Theme = theme; Service = service;
            Eldritch = (bool[])variants.Clone(); Used = Active = true;
            Completed = 0; EventChoice = -1;
            return true;
        }

        internal bool Complete(int step)
        {
            if (!Active || step != Completed + 1 || step < 1 || step > 5) return false;
            Completed = step;
            return true;
        }

        internal bool SelectEvent(int choice)
        {
            if (!Active || Completed != 1 || EventChoice >= 0 || choice < 0 || choice > 2) return false;
            EventChoice = choice;
            return true;
        }

        internal bool Return()
        {
            if (!Active || !Used) return false;
            Active = false;
            return true;
        }
    }

    internal static class ThemeBranchCatalog
    {
        internal const int TreeEvent = 93, CastleEvent = 94, DeepSeaEvent = 95;
        internal const string FourthPage = "Cluckmech.Theme.Oasis", ExitNode = "Cluckmech.Theme.Return";
        internal static string Page(int theme) => theme == 1 ? "Cluckmech.Theme.Castle" : "Cluckmech.Theme.DeepSea";
        internal static bool IsPage(string page) => page == Page(1) || page == Page(2);
        internal static string Node(int theme, int step) => Page(theme) + "." + step;
        internal static string Name(int theme) => theme == 1 ? "废弃古城堡" : "深海玻璃温室";
        internal static int Step(int theme, string node)
        {
            for (int i = 0; i <= 6; i++) if (node == Node(theme, i)) return i;
            return -1;
        }
        internal static string BattleName(int theme, int battle, bool eldritch)
        {
            string[][] normal = { new[] { "城门外庭", "破败宴会厅", "伯爵大厅" }, new[] { "裂谷观景廊", "环流泵房", "沉渊巨章" } };
            string[][] altered = { new[] { "无主之影", "席外之客", "倒悬王庭" }, new[] { "回望之井", "深海之肺", "白渊来客" } };
            return (eldritch ? altered : normal)[theme - 1][battle];
        }
    }
}
