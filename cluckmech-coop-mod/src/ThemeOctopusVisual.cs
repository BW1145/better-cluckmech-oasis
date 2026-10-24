using System.Collections;
using UnityEngine;

namespace CluckmechCoop
{
    // Authored poses and textured limb segments follow the native actor's synchronized action clock.
    public sealed class ThemeOctopusVisual : MonoBehaviour
    {
        private ThemeEnemyActor actor;
        private ThemeBattleScene scene;
        private Transform rig;
        private SpriteRenderer body, stump, shaft, tip, water, rootJoint, tipJoint;
        private ThemeActorMessage action;
        private bool core, binding, ended;
        private float releasedAt = -100;
        private Vector2 releasePoint, lastTarget;
        private uint boundTarget;
        private string bodyKey;
        // PPU32 emit canvas: 611x538; feet=(305.5,534), inner beak=(395,310).
        private static readonly Vector2 BeakOffset = new Vector2(89.5f / 32, 224f / 32);
        private static readonly Vector2 CoreOffset = new Vector2(-89.5f / 32, -7.5f);
        private static readonly float[] JetReachPixels = { 159, 163, 168, 170 };
        private static readonly float[] FanReachPixels = { 159, 161, 164, 167 };

        internal static bool Owns(string id) => id == ThemeDeepSeaData.Octopus ||
            id == ThemeDeepSeaData.SlamArm || id == ThemeDeepSeaData.BindArm;

        internal void Initialize(ThemeEnemyActor owner, ThemeBattleScene battlefield)
        {
            actor = owner; scene = battlefield; core = actor.UnitId == ThemeDeepSeaData.Octopus;
            rig = new GameObject("OctopusRig").transform; rig.SetParent(transform, false);
            if (core) rig.localPosition = CoreOffset;
            body = Part(actor.UnitId + ".idle.0");
            if (!core)
            {
                stump = Part("deepsea.octopus.part.stump.0");
                shaft = Part("deepsea.octopus.part.shaft.0");
                shaft.drawMode = SpriteDrawMode.Tiled; shaft.tileMode = SpriteTileMode.Continuous;
                tip = Part("deepsea.octopus.part.slam-tip.0");
                rootJoint = Part("deepsea.octopus.part.joint.0"); tipJoint = Part("deepsea.octopus.part.joint.0");
                stump.enabled = shaft.enabled = tip.enabled = rootJoint.enabled = tipJoint.enabled = false;
            }
            else { water = Part("deepsea.octopus.fx.fan.0"); water.enabled = false; }
            actor.ActionChanged += Changed;
            if (actor.LastAction != null) Changed(actor.LastAction);
        }

        private SpriteRenderer Part(string key)
        {
            var sprite = ThemeAssets.Named(key);
            return ThemeAssets.Part(rig, key, sprite.bounds.size.x, Vector2.zero);
        }

        private void Changed(ThemeActorMessage message)
        {
            if (ended || message.Action == "motion-pause" || message.Action == "motion-resume" ||
                message.Action == "native-rage" || message.Action.StartsWith("shared-", System.StringComparison.Ordinal)) return;
            if (message.Action == "bind-lock")
            { binding = true; boundTarget = message.Target; lastTarget = new Vector2(message.EndX, message.EndY); return; }
            if (message.Action == "bind-release")
            { binding = false; releasePoint = lastTarget; releasedAt = Time.time; return; }
            action = message;
            if (message.Action == "death") { Retire(); return; }
            if (message.Action == "arm-bind")
            { binding = true; boundTarget = message.Target; lastTarget = new Vector2(message.EndX, message.EndY); }
            if (core && (message.Action == "octopus-bomb" || message.Action == "octopus-siege" || message.Action == "octopus-bite"))
                Splash(new Vector2(message.X, message.Y), message.Width * 2);
            if (message.Action == "arm-slam")
                Splash((new Vector2(message.X, message.Y) + new Vector2(message.EndX, message.EndY)) * .5f, message.Width * 1.7f);
        }

        private void LateUpdate()
        {
            if (ended || actor == null || body == null) return;
            float age = actor.ActionElapsed;
            if (core) Core(age); else Arm(age);
            Depth(body, transform.position.y, -.025f);
        }

        private void Pose(string key)
        {
            if (bodyKey == key) return;
            body.sprite = ThemeAssets.Named(key); body.transform.localScale = Vector3.one;
            body.transform.localPosition = Vector3.zero; bodyKey = key;
        }

