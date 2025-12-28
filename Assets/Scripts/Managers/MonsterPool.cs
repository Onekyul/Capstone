using UnityEngine;
using System.Collections.Generic;

// 이름을 MonsterPool로 변경하는 것을 추천합니다.
public class MonsterPool : MonoBehaviour
{
    public static MonsterPool Instance;
    
    [System.Serializable]
    public class PoolInfo
    {
        public string tag;
        public MonsterController prefab; 
        public int size;
    }

    [SerializeField] private List<PoolInfo> poolList;
    private Dictionary<string, Queue<MonsterController>> poolDictionary;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        poolDictionary = new Dictionary<string, Queue<MonsterController>>();

        foreach (PoolInfo pool in poolList)
        {
            Queue<MonsterController> objectPool = new Queue<MonsterController>();

            for (int i = 0; i < pool.size; i++)
            {
                MonsterController monster = Instantiate(pool.prefab);
                monster.gameObject.SetActive(false);
                monster.transform.SetParent(this.transform);
                objectPool.Enqueue(monster);
            }
            poolDictionary.Add(pool.tag, objectPool);
        }
    }
    
    public MonsterController GetFromPool(string tag, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning("Pool with tag " + tag + " doesn't exist.");
            return null;
        }
        
        if (poolDictionary[tag].Count == 0)
        {
            PoolInfo info = poolList.Find(p => p.tag == tag);
            MonsterController monster = Instantiate(info.prefab);
            monster.transform.SetParent(this.transform);
            monster.gameObject.SetActive(true);
            monster.transform.position = position;
            monster.transform.rotation = rotation;
            return monster;
        }

      
        MonsterController monsterToSpawn = poolDictionary[tag].Dequeue();

        monsterToSpawn.gameObject.SetActive(true);
        monsterToSpawn.transform.position = position;
        monsterToSpawn.transform.rotation = rotation;

        return monsterToSpawn;
    }

   
    public void ReturnToPool(string tag, MonsterController monster)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning("Pool with tag " + tag + " doesn't exist.");
            Destroy(monster.gameObject); 
            return;
        }

        monster.gameObject.SetActive(false);
        poolDictionary[tag].Enqueue(monster);
    }

    public void StopSpawning()
    {
    }
}
