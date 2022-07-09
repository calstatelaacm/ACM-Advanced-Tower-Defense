using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;

public class GameLoopManager : MonoBehaviour
{
    public static List<TowerBehavior> TowersInGame;

    public static Vector3[] NodePositions;
    public static float[] NodeDistances;
    private static Queue<Enemy> EnemiesToRemove;
    private static Queue<int> EnemyIDsToSummon;

    public Transform NodeParent;
    public bool End;

    // Start is called before the first frame update
    void Start()
    {
        TowersInGame = new List<TowerBehavior>();
        EnemyIDsToSummon = new  Queue<int>();
        EnemiesToRemove = new Queue<Enemy>();
        EntitySummoner.Initialize();

        NodePositions = new Vector3[NodeParent.transform.childCount];
        for(int i = 0; i < NodePositions.Length; i++){
            NodePositions[i] = NodeParent.GetChild(i).position;
        }

        NodeDistances = new float[NodePositions.Length -1];
        for(int i = 0; i < NodeDistances.Length; i++){
            NodeDistances[i] = Vector3.Distance(NodePositions[i], NodePositions[i+1]);
        }

        StartCoroutine(GameLoop());
        InvokeRepeating("SummonTest", 0f, 1f);
    }

    void SummonTest(){
        EnqueueEnemyIDToSummon(1);
    }

    IEnumerator GameLoop(){
        while(!End){
            //Spawn Enemies
            if(EnemyIDsToSummon.Count > 0){
                for(int i = 0; i < EnemyIDsToSummon.Count; i++){
                    EntitySummoner.SummonEnemy(EnemyIDsToSummon.Dequeue());
                }
            }

            //Spawn towers

            //move enemies

            NativeArray<Vector3> NodesToUse = new NativeArray<Vector3>(NodePositions, Allocator.TempJob);
            NativeArray<float> EnemySpeeds = new NativeArray<float>(EntitySummoner.EnemiesInGame.Count, Allocator.TempJob);
            NativeArray<int> NodeIndices = new NativeArray<int>(EntitySummoner.EnemiesInGame.Count, Allocator.TempJob);
            TransformAccessArray EnemyAccess= new TransformAccessArray(EntitySummoner.EnemiesInGameTransform.ToArray(), 2);

            for(int i = 0; i < EntitySummoner.EnemiesInGame.Count; i++){
                EnemySpeeds[i] = EntitySummoner.EnemiesInGame[i].Speed;
                NodeIndices[i] = EntitySummoner.EnemiesInGame[i].NodeIndex;
            }

            MoveEnemiesJob MoveJob = new MoveEnemiesJob{
                NodePositions = NodesToUse,
                EnemySpeed = EnemySpeeds,
                NodeIndex = NodeIndices,
                deltaTime = Time.deltaTime
            };

            JobHandle MoveJobHandle = MoveJob.Schedule(EnemyAccess);
            MoveJobHandle.Complete();

            for(int i = 0; i < EntitySummoner.EnemiesInGame.Count; i++){
                EntitySummoner.EnemiesInGame[i].NodeIndex = NodeIndices[i];
                
                if(EntitySummoner.EnemiesInGame[i].NodeIndex == NodePositions.Length){
                    EnqueEnemyToRemove(EntitySummoner.EnemiesInGame[i]);
                }
            }

            EnemySpeeds.Dispose();
            NodeIndices.Dispose();
            EnemyAccess.Dispose();
            NodesToUse.Dispose();

            //tick towers

            foreach(TowerBehavior tower in TowersInGame){
                tower.Target = TowerTargeting.GetTarget(tower, TowerTargeting.TargetType.Last);
                tower.Tick();
            }

            //apply effects

            //damage enemies

            //remove enemies
            
            if(EnemiesToRemove.Count > 0){
                for(int i = 0; i < EnemiesToRemove.Count; i++){
                    EntitySummoner.RemoveEnemy(EnemiesToRemove.Dequeue());
                }
            }

            //remove towers

            yield return null;
        }
    }

    public static void EnqueueEnemyIDToSummon(int ID){
        EnemyIDsToSummon.Enqueue(ID);
    }

    public static void EnqueEnemyToRemove(Enemy EnemyToRemove){
        EnemiesToRemove.Enqueue(EnemyToRemove);
    }

}

public struct MoveEnemiesJob : IJobParallelForTransform{

    [NativeDisableParallelForRestriction]
    public NativeArray<Vector3> NodePositions;

    [NativeDisableParallelForRestriction]
    public NativeArray<float> EnemySpeed;

    [NativeDisableParallelForRestriction]
    public NativeArray<int> NodeIndex;

    public float deltaTime;

    public void Execute(int index, TransformAccess transform){
        if(NodeIndex[index] < NodePositions.Length){
            Vector3 PositionToMoveTo = NodePositions[NodeIndex[index]];
            transform.position = Vector3.MoveTowards(transform.position, PositionToMoveTo, EnemySpeed[index] * deltaTime);

            if(transform.position == PositionToMoveTo){
                NodeIndex[index]++;
            }
        }
        
    }
}
