using UnityEngine;
using Fusion;

public class DungeonArrow : NetworkBehaviour
{
    public float arrowSpeed = 15f;
    public float lifeTime = 0.5f;
    
    [Networked] public float DamageValue { get; set; }
    [Networked] public int EnchantFire { get; set; }
    [Networked] public int EnchantIce { get; set; }
    [Networked] public int EnchantLightning { get; set; }
    [Networked] public int EnchantPoison { get; set; }
    
    [Networked] public TickTimer LifeTimer { get; set; }

    // 서버가 화살을 생성하기 직전(OnBeforeSpawned)에 데이터를 밀어넣는 함수
    public void InitNetworkData(float damage, int[] enchants)
    {
        DamageValue = damage;
        if (enchants != null && enchants.Length >= 4)
        {
            EnchantFire = enchants[0];
            EnchantIce = enchants[1];
            EnchantLightning = enchants[2];
            EnchantPoison = enchants[3];
        }
    }

    public override void Spawned()
    {
        // 화살 수명 타이머 시작 (오직 서버만 타이머를 관리해도 무방함)
        if (HasStateAuthority)
        {
            LifeTimer = TickTimer.CreateFromSeconds(Runner, lifeTime);
        }
    }

    public override void FixedUpdateNetwork()
    {
        // 1. 수명이 다 되면 서버가 이 화살을 네트워크에서 삭제함
        if (HasStateAuthority && LifeTimer.Expired(Runner))
        {
            Runner.Despawn(Object);
            return;
        }
        
        transform.Translate(Vector3.up * arrowSpeed * Runner.DeltaTime, Space.Self);
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!HasStateAuthority) return;

        if (other.CompareTag("Enemy")) 
        {
            MonsterController monster = other.GetComponent<MonsterController>();
            if (monster != null)
            {
                monster.TakeDamage(DamageValue);
                
                int[] appliedEnchants = new int[] { EnchantFire, EnchantIce, EnchantLightning, EnchantPoison };
                monster.TakeElement(appliedEnchants);

                Debug.Log($"[서버] 화살 적중! 데미지: {DamageValue}");
            }
            
            Runner.Despawn(Object);
        }
    }
}