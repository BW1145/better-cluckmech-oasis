using System;
using System.Collections.Generic;
using UnityEngine;

namespace CluckmechCoop
{
    internal static class ThemeVisual
    {
        internal static void Action(ThemeEnemyActor actor, ThemeActorMessage message)
        {
            Vector2 from = new Vector2(message.X, message.Y), to = new Vector2(message.EndX, message.EndY);
            switch (message.Action)
            {
                case "flower-warning":
                    Watch(Point(to, message.Width, message.Duration + .3f), actor, message, "land", to, false); break;
                case "charge-warning": case "vine-warning": case "crack-warning": case "arm-slam-warning":
                    Watch(Band(from, to, message.Width, message.Duration), actor, message,
                        message.Action == "charge-warning" ? "charge" : message.Action == "vine-warning" ? "vine" :
                        message.Action == "crack-warning" ? "crack-bite" : "arm-slam", to); break;
                case "valve-warning":
                    Watch(Line(from, to, message.Width, message.Duration), actor, message, "valve-shot", to); break;
                case "octopus-bite-warning": case "octopus-bomb-warning":
                    Watch(Point(to, message.Width, message.Duration), actor, message,
                        message.Action.Replace("-warning", ""), to, false); break;
                case "octopus-siege-warning":
                    var octopusSiege = Watch(Point(to, message.Width, message.Duration), actor, message,
                        "octopus-siege", to, false);
                    octopusSiege.Follow = ThemeDeepSeaCombat.BaseUnit()?.transform;
                    break;
                case "white-bite-warning": case "white-spit-warning": case "white-hand-grasp-warning":
                    Watch(Point(to, message.Width, message.Duration), actor, message,
                        message.Action.Replace("-warning", ""), to); break;
                case "white-hand-slam-warning":
                    Watch(Point(from, message.Width, message.Duration), actor, message, "white-hand-slam", from, false); break;
                case "white-siege-warning":
                    var siege = Watch(Point(to, message.Width, message.Duration), actor, message, null, to);
                    var baseTarget = ThemeDeepSeaCombat.BaseUnit();
                    siege.Follow = baseTarget == null ? null : baseTarget.transform;
                    siege.Elapsed = () => { if (ThemeCombat.Friendly(baseTarget)) WaterImpact(baseTarget.transform.position, message.Width, .45f); };
                    break;
                case "white-crush-warning":
                    Watch(Band(from, to, message.Width, message.Duration), actor, message, "white-crush", to); break;
                case "white-next-side":
                    Watch(Point(to, message.Width, message.Duration), actor, message, "white-move", to); break;
                case "octopus-breath-warning":
                    Watch(Cone(from, to, message.Width * 2, message.Duration), actor, message, "octopus-breath", from, false); break;
                case "blood-sweep-warning":
                    Watch(Cone(from, to, ThemeEldritchData.BloodMotherParameters.SweepAngleDegrees, message.Duration),
                        actor, message, "blood-sweep", from, false); break;
                case "rope-warning":
                    Watch(Cone(from, to, ThemeEldritchData.HangingParameters.SweepAngleDegrees,
                        message.Duration, true), actor, message, "rope-sweep", from, false); break;
                case "sac-warning":
                    Watch(Point(to, message.Width, message.Duration), actor, message, null, to); break;
                case "sac-land":
                    ThemeVisualSprite.ResolveWarning("sac-warning", from);
                    Watch(Point(from, message.Width, message.Duration), actor, message, "sac-burst", from, false); break;
                case "arm-bind-warning":
                    Watch(Point(to, message.Width, message.Duration), actor, message, "arm-bind", to); break;
                case "shadow-warning":
                    var shadow = Watch(Line(from, to, .35f, message.Duration), actor, message, "shadow-shot", to);
                    shadow.Tint = new Color(.65f, .45f, 1, 1); break;
                case "vine":
                    float length = Vector2.Distance(from, to);
                    for (float distance = .5f; distance <= length; distance += 1.3f)
                        Owned(Effect("castle.fx.vines.", 4, Vector2.Lerp(from, to, distance / length), 2,
                            Mathf.Max(.5f, message.Duration)), actor);
                    break;
                case "crack-bite":
                    Owned(Strip("castle.fx.cable.0", 1, from, to, message.Width, message.Duration), actor);
                    NativeImpact((from + to) * .5f, message.Width * .5f); break;
                case "rope-sweep":
                    var rope = Owned(Strip("castle.fx.cable.0", 1, from, to, message.Width, message.Duration), actor);
                    rope.Sweep(from, to, ThemeEldritchData.HangingParameters.SweepAngleDegrees); break;
                case "white-crush":
                    Owned(Strip("deepsea.fx.flow.", 4, from, to, message.Width, message.Duration), actor);
                    NativeImpact((from + to) * .5f, message.Width * .5f); break;
                case "octopus-bite":
                    NativeImpact(from, message.Width * .35f); break;
                case "white-bite":
                    WaterImpact(to, message.Width, message.Duration, "deepsea.fx.beak.0"); break;
                case "octopus-bomb": case "octopus-siege":
                    NativeImpact(from, message.Width * .35f); break;
                case "white-spit":
                    WaterImpact(to, message.Width, message.Duration); break;
                case "white-hand-slam":
                    Effect("deepsea.fx.splash.", 4, from, message.Width * 2, message.Duration); break;
                case "white-hand-grasp":
                    Effect("deepsea.fx.splash.", 4, to, message.Width * 2, message.Duration); break;
                case "valve-shot":
                    Owned(Effect("deepsea.fx.globule.0", 1, from, .8f, message.Duration), actor); break;
                case "octopus-arms":
                    foreach (var arm in ThemeDeepSeaData.OctopusArms)
                        Effect("deepsea.fx.emergence.0", 1, ThemeBattleScene.Current.World(arm.Position), 2.5f, 1);
                    break;
                case "octopus-expose": case "blood-awaken":
                    Owned(Effect("deepsea.fx.emergence.0", 1, from, 3.5f, message.Duration), actor); break;
                case "transport-in": case "transport-out":
                    string portal = actor.UnitId.StartsWith("deepsea.", StringComparison.Ordinal) ? "deepsea.fx.portal." : "castle.fx.portal.0";
                    Owned(Effect(portal, portal.EndsWith(".", StringComparison.Ordinal) ? 4 : 1, from, 1.8f,
                        Mathf.Max(.15f, message.Duration), ground: true), actor); break;
                case "blood-sweep":
                    NativeImpact(from + actor.Facing * (message.Width * .7f), .8f); break;
                case "sac-burst":
                    WaterImpact(from, message.Width, message.Duration); break;
                case "sac-backlash":
                    Effect("castle.fx.wine.", 4, to, 1.4f, message.Duration); NativeImpact(to, .5f); break;
                case "land": case "pillar-stun":
                    Impact(from, Mathf.Max(1, message.Width)); break;
                case "count-stab": Impact(to, .7f); break;
                case "wine-pour": Effect("castle.fx.wine.", 4, to, 1.3f, message.Duration); break;
                case "death":
                    if (!ThemeBossPresentation.Owns(actor.UnitId))
                    {
                        if (actor.UnitId.StartsWith("deepsea.", StringComparison.Ordinal)) WaterImpact(from, .4f, .4f);
                        else Impact(from, actor.UnitId == ThemeCastleData.Count ? 2.8f : .8f);
                    }
                    break;
            }
        }

