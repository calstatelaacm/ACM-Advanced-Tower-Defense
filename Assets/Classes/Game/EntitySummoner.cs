using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntitySummoner : MonoBehaviour
{

    public static Dictionary<int, GameObject> EnemyPrefabs;
    public static Dictionary<int, Queue<Enemy>> EnemyObjectPools;
    public static List<Enemy> EnemiesInGame;
    public static List<Transform> EnemiesInGameTransform;

    public static bool initialized;

    // Start is called before the first frame update
    public static void Initialize()
    {
        if(!initialized){
            EnemyPrefabs = new Dictionary<int, GameObject>();
            EnemyObjectPools = new Dictionary<int, Queue<Enemy>>();
            EnemiesInGame = new List<Enemy>();
            EnemiesInGameTransform = new List<Transform>();

            //"C:\asdasd\asdasd\asdasd\Resources\"
            EnemySummonData[] Enemies = Resources.LoadAll<EnemySummonData>("Enemies");
            
            foreach(EnemySummonData enemy in Enemies){
                EnemyPrefabs.Add(enemy.EnemyID, enemy.EnemyPrefab);
                EnemyObjectPools.Add(enemy.EnemyID, new Queue<Enemy>());
            }
        }else{
            Debug.Log("ENTITYSUMMONER: THIS CLASS IS ALREADY INITIALIZED");
        }
    }

    public static Enemy SummonEnemy(int EnemyID){
        Enemy SummonedEnemy;

        if(EnemyPrefabs.ContainsKey(EnemyID)){
            Queue<Enemy> ReferencedQueue = EnemyObjectPools[EnemyID];

            if(ReferencedQueue.Count > 0){
                //Dequeue Enemy and intiialize
                SummonedEnemy = ReferencedQueue.Dequeue();
                SummonedEnemy.Initialize();

                SummonedEnemy.gameObject.SetActive(false);
            }else{
                //Instantiate new instance of enemy and initialize
                GameObject NewEnemy = Instantiate(EnemyPrefabs[EnemyID], GameLoopManager.NodePositions[1], Quaternion.identity);
                SummonedEnemy = NewEnemy.GetComponent<Enemy>();
                SummonedEnemy.Initialize();
            }
        }else{
            Debug.Log($"ENTITYSUMMONER: ENEMY WITH ID OF {EnemyID} DOES NOT EXIST!");
            return null;
        }
        EnemiesInGameTransform.Add(SummonedEnemy.transform);
        EnemiesInGame.Add(SummonedEnemy);
        SummonedEnemy.ID = EnemyID;
        return SummonedEnemy;
    }

    public static void RemoveEnemy(Enemy EnemyToRemove){
        EnemyObjectPools[EnemyToRemove.ID].Enqueue(EnemyToRemove);
        EnemyToRemove.gameObject.SetActive(false);
        EnemiesInGameTransform.Remove(EnemyToRemove.transform);
        EnemiesInGame.Remove(EnemyToRemove);
    }
}
