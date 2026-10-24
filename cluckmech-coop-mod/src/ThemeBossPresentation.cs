using UnityEngine;

namespace CluckmechCoop
{
    // Scene presentation subscribes once. Actors and native damage remain the authority for every outcome.
    public sealed class ThemeBossPresentation : MonoBehaviour
    {
        private static ThemeBossPresentation current;
        private static string pendingNode;
        private static float pendingElapsed;
        private static uint pendingTarget;
        private ThemeBattleScene scene;
        private ThemeWhiteVisitorVisual white;

        internal static bool Owns(string id) => id == ThemeEldritchData.WhiteVisitor ||
            id == ThemeEldritchData.WhiteHand || id == ThemeEldritchData.BloodMother || id == ThemeEldritchData.BloodSac ||
            ThemeOctopusVisual.Owns(id) || ThemeCastleBossVisual.Owns(id);

        internal static void Install(ThemeBattleScene battlefield)
        {
            if (battlefield == null || battlefield.GetComponent<ThemeBossPresentation>() != null) return;
            var owner = battlefield.gameObject.AddComponent<ThemeBossPresentation>();
            owner.scene = battlefield; current = owner;
            ThemeEnemyActor.Ready += owner.Ready;
            if (battlefield.Definition.Id != ThemeEldritchData.WhiteDock.Id) return;
            var root = new GameObject("WhiteVisitorPresentation");
            root.transform.SetParent(battlefield.transform, false);
            owner.white = root.AddComponent<ThemeWhiteVisitorVisual>(); owner.white.Initialize(battlefield);
            root.AddComponent<ThemeWhiteIntroCamera>().Initialize(owner.white);
            if (Plugin.Role == 2 && pendingNode == ModuleSettings.NodeKey)
                owner.white.ReceivePreparation(pendingElapsed, pendingTarget);
            else SyncPreparation();
            pendingNode = null;
        }

        internal static void SyncPreparation()
        {
            if (Plugin.Role != 1 || current == null || current.white == null) return;
            Plugin.Network.Broadcast(new SessionMessage { Type = "theme-intro", Token = ModuleSettings.NodeKey,
                Delay = current.white.PreparationElapsed, Unit = current.white.PreparationTarget });
        }

        internal static void ReceivePreparation(float elapsed, uint target)
        {
            if (current != null && current.white != null && current.scene == ThemeBattleScene.Current)
                current.white.ReceivePreparation(elapsed, target);
            else
            {
                pendingNode = ModuleSettings.NodeKey; pendingElapsed = elapsed;
                pendingTarget = target;
            }
        }

        private void Ready(ThemeEnemyActor actor)
        {
            if (ThemeBattleScene.Current != scene || actor == null || !Owns(actor.UnitId)) return;
            actor.GetComponent<ThemeEnemyVisual>()?.HideDefault();
            if (ThemeOctopusVisual.Owns(actor.UnitId))
                actor.gameObject.AddComponent<ThemeOctopusVisual>().Initialize(actor, scene);
            else if (ThemeCastleBossVisual.Owns(actor.UnitId))
                actor.gameObject.AddComponent<ThemeCastleBossVisual>().Initialize(actor, scene);
            else if (actor.UnitId == ThemeEldritchData.WhiteVisitor)
            {
                if (white == null)
                {
                    var root = new GameObject("WhiteVisitorPresentation"); root.transform.SetParent(scene.transform, false);
                    white = root.AddComponent<ThemeWhiteVisitorVisual>(); white.Initialize(scene);
                    white.ReceivePreparation(ThemeEldritchData.WhiteReveal.DurationSeconds, 0);
                    white.FinishPreparation();
                }
                white.Bind(actor);
            }
            else if (actor.UnitId == ThemeEldritchData.WhiteHand)
                actor.gameObject.AddComponent<ThemeWhiteHandVisual>().Initialize(actor, scene);
            else actor.gameObject.AddComponent<ThemeBloodVisual>().Initialize(actor, scene);
        }

        private void OnDestroy()
        {
            ThemeEnemyActor.Ready -= Ready;
            if (current == this) current = null;
        }
    }

