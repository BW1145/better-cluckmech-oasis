using System;
using System.Collections.Generic;
using UnityEngine;

namespace CluckmechCoop
{
    // Body poses follow the native actor packet; tethers follow the living knot actors.
    public sealed class ThemeCastleBossVisual : MonoBehaviour
    {
        private enum Suspension { Hanging, Fallen, Recovering, Rethreading }
        private sealed class Tether
        {
            internal ThemeEnemyActor Knot;
            internal SpriteRenderer Image;
            internal Vector2 Start, End;
        }

        private readonly List<Tether> tethers = new List<Tether>(2);
        private ThemeEnemyActor actor, source;
        private ThemeSharedBodyPart shared;
        private ThemeBattleScene scene;
        private ThemeActorMessage action;
        private Transform rig;
        private SpriteRenderer image;
        private Suspension suspension;
        private float width, actionAge, modeAge, modeDuration, lift, fallStartLift;
        private float previousHealth, flashUntil, nextBinding;
        private bool ended, listeningKnots, landed;
        private string renderedPose = "idle.0";
        private const float HangingLift = .65f;

        internal static bool Owns(string id) => id == ThemeEldritchData.BanquetEater ||
            id == ThemeEldritchData.HangingCount || id == ThemeEldritchData.FleshKnot;

        internal void Initialize(ThemeEnemyActor owner, ThemeBattleScene battlefield)
        {
            actor = owner; scene = battlefield; shared = actor.GetComponent<ThemeSharedBodyPart>();
            var root = new GameObject("CastleBossBody"); rig = root.transform; rig.SetParent(transform, false);
            // Equal canvases and common PPU retain each drawn pose's actual body height.
            width = ThemeAssets.Named(actor.UnitId + ".idle.0").rect.width / 32f;
            image = ThemeAssets.Part(rig, actor.UnitId + ".idle.0", width, Vector2.zero);
            previousHealth = actor.Stats.当前生命值;
            actor.ActionChanged += LocalChanged; BindSource(actor);
            if (actor.UnitId == ThemeEldritchData.HangingCount)
            {
                lift = HangingLift; listeningKnots = true;
                ThemeEnemyActor.Ready += KnotReady;
                foreach (var unit in ThemeEnemyActor.Active) KnotReady(unit);
            }
            RefreshSource();
        }

        private void BindSource(ThemeEnemyActor next)
        {
            if (source == next) return;
            if (source != null && source != actor) source.ActionChanged -= SourceChanged;
            source = next;
            if (source != actor) source.ActionChanged += SourceChanged;
            if (source.LastAction != null)
            { SourceChanged(source.LastAction); actionAge = source.ActionElapsed; }
        }

        private void RefreshSource()
        {
            if (actor.UnitId != ThemeEldritchData.BanquetEater || Time.unscaledTime < nextBinding) return;
            nextBinding = Time.unscaledTime + .15f;
            if (shared == null) shared = actor.GetComponent<ThemeSharedBodyPart>();
            if (shared != null && shared.Owner != null) BindSource(shared.Owner);
        }

        private void LocalChanged(ThemeActorMessage message)
        {
            if (message.Action == "death") { Death(); return; }
            if (source == actor) SourceChanged(message);
        }

        private void SourceChanged(ThemeActorMessage message)
        {
            if (ended) return;
            if (message.Action == "death") { Death(); return; }
            if (message.Action == "motion-pause" || message.Action == "motion-resume" || message.Action == "native-rage" ||
                message.Action.StartsWith("shared-", StringComparison.Ordinal) ||
                message.Action.StartsWith("shadow-", StringComparison.Ordinal) || message.Action == "count-supply") return;
            if (action != null && action.Unit == message.Unit && action.Sequence == message.Sequence) return;
            action = message; actionAge = 0;
            switch (message.Action)
            {
                case "hanging-fall":
                    suspension = Suspension.Fallen; modeAge = 0; modeDuration = message.Duration; fallStartLift = lift;
                    landed = false;
                    break;
                case "hanging-recover":
                    suspension = Suspension.Recovering; modeAge = 0; modeDuration = message.Duration; break;
                case "hanging-rethread":
                    suspension = Suspension.Rethreading; modeAge = 0; modeDuration = message.Duration; break;
            }
        }

