using UnityEngine;

/// <summary>
/// BaseStats 클래스 - 플레이어와 NPC의 기본 스탯 관리
/// 코인, 별 등의 스탯을 관리하고 씬 이동 시에도 데이터가 유지되도록 합니다.
/// </summary>
public abstract class BaseStats : MonoBehaviour
{
    // 스탯 데이터
    [SerializeField] protected int coins;
    public int Coins => coins;

    [SerializeField] protected int stars;
    public int Stars => stars;

    public int coinsBeforeChange;

    // 컴포넌트 참조
    protected BaseController baseController;

    // 데이터 저장용 키
    protected string coinsKey;
    protected string starsKey;

    /// <summary>
    /// BaseStats 초기화
    /// </summary>
    public virtual void Initialize()
    {
        // 컴포넌트 참조 획득
        baseController = GetComponent<BaseController>();

        // 데이터 저장용 키 설정 (각 플레이어/NPC마다 고유한 키 사용)
        string uniqueId = baseController?.GetUniqueId() ?? gameObject.name;
        coinsKey = $"Coins_{uniqueId}";
        starsKey = $"Stars_{uniqueId}";

        // 저장된 데이터 로드
        LoadStats();

        // 초기화 이벤트 발생
        if (baseController != null)
        {
            BoardEvents.OnStatsInitialized.Invoke(baseController);
        }

        coinsBeforeChange = coins;

        // 이벤트 등록
        RegisterEventListeners();
    }

    /// <summary>
    /// 컴포넌트 제거 시 이벤트 해제
    /// </summary>
    protected virtual void OnDestroy()
    {
        // 이벤트 해제
        UnregisterEventListeners();

        // 데이터 저장
        SaveStats();
    }

    /// <summary>
    /// 이벤트 리스너 등록
    /// </summary>
    protected virtual void RegisterEventListeners()
    {
        // BoardEvents에 이벤트 등록
        BoardEvents.OnCoinsChanged.AddListener(OnCoinsChangedEvent);
        BoardEvents.OnStarsChanged.AddListener(OnStarsChangedEvent);
    }

    /// <summary>
    /// 이벤트 리스너 해제
    /// </summary>
    protected virtual void UnregisterEventListeners()
    {
        // BoardEvents에서 이벤트 해제
        BoardEvents.OnCoinsChanged.RemoveListener(OnCoinsChangedEvent);
        BoardEvents.OnStarsChanged.RemoveListener(OnStarsChangedEvent);
    }

    /// <summary>
    /// 코인 변경 이벤트 처리
    /// </summary>
    protected virtual void OnCoinsChangedEvent(BaseController controller, int amount)
    {
        if (controller == baseController)
        {
            AddCoins(amount);
        }
    }

    /// <summary>
    /// 별 변경 이벤트 처리
    /// </summary>
    protected virtual void OnStarsChangedEvent(BaseController controller, int amount)
    {
        if (controller == baseController)
        {
            AddStars(amount);
        }
    }

    /// <summary>
    /// 코인 추가
    /// </summary>
    public virtual void AddCoins(int amount)
    {
        coins += amount;

        // BoardEvents를 통해 코인 변경 이벤트 발생
        if (baseController != null)
        {
            BoardEvents.OnCoinsChanged.Invoke(baseController, amount);
        }

        UpdateStats();
        SaveStats(); // 데이터 저장
    }

    /// <summary>
    /// 별 추가
    /// </summary>
    public virtual void AddStars(int amount)
    {
        stars += amount;

        // BoardEvents를 통해 별 변경 이벤트 발생
        if (baseController != null)
        {
            BoardEvents.OnStarsChanged.Invoke(baseController, amount);
        }

        UpdateStats();
        SaveStats(); // 데이터 저장
    }

    /// <summary>
    /// 코인 애니메이션
    /// </summary>
    public virtual void CoinAnimation(int value)
    {
        coinsBeforeChange += value;

        // BoardEvents를 통해 코인 애니메이션 이벤트 발생
        if (baseController != null)
        {
            BoardEvents.OnCoinAnimation.Invoke(baseController, coinsBeforeChange);
        }
    }

    /// <summary>
    /// 스탯 업데이트
    /// </summary>
    public virtual void UpdateStats()
    {
        // BoardEvents를 통해 초기화 이벤트 발생
        if (baseController != null)
        {
            BoardEvents.OnStatsInitialized.Invoke(baseController);
        }

        coinsBeforeChange = coins;
    }

    /// <summary>
    /// 스탯 저장
    /// </summary>
    public virtual void SaveStats()
    {
        PlayerPrefs.SetInt(coinsKey, coins);
        PlayerPrefs.SetInt(starsKey, stars);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 스탯 로드
    /// </summary>
    public virtual void LoadStats()
    {
        if (PlayerPrefs.HasKey(coinsKey))
            coins = PlayerPrefs.GetInt(coinsKey);

        if (PlayerPrefs.HasKey(starsKey))
            stars = PlayerPrefs.GetInt(starsKey);
    }

    /// <summary>
    /// 스탯 리셋 (디버그용)
    /// </summary>
    public virtual void ResetStats()
    {
        coins = 0;
        stars = 0;
        UpdateStats();
        SaveStats();
    }
}
