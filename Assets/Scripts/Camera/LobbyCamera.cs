using Fusion;
using Photon;
using UnityEngine;
using Unity.Cinemachine;

public class LobbyCamera :NetworkBehaviour
{
    public override void Spawned()
    {
        if (HasStateAuthority || HasInputAuthority) 
        {
            CinemachineCamera cam = FindFirstObjectByType<CinemachineCamera>();
            
            if (cam != null)
            {
                cam.Target.TrackingTarget = this.transform;
            }
        }
    }
}