        private static ThemeVisualSprite Owned(ThemeVisualSprite effect, ThemeEnemyActor actor)
        { effect.Owner = actor; return effect; }

        private static ThemeVisualSprite Watch(ThemeVisualSprite effect, ThemeEnemyActor actor, ThemeActorMessage message,
            string completedAction, Vector2 point, bool matchEnd = true)
        {
            effect.Bind(actor, message.Action, completedAction, point, matchEnd, message.Target);
            return effect;
        }

        internal static ThemeVisualSprite Point(Vector2 point, float radius, float seconds)
        {
            var effect = Effect("castle.fx.warning.0", 1, point, radius * 2, seconds, ground: true);
            effect.SetSize(radius * 2, radius * 2);
            effect.Pulse = true;
            return effect;
        }

        internal static ThemeVisualSprite Line(Vector2 start, Vector2 end, float width, float seconds)
        {
            var effect = Strip("castle.fx.warning.1", 1, start, end, width, seconds, true);
            effect.Pulse = true;
            return effect;
        }

        private static ThemeVisualSprite Band(Vector2 start, Vector2 end, float width, float seconds)
        {
            Vector2 cap = (end - start).normalized * (width * .5f);
            return Line(start - cap, end + cap, width, seconds);
        }

        private static ThemeVisualSprite Strip(string key, int count, Vector2 start, Vector2 end, float width,
            float seconds, bool ground = false)
        {
            var effect = Effect(key, count, (start + end) * .5f, Vector2.Distance(start, end), seconds, ground: ground);
            effect.Angle = Mathf.Atan2(end.y - start.y, end.x - start.x) * Mathf.Rad2Deg;
            effect.SetSize(Vector2.Distance(start, end), width);
            return effect;
        }