        private void Core(float age)
        {
            string pose = "idle." + (Mathf.FloorToInt(Time.time * 1.5f) % 2);
            water.enabled = false;
            if (action != null)
            {
                float duration = Mathf.Max(.01f, action.Duration), t = Mathf.Clamp01(age / duration);
                switch (action.Action)
                {
                    case "octopus-arms": pose = "idle.0"; break;
                    case "octopus-expose":
                        if (age < duration) pose = "open." + Mathf.Min(2, Mathf.FloorToInt(t * 3));
                        else if (age < duration + .5f) pose = "close." + Mathf.Min(1, Mathf.FloorToInt((age - duration) * 4));
                        break;
                    case "octopus-breath-warning": case "octopus-bomb-warning": case "octopus-siege-warning":
                        if (age <= duration) pose = t < .5f ? "open." + Mathf.Min(1, Mathf.FloorToInt(t * 4)) : "charge.0";
                        break;
                    case "octopus-bite-warning":
                        if (age <= duration) pose = "open." + Mathf.Min(2, Mathf.FloorToInt(t * 3));
                        break;
                    case "octopus-breath":
                        if (age <= duration)
                        {
                            pose = "emit.0";
                            Water(new Vector2(action.EndX, action.EndY), age, action.Width);
                        }
                        else if (age < duration + .45f) pose = "close." + Mathf.Min(1, Mathf.FloorToInt((age - duration) * 5));
                        break;
                    case "octopus-siege":
                        if (age <= duration)
                        {
                            pose = "emit.0";
                            Water(new Vector2(action.X, action.Y), age, 0, true);
                        }
                        else if (age < duration + .45f) pose = "close." + Mathf.Min(1, Mathf.FloorToInt((age - duration) * 5));
                        break;
                    case "octopus-bite": case "octopus-bomb":
                        if (age < .16f) pose = "emit.0";
                        else if (age < .6f) pose = "close." + Mathf.Min(1, Mathf.FloorToInt((age - .16f) * 5));
                        break;
                }
            }
            Pose("deepsea.octopus." + pose);
        }

        private void Water(Vector2 target, float age, float halfAngle, bool focused = false)
        {
            water.enabled = true;
            Vector2 mouth = rig.TransformPoint(BeakOffset);
            Vector2 delta = target - mouth;
            if (delta.sqrMagnitude < .001f) return;
            int frame = Mathf.FloorToInt(age * 12) % 4;
            water.sprite = ThemeAssets.Named("deepsea.octopus.fx." + (focused ? "jet." : "fan.") + frame);
            water.transform.position = mouth;
            water.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            float length = delta.magnitude;
            water.transform.localScale = new Vector3(length * 32 / (focused ? JetReachPixels[frame] : FanReachPixels[frame]),
                (focused ? 1.2f : 2 * length * Mathf.Tan(halfAngle * Mathf.Deg2Rad)) / water.sprite.bounds.size.y, 1);
            Depth(water, mouth.y, -.08f);
        }

        private void Arm(float age)
        {
            body.enabled = true; stump.enabled = shaft.enabled = tip.enabled = rootJoint.enabled = tipJoint.enabled = false;
            string pose = "idle.0";
            float release = Time.time - releasedAt;
            if (release < .7f)
            {
                Limb(releasePoint, "bind-tip.3", 1 - Smooth(release / .7f), false);
                return;
            }
            if (action != null)
            {
                float t = Mathf.Clamp01(age / Mathf.Max(.01f, action.Duration));
                if (action.Action == "arm-slam-warning" && age <= action.Duration)
                {
                    pose = t < .4f ? "windup.0" : "windup.1";
                    if (t >= .72f)
                    {
                        Limb(SlamPoint(), "slam-tip.0", Smooth((t - .72f) / .28f), true);
                        return;
                    }
                }
                else if (action.Action == "arm-slam" && age < .85f)
                {
                    Limb(SlamPoint(), age < .12f ? "slam-tip.1" : "slam-tip.0",
                        age < .12f ? 1 : 1 - Smooth((age - .12f) / .73f), age >= .12f);
                    return;
                }
                else if (action.Action == "arm-bind-warning" && age < action.Duration + .35f)
                {
                    lastTarget = new Vector2(action.EndX, action.EndY);
                    float reach = t <= .3f ? 0 : Smooth((t - .3f) / .7f);
                    if (age > action.Duration) reach *= 1 - Smooth((age - action.Duration) / .35f);
                    if (reach > .01f) { Limb(lastTarget, "bind-tip.0", reach, false); return; }
                    pose = "reach.0";
                }
                else if (action.Action == "arm-bind" && binding)
                {
                    if (Mirror.NetworkClient.spawned.TryGetValue(boundTarget, out var unit)) lastTarget = unit.transform.position;
                    Limb(lastTarget, age < .25f ? "bind-tip.1" : "bind-tip.2", 1, false);
                    return;
                }
            }
            // Bind arms may use a heavy sweep when no legal binding target exists.
            string source = action != null && action.Action.StartsWith("arm-slam", System.StringComparison.Ordinal)
                ? ThemeDeepSeaData.SlamArm : actor.UnitId;
            Pose(source + "." + pose);
        }

        private Vector2 SlamPoint() => (new Vector2(action.X, action.Y) + new Vector2(action.EndX, action.EndY)) * .5f;

