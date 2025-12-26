using UnityEngine;
using System.Collections.Generic;

// 이름을 MonsterPool로 변경하는 것을 추천합니다.
public class MonsterPool : MonoBehaviour
{
    public static MonsterPool Instance;

    // PoolInfo의 prefab 타입을 GameObject -> MonsterController로 변경
    [System.Serializable]
    public class PoolInfo
    {
        public string tag;
        public MonsterController prefab; // MonsterController 컴포넌트가 있는 프리팹만 등록 가능
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
        // 딕셔너리의 Queue 타입을 MonsterController로 변경
        poolDictionary = new Dictionary<string, Queue<MonsterController>>();

        foreach (PoolInfo pool in poolList)
        {
            Queue<MonsterController> objectPool = new Queue<MonsterController>();

            for (int i = 0; i < pool.size; i++)
            {
                // 프리팹(의 게임오브젝트)을 생성하고, MonsterController 컴포넌트를 가져옴
                MonsterController monster = Instantiate(pool.prefab);
                monster.gameObject.SetActive(false);
                // 씬이 지저분해지지 않도록 Pool 매니저의 자식으로 설정
                monster.transform.SetParent(this.transform);
                objectPool.Enqueue(monster);
            }
            poolDictionary.Add(pool.tag, objectPool);
        }
    }

    // 반환 타입을 MonsterController로 변경
    public MonsterController GetFromPool(string tag, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning("Pool with tag " + tag + " doesn't exist.");
            return null;
        }

        // 만약 풀이 비었다면, 동적으로 하나 더 생성 (안전장치)
        if (poolDictionary[tag].Count == 0)
        {
            PoolInfo info = poolList.Find(p => p.tag == tag);
            MonsterController monster = Instantiate(info.prefab);
            monster.transform.SetParent(this.transform);
            // 새로 생성한 몬스터는 바로 사용해야 하므로 큐에 넣지 않음
            monster.gameObject.SetActive(true);
            monster.transform.position = position;
            monster.transform.rotation = rotation;
            return monster;
        }

        // 풀에서 몬스터를 하나 꺼냄
        MonsterController monsterToSpawn = poolDictionary[tag].Dequeue();

        monsterToSpawn.gameObject.SetActive(true);
        monsterToSpawn.transform.position = position;
        monsterToSpawn.transform.rotation = rotation;

        return monsterToSpawn;
    }

    // 몬스터를 다시 풀에 반납하는 함수
    public void ReturnToPool(string tag, MonsterController monster)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning("Pool with tag " + tag + " doesn't exist.");
            Destroy(monster.gameObject); // 풀이 없으면 그냥 파괴
            return;
        }

        monster.gameObject.SetActive(false);
        poolDictionary[tag].Enqueue(monster);
    }

    public void StopSpawning()
    {
        //스폰을 더 하지 않도록 하는 코드
        //시간을 멈춰서 스폰을 막고 결과 창이 뜸
        
    }
}