    public sealed class ThemeWhiteHandVisual : MonoBehaviour
    {
        private ThemeEnemyActor actor;
        private ThemeBattleScene scene;
        private Transform rig;
        private SpriteRenderer image;
        private ThemeActorMessage action;
        private Vector2 previous;
        private float arrivalStarted, arrivalDuration = 1;
        private float graspElapsed, graspDuration;
        private bool ended, grasping;

        internal void Initialize(ThemeEnemyActor owner, ThemeBattleScene battlefield)
        {
            actor = owner; scene = battlefield; previous = transform.position;
            rig = new GameObject("WhiteHandRig").transform; rig.SetParent(transform, false);
            image = ThemeAssets.Part(rig, "deepsea.white-hand.idle.0", 5.6f, Vector2.zero);
            arrivalStarted = Time.time;
            actor.ActionChanged += Changed;
            if (actor.LastAction != null) Changed(actor.LastAction);
        }

        private void Changed(ThemeActorMessage message)
        {
            if (message.Action == "motion-pause" || message.Action == "motion-resume" ||
                message.Action.StartsWith("shared-", System.StringComparison.Ordinal)) return;
            action = message;
            if (message.Action == "white-hand-grasp-warning")
            { grasping = true; graspElapsed = 0; graspDuration = Mathf.Max(.01f, message.Duration); }
            else if (message.Action == "white-hand-grasp-cancel" || message.Action == "white-hand-grasp" || message.Action == "death")
                grasping = false;
            if (message.Action == "white-hand-arrive")
            { arrivalStarted = Time.time; arrivalDuration = Mathf.Max(.2f, message.Duration); }
            if (message.Action == "death")
            {
                ended = true;
                ThemeBossRetreat.Begin(image, scene, "deepsea.white-hand.death.", 1, 1.3f,
                    Vector2.up * 2.5f, .72f, 5.6f);
                image = null;
            }
            if (message.Action == "white-hand-slam" || message.Action == "white-hand-grasp")
                scene.GetComponent<ThemeNativeEffects>()?.Impact(new Vector2(message.EndX, message.EndY) == Vector2.zero ?
                    new Vector2(message.X, message.Y) : new Vector2(message.EndX, message.EndY), message.Width * .25f);
        }

        private void LateUpdate()
        {
            if (ended || actor == null || image == null) return;
            Vector2 point = transform.position;
            bool moving = (point - previous).sqrMagnitude > .00001f; previous = point;
            string pose = moving ? "approach.0" : "idle.0";
            float age = actor.ActionElapsed, t = actor.ActionProgress;
            float lift = 0, press = 0, rotation = 0;
            if (grasping)
            {
                if (!actor.Controlled) graspElapsed += Time.deltaTime;
                float grip = Mathf.Clamp01(graspElapsed / graspDuration);
                pose = grip < .6f ? "attack.0" : "attack.1";
                lift = .45f * Smooth(grip);
            }
            if (action != null)
            {
                switch (action.Action)
                {
                    case "white-hand-arrive":
                        float arrival = Mathf.Clamp01((Time.time - arrivalStarted) / arrivalDuration);
                        pose = "approach.0"; lift = (1 - Smooth(arrival)) * 2.5f; break;
                    case "white-hand-recoil":
                        if (age <= action.Duration + .25f)
                        { pose = "recoil.0"; lift = .35f * Mathf.Sin(t * Mathf.PI); rotation = Mathf.Sin(t * Mathf.PI) * -6; }
                        break;
                    case "white-hand-slam-warning": case "white-hand-grasp-warning":
                        if (age <= action.Duration + .1f)
                        { pose = t < .6f ? "attack.0" : "attack.1"; lift = .45f * Smooth(t); }
                        break;
                    case "white-hand-slam": case "white-hand-grasp":
                        if (age <= action.Duration + .2f)
                        { pose = "attack.1"; press = .15f * Mathf.Sin(t * Mathf.PI); }
                        break;
                    case "white-hand-grasp-cancel":
                        if (age <= action.Duration)
                        { pose = "recoil.0"; lift = (1 - Smooth(t)) * .45f; }
                        break;
                }
            }
            ThemeAssets.SetPart(image, "deepsea.white-hand." + pose, 5.6f, Vector2.zero);
            rig.localPosition = new Vector3(0, lift - press, StratagemAssets.Z(point.y) - transform.position.z - .035f);
            var baseUnit = ThemeDeepSeaCombat.BaseUnit();
            float face = baseUnit != null && baseUnit.transform.position.x < point.x ? -1 : 1;
            rig.localScale = new Vector3(face * (1 + press * .2f), 1 - press * .15f, 1);
            rig.localRotation = Quaternion.Euler(0, 0, rotation * face);
        }
        private static float Smooth(float t) => t * t * (3 - 2 * t);
        private void OnDestroy() { if (actor != null) actor.ActionChanged -= Changed; }
    }

