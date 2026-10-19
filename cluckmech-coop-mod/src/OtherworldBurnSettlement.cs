using System;
using System.Collections.Generic;

namespace CluckmechCoop
{
    internal static class OtherworldBurnSettlement
    {
        internal static float Consume(List<模块_状态属性.状态类> states)
        {
            float damage=0;
            for(int i=states.Count-1;i>=0;i--)
            {
                var state=states[i];
                if(state.状态类型!=攻击类型与状态数据库.状态枚举.灼烧)continue;
                damage+=state.加成属性.当前属性效果.x*Math.Max(0,state.剩余时间)*2;
                states.RemoveAt(i);
            }
            return damage;
        }
    }
}