        private static ThemeVisualSprite Cone(Vector2 origin, Vector2 end, float angle, float seconds,
            bool pillarStops = false, string texture = "castle.fx.warning.0")
        {
            var effect = Effect(texture, texture.EndsWith(".", StringComparison.Ordinal) ? 4 : 1, origin, 1, seconds, ground: true);
            effect.MakeCone(origin, end, angle, pillarStops, texture == "castle.fx.warning.0");
            effect.Pulse = texture == "castle.fx.warning.0";
            return effect;
        }

        internal static ThemeVisualSprite Effect(string key, int count, Vector2 point, float width,
            float seconds, Transform parent = null, bool ground = false)
        {
            string first = count == 1 ? key : key + "0";
            var image = ThemeAssets.Renderer(parent, first, width, Vector2.zero, ground);
            image.transform.position = new Vector3(point.x, point.y, StratagemAssets.Z(point.y) + (ground ? .25f : -.05f));
            var effect = image.gameObject.AddComponent<ThemeVisualSprite>();
            effect.Initialize(image, key, count, Mathf.Max(.05f, seconds));
            return effect;
        }

        private static void WaterImpact(Vector2 point, float radius, float duration, string key = null)
        {
            var effect = Effect(key ?? "deepsea.fx.splash.", key == null ? 4 : 1, point, radius * 2, duration);
            effect.SetSize(radius * 2, radius * 2);
            NativeImpact(point, radius * .35f);
        }

        private static void NativeImpact(Vector2 point, float width)
        { ThemeBattleScene.Current?.GetComponent<ThemeNativeEffects>()?.Impact(point, width); }

        internal static void Impact(Vector2 point, float width)
        {
            Effect("castle.fx.impact.", 4, point, width, .5f);
            NativeImpact(point, width * .35f);
        }
    }

    public sealed class ThemeVisualSprite : MonoBehaviour
    {
        private static readonly List<ThemeVisualSprite> warnings = new List<ThemeVisualSprite>();
        internal SpriteRenderer Image;
        internal bool Pulse;
        internal ThemeEnemyActor Owner;
        internal Color Tint = Color.white;
        internal float Angle;
        internal int FramesPerSecond;
        internal uint MatchTarget;
        internal Action Elapsed;
        internal Transform Follow;
        private Sprite[] frames;
        private float started, seconds;
        private SceneLifetime scene;
        private Vector2 center, size;
        private string warningAction, completedAction;
        private Vector2 completionPoint;
        private bool matchEnd, subscribed, fitSize;
        private Mesh mesh;
        private Vector2[] sectorUv;
        private MeshRenderer area;
        private Material fill, surface;
        private bool rotating;
        private Vector2 sweepOrigin;
        private float sweepStart, sweepAngle, sweepRange, fillOpacity;