    public sealed class ThemeBloodVisual : MonoBehaviour
    {
        private ThemeEnemyActor actor;
        private ThemeBattleScene scene;
        private Transform rig;
        private SpriteRenderer image;
        private ThemeActorMessage action;
        private float initialHealth, size = 1, awakeSize = 1;
        private bool sac, awakened, ended, bursting;

        internal void Initialize(ThemeEnemyActor owner, ThemeBattleScene battlefield)
        {
            actor = owner; scene = battlefield; sac = actor.UnitId == ThemeEldritchData.BloodSac;
            initialHealth = Mathf.Max(1, actor.Stats.当前生命值);
            rig = new GameObject(sac ? "BloodSacRig" : "BloodMotherRig").transform;
            rig.SetParent(transform, false);
            image = ThemeAssets.Part(rig, actor.UnitId + ".idle.0", sac ? 1.5f : 5.5f, Vector2.zero);
            actor.ActionChanged += Changed;
            if (actor.LastAction != null) Changed(actor.LastAction);
        }

        private void Changed(ThemeActorMessage message)
        {
            if (message.Action == "motion-pause" || message.Action == "motion-resume" ||
                message.Action == "native-rage") return;
            action = message;
            if (message.Action == "sac-burst") bursting = true;
            if (message.Action == "blood-awaken") { awakened = true; awakeSize = size; }
            if (message.Action == "sac-land") scene.GetComponent<ThemeNativeEffects>()?.Impact(transform.position, .4f);
            if (message.Action == "death")
            {
                ended = true;
                ThemeBossRetreat.Begin(image, scene, actor.UnitId + (bursting ? ".burst." : ".death."), 4,
                    sac ? .7f : 1.3f, sac ? Vector2.zero : Vector2.down * .5f, sac ? .65f : .85f, sac ? 1.5f : 5.5f);
                image = null;
            }
        }

