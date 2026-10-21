// This standalone suite compiles both production implementations unchanged.
// Only the surrounding game types and coroutine/render host are substitutes.
using System;
using System.Collections.Generic;
using UnityEngine;

public class 世界类
{
    public class 伤害数据包
    {
        public GameObject 攻击者;
        public float 攻击伤害,暴击倍率;
        public bool 是否暴击;
    }
}
public partial class 模块_受伤
{
    public void 受到伤害(float value)=>throw new InvalidOperationException("Display tests must not enter gameplay damage");
}
namespace CluckmechCoop
{
    internal static class OtherworldFlatDamage { }
    internal struct OtherworldMessage { }
    internal sealed class KamuiSavedUnit
    {
        public string Key,PrefabName;
        public uint AssetId;
        public int Id;
        public Dictionary<string,Dictionary<string,float[]>> Components=new();
    }
    internal sealed class KamuiSavedRoom
    {
        public int Owner;
        public List<KamuiSavedUnit> Units=new();
    }
}
