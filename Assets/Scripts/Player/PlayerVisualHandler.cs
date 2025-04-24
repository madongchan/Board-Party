public class PlayerVisualHandler : BaseVisualHandler
{
    private PlayerController playerController;
    private PlayerStats playerStats;

    protected override void Start()
    {
        // 기본 클래스의 Start 메서드 호출
        base.Start();
        
        // 플레이어 특화 컴포넌트 참조 획득
        playerController = GetComponentInParent<PlayerController>();
        playerStats = GetComponentInParent<PlayerStats>();
    }

    protected override void RegisterEventListeners()
    {
        // 기본 이벤트 리스너 등록
        base.RegisterEventListeners();
        
        // 플레이어 특화 이벤트 리스너 등록
        if (playerController != null)
        {
            // 플레이어 특화 이벤트가 있다면 여기에 추가
        }
    }

    // 플레이어 특화 메서드 추가
    public void PlayVictoryAnimation()
    {
        if (animator != null)
            animator.SetTrigger("Victory");
    }
}