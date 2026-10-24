using System;
using CluckmechCoop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

static class ThemeBranchHarness
{
    private static int checks;
    private static void Check(bool value, string name)
    { checks++; if (!value) throw new Exception(name); }

    // Route payloads retain fields owned by the native save, including data that
    // the branch state does not interpret. Returning uses the exact saved route.
    internal static string Map(string page, string node) => new JObject
    {
        ["当前地图页位置"] = 0,
        ["当前地图页"] = page,
        ["当前节点名"] = node,
        ["地图页列表"] = new JArray(page),
        ["走过的节点"] = new JArray("起点", "补给"),
        ["节点作用列表"] = new JArray(),
        ["当前场景名"] = "火山",
        ["是否位于工匠"] = false
    }.ToString(Formatting.None);

    internal static ThemeBranchState Entered(int theme = 1, int service = 0)
    {
        var state = new ThemeBranchState();
        if (!state.Begin(Map("原版第三层地图", "问号"), Guid.NewGuid().ToString("N"), theme, service,
            new[] { true, false, true })) throw new Exception("branch fixture entry failed");
        state.ActiveMap = Map(ThemeBranchCatalog.Page(theme), ThemeBranchCatalog.Node(theme, 0));
        return state;
    }

    private static ThemeBranchState RoundTrip(ThemeBranchState state) =>
        JsonConvert.DeserializeObject<ThemeBranchState>(JsonConvert.SerializeObject(state));

    private static void Bad(string name, Action<ThemeBranchState> corrupt)
    {
        var state = Entered();
        corrupt(state);
        string original = JsonConvert.SerializeObject(state);
        Check(!state.Valid, name);
        Check(JsonConvert.SerializeObject(state) == original, name + " preserves invalid input for save protection");
    }

    internal static void Run()
    {
        var fresh = new ThemeBranchState();
        Check(fresh.Valid && !fresh.Active && !fresh.Used && fresh.EventChoice == -1 && fresh.Completed == 0,
            "new run has an unused branch with no event or progress");
        Check(!fresh.Complete(1) && !fresh.SelectEvent(0) && !fresh.Return(),
            "inactive route cannot advance, claim an event or return");
        Check(RoundTrip(fresh).Valid, "unused branch survives JSON");
        var legacy = JsonConvert.DeserializeObject<RunProgress>("{}");
        Check(legacy.Branch != null && legacy.Branch.Valid && !legacy.Branch.Used,
            "earlier progress without branch data receives the unused default");
        var modules = JsonConvert.DeserializeObject<GameplayModules>(JsonConvert.SerializeObject(
            new GameplayModules { Growth = true, ThemeBranches = true }));
        Check(modules.ThemeBranches && modules.Growth && !modules.Shop,
            "theme setting survives JSON alongside existing gameplay modules");

        for (int theme = 1; theme <= 2; theme++)
        for (int service = 0; service <= 1; service++)
        for (int choice = 0; choice <= 2; choice++)
        {
            var variants = new[] { true, false, true };
            var state = new ThemeBranchState();
            string main = Map("原版第四层地图", "补给");
            string token = Guid.NewGuid().ToString("N");
            Check(state.Begin(main, token, theme, service, variants), "both themes and service outcomes can enter");
            state.ActiveMap = Map(ThemeBranchCatalog.Page(theme), ThemeBranchCatalog.Node(theme, 0));
            variants[0] = false; variants[1] = true;
            Check(state.Valid && state.Eldritch[0] && !state.Eldritch[1] && state.Eldritch[2],
                "three generated battle variants are owned by the run");
            string entered = JsonConvert.SerializeObject(state);
            Check(!state.Begin(main, Guid.NewGuid().ToString("N"), 3 - theme, 1 - service, variants) &&
                JsonConvert.SerializeObject(state) == entered, "repeated entry preserves the chosen route");
            Check(!state.SelectEvent(choice) && !state.Complete(0) && !state.Complete(2),
                "event and later nodes wait for the first battle");
            Check(state.Complete(1) && !state.Complete(1), "first battle advances once");
            Check(!state.SelectEvent(-1) && !state.SelectEvent(3) && state.SelectEvent(choice) &&
                !state.SelectEvent((choice + 1) % 3), "all three event choices resolve once in the event node");
            state = RoundTrip(state);
            Check(state.Valid && state.EventChoice == choice && state.Completed == 1 && state.Theme == theme &&
                state.Service == service && state.Token == token && state.MainMap == main,
                "JSON continuation retains event, theme, service, token and exact main route");
            for (int step = 2; step <= 5; step++)
            {
                Check(!state.Complete(step + 1) && state.Complete(step), "route advances by the next node only");
                Check(!state.SelectEvent(choice), "completed event remains claimed at later nodes");
                state.ActiveMap = Map(ThemeBranchCatalog.Page(theme), ThemeBranchCatalog.Node(theme, step));
                state = RoundTrip(state);
                Check(state.Valid && state.Completed == step, "each native service or battle node can resume");
            }
            Check(!state.Complete(6) && state.Return() && !state.Active && state.Used && state.Valid,
                "completed branch returns while retaining its consumed entry");
            state = RoundTrip(state);
            Check(state.Valid && !state.Return() && !state.Begin(main, token, theme, service, variants) &&
                state.MainMap == main && state.EventChoice == choice, "saved return cannot enter or reward the branch again");
        }

        var withdrawal = Entered();
        Check(withdrawal.Complete(1) && withdrawal.Return() && withdrawal.Valid && withdrawal.Used &&
            !withdrawal.Complete(2), "early return preserves the consumed branch and stops its route");
        foreach (bool enabled in new[] { false, true })
        {
            var fourth = new ThemeBranchState { FourthPageChosen = true, FourthPageEnabled = enabled };
            var restored = RoundTrip(fourth);
            Check(restored.Valid && restored.FourthPageChosen && restored.FourthPageEnabled == enabled,
                "both fourth-layer page outcomes persist before entry");
        }
        Check(!new ThemeBranchState { FourthPageEnabled = true }.Valid,
            "fourth-layer side branch requires a saved page choice");

        foreach (var attempt in new Action<ThemeBranchState>[] {
            s => s.Eldritch = null, s => s.Eldritch = new bool[2], s => s.Theme = 0, s => s.Theme = 3,
            s => s.Service = -1, s => s.Service = 2, s => s.Completed = -1, s => s.Completed = 6,
            s => s.EventChoice = -2, s => s.EventChoice = 3, s => s.Token = "invalid",
            s => s.MainMap = "{", s => s.MainMap = Map(ThemeBranchCatalog.Page(1), "branch"),
            s => s.ActiveMap = Map("原版地图", "问号"), s => s.ActiveMap = null,
            s => s.ActiveMap = Map(ThemeBranchCatalog.Page(2), ThemeBranchCatalog.Node(2,0)),
            s => { var map = JObject.Parse(s.MainMap); map["当前地图页"] = new JObject(); s.MainMap = map.ToString(); },
            s => { var map = JObject.Parse(s.MainMap); map["当前地图页位置"] = -1; s.MainMap = map.ToString(); },
            s => { var map = JObject.Parse(s.MainMap); map["地图页列表"] = new JArray(); s.MainMap = map.ToString(); },
            s => { var map = JObject.Parse(s.MainMap); map.Remove("走过的节点"); s.MainMap = map.ToString(); },
            s => { var map = JObject.Parse(s.MainMap); map["节点作用列表"] = null; s.MainMap = map.ToString(); }
        }) Bad("invalid branch snapshot " + checks, attempt);
        Console.WriteLine($"PASS: {checks} production branch route, choice, return and JSON checks");
    }
}
