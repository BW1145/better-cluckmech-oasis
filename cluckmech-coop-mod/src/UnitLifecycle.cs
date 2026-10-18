using UnityEngine;

namespace CluckmechCoop
{
    internal static class UnitLifecycle
    {
        internal static void ClearScene()
        {
            foreach (var unit in OtherworldWorld.Units.Values)
                if (unit != null) unit.Actions.Invalidate();
        }

        internal static void ChangingSpace(OtherworldUnit unit)
        {
            unit.Actions.Invalidate();
            // Area membership belongs to the old map, even before trigger exits run.
            StratagemFields.Remove(unit.Health);
        }

        internal static void Removed(模块_基本属性 health)
        {
            if (ReferenceEquals(health,null)) return;
            var unit=health.GetComponent<OtherworldUnit>();
            if (unit!=null)
            {
                unit.Actions.Invalidate();
                OtherworldWorld.Remove(unit);
                KamuiVisibility.Removed(unit);
            }
            // Native OnDisable also covers units while Otherworld is disabled.
            StratagemWorld.Remove(health);
        }

        internal static void Destroyed(GameObject obj)
        {
            Removed(obj.GetComponent<模块_基本属性>());
            KamuiVisibility.Forget(obj);
        }
    }
}
