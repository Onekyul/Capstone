using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StageManager : MonoBehaviour
{
    [Tooltip("실행할 스테이지의 설계도")]
    public StageSO currentStage;
    [Tooltip("스폰의 기준이 될 플레이어 또는 카메라")]
    public Transform spawnCenter;

    private float elapsedTime = 0f;
    private int currentPhaseIndex = 0;
    private Camera mainCamera;

    // 4개의 대각선 꼭짓점 방향 (스폰 위치용)
    private readonly Vector2[] diagonalSpawnPoints =
    {
        new Vector2(1, 1).normalized,   // 오른쪽 위
        new Vector2(1, -1).normalized,  // 오른쪽 아래
        new Vector2(-1, -1).normalized, // 왼쪽 아래
        new Vector2(-1, 1).normalized   // 왼쪽 위
    };

    void Start()
    {
        mainCamera = Camera.main;
        if (spawnCenter == null) spawnCenter = mainCamera.transform;
    }

    void Update()
    {
        if (currentStage == null || currentPhaseIndex >= currentStage.phases.Count)
        {
            // 모든 페이즈가 끝났으면 중단
            return;
        }

        elapsedTime += Time.deltaTime;

        // 현재 페이즈의 시작 시간이 되었는지 확인
        if (elapsedTime >= currentStage.phases[currentPhaseIndex].timestamp)
        {
            StartPhase(currentStage.phases[currentPhaseIndex]);
            currentPhaseIndex++;
        }
    }

    void StartPhase(Phase phase)
    {
        // 한 페이즈에 포함된 모든 몬스터 그룹을 동시 소환
        foreach (SpawnData group in phase.spawnGroups)
        {
            StartCoroutine(SpawnMonsterGroup(group));
        }
    }

    IEnumerator SpawnMonsterGroup(SpawnData data)
    {
        Vector2 spawnDirection = Vector2.zero;
        Vector3 baseSpawnPosition = Vector3.zero; // 그룹의 중심 위치

        // 패턴에 따라 그룹의 '중심' 위치를 먼저 계산
        switch (data.spawnPattern)
        {
            case SpawnPattern.Circle:
                // Circle 패턴은 개별 위치 계산이 중요하므로 이 로직은 for문 안에 둡니다.
                break;
            case SpawnPattern.RandomOutsideCamera:
                // Random 패턴도 매번 위치가 달라져야 하므로 for문 안에 둡니다.
                break;
            case SpawnPattern.DiagonalEntrance:
                spawnDirection = diagonalSpawnPoints[Random.Range(0, diagonalSpawnPoints.Length)];
                baseSpawnPosition = (Vector2)spawnCenter.position + spawnDirection * data.spawnRadius;
                break;
        }

        for (int i = 0; i < data.count; i++)
        {
            Vector3 finalSpawnPosition = Vector3.zero;

            // 패턴에 따라 최종 스폰 위치 결정
            switch (data.spawnPattern)
            {
                case SpawnPattern.Circle:
                    float angle = i * (360f / data.count);
                    Vector3 dir = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad), 0);
                    // 원형 패턴에도 약간의 무작위성을 더해줄 수 있습니다.
                    finalSpawnPosition = spawnCenter.position + dir * (data.spawnRadius + Random.Range(-data.groupSpread / 2, data.groupSpread / 2));
                    break;

                case SpawnPattern.RandomOutsideCamera:
                    Vector3 screenPoint = new Vector3();
                    int edge = Random.Range(0, 4);
                    if (edge == 0) screenPoint = new Vector3(Random.Range(0, Screen.width), -50f, 10f);
                    else if (edge == 1) screenPoint = new Vector3(Random.Range(0, Screen.width), Screen.height + 50f, 10f);
                    else if (edge == 2) screenPoint = new Vector3(-50f, Random.Range(0, Screen.height), 10f);
                    else screenPoint = new Vector3(Screen.width + 50f, Random.Range(0, Screen.height), 10f);
                    finalSpawnPosition = mainCamera.ScreenToWorldPoint(screenPoint);
                    finalSpawnPosition.z = 0;
                    break;

                case SpawnPattern.DiagonalEntrance:
                    // 중심 위치에 랜덤 오프셋을 더해 최종 위치 결정
                    Vector2 randomOffset = Random.insideUnitCircle * data.groupSpread;
                    finalSpawnPosition = baseSpawnPosition + (Vector3)randomOffset; // ★★★ 바로 이 부분! ★★★
                    break;
            }

            MonsterController monster = MonsterPool.Instance.GetFromPool(data.monsterTag, finalSpawnPosition, Quaternion.identity);

            if (monster is DiagonalMoveMonsterController diagonalMover)
            {
                diagonalMover.SetDirection(-spawnDirection);
            }

            yield return new WaitForSeconds(data.spawnInterval);
        }
    }
}