using System.Collections;
using System.Linq;
using UnityEngine;

namespace CluckmechCoop
{
    public sealed class ThemeCastleChandelier : ThemeInteractive
    {
        private CastleChandelierDefinition definition;
        private ThemeBattleScene scene;
        private SpriteRenderer image;
        private 模块_基本属性 activator;
        private int sequence, receivedSequence, remaining;

        internal static void Install(ThemeBattleScene scene)
        {
            foreach (var definition in scene.Eldritch ? ThemeEldritchData.TableChandeliers : ThemeCastleData.Chandeliers)
            {
                var root = new GameObject("ThemeChandelier_" + definition.Id);
                root.transform.SetParent(scene.transform, false); root.transform.position = scene.World(definition.Winch);
                var chandelier = root.AddComponent<ThemeCastleChandelier>();
                chandelier.scene = scene; chandelier.definition = definition; chandelier.remaining = definition.Uses;
                chandelier.Bind(definition.Id, definition.InteractionRadius, "E  放下吊灯");
                chandelier.image = ThemeAssets.Renderer(scene.transform, "castle.chandelier", 3, scene.World(definition.Impact) + Vector2.up * 4.5f);
                ThemeAssets.Renderer(root.transform, "castle.lamp", .8f, Vector2.zero);
            }
        }

        internal override void TryUse(模块_基本属性 player)
        {
            if (Plugin.Role == 2 || !CanUse(player) || remaining <= 0) return;
            activator = player; remaining--;
            Broadcast(new ThemeMechanismMessage { Sequence = ++sequence, Value = remaining,
                Source = NetworkObjects.Id(player.gameObject), Seconds = definition.WarningSeconds });
        }

        internal override void Apply(ThemeMechanismMessage update)
        {
            if (update.Sequence <= receivedSequence) return;
            receivedSequence = update.Sequence; remaining = update.Value; Available = false;
            StartCoroutine(Fall(update.Seconds));
        }

        private IEnumerator Fall(float seconds)
        {
            string hold = "chandelier:" + Id;
            if (Plugin.Role != 2) ThemeBattleWaves.Hold(hold);
            Vector2 point = scene.World(definition.Impact), start = point + Vector2.up * 4.5f;
            ThemeVisual.Point(point, definition.Radius, seconds);
            float elapsed = 0;
            while (elapsed < seconds)
            {
                elapsed += Time.deltaTime; float fraction = Mathf.Clamp01(elapsed / Mathf.Max(.01f, seconds));
                var position = Vector2.Lerp(start, point, fraction * fraction);
                image.transform.position = new Vector3(position.x, position.y, StratagemAssets.Z(point.y) - .1f);
                image.transform.rotation = Quaternion.Euler(0, 0, Mathf.Sin(fraction * Mathf.PI * 3) * 5 * fraction);
                yield return null;
            }
            image.transform.position = new Vector3(point.x, point.y, StratagemAssets.Z(point.y) + .15f);
            image.transform.rotation = Quaternion.Euler(0, 0, 12);
            image.transform.localScale = new Vector3(image.transform.localScale.x, image.transform.localScale.y * .45f, 1);
            image.color = new Color(.7f, .65f, .6f, 1);
            ThemeVisual.Impact(point, 4);
            if (Plugin.Role != 2)
            {
                var owner = activator == null ? null : activator.gameObject;
                using (ThemeSharedDamageScope.Begin())
                foreach (var unit in ThemeBattleWaves.Current.Living.ToArray())
                {
                    if (unit == null || !unit.TryGetComponent<模块_基本属性>(out var stats) || stats.当前生命值 <= 0 ||
                        stats.当前是否无敌 || ThemeCombat.ShapeDistance(stats, point) > definition.Radius * definition.Radius) continue;
                    unit.GetComponent<模块_受伤>()?.受到伤害(new 世界类.伤害数据包 {
                        攻击者 = owner, 攻击目标 = unit.transform, 攻击坐标 = point,
                        所属阵营 = 阵营数据库.阵营.友方, 攻击伤害 = definition.FinalDamage,
                        是否产生仇恨 = owner != null,
                        造成状态 = new System.Collections.Generic.List<模块_状态属性.状态类> {
                            OtherworldActions.State(owner, 攻击类型与状态数据库.状态枚举.眩晕, definition.StunSeconds, Vector4.zero)
                        }
                    });
                }
                if (scene.Eldritch) ThemeCastleEldritch.ChandelierDropped(Id);
                ThemeBattleWaves.Release(hold);
            }
        }

        protected override void OnDestroy()
        {
            if (Plugin.Role != 2 && Id != null) ThemeBattleWaves.Release("chandelier:" + Id);
            base.OnDestroy();
        }
    }
}