        internal void Initialize(SpriteRenderer image, string key, int count, float duration)
        {
            Image = image; started = Time.time; seconds = duration; scene = SceneLifecycle.Current;
            center = image.transform.position;
            size = Vector2.Scale(image.sprite.bounds.size, image.transform.localScale);
            frames = new Sprite[count];
            for (int i = 0; i < count; i++) frames[i] = ThemeAssets.Named(count == 1 ? key : key + i);
        }

        internal void SetSize(float width, float height)
        { fitSize = true; size = new Vector2(Mathf.Max(.01f, width), Mathf.Max(.01f, height)); Place(); }

        private void Place()
        {
            var bounds = Image.sprite.bounds;
            Image.transform.localScale = new Vector3(size.x / bounds.size.x, size.y / bounds.size.y, 1);
            Image.transform.rotation = Quaternion.Euler(0, 0, Angle);
            Vector3 shift = Image.transform.rotation * Vector3.Scale(bounds.center, Image.transform.localScale);
            var point = Image.transform.position;
            Image.transform.position = new Vector3(center.x - shift.x, center.y - shift.y, point.z);
        }

        internal void Bind(ThemeEnemyActor actor, string warning, string completed, Vector2 point, bool end, uint target)
        {
            Owner = actor; warningAction = warning; completedAction = completed;
            completionPoint = point; matchEnd = end; MatchTarget = target;
            actor.ActionChanged += Changed; subscribed = true; warnings.Add(this);
        }

        internal static void ResolveWarning(string action, Vector2 point)
        {
            foreach (var warning in warnings)
                if (warning != null && warning.warningAction == action && (warning.completionPoint - point).sqrMagnitude < .01f)
                    Destroy(warning.gameObject);
        }

        private void Changed(ThemeActorMessage message)
        {
            if (message.Action == "death" ||
                message.Action == "shadow-cancel" && warningAction == "shadow-warning" && message.Target == MatchTarget ||
                message.Action == "white-hand-grasp-cancel" && warningAction == "white-hand-grasp-warning" ||
                message.Action == "white-phase" && warningAction == "white-next-side" ||
                message.Action == "idle" && warningAction == "valve-warning")
            { Destroy(gameObject); return; }
            if (message.Action != completedAction) return;
            if (warningAction == "shadow-warning" && message.Target != MatchTarget) return;
            Vector2 point = matchEnd ? new Vector2(message.EndX, message.EndY) : new Vector2(message.X, message.Y);
            if ((point - completionPoint).sqrMagnitude < .01f) Destroy(gameObject);
        }

        internal void Sweep(Vector2 origin, Vector2 end, float degrees)
        {
            rotating = true; sweepOrigin = origin; sweepRange = Vector2.Distance(origin, end);
            sweepStart = Mathf.Atan2(end.y - origin.y, end.x - origin.x) * Mathf.Rad2Deg - degrees;
            sweepAngle = degrees;
        }

