using UnityEngine;

public class NPCStats : BaseStats
{
    // NPC 특화 기능 추가
    // 예: 초기 코인/별 설정
    protected override void Start()
    {
        // 초기 설정
        coins = Random.Range(10, 20);
        base.Start();
    }
}
