using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Splines;

/// <summary>
/// VisualEffectsManager 클래스 - 모든 시각적 효과를 중앙에서 관리
/// BoardEvents 기반 이벤트 시스템을 사용하여 이벤트를 처리합니다.
/// </summary>
public class VisualEffectsManager : MonoBehaviour
{
    [Header("Dice References")]
    [SerializeField] private GameObject diceObject;
    [SerializeField] private Transform diceTransform;

    [Header("Dice Animation Parameters")]
    [SerializeField] private float rotationSpeed = 360f;
    [SerializeField] private float tiltAmplitude = 15f;
    [SerializeField] private float tiltFrequency = 2f;
    [SerializeField] private float numberAnimationSpeed = .15f;
    private float tiltTime = 0f;
    private TextMeshPro[] numberLabels;
    private Sequence diceSpinSequence;
    private Tweener numberTweener;

    [Header("Junction Visuals")]
    [SerializeField] private GameObject junctionVisualsObject;
    [SerializeField] private Transform junctionArrowPrefab;
    [SerializeField] private Color selectedJunctionColor = Color.yellow;
    [SerializeField] private Color defaultJunctionColor = Color.white;
    private List<GameObject> junctionArrows = new List<GameObject>();

    [Header("Particle Effects")]
    [SerializeField] private ParticleSystem coinGainParticle;
    [SerializeField] private ParticleSystem coinLossParticle;
    [SerializeField] private ParticleSystem diceHitParticle;
    [SerializeField] private ParticleSystem diceResultParticle;
    private float particleRepeatInterval;

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
        diceObject.SetActive(false);
        junctionVisualsObject.SetActive(false);
        
        // 주사위 텍스트 레이블 참조 설정
        if (diceObject != null)
        {
            numberLabels = diceObject.GetComponentsInChildren<TextMeshPro>();
        }
        
        // DOTween 초기 설정
        DOTween.SetTweensCapacity(500, 50);
        
        // 파티클 반복 간격 설정
        if (coinGainParticle != null)
        {
            particleRepeatInterval = coinGainParticle.emission.GetBurst(0).repeatInterval;
        }
        
        // 이벤트 리스너 등록
        RegisterEventListeners();
        
