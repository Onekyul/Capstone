using Fusion;
using UnityEngine;

public struct NetworkInputData : INetworkInput
{
    public Vector2 direction;       // WASD 이동 방향
    public Vector2 lookDirection;   // 마우스 바라보는 방향
    public NetworkButtons buttons;  // 기타 클릭/스킬 버튼들
}