        internal void MakeCone(Vector2 origin, Vector2 end, float degrees, bool pillarStops, bool warning)
        {
            Image.enabled = false;
            var shape = new GameObject("ThemeWarningSector"); shape.transform.SetParent(transform, false);
            shape.transform.localPosition = Vector3.zero;
            transform.localScale = Vector3.one; transform.rotation = Quaternion.identity;
            transform.position = new Vector3(origin.x, origin.y, StratagemAssets.Z(origin.y) + .25f);
            var sprite = Image.sprite;
            area = shape.AddComponent<MeshRenderer>(); area.sortingLayerID = Image.sortingLayerID;
            area.sortingOrder = Image.sortingOrder;
            var filter = shape.AddComponent<MeshFilter>(); mesh = new Mesh { name = "ThemeAttackSector" }; filter.sharedMesh = mesh;
            fill = new Material(Image.sharedMaterial) { mainTexture = Texture2D.whiteTexture };
            surface = new Material(Image.sharedMaterial) { mainTexture = sprite.texture };
            area.sharedMaterials = new[] { fill, surface };
            float range = Vector2.Distance(origin, end), heading = Mathf.Atan2(end.y - origin.y, end.x - origin.x);
            int segments = Mathf.Max(2, Mathf.CeilToInt(degrees / 3));
            var vertices = new Vector3[segments + 2]; var uv = new Vector2[vertices.Length];
            var triangles = new int[segments * 3];
            uv[0] = warning ? Vector2.one * .5f : new Vector2(0, .5f);
            ThemeBattlePillar exclude = NearestPillar(origin);
            for (int i = 0; i <= segments; i++)
            {
                float offset = Mathf.Lerp(-degrees * .5f, degrees * .5f, (float)i / segments) * Mathf.Deg2Rad;
                Vector2 direction = new Vector2(Mathf.Cos(heading + offset), Mathf.Sin(heading + offset));
                Vector2 tip = pillarStops && ThemeBattleScene.Current != null ?
                    ThemeCastleAttackGeometry.StopAtPillar(ThemeBattleScene.Current, origin, direction, range, exclude) : origin + direction * range;
                if (!pillarStops && ThemeBattleScene.Current?.BlocksShot(origin, tip) == true)
                    foreach (var hit in Physics2D.RaycastAll(origin, direction, range, 1 << 10))
                        if (ThemeBattleScene.Current.BlocksShot(origin, hit.point + direction * .02f))
                            tip = origin + direction * Mathf.Min(Vector2.Distance(origin, tip), hit.distance);
                Vector2 local = tip - origin; vertices[i + 1] = local;
                Vector2 sample = warning ? new Vector2(.5f + local.x / (2 * range), .5f + local.y / (2 * range)) :
                    new Vector2(local.magnitude * Mathf.Cos(offset) / range,
                        .5f + local.magnitude * Mathf.Sin(offset) / (2 * range * Mathf.Sin(degrees * .5f * Mathf.Deg2Rad)));
                uv[i + 1] = sample;
                if (i == segments) continue;
                triangles[i * 3] = 0; triangles[i * 3 + 1] = i + 1; triangles[i * 3 + 2] = i + 2;
            }
            var colors = new Color[vertices.Length];
            for (int i = 0; i < colors.Length; i++) colors[i] = Color.white;
            mesh.vertices = vertices; sectorUv = uv; SectorTexture(); mesh.colors = colors; mesh.subMeshCount = 2;
            mesh.SetTriangles(triangles, 0); mesh.SetTriangles(triangles, 1); mesh.RecalculateBounds();
            fillOpacity = warning ? .18f : 0;
            fill.color = warning ? new Color(1, .1f, .2f, fillOpacity) : Color.clear;
            surface.color = Color.white;
        }

        private void SectorTexture()
        {
            var sprite = Image.sprite; var rect = sprite.textureRect;
            var uv = new Vector2[sectorUv.Length];
            for (int i = 0; i < uv.Length; i++) uv[i] = new Vector2(
                (rect.x + sectorUv[i].x * rect.width) / sprite.texture.width,
                (rect.y + sectorUv[i].y * rect.height) / sprite.texture.height);
            mesh.uv = uv; surface.mainTexture = sprite.texture;
        }

        private static ThemeBattlePillar NearestPillar(Vector2 origin)
        {
            ThemeBattlePillar nearest = null; float distance = float.PositiveInfinity;
            if (ThemeBattleScene.Current == null) return null;
            foreach (var pillar in ThemeBattleScene.Current.Pillars)
            {
                float candidate = (pillar.Center - origin).sqrMagnitude;
                if (candidate < distance) { distance = candidate; nearest = pillar; }
            }
            return nearest;
        }

