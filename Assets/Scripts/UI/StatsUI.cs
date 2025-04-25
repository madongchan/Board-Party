using DG.Tweening;
using TMPro;
using UnityEngine;

public class StatsUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI starCount;
    [SerializeField] private TextMeshProUGUI coinCount;
    
    private BaseStats currentStats;
    
    void Start()
    {
        // GameManager가 초기화된 후 현재 플레이어 통계 설정
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerChanged.AddListener(SetCurrentPlayer);
            SetCurrentPlayer(GameManager.Instance.GetCurrentPlayer());
        }
    }
    
    void OnDestroy()
    {
        // 이벤트 리스너 해제
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerChanged.RemoveListener(SetCurrentPlayer);
        }
        
        UnregisterEvents();
    }
    
    // 현재 플레이어/NPC 설정
    public void SetCurrentPlayer(BaseController controller)
    {
        if (controller == null) return;
        
        // 이전 이벤트 리스너 해제
        UnregisterEvents();
        
        // 새 플레이어/NPC의 통계 참조 획득
        currentStats = controller.GetComponent<BaseStats>();
        
        // 새 이벤트 리스너 등록
        if (currentStats != null)
        {
            currentStats.OnInitialize.AddListener(UpdateStats);
            currentStats.OnAnimation.AddListener(AnimateStats);
            
            // 초기 통계 업데이트
            UpdateStats();
        }
    }
    
    // 이벤트 리스너 해제
    private void UnregisterEvents()
    {
        if (currentStats != null)
        {
            currentStats.OnInitialize.RemoveListener(UpdateStats);
            currentStats.OnAnimation.RemoveListener(AnimateStats);
            currentStats = null;
        }
    }
    
    // 통계 업데이트
    private void UpdateStats()
    {
        if (currentStats != null)
        {
            starCount.text = currentStats.Stars.ToString();
            coinCount.text = currentStats.Coins.ToString();
        }
    }
    
    // 통계 애니메이션
    private void AnimateStats(int value)
    {
        coinCount.text = value.ToString();
        
        // 애니메이션 효과 추가 (선택사항)
        coinCount.transform.DOComplete();
        coinCount.transform.DOPunchScale(Vector3.one * 0.3f, 0.3f, 5, 0.5f);
    }
}
