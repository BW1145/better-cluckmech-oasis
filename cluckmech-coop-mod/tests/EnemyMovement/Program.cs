using System;
using System.Reflection;
using UnityEngine;
using PolyNav;

static class Program
{
    static int checks;
    static void Check(bool condition, string name) { if (!condition) throw new Exception(name); checks++; }
    static void Call(object target, string name) => target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null).Invoke(target, null);
    static GameObject MakeEnemy(string name, Vector2 position, 模块_基本属性.单位类型枚举 kind)
    {
        var go = new GameObject(name); go.transform.position = position;
        go.AddComponent<模块_基本属性>().单位类型 = kind;
        go.AddComponent<Rigidbody2D>(); go.AddComponent<模块_仇恨>();
        go.AddComponent<模块_移动属性>().移动速度 = 2;
        var agent = go.AddComponent<PolyNavAgent>();
        Call(agent, "Awake"); Call(agent, "OnEnable");
        var move = go.AddComponent<模块_移动>();
        Call(move, "OnEnable"); Call(move, "Start");
        Call(go.GetComponent<模块_移动属性>(), "Start");
        return go;
    }
    static void Target(GameObject go, GameObject target)
    {
        go.GetComponent<模块_仇恨>().增加固定仇恨(new 世界类.仇恨数据包 { 仇恨值 = 100, 仇恨目标 = target });
        Call(go.GetComponent<模块_移动属性>(), "移动目标赋值");
        Call(go.GetComponent<模块_移动>(), "移动目标赋值");
    }
    static void Main()
    {
        var world = new GameObject("世界");
        CluckmechCoop.Plugin.Spawner = world.AddComponent<刷怪器>();
        var feedback = world.AddComponent<效果物体引用器>();
        feedback.预制体.Boss死亡反馈 = new GameObject("通关反馈UI");
        var spawner = CluckmechCoop.Plugin.Spawner;
        spawner.当前波次 = 2; spawner.倒计时 = 35; spawner.关卡状态 = 刷怪器.关卡状态枚举.无;
        var countdown = new GameObject("countdown").AddComponent<模块_波次倒计时UI>();
        countdown.倒计时 = new GameObject("timer-text").AddComponent<UnityEngine.UI.Text>();
        countdown.波次 = new GameObject("stage-text").AddComponent<UnityEngine.UI.Text>();
        countdown.当前波次 = new GameObject("current-stage").AddComponent<UnityEngine.UI.Text>();
        countdown.总波次 = new GameObject("total-stages").AddComponent<UnityEngine.UI.Text>();
        countdown.波次倒计时 = new GameObject("321").AddComponent<Animator>();
        countdown.波次显示 = new GameObject("stage-animation").AddComponent<Animator>();
        Call(countdown, "Start"); Call(countdown, "Update");
        Check(!countdown.波次倒计时.gameObject.activeSelf && countdown.当前波次.text == "3", "native UI remains unchanged before playback");
        CluckmechCoop.Difficulty.Playback(0, 0); Call(countdown, "Update");
        Check(!countdown.波次倒计时.gameObject.activeSelf && countdown.当前波次.text == "1", "first additional spawn has no preparation wait");
        Time.time = 10; CluckmechCoop.Difficulty.Playback(1, 8); Call(countdown, "Update");
        Check(countdown.倒计时.text == "8" && countdown.波次倒计时.gameObject.activeSelf && !countdown.波次显示.gameObject.activeSelf, "existing spawn interval uses native countdown UI");
        Time.time = 14; Call(countdown, "Update");
        Check(countdown.波次倒计时.Trigger == "触发倒计时321", "native countdown triggers original 321 animation");
        Time.time = 16.5f; Call(countdown, "Update");
        Check(!countdown.倒计时.gameObject.activeSelf && countdown.波次显示.gameObject.activeSelf && countdown.波次.text == "2", "native stage animation shows the upcoming additional batch");
        CluckmechCoop.Difficulty.Playback(1, 0); Call(countdown, "Update");
        Check(!countdown.波次倒计时.gameObject.activeSelf, "countdown ends when additional spawning resumes");
        Check(spawner.当前波次 == 2 && spawner.倒计时 == 35 && spawner.关卡状态 == 刷怪器.关卡状态枚举.无 && !spawner.是否计时, "display readers do not change native encounter state or spawn timing");
        CluckmechCoop.ModuleSettings.Active = false; Call(countdown, "Update");
        Check(countdown.当前波次.text == "3" && !countdown.波次倒计时.gameObject.activeSelf, "disabled expedition uses native UI values");
        CluckmechCoop.ModuleSettings.Active = true;
        var maps = new GameObject("navigation").AddComponent<导航管理器>();
        maps.地面导航地图 = new GameObject("ground").AddComponent<PolyNavMap>();
        maps.空中导航地图 = new GameObject("air").AddComponent<PolyNavMap>();
        PolyNavMap.current = maps.地面导航地图;
        var car = new GameObject("基地车(Clone)"); car.transform.position = new Vector3(10, 0);
        var carHp = car.AddComponent<模块_基本属性>();

        var enemy = MakeEnemy("repeated-ground", Vector2.zero, 模块_基本属性.单位类型枚举.地面单位);
        var movement = enemy.GetComponent<模块_移动属性>(); var agent = enemy.GetComponent<PolyNavAgent>();
        Check(agent.map == maps.地面导航地图, "repeated ground enemy initializes its original ground map");
        Check(agent.maxSpeed > 0 && movement.当前移动速度 == 2, "native initialization retains positive movement speed");

        var boss = new GameObject("boss").AddComponent<词条_100028_Boss胜利判断>();
        boss.基地车 = car; boss.是否死亡 = true; boss.是否出天赋 = true; boss.天赋UI = new GameObject("talent"); boss.延时 = 0;
        Call(boss, "延迟");
        Check(carHp.是否无敌 && carHp.当前是否无敌 && UnityEngine.Object.RewardCount == 1, "native first boss completion enables base invulnerability and rewards");
        Target(enemy, car);
        Check(enemy.GetComponent<模块_仇恨>().当前仇恨列表.Count == 0, "native targeting discards the invulnerable base");
        Check(movement.目标坐标 == Vector2.zero && !agent.hasPath, "native target becomes spawn position and navigation stops");
        Call(agent, "FixedUpdate");
        Check((Vector2)enemy.transform.position == Vector2.zero, "reproduce additional enemy standing still with a valid map and positive speed");

        carHp.是否无敌 = false; carHp.当前是否无敌 = false; UnityEngine.Object.RewardCount = 0;
        boss.是否死亡 = true;
        var gate = typeof(CluckmechCoop.BossRewardGate).GetMethod("Prefix", BindingFlags.Static | BindingFlags.NonPublic);
        Check(!(bool)gate.Invoke(null, new object[] { boss, feedback }), "production prefix defers the native completion feedback creation");
        Check(UnityEngine.Object.FeedbackCount == 0, "completion banner is absent throughout additional combat");
        Call(boss, "延迟");
        Check(!carHp.是否无敌 && !carHp.当前是否无敌 && UnityEngine.Object.RewardCount == 0, "production completion gate keeps base targetable and reward pending");
        Target(enemy, car);
        Check(enemy.GetComponent<模块_仇恨>().当前仇恨列表.Count == 1, "native targeting retains the base after the production fix");
        Check(movement.目标坐标 == (Vector2)car.transform.position && agent.hasPath, "fixed native enemy receives a path to the base");
        Call(agent, "FixedUpdate");
        Check(enemy.transform.position.x > 0 && agent.currentSpeed > 0, "native navigation actually advances the additional enemy");

        var flyer = MakeEnemy("repeated-flyer", new Vector2(0, 3), 模块_基本属性.单位类型枚举.飞行单位);
        Check(flyer.GetComponent<PolyNavAgent>().map == maps.空中导航地图, "repeated flying enemy receives its air navigation map");
        Target(flyer, car); Call(flyer.GetComponent<PolyNavAgent>(), "FixedUpdate");
        Check(flyer.transform.position.x > 0, "native flying navigation moves toward the base");

        var direct = MakeEnemy("direct-base", new Vector2(0, -3), 模块_基本属性.单位类型枚举.地面单位);
        direct.GetComponent<模块_移动属性>().前进副目标 = car.transform;
        Target(direct, car); Call(direct.GetComponent<PolyNavAgent>(), "FixedUpdate");
        Check(direct.transform.position.x > 0, "original direct-target behavior stays active");

        CluckmechCoop.Difficulty.CaptureBossReward(boss);
        CluckmechCoop.Difficulty.Complete();
        foreach (var task in CluckmechCoop.Plugin.Instance.Coroutines) while (task.MoveNext()) { }
        Check(carHp.是否无敌 && UnityEngine.Object.RewardCount == 1, "production gate grants base protection and one reward only after completion");
        Check(UnityEngine.Object.FeedbackCount == 1, "original completion banner appears once after all enemies are cleared");
        int tasks = CluckmechCoop.Plugin.Instance.Coroutines.Count;
        CluckmechCoop.Difficulty.Complete();
        Check(CluckmechCoop.Plugin.Instance.Coroutines.Count == tasks, "duplicate completion schedules no second reward");
        Check(UnityEngine.Object.FeedbackCount == 1, "duplicate boss completion cannot create a second banner");
        CluckmechCoop.ModuleSettings.Active = false;
        Check((bool)gate.Invoke(null, new object[] { boss, feedback }), "disabled expedition keeps native boss feedback creation");
        CluckmechCoop.ModuleSettings.Active = true;
        CluckmechCoop.Difficulty.Playback(0, 8); Call(countdown, "Update");
        Check(countdown.当前波次.text == "3" && !countdown.波次倒计时.gameObject.activeSelf, "late playback message does not reopen countdown after completion");
        Console.WriteLine($"PASS: {checks} native enemy targeting, ground/air movement and production completion checks; unobstructed test map");
    }
}
