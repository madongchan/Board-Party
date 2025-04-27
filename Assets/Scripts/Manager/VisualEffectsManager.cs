using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Splines;

/// <summary>
/// 모든 시각적 효과를 중앙에서 관리하는 매니저 클래스
/// </summary>
public class VisualEffectsManager : MonoBehaviour
{
    // 싱글톤 인스턴스
    public static VisualEffectsManager Instance { get; private set; }
    
    [Header("Dice References")]
    [SerializeField] private GameObject diceObject;
    [SerializeField] private Transform diceTransform;
    [SerializeField] private ParticleSystem diceRollParticle;
    [SerializeField] private ParticleSystem diceResultParticle;
    [SerializeField] private Animator diceAnimator;
    
    [Header("Junction Visuals")]
    [SerializeField] private GameObject junctionVisualsObject;
    [SerializeField] private Transform junctionArrow;
    [SerializeField] private Material junctionHighlightMaterial;
    [SerializeField] private Material junctionNormalMaterial;
    
    [Header("Particle Effects")]
    [SerializeField] private ParticleSystem coinGainParticle;
    [SerializeField] private ParticleSystem coinLossParticle;
    [SerializeField] private ParticleSystem starGainParticle;
    [SerializeField] private ParticleSystem movementParticle;
    [SerializeField] private ParticleSystem landingParticle;
    
    [Header("Animation Parameters")]
    [SerializeField] private float diceRollHeight = 2f;
    [SerializeField] private float diceRollDuration = 0.5f;
    [SerializeField] private float diceResultDuration = 1f;
    [SerializeField] private float junctionArrowSpeed = 2f;
    
    // 현재 활성화된 캐릭터
    private BaseController currentController;
    private SplineKnotAnimate currentSplineKnotAnimator;
    
    // 상태 변수
    private bool isDiceRolling = false;
    private bool isInJunction = false;
    private int currentJunctionIndex = 0;
    private List<SplineKnotIndex> currentJunctionOptions = new List<SplineKnotIndex>();
    
    private void Awake()
    {
        // 싱글톤 설정
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // 초기 상태 설정
        diceObject.SetActive(false);
        junctionVisualsObject.SetActive(false);
    }
    