        private void Limb(Vector2 target, string endPose, float extension, bool lifted)
        {
            body.enabled = false; stump.enabled = shaft.enabled = tip.enabled = rootJoint.enabled = tipJoint.enabled = true;
            Vector2 root = (Vector2)transform.position + Vector2.up * (52f / 32);
            Vector2 end = Vector2.Lerp(root + Vector2.up * .9f, target, Mathf.Clamp01(extension));
            if (lifted) end += Vector2.up * (1.1f * Mathf.Sin(Mathf.Clamp01(extension) * Mathf.PI));
            bool coil = endPose.StartsWith("bind-", System.StringComparison.Ordinal);
            float angle = Mathf.Atan2(end.y + (coil ? .65f : 0) - root.y, end.x - root.x) * Mathf.Rad2Deg - 90;
            Vector2 socket = coil ? end + Vector2.up * .65f - Rotate(CoilCenter(endPose), angle) : end;
            Vector2 direction = action == null ? Vector2.right : new Vector2(action.EndX - action.X, action.EndY - action.Y).normalized;
            if (!coil && endPose == "slam-tip.1") socket -= direction * 1.55f;
            SetNative(stump, "deepsea.octopus.part.stump.0", transform.position);
            SetNative(tip, "deepsea.octopus.part." + endPose, socket);
            tip.transform.rotation = Quaternion.Euler(0, 0, angle);
            if (!coil && endPose == "slam-tip.1")
            {
                tip.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            }
            Vector2 delta = socket - root;
            SetNative(shaft, "deepsea.octopus.part.shaft.0", (root + socket) * .5f);
            shaft.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg - 90);
            shaft.size = new Vector2(shaft.sprite.bounds.size.x, Mathf.Max(.1f, delta.magnitude + .6f));
            SetNative(rootJoint, "deepsea.octopus.part.joint.0", root);
            SetNative(tipJoint, "deepsea.octopus.part.joint.0", socket);
            rootJoint.transform.rotation = tipJoint.transform.rotation = shaft.transform.rotation;
            rootJoint.transform.localScale = tipJoint.transform.localScale = Vector3.one * 1.3f;
            Depth(stump, root.y, -.03f); Depth(shaft, Mathf.Min(root.y, socket.y), -.04f); Depth(tip, target.y, -.055f);
            Depth(rootJoint, Mathf.Min(root.y, socket.y), -.08f); Depth(tipJoint, Mathf.Min(target.y, socket.y), -.08f);
        }

        private static Vector2 Rotate(Vector2 value, float angle)
        {
            float radians = angle * Mathf.Deg2Rad, c = Mathf.Cos(radians), s = Mathf.Sin(radians);
            return new Vector2(value.x * c - value.y * s, value.x * s + value.y * c);
        }

        private static Vector2 CoilCenter(string pose)
        {
            if (pose == "bind-tip.1") return new Vector2(36f / 32, 35f / 32);
            if (pose == "bind-tip.2") return new Vector2(25f / 32, 38f / 32);
            return pose == "bind-tip.0" ? new Vector2(1.125f, 1.4f) : new Vector2(1.1875f, 1);
        }

        private static void SetNative(SpriteRenderer image, string key, Vector2 position)
        {
            image.sprite = ThemeAssets.Named(key); image.transform.position = position;
            image.transform.localScale = Vector3.one; image.transform.rotation = Quaternion.identity;
        }
        private static void Depth(SpriteRenderer image, float y, float offset)
        {
            Vector3 at = image.transform.position; at.z = StratagemAssets.Z(y) + offset; image.transform.position = at;
        }
        private static float Smooth(float t) { t = Mathf.Clamp01(t); return t * t * (3 - 2 * t); }

        private void Splash(Vector2 point, float width)
        {
            ThemeVisual.Effect("deepsea.octopus.fx.splash.", 4, point, width, .5f, scene.transform, true);
        }

        private void Retire()
        {
            ended = true; body.enabled = true;
            if (stump != null) stump.enabled = shaft.enabled = tip.enabled = rootJoint.enabled = tipJoint.enabled = false;
            if (water != null) water.enabled = false;
            // The scene keeps the complete collapse after native actor destruction.
            rig.SetParent(scene.transform, true);
            scene.StartCoroutine(Collapse(body, rig, scene, actor.UnitId, core));
        }

        private static IEnumerator Collapse(SpriteRenderer image, Transform root, ThemeBattleScene scene, string unit, bool core)
        {
            var lifetime = SceneLifecycle.Current;
            float started = Time.time;
            while (SceneLifecycle.IsCurrent(lifetime) && root != null && Time.time - started < (core ? 1.5f : .7f))
            {
                float age = Time.time - started;
                int frame = core ? age < .35f ? 0 : age < .8f ? 1 : 2 : 0;
                image.sprite = ThemeAssets.Named(unit + ".death." + frame);
                image.transform.localScale = Vector3.one; image.transform.localPosition = Vector3.zero;
                Depth(image, root.position.y, -.025f);
                yield return null;
            }
            if (SceneLifecycle.IsCurrent(lifetime) && root != null)
            {
                ThemeBossRetreat.Begin(image, scene, unit + ".rest.", 1,
                    core ? 1.4f : .7f, Vector2.up * (core ? 1.8f : .8f), .85f, image.sprite.bounds.size.x);
                Destroy(root.gameObject);
            }
        }
        private void OnDestroy() { if (actor != null) actor.ActionChanged -= Changed; }
    }
}
