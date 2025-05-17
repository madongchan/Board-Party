using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// NPCVisualHandler 클래스 - NPC 캐릭터의 시각적 효과 관리
/// NPC 고유의 애니메이션과 시각적 피드백을 처리합니다.
/// BoardEvents 기반 이벤트 시스템을 사용하여 이벤트를 처리합니다.
/// </summary>
public class NPCVisualHandler : BaseVisualHandler
{
    // NPC 고유 시각 효과 파라미터
    [Header("NPC Visual Effects")]
    [SerializeField] private ParticleSystem decisionParticle;
    [SerializeField] private GameObject thoughtBubble;
    
    // 생각 버블 표시 시간
    [SerializeField] private float thoughtBubbleDisplayTime = 1.5f;
    
    /// <summary>
    /// 초기화
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();
        
        // NPC 고유 이벤트 리스너 등록
        RegisterNPCEventListeners();
        
        // 생각 버블 초기 상태 설정
        if (thoughtBubble != null)
            thoughtBubble.SetActive(false);
    }
    
    /// <summary>
    /// 컴포넌트 제거 시 이벤트 리스너 해제
    /// </summary>
    protected override void OnDestroy()
    {
        base.OnDestroy();
        
        // NPC 고유 이벤트 리스너 해제
        UnregisterNPCEventListeners();
    }
    
    /// <summary>
    /// NPC 고유 이벤트 리스너 등록
    /// </summary>
    private void RegisterNPCEventListeners()
    {
        // 코인 및 별 변경 이벤트 리스너 등록
        BoardEvents.OnCoinsChanged.AddListener(OnCoinsChangedEvent);
        BoardEvents.OnStarsChanged.AddListener(OnStarsChangedEvent);
        
        // 별 구매 결정 이벤트 리스너 등록
        BoardEvents.OnStarPurchaseDecision.AddListener(OnStarPurchaseDecision);
    }
    
    /// <summary>
    /// NPC 고유 이벤트 리스너 해제
    /// </summary>
    private void UnregisterNPCEventListeners()
    {
        // 코인 및 별 변경 이벤트 리스너 해제
        BoardEvents.OnCoinsChanged.RemoveListener(OnCoinsChangedEvent);
        BoardEvents.OnStarsChanged.RemoveListener(OnStarsChangedEvent);
        
        // 별 구매 결정 이벤트 리스너 해제
        BoardEvents.OnStarPurchaseDecision.RemoveListener(OnStarPurchaseDecision);
    }
    
    /// <summary>
    /// 코인 변경 시 처리
    /// </summary>
    private void OnCoinsChangedEvent(BaseController controller, int amount)
    {
        if (controller != baseController) return;
        
        if (amount > 0)
        {
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
        
        if (amount > 0 && decisionParticle != null)
        {
            decisionParticle.Play();
            //PlayCelebrateAnimation();
        }
    }
    
    /// <summary>
    /// 별 구매 결정 시 처리
    /// </summary>
    private void OnStarPurchaseDecision(BaseController controller, bool decision)
    {
        if (controller != baseController) return;
        
        // 결정에 따른 시각적 효과
        if (decision)
        {
            if (decisionParticle != null)
                decisionParticle.Play();
                
            //PlayCelebrateAnimation();
        }
        else
        {
            //PlaySadAnimation();
        }
    }
    
    /// <summary>
    /// 생각 버블 표시
    /// </summary>
    public void ShowThoughtBubble()
    {
        if (thoughtBubble != null)
        {
            thoughtBubble.SetActive(true);
            StartCoroutine(HideThoughtBubbleAfterDelay());
        }
    }
    
    /// <summary>
    /// 지연 후 생각 버블 숨기기
    /// </summary>
    private IEnumerator HideThoughtBubbleAfterDelay()
    {
        yield return new WaitForSeconds(thoughtBubbleDisplayTime);
        
        if (thoughtBubble != null)
            thoughtBubble.SetActive(false);
    }
    
    /// <summary>
    /// 결정 이펙트 재생
    /// </summary>
    public void PlayDecisionEffect()
    {
        if (decisionParticle != null)
            decisionParticle.Play();
            
        ShowThoughtBubble();
    }
}