    private void Start()
    {
        // GameManager의 플레이어 변경 이벤트 구독
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerChanged.AddListener(SetCurrentPlayer);
            SetCurrentPlayer(GameManager.Instance.GetCurrentPlayer());
        }
    }
    
    /// <summary>
    /// 현재 플레이어 설정
    /// </summary>
    public void SetCurrentPlayer(BaseController controller)
    {
        if (controller == null) return;
        
        // 이전 이벤트 리스너 해제
        UnregisterEvents();
        
        // 새 컨트롤러 참조 설정
        currentController = controller;
        currentSplineKnotAnimator = controller.GetComponent<SplineKnotAnimate>();
        
        // 이벤트 리스너 등록
        RegisterEvents();
    }
    
    /// <summary>
    /// 이벤트 리스너 등록
    /// </summary>
    private void RegisterEvents()
    {
        if (currentController != null)
        {
            currentController.OnRollStart.AddListener(OnRollStart);
            currentController.OnRollJump.AddListener(OnRollJump);
            currentController.OnRollDisplay.AddListener(OnRollDisplay);
            currentController.OnRollEnd.AddListener(OnRollEnd);
            currentController.OnRollCancel.AddListener(OnRollCancel);
            currentController.OnMovementStart.AddListener(OnMovementStart);
        }
        
        if (currentSplineKnotAnimator != null)
        {
            currentSplineKnotAnimator.OnEnterJunction.AddListener(OnEnterJunction);
            currentSplineKnotAnimator.OnJunctionSelection.AddListener(OnJunctionSelection);
            currentSplineKnotAnimator.OnKnotEnter.AddListener(OnKnotEnter);
            currentSplineKnotAnimator.OnKnotLand.AddListener(OnKnotLand);
        }
        
        // 스탯 이벤트 구독
        BaseStats stats = currentController?.GetComponent<BaseStats>();
        if (stats != null)
        {
            stats.OnCoinsChanged.AddListener(OnCoinsChanged);
            stats.OnStarsChanged.AddListener(OnStarsChanged);
        }
    }
    
    /// <summary>
    /// 이벤트 리스너 해제
    /// </summary>
    private void UnregisterEvents()
    {
        if (currentController != null)
        {
            currentController.OnRollStart.RemoveListener(OnRollStart);
            currentController.OnRollJump.RemoveListener(OnRollJump);
            currentController.OnRollDisplay.RemoveListener(OnRollDisplay);
            currentController.OnRollEnd.RemoveListener(OnRollEnd);
            currentController.OnRollCancel.RemoveListener(OnRollCancel);
            currentController.OnMovementStart.RemoveListener(OnMovementStart);
        }
        
        if (currentSplineKnotAnimator != null)
        {
            currentSplineKnotAnimator.OnEnterJunction.RemoveListener(OnEnterJunction);
            currentSplineKnotAnimator.OnJunctionSelection.RemoveListener(OnJunctionSelection);
            currentSplineKnotAnimator.OnKnotEnter.RemoveListener(OnKnotEnter);
            currentSplineKnotAnimator.OnKnotLand.RemoveListener(OnKnotLand);
        }
        
        // 스탯 이벤트 구독 해제
        BaseStats stats = currentController?.GetComponent<BaseStats>();
        if (stats != null)
        {
            stats.OnCoinsChanged.RemoveListener(OnCoinsChanged);
            stats.OnStarsChanged.RemoveListener(OnStarsChanged);
        }
    }
    
    #region Dice Event Handlers
    
    /// <summary>
    /// 주사위 굴림 시작 이벤트 핸들러
    /// </summary>
    public void OnRollStart()
    {
        // 주사위 활성화 및 위치 설정
        diceObject.SetActive(true);
        diceTransform.position = currentController.transform.position + Vector3.up * diceRollHeight;
        isDiceRolling = true;
        
        // 주사위 애니메이션 초기화
        if (diceAnimator != null)
        {
            diceAnimator.SetTrigger("Idle");
        }
    }
    
    /// <summary>
    /// 주사위 점프 이벤트 핸들러
    /// </summary>
    public void OnRollJump()
    {
        // 주사위 점프 애니메이션 및 파티클 재생
        if (diceRollParticle != null)
        {
            diceRollParticle.transform.position = diceTransform.position;
            diceRollParticle.Play();
        }
        
        // 주사위 애니메이션 재생
        if (diceAnimator != null)
        {
            diceAnimator.SetTrigger("Roll");
        }
        
        // 주사위 점프 애니메이션
        StartCoroutine(DiceJumpAnimation());
    }
    
    /// <summary>
    /// 주사위 결과 표시 이벤트 핸들러
    /// </summary>
    public void OnRollDisplay(int rollValue)
    {
        // 주사위 결과 애니메이션 및 파티클 재생
        if (diceResultParticle != null && rollValue > 0)
        {
            diceResultParticle.transform.position = diceTransform.position;
            diceResultParticle.Play();
        }
        
        // 주사위 결과 애니메이션
        if (diceAnimator != null && rollValue > 0)
        {
            diceAnimator.SetInteger("Result", rollValue);
            diceAnimator.SetTrigger("ShowResult");
        }
    }
    
    /// <summary>
    /// 주사위 굴림 종료 이벤트 핸들러
    /// </summary>
    public void OnRollEnd()
    {
        // 주사위 결과 표시 후 일정 시간 후 비활성화
        StartCoroutine(HideDiceAfterDelay(diceResultDuration));
        isDiceRolling = false;
    }
    
    /// <summary>
    /// 주사위 굴림 취소 이벤트 핸들러
    /// </summary>
    public void OnRollCancel()
    {
        // 주사위 즉시 비활성화
        diceObject.SetActive(false);
        isDiceRolling = false;
    }
    
    /// <summary>
    /// 주사위 점프 애니메이션 코루틴
    /// </summary>
    public IEnumerator DiceJumpAnimation()
    {
        Vector3 startPos = diceTransform.position;
        Vector3 jumpPos = startPos + Vector3.up * 1f;
        
        // 위로 점프
        float elapsed = 0f;
        while (elapsed < diceRollDuration / 2)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (diceRollDuration / 2);
            diceTransform.position = Vector3.Lerp(startPos, jumpPos, t);
            yield return null;
        }
        
        // 아래로 떨어짐
        elapsed = 0f;
        while (elapsed < diceRollDuration / 2)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (diceRollDuration / 2);
            diceTransform.position = Vector3.Lerp(jumpPos, startPos, t);
            yield return null;
        }
        
        diceTransform.position = startPos;
    }
    
    /// <summary>
    /// 주사위 숨기기 코루틴
    /// </summary>
    public IEnumerator HideDiceAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        diceObject.SetActive(false);
    }
    
    #endregion
    
    #region Movement Event Handlers
    
    /// <summary>
    /// 이동 시작 이벤트 핸들러
    /// </summary>
    public void OnMovementStart(bool started)
    {
        if (started)
        {
            // 이동 시작 시 파티클 활성화
            if (movementParticle != null)
            {
                movementParticle.gameObject.SetActive(true);
                movementParticle.Play();
            }
        }
        else
        {
            // 이동 종료 시 파티클 비활성화
            if (movementParticle != null)
            {
                movementParticle.Stop();
                movementParticle.gameObject.SetActive(false);
            }
        }
    }
    
    /// <summary>
    /// 노트 진입 이벤트 핸들러
    /// </summary>
    public void OnKnotEnter(SplineKnotIndex knotIndex)
    {
        // 노트 진입 시 필요한 시각 효과
    }
    
    /// <summary>
    /// 노트 착지 이벤트 핸들러
    /// </summary>
    public void OnKnotLand(SplineKnotIndex knotIndex)
    {
        // 노트 착지 시 파티클 재생
        if (landingParticle != null)
        {
            landingParticle.transform.position = currentController.transform.position;
            landingParticle.Play();
        }
    }
    
    #endregion
    
    #region Junction Event Handlers
    
    /// <summary>
    /// 분기점 진입 이벤트 핸들러
    /// </summary>
    public void OnEnterJunction(bool entered)
    {
        isInJunction = entered;
        
        if (entered)
        {
            // 분기점 시각화 활성화
            junctionVisualsObject.SetActive(true);
            
            // 분기점 옵션 저장
            currentJunctionOptions = new List<SplineKnotIndex>(currentSplineKnotAnimator.walkableKnots);
            currentJunctionIndex = 0;
            
            // 초기 선택 표시
            UpdateJunctionVisuals(currentJunctionIndex);
        }
        else
        {
            // 분기점 시각화 비활성화
            junctionVisualsObject.SetActive(false);
            currentJunctionOptions.Clear();
        }
    }
    
    /// <summary>
    /// 분기점 선택 이벤트 핸들러
    /// </summary>
    public void OnJunctionSelection(int selectionIndex)
    {
        currentJunctionIndex = selectionIndex;
        UpdateJunctionVisuals(selectionIndex);
    }
    
    /// <summary>
    /// 분기점 시각화 업데이트
    /// </summary>
    public void UpdateJunctionVisuals(int selectionIndex)
    {
        if (!isInJunction || currentJunctionOptions.Count == 0) return;
        
        // 선택된 경로 위치 계산
        Vector3 targetPosition = currentSplineKnotAnimator.GetJunctionPathPosition(selectionIndex);
        
        // 화살표 위치 및 방향 설정
        junctionArrow.position = currentController.transform.position + Vector3.up * 0.5f;
        junctionArrow.LookAt(targetPosition);
        
        // 하이라이트 효과
        for (int i = 0; i < currentJunctionOptions.Count; i++)
        {
            // 여기서는 실제 노드 하이라이트 로직을 구현해야 함
            // 예시 코드이므로 실제 구현은 게임 구조에 맞게 수정 필요
        }
    }
    
    #endregion
    
    #region Stats Event Handlers
    
    /// <summary>
    /// 코인 변경 이벤트 핸들러
    /// </summary>
    public void OnCoinsChanged(int amount)
    {
        if (amount > 0)
        {
            // 코인 획득 파티클 재생
            if (coinGainParticle != null)
            {
                coinGainParticle.transform.position = currentController.transform.position + Vector3.up;
                
                // 코인 양에 따라 파티클 수 조절
                var emission = coinGainParticle.emission;
                emission.rateOverTime = Mathf.Min(amount * 2, 20);
                
                coinGainParticle.Play();
            }
        }
        else if (amount < 0)
        {
            // 코인 손실 파티클 재생
            if (coinLossParticle != null)
            {
                coinLossParticle.transform.position = currentController.transform.position + Vector3.up;
                
                // 코인 양에 따라 파티클 수 조절
                var emission = coinLossParticle.emission;
                emission.rateOverTime = Mathf.Min(Mathf.Abs(amount) * 2, 20);
                
                coinLossParticle.Play();
            }
        }
    }
    
    /// <summary>
    /// 별 변경 이벤트 핸들러
    /// </summary>
    public void OnStarsChanged(int amount)
    {
        if (amount > 0)
        {
            // 별 획득 파티클 재생
            if (starGainParticle != null)
            {
                starGainParticle.transform.position = currentController.transform.position + Vector3.up;
                starGainParticle.Play();
            }
        }
    }
    
    #endregion
    
    #region Public Methods
    
    /// <summary>
    /// 주사위 Transform 반환 (RollUI에서 사용)
    /// </summary>
    public Transform GetDiceTransform()
    {
        return diceTransform;
    }
    
    /// <summary>
    /// 코인 획득 파티클 재생
    /// </summary>
    public void PlayCoinParticle(Vector3 position, int amount)
    {
        if (coinGainParticle != null && amount > 0)
        {
            coinGainParticle.transform.position = position + Vector3.up;
            
            // 코인 양에 따라 파티클 수 조절
            var emission = coinGainParticle.emission;
            emission.rateOverTime = Mathf.Min(amount * 2, 20);
            
            coinGainParticle.Play();
        }
        else if (coinLossParticle != null && amount < 0)
        {
            coinLossParticle.transform.position = position + Vector3.up;
            
            // 코인 양에 따라 파티클 수 조절
            var emission = coinLossParticle.emission;
            emission.rateOverTime = Mathf.Min(Mathf.Abs(amount) * 2, 20);
            
            coinLossParticle.Play();
        }
    }
    
    /// <summary>
    /// 별 획득 파티클 재생
    /// </summary>
    public void PlayStarParticle(Vector3 position)
    {
        if (starGainParticle != null)
        {
            starGainParticle.transform.position = position + Vector3.up;
            starGainParticle.Play();
        }
    }
    
    #endregion
}
