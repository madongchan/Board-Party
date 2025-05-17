using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PlayerVisualHandler 클래스 - 플레이어 캐릭터의 시각적 효과 관리
/// 플레이어 고유의 애니메이션과 시각적 피드백을 처리합니다.
/// BoardEvents 기반 이벤트 시스템을 사용하여 이벤트를 처리합니다.
/// </summary>
public class PlayerVisualHandler : BaseVisualHandler
{
    // 플레이어 고유 시각 효과 파라미터
    [Header("Player Visual Effects")]
    [SerializeField] private ParticleSystem victoryParticle;
    [SerializeField] private ParticleSystem coinParticle;
    
    /// <summary>
    /// 초기화
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();
        
        // 플레이어 고유 이벤트 리스너 등록
        RegisterPlayerEventListeners();
    }
    
    /// <summary>
    /// 컴포넌트 제거 시 이벤트 리스너 해제
    /// </summary>
    protected override void OnDestroy()
    {
        base.OnDestroy();
        
        // 플레이어 고유 이벤트 리스너 해제
        UnregisterPlayerEventListeners();
    }
    
    /// <summary>
    /// 플레이어 고유 이벤트 리스너 등록
    /// </summary>
    private void RegisterPlayerEventListeners()
    {
        // 코인 및 별 변경 이벤트 리스너 등록
        BoardEvents.OnCoinsChanged.AddListener(OnCoinsChangedEvent);
        BoardEvents.OnStarsChanged.AddListener(OnStarsChangedEvent);
    }
    
    /// <summary>
    /// 플레이어 고유 이벤트 리스너 해제
    /// </summary>
    private void UnregisterPlayerEventListeners()
    {
        // 코인 및 별 변경 이벤트 리스너 해제
        BoardEvents.OnCoinsChanged.RemoveListener(OnCoinsChangedEvent);
        BoardEvents.OnStarsChanged.RemoveListener(OnStarsChangedEvent);
    }
    
    /// <summary>
    /// 코인 변경 시 처리
    /// </summary>
    private void OnCoinsChangedEvent(BaseController controller, int amount)
    {
        if (controller != baseController) return;
        
        if (amount > 0 && coinParticle != null)
        {
            coinParticle.Play();
            //PlayCelebrateAnimation();
        }
        else if (amount < 0)
        {
            //PlaySadAnimation();
        }
    }
    
    /// <summary>
    /// 별 변경 시 처리
    /// </summary>
    private void OnStarsChangedEvent(BaseController controller, int amount)
    {
        if (controller != baseController) return;
        
        if (amount > 0 && victoryParticle != null)
        {
            victoryParticle.Play();
            //PlayCelebrateAnimation();
        }
    }
    
    /// <summary>
    /// 승리 이펙트 재생
    /// </summary>
    public void PlayVictoryEffect()
    {
        if (victoryParticle != null)
            victoryParticle.Play();
            
        //PlayCelebrateAnimation();
    }
}
