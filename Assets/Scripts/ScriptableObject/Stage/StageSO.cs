using UnityEngine;
using System.Collections.Generic;

// 1. 어떤 패턴으로 소환할지 선택하는 옵션 (Enum)
public enum SpawnPattern
{
    Circle,             // 원형으로 둘러싸기
    RandomOutsideCamera,// 화면 밖 랜덤
    DiagonalEntrance    // 대각선에서 진입
}

// 2. 몬스터 한 그룹의 소환 정보를 담는 클래스
[System.Serializable]
public class SpawnData
{
    [Tooltip("어떤 몬스터를 소환할지 MonsterPool에 등록된 태그")]
    public string monsterTag;
    [Tooltip("몇 마리를 소환할지")]
    public int count;
    [Tooltip("어떤 패턴으로 소환할지")]
    public SpawnPattern spawnPattern;
    [Tooltip("원형/대각선 패턴에서 중심으로부터의 거리")]
    public float spawnRadius = 12f;
    [Tooltip("그룹 내 몬스터가 한 마리씩 나올 때의 시간 간격")]
    public float spawnInterval = 0.2f;
    [Tooltip("그룹 내 몬스터들이 생성될 때의 흩어짐 정도(반경)")]
    public float groupSpread = 2.0f;
}

// 3. 특정 시간에 실행될 페이즈 정보를 담는 클래스
[System.Serializable]
public class Phase
{
    [Tooltip("스테이지 시작 후 이 페이즈가 시작될 시간(초)")]
    public float timestamp;
    [Tooltip("이 페이즈에서 소환될 몬스터 그룹 목록")]
    public List<SpawnData> spawnGroups;
}

// 4. 하나의 스테이지 전체 정보를 담는 스크립터블 오브젝트
[CreateAssetMenu(fileName = "New Stage", menuName = "Stage/Stage Data")]
public class StageSO : ScriptableObject
{
    public List<Phase> phases;
}