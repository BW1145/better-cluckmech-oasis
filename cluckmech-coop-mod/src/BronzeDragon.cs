using System.Collections;
using UnityEngine;
using Object=UnityEngine.Object;

namespace CluckmechCoop
{
    internal sealed class BronzeDragon : MonoBehaviour
    {
        internal BronzeDragonState State = new BronzeDragonState();
        internal BronzeDragonState Resume;
        private 模块_基本属性 stats;
        private GameObject coin;
        private bool ready;
        private IEnumerator Start()
        {
            stats=GetComponent<模块_基本属性>();
            yield return null;yield return null;
            while(OtherworldWorld.Unit(gameObject)?.Restoring==true)yield return null;
            if(Plugin.Role==2||Plugin.Spawner==null)yield break;
            coin=Plugin.Spawner.GetComponent<效果物体引用器>().预制体.掉落_货币小;
            if(Resume!=null)State=Resume;
            else State.Remaining=Has(25102)?6:10;
            if(!State.Started)
            {
                State.Started=true;
                if(Has(25104))Produce(3);
            }
            ready=true;
        }
        private bool Has(int id)=>stats.强化词条列表.Contains(id);
        private void Update()
        {
            if(!ready||!Plugin.InCombat||stats.当前生命值<=0)return;
            int count=State.Advance(Time.deltaTime,Has(25101),Has(25102),Has(25103));
            if(count>0)Produce(count);
        }
        private void Produce(int count)
        {
            SpawnCoins(coin,transform.position,count);
            if(Plugin.Role==1&&OtherworldWorld.Space(gameObject)==0)
                Plugin.Network.Broadcast(new SessionMessage{Type="bronze-coins",Token=Plugin.Combat.Token,X=transform.position.x,Y=transform.position.y,Stage=count});
        }
        internal static void Receive(SessionMessage message)
        {
            if(!ModuleSettings.Extra.Bronze||!Plugin.InCombat||message.Token!=Plugin.Combat?.Token)return;
            SpawnCoins(Plugin.Spawner.GetComponent<效果物体引用器>().预制体.掉落_货币小,new Vector2(message.X,message.Y),message.Stage);
        }
        private static void SpawnCoins(GameObject prefab,Vector2 point,int count)
        {
            for(int i=0;i<count;i++)
            {
                float angle=i*2.399963f;
                Object.Instantiate(prefab,point+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*.35f,Quaternion.identity);
            }
        }
    }
}
