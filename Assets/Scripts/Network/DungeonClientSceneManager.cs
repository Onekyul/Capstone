using System.Collections;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 보스던전 클라이언트 전용 씬 매니저.
/// 서버 빌드(BossTestScene=index 0)와 클라이언트 빌드(BossTestScene=index 1)의 씬 인덱스 불일치 대응.
///
/// 해결 전략: 보스씬 경로에 대한 GetSceneRef()가 서버가 보낸 sceneRef(0)를 반환하도록 재정의.
/// 이렇게 하면 FindSceneToTakeOver(sceneRef=0)가 이미 로드된 BossTestScene을 찾아 take-over하고,
/// InvokeSceneLoadDone도 sceneRef=0으로 호출되어 서버와 클라이언트의 NetworkObject 매핑이 일치.
/// </summary>
public class DungeonClientSceneManager : NetworkSceneManagerDefault
{
    // 서버가 보낸 BossTestScene의 SceneRef (서버 빌드 기준 index=0)
    // LoadSceneCoroutine에서 불일치를 감지했을 때 설정됨
    private SceneRef _bossSceneServerRef = SceneRef.None;

    /// <summary>
    /// 씬 경로 → SceneRef 변환 재정의.
    /// 보스씬 경로는 서버가 보낸 sceneRef를 반환하여 take-over 및 NetworkObject 매핑이 서버와 일치.
    /// </summary>
    public override SceneRef GetSceneRef(string sceneNameOrPath)
    {
        if (_bossSceneServerRef != SceneRef.None &&
            (sceneNameOrPath.Contains("BossTestScene") || sceneNameOrPath.Contains("BossTest")))
        {
            return _bossSceneServerRef;
        }
        return base.GetSceneRef(sceneNameOrPath);
    }

    protected override IEnumerator LoadSceneCoroutine(SceneRef sceneRef, NetworkLoadSceneParameters parameters)
    {
        // 보스씬이 이미 로드되어 있고 인덱스가 불일치하는 경우 감지
        if (IsSceneTakeOverEnabled && sceneRef.IsIndex && _bossSceneServerRef == SceneRef.None)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var s = SceneManager.GetSceneAt(i);
                if (!s.IsValid() || !s.isLoaded) continue;
                if (s.buildIndex == sceneRef.AsIndex) break; // 인덱스 일치 → 기본 처리로 충분

                if (s.name.Contains("Boss"))
                {
                    // 서버 sceneRef를 보스씬 경로의 기준으로 등록
                    _bossSceneServerRef = sceneRef;
                    Debug.Log($"[DungeonClientSceneManager] 빌드 인덱스 불일치 감지: server sceneRef={sceneRef.AsIndex}, client buildIndex={s.buildIndex} ({s.name}). 서버 ref로 take-over 진행.");
                    break;
                }
            }
        }

        // 기본 LoadSceneCoroutine 실행.
        // GetSceneRef가 재정의되어 있으므로 FindSceneToTakeOver(sceneRef)가 보스씬을 올바르게 찾음.
        yield return base.LoadSceneCoroutine(sceneRef, parameters);
    }
}
