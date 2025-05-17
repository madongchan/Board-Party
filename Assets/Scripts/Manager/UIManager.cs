using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using UnityEngine.EventSystems;

/// <summary>
/// UIManager 클래스 - 모든 UI 요소를 중앙에서 관리
/// BoardEvents 기반 이벤트 시스템을 사용하여 이벤트를 처리합니다.
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("Canvas References")]
    [SerializeField] private Canvas mainCanvas;

    [Header("Roll UI")]
    [SerializeField] private GameObject rollUIPanel;
    [SerializeField] private CanvasGroup rollCanvasGroup;
    [SerializeField] private TextMeshProUGUI rollValueText;
    [SerializeField] private AnimationCurve rollTextScaleEase;

    [Header("Star Purchase UI")]
    [SerializeField] private GameObject starPurchaseUIPanel;
    [SerializeField] private CanvasGroup starPurchaseCanvasGroup;
    [SerializeField] public Button starConfirmButton;
    [SerializeField] public Button starCancelButton;
    [SerializeField] private TextMeshProUGUI starPriceText;

    [Header("Stats UI")]
    [SerializeField] private GameObject statsUIPanel;
    [SerializeField] private TextMeshProUGUI coinsText;
    [SerializeField] private TextMeshProUGUI starsText;
    [SerializeField] private Image coinIcon;
    [SerializeField] private Image starIcon;

    [Header("Player UI")]
    [SerializeField] private GameObject playerUIPanel;
    [SerializeField] private TextMeshProUGUI currentPlayerNameText;
    [SerializeField] private Image currentPlayerIcon;
    [SerializeField] private Sprite[] playerIcons;

    [Header("Junction UI")]
    [SerializeField] private GameObject junctionUIPanel;
    [SerializeField] private TextMeshProUGUI junctionInstructionText;
    
    [Header("NPC State UI")]
    [SerializeField] private TextMeshProUGUI NPCstateText;

    [Header("Animation Parameters")]
    [SerializeField] private float uiFadeDuration = 0.3f;
    [SerializeField] private float statsAnimationDuration = 0.5f;

    // 현재 활성화된 캐릭터
    private BaseController currentController;
    private BaseStats currentPlayerStats;
    private SplineKnotAnimate currentSplineKnotAnimator;

    // 상태 변수
    private bool isRolling = false;
    private bool isInJunction = false;
    private Vector3 rollTextOriginalScale;
    
    // BoardManager 참조
    private BoardManager boardManager;

    /// <summary>
    /// 초기화
    /// </summary>
    public void Initialize()
    {
        // BoardManager 참조 획득
        boardManager = BoardManager.GetInstance();
        
        // 초기 상태 설정
        rollTextOriginalScale = rollValueText.transform.localScale;

        // UI 패널 초기화
        rollUIPanel.SetActive(false);
        starPurchaseUIPanel.SetActive(false);
        junctionUIPanel.SetActive(false);

        // 스타 구매 버튼 이벤트 등록
        starConfirmButton.onClick.AddListener(OnStarConfirmButtonClicked);
        starCancelButton.onClick.AddListener(OnStarCancelButtonClicked);
        
        // 이벤트 리스너 등록
        RegisterEventListeners();
        
        Debug.Log("UIManager initialized");
    }

    /// <summary>
    /// 컴포넌트 제거 시 이벤트 리스너 해제
    /// </summary>
    private void OnDestroy()
    {
        // 스타 구매 버튼 이벤트 해제
        starConfirmButton.onClick.RemoveListener(OnStarConfirmButtonClicked);
        starCancelButton.onClick.RemoveListener(OnStarCancelButtonClicked);
        
        // 이벤트 리스너 해제
        UnregisterEventListeners();
    }

    /// <summary>
    /// 이벤트 리스너 등록
    /// </summary>
    private void RegisterEventListeners()
    {
        // 턴 관련 이벤트 리스너 등록
        BoardEvents.OnTurnStart.AddListener(OnTurnStart);
        
        // 주사위 관련 이벤트 리스너 등록
        BoardEvents.OnRollStart.AddListener(OnRollStart);
        BoardEvents.OnRollDisplay.AddListener(OnRollDisplay);
        BoardEvents.OnRollEnd.AddListener(OnRollEnd);
        BoardEvents.OnRollCancel.AddListener(OnRollCancel);
        
        // 이동 관련 이벤트 리스너 등록
        BoardEvents.OnMovementStart.AddListener(OnMovementStart);
        
        // 분기점 관련 이벤트 리스너 등록
        BoardEvents.OnEnterJunction.AddListener(OnEnterJunction);
        
        // 스탯 관련 이벤트 리스너 등록
        BoardEvents.OnCoinsChanged.AddListener(OnCoinsChanged);
        BoardEvents.OnStarsChanged.AddListener(OnStarsChanged);
        BoardEvents.OnStatsInitialized.AddListener(OnStatsInitialized);
        
        // NPC 상태 관련 이벤트 리스너 등록
        BoardEvents.OnNPCStateChanged.AddListener(OnNPCStateChanged);
    }

    /// <summary>
    /// 이벤트 리스너 해제
    /// </summary>
    private void UnregisterEventListeners()
    {
        // 턴 관련 이벤트 리스너 해제
        BoardEvents.OnTurnStart.RemoveListener(OnTurnStart);
        
        // 주사위 관련 이벤트 리스너 해제
        BoardEvents.OnRollStart.RemoveListener(OnRollStart);
        BoardEvents.OnRollDisplay.RemoveListener(OnRollDisplay);
        BoardEvents.OnRollEnd.RemoveListener(OnRollEnd);
        BoardEvents.OnRollCancel.RemoveListener(OnRollCancel);
        
        // 이동 관련 이벤트 리스너 해제
        BoardEvents.OnMovementStart.RemoveListener(OnMovementStart);
        
        // 분기점 관련 이벤트 리스너 해제
        BoardEvents.OnEnterJunction.RemoveListener(OnEnterJunction);
        
        // 스탯 관련 이벤트 리스너 해제
        BoardEvents.OnCoinsChanged.RemoveListener(OnCoinsChanged);
        BoardEvents.OnStarsChanged.RemoveListener(OnStarsChanged);
        BoardEvents.OnStatsInitialized.RemoveListener(OnStatsInitialized);
        
        // NPC 상태 관련 이벤트 리스너 해제
        BoardEvents.OnNPCStateChanged.RemoveListener(OnNPCStateChanged);
    }

    private void LateUpdate()
    {
        // 주사위 UI 위치 업데이트
        if (isRolling && boardManager != null)
        {
            VisualEffectsManager visualEffectsManager = boardManager.GetVisualEffectsManager();
            if (visualEffectsManager != null)
            {
                Transform diceTransform = visualEffectsManager.GetDiceTransform();
                if (diceTransform != null && diceTransform.gameObject.activeSelf)
                {
                    Vector3 screenPosition = Camera.main.WorldToScreenPoint(diceTransform.position);
                    rollValueText.transform.position = screenPosition;
                }
            }
        }
    }

    /// <summary>
    /// 턴 시작 이벤트 핸들러
    /// </summary>
    private void OnTurnStart(BaseController controller)
    {
        // 현재 컨트롤러 참조 설정
        currentController = controller;
        currentPlayerStats = controller.GetComponent<BaseStats>();
        currentSplineKnotAnimator = controller.GetComponent<SplineKnotAnimate>();
        
        // UI 업데이트
        UpdatePlayerUI();
        UpdateStatsUI();
    }

    #region Roll UI Methods

    /// <summary>
    /// 주사위 굴림 시작 이벤트 핸들러
    /// </summary>
    private void OnRollStart(BaseController controller)
    {
        if (controller != currentController) return;
        
        isRolling = true;
        rollUIPanel.SetActive(true);
        rollValueText.gameObject.SetActive(false);
        rollCanvasGroup.alpha = 1f;
    }

    /// <summary>
    /// 주사위 결과 표시 이벤트 핸들러
    /// </summary>
    private void OnRollDisplay(BaseController controller, int rollValue)
    {
        if (controller != currentController) return;
        
        if (rollValue <= 0)
        {
            rollValueText.gameObject.SetActive(false);
            return;
        }

        rollValueText.text = rollValue.ToString();
        rollValueText.gameObject.SetActive(true);

        // 스케일 애니메이션
        rollValueText.transform.localScale = Vector3.zero;
        rollValueText.transform.DOScale(rollTextOriginalScale, 0.2f).SetEase(rollTextScaleEase);
    }

    /// <summary>
    /// 주사위 결과 텍스트 페이드 아웃
    /// </summary>
    public void FadeRollText(bool fadeText)
    {
        rollCanvasGroup.DOFade(fadeText ? 0 : 1, .3f);
    }

    /// <summary>
    /// 주사위 굴림 종료 이벤트 핸들러
    /// </summary>
    private void OnRollEnd(BaseController controller)
    {
        if (controller != currentController) return;
        
        // 주사위 UI는 일정 시간 동안 표시 후 페이드 아웃
        StartCoroutine(FadeOutRollUI());
    }

    /// <summary>
    /// 주사위 굴림 취소 이벤트 핸들러
    /// </summary>
    private void OnRollCancel(BaseController controller)
    {
        if (controller != currentController) return;
        
        isRolling = false;
        rollUIPanel.SetActive(false);
    }

    /// <summary>
    /// 주사위 UI 페이드 아웃 코루틴
    /// </summary>
    public IEnumerator FadeOutRollUI()
    {
        yield return new WaitForSeconds(1f);

        rollCanvasGroup.DOFade(0f, uiFadeDuration).OnComplete(() =>
        {
            isRolling = false;
            rollUIPanel.SetActive(false);
            rollCanvasGroup.alpha = 1f;
        });
    }

    /// <summary>
    /// 이동 시작 이벤트 핸들러
    /// </summary>
    private void OnMovementStart(BaseController controller, bool started)
    {
        if (controller != currentController) return;
        
        if (!started)
        {
            // 이동 종료 시 주사위 UI 비활성화
            isRolling = false;
            rollUIPanel.SetActive(false);
        }
    }

    #endregion

    #region Star Purchase UI Methods

    /// <summary>
    /// 별 구매 UI 표시
    /// </summary>
    public void ShowStarPurchaseUI(bool show, int price = 20)
    {
        starPurchaseUIPanel.SetActive(show);

        if (show)
        {
            starPriceText.text = price.ToString();
            starPurchaseCanvasGroup.alpha = 0f;
            starPurchaseCanvasGroup.DOFade(1f, uiFadeDuration);

            // 버튼 선택 상태 설정
            EventSystem.current.SetSelectedGameObject(starConfirmButton.gameObject);
        }
        else
        {
            starPurchaseCanvasGroup.DOFade(0f, uiFadeDuration).OnComplete(() =>
            {
                starPurchaseUIPanel.SetActive(false);
            });
        }
    }

    /// <summary>
    /// 별 구매 확인 버튼 클릭 이벤트 핸들러
    /// </summary>
    public void OnStarConfirmButtonClicked()
    {
        // 별 구매 로직 실행
        if (currentController != null)
        {
            // 별 구매 이벤트 발생
            BoardEvents.OnStarPurchaseDecision.Invoke(currentController, true);

            // UI 숨김
            ShowStarPurchaseUI(false);
        }
    }

    /// <summary>
    /// 별 구매 취소 버튼 클릭 이벤트 핸들러
    /// </summary>
    public void OnStarCancelButtonClicked()
    {
        // 별 구매 취소 이벤트 발생
        if (currentController != null)
        {
            BoardEvents.OnStarPurchaseDecision.Invoke(currentController, false);
        }
        
        // UI 숨김
        ShowStarPurchaseUI(false);
    }

    #endregion

    #region Stats UI Methods

    /// <summary>
    /// 스탯 초기화 이벤트 핸들러
    /// </summary>
    private void OnStatsInitialized(BaseController controller)
    {
        if (controller != currentController) return;
        
        UpdateStatsUI();
    }

    /// <summary>
    /// 스탯 UI 업데이트
    /// </summary>
    public void UpdateStatsUI()
    {
        if (currentPlayerStats == null) return;

        coinsText.text = currentPlayerStats.Coins.ToString();
        starsText.text = currentPlayerStats.Stars.ToString();
    }

    /// <summary>
    /// 코인 변경 이벤트 핸들러
    /// </summary>
    private void OnCoinsChanged(BaseController controller, int amount)
    {
        if (controller != currentController) return;
        
        // 현재 코인 수 표시
        coinsText.text = currentPlayerStats.Coins.ToString();

        // 코인 아이콘 애니메이션
        AnimateCoinIcon();
    }

    /// <summary>
    /// 별 변경 이벤트 핸들러
    /// </summary>
    private void OnStarsChanged(BaseController controller, int amount)
    {
        if (controller != currentController) return;
        
        // 현재 별 수 표시
        starsText.text = currentPlayerStats.Stars.ToString();

        // 별 아이콘 애니메이션
        AnimateStarIcon();
    }

    /// <summary>
    /// 코인 아이콘 애니메이션
    /// </summary>
    public void AnimateCoinIcon()
    {
        coinIcon.transform.DOKill();
        coinIcon.transform.localScale = Vector3.one;

        Sequence sequence = DOTween.Sequence();
        sequence.Append(coinIcon.transform.DOScale(1.2f, statsAnimationDuration / 2));
        sequence.Append(coinIcon.transform.DOScale(1f, statsAnimationDuration / 2));
    }

    /// <summary>
    /// 별 아이콘 애니메이션
    /// </summary>
    public void AnimateStarIcon()
    {
        starIcon.transform.DOKill();
        starIcon.transform.localScale = Vector3.one;

        Sequence sequence = DOTween.Sequence();
        sequence.Append(starIcon.transform.DOScale(1.2f, statsAnimationDuration / 2));
        sequence.Append(starIcon.transform.DOScale(1f, statsAnimationDuration / 2));
    }

    #endregion

    #region Player UI Methods

    /// <summary>
    /// Player UI 업데이트
    /// </summary>
    public void UpdatePlayerUI()
    {
        if (currentController == null) return;

        // 플레이어 이름 설정
        if (currentPlayerNameText != null)
        {
            if (currentController is PlayerController)
            {
                currentPlayerNameText.text = "Player";
            }
            else if (currentController is NPCController)
            {
                currentPlayerNameText.text = currentController.name;
            }
        }

        // 플레이어 아이콘 설정
        if (currentPlayerIcon != null && playerIcons.Length > 0)
        {
            int iconIndex = (currentController is PlayerController) ? 0 : 1;
            currentPlayerIcon.sprite = playerIcons[iconIndex];
        }
    }

    #endregion

    #region Junction UI Methods

    /// <summary>
    /// 분기점 진입 이벤트 핸들러
    /// </summary>
    private void OnEnterJunction(BaseController controller, bool entered)
    {
        if (controller != currentController) return;
        
        isInJunction = entered;

        if (entered)
        {
            // 분기점 UI 활성화
            junctionUIPanel.SetActive(true);

            // 플레이어/NPC에 따라 다른 안내 텍스트 표시
            if (currentController is PlayerController)
            {
                junctionInstructionText.text = "Select the direction key and press the space to confirm";
            }
            else
            {
                junctionInstructionText.text = "NPC is selecting a path...";
            }
        }
        else
        {
            // 분기점 UI 비활성화
            junctionUIPanel.SetActive(false);
        }
    }

    #endregion
    
    #region NPC State UI Methods
    
    /// <summary>
    /// NPC 상태 변경 이벤트 핸들러
    /// </summary>
    private void OnNPCStateChanged(BaseController controller, string state)
    {
        if (controller != currentController || !(controller is NPCController)) return;
        
        if (NPCstateText != null)
        {
            NPCstateText.text = state;
        }
    }
    
    #endregion

    #region Public Methods

    /// <summary>
    /// 모든 UI 업데이트
    /// </summary>
    public void UpdateAllUI()
    {
        UpdatePlayerUI();
        UpdateStatsUI();
    }

    /// <summary>
    /// 화면 페이드 효과
    /// </summary>
    public void FadeScreen(bool fadeOut)
    {
        // 화면 페이드 효과 구현
        // 이 메서드는 씬 전환 시 사용
    }

    /// <summary>
    /// 확인 대화상자 표시
    /// </summary>
    public void ShowConfirmDialog(string title, string message, System.Action onConfirm, System.Action onCancel)
    {
        // 확인 대화상자 구현
        // 이 메서드는 중요한 결정을 내릴 때 사용
    }
    
    #endregion
}