        private void LateUpdate()
        {
            if (ended || actor == null || image == null) return;
            if (scene == null || scene != ThemeBattleScene.Current) { Destroy(gameObject); return; }
            RefreshSource();
            if (!actor.Alive || source == null || !source.Alive) { Death(); return; }
            if (!source.ActionPaused) actionAge += Time.deltaTime;
            // The fallen window and eight-second grounded recovery use game time in the controller.
            if (suspension != Suspension.Rethreading || !actor.ActionPaused) modeAge += Time.deltaTime;
            float health = actor.Stats.当前生命值;
            if (health < previousHealth) flashUntil = Time.time + .09f;
            previousHealth = health;

            string pose = "idle.0";
            Vector2 offset = Vector2.zero;
            Vector2 scale = Vector2.one;
            if (actor.UnitId == ThemeEldritchData.BanquetEater) Banquet(ref pose, ref offset, ref scale);
            else if (actor.UnitId == ThemeEldritchData.FleshKnot) Knot(ref pose, ref scale);
            else Hanging(ref pose, ref offset, ref scale);
            renderedPose = pose;
            ThemeAssets.SetPart(image, actor.UnitId + "." + pose, width, Vector2.zero);
            Vector2 point = transform.position;
            rig.localPosition = new Vector3(offset.x, offset.y,
                StratagemAssets.Z(point.y) - transform.position.z - .04f);
            rig.localScale = new Vector3(scale.x, scale.y, 1);
            image.flipX = actor.UnitId == ThemeEldritchData.HangingCount && actor.Facing.x < -.05f;
            image.color = Time.time < flashUntil ? new Color(1, .65f, .65f, 1) : Color.white;
            if (actor.UnitId == ThemeEldritchData.HangingCount) DrawTethers();
        }

        private void Banquet(ref string pose, ref Vector2 offset, ref Vector2 scale)
        {
            float breathe = Mathf.Sin(Time.time * 2.1f) * .012f;
            scale = new Vector2(1 + breathe, 1 - breathe * .4f);
            if (action == null) return;
            float duration = Mathf.Max(.01f, action.Duration), t = Mathf.Clamp01(actionAge / duration);
            bool activeMouth = ((Vector2)transform.position - new Vector2(action.X, action.Y)).sqrMagnitude < 1;
            switch (action.Action)
            {
                case "banquet-open":
                    if (t < .3f) pose = "idle.0";
                    else if (t < .65f) pose = "attack.0";
                    else if (t < .9f) pose = "attack.1";
                    else if (actionAge < duration + .35f) pose = "attack.0";
                    break;
                case "crack-warning":
                    pose = t < .25f ? "idle.0" : t < .8f || !activeMouth ? "attack.0" : "attack.1";
                    float tension = Smooth(t) * (activeMouth ? .045f : .02f);
                    scale = new Vector2(1 - tension * .3f, 1 + tension);
                    break;
                case "crack-bite":
                    if (actionAge < duration * .45f) pose = activeMouth ? "attack.1" : "attack.0";
                    else if (actionAge < duration + .4f) pose = "attack.0";
                    float snap = Mathf.Sin(Mathf.Clamp01(actionAge / duration) * Mathf.PI);
                    scale = new Vector2(1 + .035f * snap, 1 - .025f * snap);
                    if (activeMouth)
                        offset = (new Vector2(action.EndX, action.EndY) - new Vector2(action.X, action.Y)).normalized * (.06f * snap);
                    break;
            }
        }