        private void Update()
        {
            float age = Time.time - started;
            if (!SceneLifecycle.IsCurrent(scene)) { Destroy(gameObject); return; }
            if (Follow != null) { center = Follow.position; if (fitSize) Place(); }
            if (!ReferenceEquals(Owner, null))
            {
                if (Owner == null || !Owner.Alive) { Destroy(gameObject); return; }
                // A thrown sac flies on its own clock after the mother's windup.
                bool independentFlight = warningAction == "sac-warning" && age >= ThemeEldritchData.BloodMotherParameters.SacWindupSeconds;
                bool flowerTravelPaused = warningAction == "flower-warning" && Owner.Action == "dive" && !Owner.CanAct;
                if (Owner.ActionPaused && !independentFlight || flowerTravelPaused) { started += Time.deltaTime; return; }
            }
            if (age >= seconds) { Elapsed?.Invoke(); Elapsed = null; Destroy(gameObject); return; }
            float progress = age / seconds;
            int frame = FramesPerSecond > 0 ? Mathf.FloorToInt(age * FramesPerSecond) % frames.Length :
                Mathf.Min(frames.Length - 1, Mathf.FloorToInt(progress * frames.Length));
            var nextFrame = frames[frame];
            if (Image.sprite != nextFrame) { Image.sprite = nextFrame; if (area != null) SectorTexture(); }
            if (area == null && (fitSize || rotating))
            {
                if (rotating)
                {
                    float angle = sweepStart + sweepAngle * progress;
                    Vector2 direction = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
                    Vector2 end = ThemeBattleScene.Current == null ? sweepOrigin + direction * sweepRange :
                        ThemeCastleAttackGeometry.StopAtPillar(ThemeBattleScene.Current, sweepOrigin, direction, sweepRange, NearestPillar(sweepOrigin));
                    center = (sweepOrigin + end) * .5f; size.x = Vector2.Distance(sweepOrigin, end); Angle = angle;
                }
                Place();
            }
            float opacity = Pulse ? .75f + .2f * Mathf.Sin(age * 7) : Mathf.Clamp01((1 - progress) * 5);
            Image.color = new Color(Tint.r, Tint.g, Tint.b, opacity * Tint.a);
            if (area != null)
            {
                surface.color = Image.color;
                var color = fill.color; color.a = fillOpacity * opacity; fill.color = color;
            }
        }

        private void OnDestroy()
        {
            warnings.Remove(this);
            if (subscribed && Owner != null) Owner.ActionChanged -= Changed;
            if (mesh != null) Destroy(mesh);
            if (fill != null) Destroy(fill);
            if (surface != null) Destroy(surface);
        }
    }

    // Native particles retain their source gradients; the generated frames supply the impact silhouette.
    public sealed class ThemeNativeEffects : MonoBehaviour
    {
        private GameObject impact;
        private void Start()
        {
            var native = GetComponent<ThemeBattleScene>()?.GetComponentInParent<效果物体引用器>();
            if (native == null) native = UnityEngine.Object.FindObjectOfType<效果物体引用器>();
            impact = native?.特效.普通攻击效果;
        }
        internal void Impact(Vector2 point, float size)
        {
            if (impact == null) return;
            var burst = Instantiate(impact, new Vector3(point.x, point.y, StratagemAssets.Z(point.y) - .04f), Quaternion.identity);
            burst.transform.SetParent(transform, true); burst.transform.localScale = Vector3.one * size;
            foreach (var sound in burst.GetComponentsInChildren<AudioSource>(true)) sound.enabled = false;
            foreach (var particles in burst.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = particles.main; main.loop = false; main.useUnscaledTime = false;
            }
            Destroy(burst, 2);
        }
    }
}
