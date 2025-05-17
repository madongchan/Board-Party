public class NPCVisualHandler : BaseVisualHandler
{
    private NPCController npcController;
    private NPCStats npcStats;

    protected override void Start()
    {
        // 기본 클래스의 Start 메서드 호출
        base.Start();
        
        // NPC 특화 컴포넌트 참조 획득
        npcController = GetComponentInParent<NPCController>();
        npcStats = GetComponentInParent<NPCStats>();
    }

    protected override void RegisterEventListeners()
    {
        // 기본 이벤트 리스너 등록
        base.RegisterEventListeners();
        
        // NPC 특화 이벤트 리스너 등록
        if (npcController != null)
        {
            // NPC 상태 변경 이벤트 리스너 등록
            npcController.OnStateChanged += OnNPCStateChanged;
        }
    }

    // NPC 상태 변경 이벤트 핸들러
    private void OnNPCStateChanged(NPCState newState)
    {
        // 상태에 따른 시각적 피드백
        if (newState is IdleState)
        {
            // 대기 상태 시각적 피드백
        }
        else if (newState is TurnStartState)
        {
            // 턴 시작 상태 시각적 피드백
            PlayCelebrateAnimation();
        }
        else if (newState is RollingState)
        {
            // 주사위 굴림 상태 시각적 피드백
        }
        else if (newState is MovingState)
        {
            // 이동 상태 시각적 피드백
            SetMovingAnimation(true);
        }
        else if (newState is JunctionDecisionState)
        {
            // 분기점 선택 상태 시각적 피드백
        }
        else if (newState is EventProcessingState)
        {
            // 이벤트 처리 상태 시각적 피드백
        }
        else if (newState is TurnEndState)
        {
            // 턴 종료 상태 시각적 피드백
            SetMovingAnimation(false);
        }
        else if (newState is StarPurchaseDecisionState)
        {
            // 별 구매 결정 상태 시각적 피드백
        }
    }

    // NPC 특화 메서드 오버라이드
    public override void PlayCelebrateAnimation()
    {
        // NPC 특화 축하 애니메이션
        base.PlayCelebrateAnimation();
    }

    // NPC 특화 메서드 추가
    public void PlayThinkingAnimation()
    {
        if (animator != null)
            animator.SetTrigger("Thinking");
    }
}