        private void Knot(ref string pose, ref Vector2 scale)
        {
            float pulse = Mathf.Sin(Time.time * 3) * .025f;
            scale = new Vector2(1 + pulse, 1 - pulse);
            if (action == null) return;
            float t = Mathf.Clamp01(actionAge / Mathf.Max(.01f, action.Duration));
            if (action.Action == "rope-warning" && actionAge <= action.Duration)
            {
                pose = t < .35f ? "idle.0" : "attack.0";
                scale = new Vector2(1 - .05f * t, 1 + .06f * t);
            }
            else if (action.Action == "rope-sweep")
            {
                if (actionAge <= action.Duration) pose = t < .2f || t > .85f ? "attack.0" : "attack.1";
                else if (actionAge < action.Duration + .3f) pose = "attack.0";
            }
        }

        private void Hanging(ref string pose, ref Vector2 offset, ref Vector2 scale)
        {
            if (suspension == Suspension.Fallen)
            {
                float fall = Mathf.Clamp01(modeAge / .45f);
                lift = Mathf.Lerp(fallStartLift, 0, Smooth(fall));
                pose = fall < .25f ? "idle.0" : fall < .7f ? "attack.0" : "attack.1";
                if (!landed && fall >= 1) { landed = true; ThemeVisual.Impact(transform.position, 2.5f); }
            }
            else if (suspension == Suspension.Recovering)
            {
                lift = 0;
                pose = modeAge < .18f ? "attack.1" : modeAge < .42f ? "attack.0" : "idle.0";
                if (modeAge > .35f) Melee(ref pose, ref offset);
            }
            else if (suspension == Suspension.Rethreading)
            {
                lift = 0;
                float pull = Smooth(Mathf.Clamp01(modeAge / Mathf.Max(.01f, modeDuration)));
                scale = new Vector2(1 - .025f * pull, 1 + .035f * pull);
                if (modeAge >= modeDuration && LivingTethers() > 0) suspension = Suspension.Hanging;
            }
            else
            {
                lift = Mathf.MoveTowards(lift, HangingLift, Time.deltaTime * 2.6f);
                Melee(ref pose, ref offset);
                offset.x += Mathf.Sin(Time.time * 1.8f) * .035f;
            }
            offset.y += lift;
        }

        private void Melee(ref string pose, ref Vector2 offset)
        {
            if (action == null || action.Action != "attack") return;
            float duration = Mathf.Max(.01f, action.Duration);
            if (actionAge > duration + .3f) return;
            float advance = actionAge <= duration ? Smooth(Mathf.Clamp01(actionAge / duration)) :
                1 - Smooth(Mathf.Clamp01((actionAge - duration) / .3f));
            pose = advance > .25f ? "attack.0" : "idle.0";
            offset += actor.Facing * (.12f * advance);
        }

        private void KnotReady(ThemeEnemyActor unit)
        {
            if (ended || unit == null || !unit.Alive || unit.UnitId != ThemeEldritchData.FleshKnot || unit.gameObject.scene != actor.gameObject.scene) return;
            foreach (var tether in tethers) if (tether.Knot == unit) return;
            var image = ThemeAssets.Renderer(scene.transform, "castle.fx.cable.0", 1, Vector2.zero);
            float side = unit.transform.position.x < transform.position.x ? -1 : 1;
            Vector2 from = Attachment(side);
            Vector2 to = (Vector2)unit.transform.position + Vector2.up * .85f;
            SetRope(image, from, to, .16f);
            tethers.Add(new Tether { Knot = unit, Image = image, Start = from, End = to });
        }

        private int LivingTethers()
        {
            int count = 0;
            foreach (var tether in tethers) if (tether.Knot != null && tether.Knot.Alive) count++;
            return count;
        }