        private void LateUpdate()
        {
            if (ended || actor == null || image == null) return;
            string pose = "idle." + (Mathf.FloorToInt(Time.time * 2.5f) % 2);
            float age = actor.ActionElapsed, t = actor.ActionProgress;
            float lift = 0, stretch = 0, lean = 0;
            if (!sac)
            {
                float desired = awakened ? awakeSize : 1 + Mathf.Clamp(actor.Stats.当前生命值 / initialHealth - 1, 0, 4) * .12f;
                size = Mathf.MoveTowards(size, desired, Time.deltaTime * .3f);
            }
            if (action != null)
            {
                switch (action.Action)
                {
                    case "blood-dormant": case "blood-feed":
                        if (action.Action == "blood-feed" && age < .4f)
                            stretch = .045f * Mathf.Sin(Mathf.Clamp01(age / .4f) * Mathf.PI);
                        break;
                    case "blood-awaken":
                        if (age <= action.Duration)
                        { pose = "awaken." + Mathf.Min(3, Mathf.FloorToInt(t * 4)); stretch = .12f * Mathf.Sin(t * Mathf.PI); }
                        break;
                    case "attack":
                        if (age <= action.Duration + .25f)
                        {
                            pose = "idle.1";
                            float push = age <= action.Duration ? Mathf.Sin(t * Mathf.PI * .5f) :
                                1 - Mathf.Clamp01((age - action.Duration) / .25f);
                            lean = .45f * push; stretch = -.08f * push;
                        }
                        break;
                    case "sac-warning":
                        float windup = ThemeEldritchData.BloodMotherParameters.SacWindupSeconds;
                        if (age <= action.Duration)
                        {
                            pose = "attack." + (age < windup * .5f ? 0 : age < windup ? 1 : age < windup + .25f ? 2 : 3);
                            stretch = age < windup ? .08f * Mathf.Clamp01(age / windup) :
                                .08f * (1 - Mathf.Clamp01((age - windup) / Mathf.Max(.01f, action.Duration - windup)));
                            lean = age < windup ? -.15f * Mathf.Clamp01(age / windup) : .2f * Mathf.Sin(t * Mathf.PI);
                        }
                        break;
                    case "blood-sweep-warning":
                        if (age <= action.Duration)
                        { pose = "sweep." + (t < .5f ? 0 : 1); lean = -.2f * t; }
                        break;
                    case "blood-sweep":
                        if (age <= action.Duration)
                        { pose = "sweep." + (t < .5f ? 2 : 3); lean = .4f * Mathf.Sin(t * Mathf.PI); }
                        break;
                    case "sac-flight":
                        pose = "flight." + (t < .5f ? 0 : 1); lift = Mathf.Sin(t * Mathf.PI) * 2.3f;
                        stretch = .15f * Mathf.Sin(t * Mathf.PI); break;
                    case "sac-land":
                        float pulse = Mathf.Sin(age * Mathf.Lerp(5, 14, t));
                        stretch = pulse * Mathf.Lerp(.025f, .085f, t); break;
                    case "sac-burst":
                        pose = "burst." + Mathf.Min(3, Mathf.FloorToInt(t * 4)); break;
                    case "sac-backlash":
                        pose = "death." + Mathf.Min(3, Mathf.FloorToInt(t * 4)); break;
                }
            }
            ThemeAssets.SetPart(image, actor.UnitId + "." + pose, sac ? 1.5f : 5.5f, Vector2.zero);
            Vector2 point = transform.position;
            rig.localPosition = new Vector3(actor.Facing.x * lean, lift + actor.Facing.y * lean,
                StratagemAssets.Z(point.y) - transform.position.z - .04f);
            rig.localScale = new Vector3(size * (1 - stretch * .4f), size * (1 + stretch), 1);
            image.flipX = actor.Facing.x < -.05f;
        }
        private void OnDestroy() { if (actor != null) actor.ActionChanged -= Changed; }
    }

    public sealed class ThemeBossRetreat : MonoBehaviour
    {
        private SpriteRenderer image;
        private string prefix;
        private int frames;
        private Vector3 position, scale;
        private Vector2 velocity;
        private float started, duration, finalScale, width;

        internal static void Begin(SpriteRenderer source, ThemeBattleScene scene, string prefix, int frames,
            float duration, Vector2 displacement, float finalScale, float width)
        {
            if (source == null || scene == null) return;
            var root = new GameObject("ThemeBossRetreat"); root.transform.SetParent(scene.transform, false);
            root.transform.position = source.transform.parent.position;
            root.transform.rotation = source.transform.parent.rotation;
            root.transform.localScale = source.transform.parent.lossyScale;
            source.transform.SetParent(root.transform, false);
            var current = root.AddComponent<ThemeBossRetreat>();
            current.image = source; current.prefix = prefix; current.frames = frames;
            current.position = root.transform.position; current.scale = root.transform.localScale;
            current.velocity = displacement; current.started = Time.time; current.duration = duration;
            current.finalScale = finalScale; current.width = width;
        }
        private void Update()
        {
            float t = Mathf.Clamp01((Time.time - started) / duration);
            // Authored key poses run at 8 fps, followed by the final pose's continuous withdrawal.
            ThemeAssets.SetPart(image, prefix + Mathf.Min(frames - 1, Mathf.FloorToInt((Time.time - started) * 8)), width, Vector2.zero);
            transform.position = position + (Vector3)(velocity * (t * t * (3 - 2 * t)));
            transform.localScale = scale * Mathf.Lerp(1, finalScale, t);
            image.color = new Color(1, 1, 1, Mathf.Clamp01((1 - t) * 3));
            if (t >= 1) Destroy(gameObject);
        }
    }
}
