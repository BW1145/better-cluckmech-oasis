using UnityEngine;

namespace CluckmechCoop
{
    // Presentation consumes the same action packet on host and guest; it never applies damage.
    public sealed class ThemeEnemyVisual : MonoBehaviour
    {
        private ThemeEnemyActor actor;
        private SpriteRenderer image;
        private Vector2 previous;
        private float flashUntil;
        private float previousHealth;
        private string spriteKey;
        private int deathFrames, walkFrames;
        private bool empty, hide;

        private void Awake() { actor = GetComponent<ThemeEnemyActor>(); }
        private void OnEnable() { if (actor != null) actor.ActionChanged += Changed; }
        private void Start()
        {
            if (ThemeBossPresentation.Owns(actor.UnitId))
            { hide = true; return; }
            image = ThemeAssets.Renderer(transform, actor.UnitId + ".idle.0", 1, Vector2.zero);
            image.transform.localScale = Vector3.one;
            if (ThemeEnemyRegistry.IsLeader(actor.UnitId)) image.transform.localScale *= 1.35f;
            previous = transform.position;
            previousHealth = actor.Stats == null ? 0 : actor.Stats.当前生命值;
            deathFrames = ThemeAssets.FrameCount(actor.UnitId + ".death.");
            walkFrames = ThemeAssets.FrameCount(actor.UnitId + ".walk.");
            if (actor.LastAction != null) Changed(actor.LastAction);
        }
        private void OnDisable() { if (actor != null) actor.ActionChanged -= Changed; }

        private void Changed(ThemeActorMessage message)
        {
            if (message.Action == "coffin-empty" || message.Action == "wine-empty") empty = true;
            ThemeVisual.Action(actor, message);
        }

        internal void HideDefault() { hide = true; if (image != null) image.enabled = false; }

        private void Update()
        {
            if (hide || image == null || actor == null) return;
            Vector2 point = transform.position;
            bool moving = (point - previous).sqrMagnitude > .00001f;
            previous = point;
            if (actor.Stats != null)
            {
                float health = actor.Stats.当前生命值;
                if (health < previousHealth) Hit();
                previousHealth = health;
            }
            string pose = Pose(moving);
            bool knight = actor.UnitId == ThemeCastleData.Knight || ThemeEnemyRegistry.IsLeader(actor.UnitId) || actor.UnitId == ThemeEldritchData.ShadowKnight;
            if (knight && actor.Facing.y > .25f) pose = "back." + (pose.StartsWith("death.") ? "death.0" : pose);
            string next = actor.UnitId + "." + pose;
            if (next != spriteKey) { image.sprite = ThemeAssets.Named(next); spriteKey = next; }
            image.flipX = actor.Facing.x < -.05f;
            var at = image.transform.position;
            at.z = StratagemAssets.Z(point.y) - .025f;
            image.transform.position = at;
            image.color = Time.time < flashUntil ? new Color(1, .65f, .65f, 1) : Color.white;
        }

        private string Pose(bool moving)
        {
            string action = actor.Action;
            if (actor.Dead || action == "death")
                return "death." + Mathf.Min(deathFrames - 1, Mathf.FloorToInt(actor.ActionElapsed * 6));
            bool during = actor.LastAction != null && actor.ActionElapsed <= actor.LastAction.Duration;
            if (actor.UnitId == ThemeCastleData.Count)
            {
                if (during && action == "charge-warning") return "charge.0";
                if (during && action == "charge") return "charge.1";
                if (during && action == "charge-recover") return "charge.2";
                if (during && (action == "vine-warning" || action == "vine")) return "vines.0";
                if (during && action == "pillar-stun") return "stun.0";
                if (during && action == "count-toast") return "phase.0";
            }
            if (empty)
            {
                if (during && (action == "attack" || action == "shot")) return "empty.3";
                return moving ? "empty." + (1 + Mathf.FloorToInt(Time.time * 7) % 2) : "empty.0";
            }
            if (actor.UnitId == ThemeCastleData.FlowerGhost && action == "unfold")
                return "unfold." + (actor.ActionProgress < .5f ? 0 : 1);
            if (during && (action == "shot" || action == "count-stab" || action == "coffin-release" || action == "land")) return "attack.1";
            if (during && (action == "attack" || action == "bow-warning" || action == "wine-pour" || action == "coffin-open")) return "attack.0";
            if (during && action == "dive") return "attack.0";
            if (actor.UnitId == ThemeCastleData.WineBat || actor.UnitId == ThemeCastleData.BossWineBat)
            {
                int flap = Mathf.FloorToInt(Time.time * 9) % 4;
                return flap == 0 ? "idle.0" : "walk." + (flap == 2 ? 1 : 0);
            }
            if (moving || actor.UnitId == ThemeCastleData.WineBat || actor.UnitId == ThemeCastleData.BossWineBat)
                return "walk." + (Mathf.FloorToInt(Time.time * 7) % walkFrames);
            return "idle.0";
        }

        internal void Hit() { flashUntil = Time.time + .09f; }
    }
}