        private void DrawTethers()
        {
            for (int i = tethers.Count - 1; i >= 0; i--)
            {
                var tether = tethers[i];
                if (tether.Knot == null || !tether.Knot.Alive)
                {
                    ThemeCastleRopeSnap.Begin(tether.Image, tether.Start, tether.End);
                    tethers.RemoveAt(i); continue;
                }
                float side = tether.Knot.transform.position.x < transform.position.x ? -1 : 1;
                tether.Start = Attachment(side);
                tether.End = (Vector2)tether.Knot.transform.position + Vector2.up * .85f;
                tether.Image.enabled = suspension != Suspension.Fallen && suspension != Suspension.Recovering;
                SetRope(tether.Image, tether.Start, tether.End, .16f);
            }
        }

        private Vector2 Attachment(float side)
        {
            Vector2 mount = renderedPose == "attack.0" ? new Vector2(-.55f, 1.2f) : new Vector2(0, 1.85f);
            if (image != null && image.flipX) mount.x = -mount.x;
            return rig.TransformPoint(new Vector3(mount.x + side * .28f, mount.y, 0));
        }

        internal static void SetRope(SpriteRenderer line, Vector2 from, Vector2 to, float thickness)
        {
            Vector2 direction = to - from, center = (from + to) * .5f;
            var bounds = line.sprite.bounds;
            line.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            line.transform.localScale = new Vector3(Mathf.Max(.01f, direction.magnitude) / bounds.size.x, thickness / bounds.size.y, 1);
            var shift = line.transform.rotation * Vector3.Scale(bounds.center, line.transform.localScale);
            line.transform.position = new Vector3(center.x - shift.x, center.y - shift.y, StratagemAssets.Z(center.y) + .015f);
        }

        private void Death()
        {
            if (ended || image == null) return;
            ended = true;
            foreach (var tether in tethers) ThemeCastleRopeSnap.Begin(tether.Image, tether.Start, tether.End);
            tethers.Clear();
            rig.localPosition = new Vector3(0, 0, StratagemAssets.Z(transform.position.y) - transform.position.z - .04f);
            ThemeAssets.SetPart(image, actor.UnitId + ".death.0", width, Vector2.zero); image.color = Color.white;
            ThemeBossRetreat.Begin(image, scene, actor.UnitId + ".death.", 1, 1.2f, Vector2.zero, 1, width);
            scene.GetComponent<ThemeNativeEffects>()?.Impact(transform.position, actor.UnitId == ThemeEldritchData.FleshKnot ? .4f : .7f);
            image = null;
        }

        private static float Smooth(float t) => t * t * (3 - 2 * t);

        private void OnDestroy()
        {
            if (actor != null) actor.ActionChanged -= LocalChanged;
            if (source != null && source != actor) source.ActionChanged -= SourceChanged;
            if (listeningKnots) ThemeEnemyActor.Ready -= KnotReady;
            foreach (var tether in tethers) if (tether.Image != null) Destroy(tether.Image.gameObject);
        }
    }

    public sealed class ThemeCastleRopeSnap : MonoBehaviour
    {
        private SpriteRenderer image;
        private Vector2 start, end;
        private float started;
        private SceneLifetime scene;

        internal static void Begin(SpriteRenderer image, Vector2 start, Vector2 end)
        {
            if (image == null) return;
            image.enabled = true; image.sprite = ThemeAssets.Named("castle.fx.cable.1");
            var snap = image.gameObject.AddComponent<ThemeCastleRopeSnap>();
            snap.image = image; snap.start = start; snap.end = end; snap.started = Time.time; snap.scene = SceneLifecycle.Current;
        }

        private void Update()
        {
            float t = Mathf.Clamp01((Time.time - started) / .4f);
            if (!SceneLifecycle.IsCurrent(scene) || t >= 1) { Destroy(gameObject); return; }
            Vector2 tip = Vector2.Lerp(end, start, t * t);
            ThemeCastleBossVisual.SetRope(image, start, tip, .16f);
            image.color = new Color(1, 1, 1, 1 - t);
        }
    }
}