        Debug.Log("VisualEffectsManager initialized");
    }

    /// <summary>
    /// 컴포넌트 제거 시 이벤트 리스너 해제
    /// </summary>
    private void OnDestroy()
    {
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
        BoardEvents.OnRollJump.AddListener(OnRollJump);
        BoardEvents.OnRollDisplay.AddListener(OnRollDisplay);
        BoardEvents.OnRollEnd.AddListener(OnRollEnd);
        BoardEvents.OnRollCancel.AddListener(OnRollCancel);
        
        // 이동 관련 이벤트 리스너 등록
        BoardEvents.OnMovementStart.AddListener(OnMovementStart);
        BoardEvents.OnKnotEnter.AddListener(OnKnotEnter);
        BoardEvents.OnKnotLand.AddListener(OnKnotLand);
        
        // 분기점 관련 이벤트 리스너 등록
        BoardEvents.OnEnterJunction.AddListener(OnEnterJunction);
        BoardEvents.OnJunctionSelection.AddListener(OnJunctionSelection);
        
        // 스탯 관련 이벤트 리스너 등록
        BoardEvents.OnCoinsChanged.AddListener(OnCoinsChanged);
        BoardEvents.OnStarsChanged.AddListener(OnStarsChanged);
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
        BoardEvents.OnRollJump.RemoveListener(OnRollJump);
        BoardEvents.OnRollDisplay.RemoveListener(OnRollDisplay);
        BoardEvents.OnRollEnd.RemoveListener(OnRollEnd);
        BoardEvents.OnRollCancel.RemoveListener(OnRollCancel);
        
        // 이동 관련 이벤트 리스너 해제
        BoardEvents.OnMovementStart.RemoveListener(OnMovementStart);
        BoardEvents.OnKnotEnter.RemoveListener(OnKnotEnter);
        BoardEvents.OnKnotLand.RemoveListener(OnKnotLand);
        
        // 분기점 관련 이벤트 리스너 해제
        BoardEvents.OnEnterJunction.RemoveListener(OnEnterJunction);
        BoardEvents.OnJunctionSelection.RemoveListener(OnJunctionSelection);
        
        // 스탯 관련 이벤트 리스너 해제
        BoardEvents.OnCoinsChanged.RemoveListener(OnCoinsChanged);
        BoardEvents.OnStarsChanged.RemoveListener(OnStarsChanged);
    }

    private void Update()
    {
        // 주사위 회전 업데이트
        if (isDiceRolling && diceObject != null && diceObject.activeSelf)
        {
            SpinDice();
        }
    }

    /// <summary>
    /// 턴 시작 이벤트 핸들러
    /// </summary>
    private void OnTurnStart(BaseController controller)
    {
        // 현재 컨트롤러 참조 설정
        currentController = controller;
        currentSplineKnotAnimator = controller.GetComponent<SplineKnotAnimate>();
    }

    #region Dice Event Handlers

    /// <summary>
    /// 주사위 굴림 시작 이벤트 핸들러
    /// </summary>
    private void OnRollStart(BaseController controller)
    {
        if (controller != currentController) return;
        
        // 주사위 활성화 및 위치 설정
        diceObject.SetActive(true);
        diceTransform.position = controller.transform.position + Vector3.up * diceRollHeight;
        isDiceRolling = true;

        // 기존 애니메이션 중지
        diceTransform.DOKill();

        // 숫자 애니메이션 시작
        AnimateDiceNumbers();
    }

    private void AnimateDiceNumbers()
    {
        int currentNumber = 1;

        numberTweener = DOTween.To(() => currentNumber, x => currentNumber = x, 9, 0.5f)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Restart)
            .OnUpdate(() =>
            {
                foreach (TextMeshPro label in numberLabels)
                {
                    label.text = currentNumber.ToString();
                }
            })
            .SetLink(gameObject);
    }
    
    private void SpinDice()
    {
        diceTransform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);

        tiltTime += Time.deltaTime * tiltFrequency;
        float tiltAngle = Mathf.Sin(tiltTime) * tiltAmplitude;

        diceTransform.rotation = Quaternion.Euler(tiltAngle, diceTransform.rotation.eulerAngles.y, 0);
    }

    /// <summary>
    /// 주사위 점프 이벤트 핸들러
    /// </summary>
    private void OnRollJump(BaseController controller)
    {
        if (controller != currentController) return;
        
        // 주사위 점프 애니메이션 및 파티클 재생
        if (diceHitParticle != null)
        {
            diceHitParticle.transform.position = diceTransform.position;
            diceHitParticle.Play();
        }

        // 주사위 크기 변동 효과
        diceTransform.DOScale(new Vector3(1.2f, 1.2f, 1.2f), 0.2f)
            .SetLoops(2, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
    }

    /// <summary>
    /// 주사위 결과 표시 이벤트 핸들러
    /// </summary>
    private void OnRollDisplay(BaseController controller, int rollValue)
    {
        if (controller != currentController) return;
        
        // 주사위 결과 애니메이션 및 파티클 재생
        if (diceResultParticle != null && rollValue > 0)
        {
            diceResultParticle.transform.position = diceTransform.position;
            diceResultParticle.Play();

            // 모든 애니메이션 중지
            if (diceSpinSequence != null)
                diceSpinSequence.Kill();
                
            if (numberTweener != null)
                numberTweener.Kill();

            SetDiceNumber(rollValue);

            // 자연스러운 정지 애니메이션
            diceTransform.DORotate(Vector3.zero, 0.1f)
                .SetEase(Ease.OutBack)
                .OnComplete(() => diceObject.SetActive(false));
        }
    }

    private void SetDiceNumber(int value)
    {
        if (numberLabels == null) return;

        foreach (TextMeshPro p in numberLabels)
        {
            p.text = value.ToString();
        }
    }

    /// <summary>
    /// 주사위 굴림 종료 이벤트 핸들러
    /// </summary>
    private void OnRollEnd(BaseController controller)
    {
        if (controller != currentController) return;
        
        // 주사위 결과 표시 후 일정 시간 후 비활성화
        StartCoroutine(HideDiceAfterDelay(diceResultDuration));
        isDiceRolling = false;
    }

    /// <summary>
    /// 주사위 굴림 취소 이벤트 핸들러
    /// </summary>
    private void OnRollCancel(BaseController controller)
    {
        if (controller != currentController) return;
        
        // 주사위 즉시 비활성화
        diceObject.SetActive(false);
        isDiceRolling = false;
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
    private void OnMovementStart(BaseController controller, bool started)
    {
        if (controller != currentController) return;
        
        // 이동 시작/종료 시 필요한 시각 효과
    }

    /// <summary>
    /// 노트 진입 이벤트 핸들러
    /// </summary>
    private void OnKnotEnter(BaseController controller, SplineKnotIndex knotIndex)
    {
        if (controller != currentController) return;
        
        // 노트 진입 시 필요한 시각 효과
    }

    /// <summary>
    /// 노트 착지 이벤트 핸들러
    /// </summary>
    private void OnKnotLand(BaseController controller, SplineKnotIndex knotIndex)
    {
        if (controller != currentController) return;
        
        // 노트 착지 시 필요한 시각 효과
    }

    #endregion

    #region Junction Event Handlers

    /// <summary>
    /// 분기점 진입 이벤트 핸들러
    /// </summary>
    private void OnEnterJunction(BaseController controller, bool entered)
    {
        if (controller != currentController) return;
        
        junctionVisualsObject.SetActive(entered);
        isInJunction = entered;

        if (entered)
        {
            CreateJunctionArrows();
        }
        else
        {
            ClearJunctionArrows();
        }
    }

    private void CreateJunctionArrows()
    {
        ClearJunctionArrows();

        if (currentSplineKnotAnimator == null || junctionArrowPrefab == null) return;

        for (int i = 0; i < currentSplineKnotAnimator.walkableKnots.Count; i++)
        {
            GameObject arrow = Instantiate(junctionArrowPrefab.gameObject, currentController.transform.position, Quaternion.identity);
            junctionArrows.Add(arrow);

            // 화살표 방향 설정
            Vector3 targetPosition = currentSplineKnotAnimator.GetJunctionPathPosition(i);
            arrow.transform.LookAt(targetPosition, Vector3.up);

            // 기본 색상 설정
            Renderer renderer = arrow.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = defaultJunctionColor;
            }
        }
    }

    private void ClearJunctionArrows()
    {
        foreach (GameObject arrow in junctionArrows)
        {
            if (arrow != null)
            {
                Destroy(arrow);
            }
        }

        junctionArrows.Clear();
    }

    /// <summary>
    /// 분기점 선택 이벤트 핸들러
    /// </summary>
    private void OnJunctionSelection(BaseController controller, int selectionIndex)
    {
        if (controller != currentController) return;
        
        currentJunctionIndex = selectionIndex;
        
        // 선택된 화살표 하이라이트
        for (int i = 0; i < junctionArrows.Count; i++)
        {
            if (junctionArrows[i] == null) continue;

            Renderer renderer = junctionArrows[i].GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = (i == selectionIndex) ? selectedJunctionColor : defaultJunctionColor;
            }

            if (i == selectionIndex)
            {
                junctionArrows[i].transform.DOPunchScale(Vector3.one / 4, .3f, 10, 1);
            }
        }
    }

    #endregion

    #region Stats Event Handlers

    /// <summary>
    /// 코인 변경 이벤트 핸들러
    /// </summary>
    private void OnCoinsChanged(BaseController controller, int amount)
    {
        if (controller != currentController) return;
        
        // 코인 획득/손실 파티클 재생
        if (amount > 0 && coinGainParticle != null)
        {
            coinGainParticle.transform.position = controller.transform.position + Vector3.up;

            // 코인 양에 따라 파티클 수 조절
            short count = (short)Mathf.Clamp(amount, 1, 10);
            ParticleSystem.Burst burst = new ParticleSystem.Burst(0, count, count, amount, particleRepeatInterval / Mathf.Sqrt(count));
            coinGainParticle.emission.SetBurst(0, burst);

            coinGainParticle.Play();
        }
        else if (amount < 0 && coinLossParticle != null)
        {
            coinLossParticle.transform.position = controller.transform.position + Vector3.up;

            // 코인 양에 따라 파티클 수 조절
            short count = (short)Mathf.Clamp(Mathf.Abs(amount), 1, 10);
            ParticleSystem.Burst burst = new ParticleSystem.Burst(0, count, count, Mathf.Abs(amount), particleRepeatInterval / Mathf.Sqrt(count));
            coinLossParticle.emission.SetBurst(0, burst);

            coinLossParticle.Play();
        }
    }

    /// <summary>
    /// 별 변경 이벤트 핸들러
    /// </summary>
    private void OnStarsChanged(BaseController controller, int amount)
    {
        if (controller != currentController) return;
        
        // 별 획득 파티클 재생 (구현 필요)
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 주사위 Transform 반환 (UI에서 사용)
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
        // 별 획득 파티클 재생 (구현 필요)
    }

    #endregion
}
