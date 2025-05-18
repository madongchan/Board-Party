using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
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
    private List<SplineKnotIndex> currentJunctionOptions = new List<SplineKnotIndex>();

    private void Awake()
    {
        // 싱글톤 설정
        if (Instance == null)
        {
            Instance = this;
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
    public void OnRollJump()
    {
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
    public void OnRollDisplay(int rollValue)
    {
        // 주사위 결과 애니메이션 및 파티클 재생
        if (diceResultParticle != null && rollValue > 0)
        {
            diceResultParticle.transform.position = diceTransform.position;
            diceResultParticle.Play();

            // 모든 애니메이션 중지
            diceSpinSequence.Kill();
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
            // if (movementParticle != null)
            // {
            //     movementParticle.gameObject.SetActive(true);
            //     movementParticle.Play();
            // }
        }
        else
        {
            // 이동 종료 시 파티클 비활성화
            // if (movementParticle != null)
            // {
            //     movementParticle.Stop();
            //     movementParticle.gameObject.SetActive(false);
            // }
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
        // if (landingParticle != null)
        // {
        //     landingParticle.transform.position = currentController.transform.position;
        //     landingParticle.Play();
        // }
    }

    #endregion

    #region Junction Event Handlers

    /// <summary>
    /// 분기점 진입 이벤트 핸들러
    /// </summary>
    public void OnEnterJunction(bool entered)
    {
        junctionVisualsObject.SetActive(entered);

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

    public void OnJunctionSelection(int selectionIndex)
    {
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
    public void OnCoinsChanged(int amount)
    {
        // 코인 획득/손실 파티클 재생
        if (amount > 0 && coinGainParticle != null)
        {
            coinGainParticle.transform.position = currentController.transform.position + Vector3.up;

            // 코인 양에 따라 파티클 수 조절
            short count = (short)Mathf.Clamp(amount, 1, 10);
            ParticleSystem.Burst burst = new ParticleSystem.Burst(0, count, count, amount, particleRepeatInterval / Mathf.Sqrt(count));
            coinGainParticle.emission.SetBurst(0, burst);

            coinGainParticle.Play();
        }
        else if (amount < 0 && coinLossParticle != null)
        {
            coinLossParticle.transform.position = currentController.transform.position + Vector3.up;

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
    public void OnStarsChanged(int amount)
    {
        if (amount > 0)
        {
            // 별 획득 파티클 재생
            // if (starGainParticle != null)
            // {
            //     starGainParticle.transform.position = currentController.transform.position + Vector3.up;
            //     starGainParticle.Play();
            // }
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
        // if (starGainParticle != null)
        // {
        //     starGainParticle.transform.position = position + Vector3.up;
        //     starGainParticle.Play();
        // }
    }

    #endregion
}
