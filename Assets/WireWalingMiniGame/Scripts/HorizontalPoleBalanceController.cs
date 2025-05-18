using UnityEngine;

/// <summary>
/// 물리 기반 가로 균형 막대 컨트롤러 (수정 버전)
/// 가로 막대를 좌우로 이동시켜 균형을 맞추는 줄타기 캐릭터를 제어합니다.
/// 줄과 수직 방향으로 막대가 배치됩니다.
/// </summary>
public class HorizontalPoleBalanceController : MonoBehaviour
{
    [Header("균형 막대 설정")]
    [SerializeField] private Transform horizontalPole; // 가로 막대 트랜스폼
    [SerializeField] private float maxPoleOffset = 1.5f; // 최대 막대 좌우 이동 거리
    [SerializeField] private float poleMovementSpeed = 5.0f; // 막대 이동 속도
    [SerializeField] private float fallThreshold = 40.0f; // 떨어지는 각도 임계값

    [Header("물리 설정")]
    [SerializeField] private Rigidbody characterRigidbody; // 캐릭터 Rigidbody
    [SerializeField] private float balanceForce = 10.0f; // 균형 유지 힘
    [SerializeField] private float counterBalanceMultiplier = 2.0f; // 막대 위치에 따른 균형 보정 계수

    [Header("바람 효과 설정")]
    [SerializeField] private float minWindForce = 1.0f; // 최소 바람 힘
    [SerializeField] private float maxWindForce = 5.0f; // 최대 바람 힘
    [SerializeField] private float minWindDuration = 1.0f; // 최소 바람 지속 시간
    [SerializeField] private float maxWindDuration = 5.0f; // 최대 바람 지속 시간
    [SerializeField] private float windCooldown = 2.0f; // 바람 간 대기 시간
    [SerializeField] private ParticleSystem leftWindParticle; // 왼쪽 바람 파티클
    [SerializeField] private ParticleSystem rightWindParticle; // 오른쪽 바람 파티클

    // 상태 변수
    private float currentPoleOffset = 0f; // 현재 막대 오프셋
    private float targetPoleOffset = 0f; // 목표 막대 오프셋
    private bool hasFallen = false; // 떨어짐 여부
    private bool isActive = true; // 활성화 여부

    // 바람 관련 변수
    private float currentWindForce = 0f; // 현재 바람 힘
    private float windTimer = 0f; // 바람 타이머
    private float cooldownTimer = 0f; // 대기 타이머
    private bool isWindActive = false; // 바람 활성화 여부
    private bool isWindFromLeft = false; // 바람 방향 (true: 왼쪽에서, false: 오른쪽에서)

    // 이벤트
    public delegate void OnFallHandler();
    public event OnFallHandler OnFall;

    private void Start()
    {
        // 초기화
        ResetPole();

        // 파티클 시스템 초기 상태
        if (leftWindParticle) leftWindParticle.Stop();
        if (rightWindParticle) rightWindParticle.Stop();

        // Rigidbody 확인
        if (characterRigidbody == null)
        {
            characterRigidbody = GetComponent<Rigidbody>();
            if (characterRigidbody == null)
            {
                Debug.LogError("Rigidbody가 없습니다. 캐릭터에 Rigidbody 컴포넌트를 추가해주세요.");
            }
        }

        // 막대 방향 설정 (줄과 수직이 되도록)
        if (horizontalPole != null)
        {
            // 막대가 Z축 방향으로 향하도록 회전 (줄은 X축 방향)
            horizontalPole.localRotation = Quaternion.Euler(0, 90, 0);
        }
    }

    private void Update()
    {
        if (!isActive || hasFallen) return;

        // 바람 효과 업데이트
        UpdateWind();

        // 균형 막대 위치 업데이트
        UpdatePolePosition();

        // 떨어짐 검사
        CheckFall();
    }

    private void FixedUpdate()
    {
        if (!isActive || hasFallen) return;

        // 바람 물리 효과 적용
        ApplyWindPhysics();
    }

    /// <summary>
    /// 균형 막대 위치 업데이트
    /// </summary>
    private void UpdatePolePosition()
    {
        // 현재 오프셋을 목표 오프셋으로 부드럽게 보간
        currentPoleOffset = Mathf.Lerp(currentPoleOffset, targetPoleOffset, Time.deltaTime * poleMovementSpeed);

        // 막대 위치 적용 (Z축 방향으로 이동)
        if (horizontalPole != null)
        {
            Vector3 localPos = horizontalPole.localPosition;
            localPos.z = currentPoleOffset;
            horizontalPole.localPosition = localPos;
        }
    }

    /// <summary>
    /// 바람 물리 효과 적용
    /// </summary>
    private void ApplyWindPhysics()
    {
        if (characterRigidbody == null || !isWindActive) return;

        // 바람 방향 결정 (줄 기준 좌우)
        Vector3 windDirection = isWindFromLeft ? Vector3.forward : Vector3.back;

        // 바람 힘 적용
        characterRigidbody.AddForce(windDirection * currentWindForce, ForceMode.Force);

        // 균형 유지 힘 적용 (막대 위치에 따라)
        // 막대가 앞쪽에 있으면 뒤로 기울어지는 힘을 상쇄하고, 반대도 마찬가지
        Vector3 balanceDirection = new Vector3(0, 0, currentPoleOffset * counterBalanceMultiplier);
        characterRigidbody.AddForce(balanceDirection * balanceForce, ForceMode.Force);

        // 추가 균형 유지 토크 적용 (회전 제어)
        float currentTilt = transform.rotation.eulerAngles.x;
        if (currentTilt > 180) currentTilt -= 360; // -180 ~ 180 범위로 변환

        // 기울어진 방향의 반대로 토크 적용
        float counterTorque = -currentTilt * 0.1f;
        characterRigidbody.AddTorque(counterTorque, 0, 0, ForceMode.Force);
    }

    /// <summary>
    /// 바람 효과 업데이트
    /// </summary>
    private void UpdateWind()
    {
        // 대기 시간 중이면 타이머 감소
        if (cooldownTimer > 0)
        {
            cooldownTimer -= Time.deltaTime;

            // 대기 시간이 끝나면 새 바람 생성
            if (cooldownTimer <= 0)
            {
                GenerateNewWind();
            }
            return;
        }

        // 바람이 활성화되어 있으면 타이머 감소
        if (isWindActive)
        {
            windTimer -= Time.deltaTime;

            // 바람 시간이 끝나면 바람 중지
            if (windTimer <= 0)
            {
                StopWind();
                cooldownTimer = windCooldown;
            }
        }
    }

    /// <summary>
    /// 새로운 바람 생성
    /// </summary>
    private void GenerateNewWind()
    {
        // 바람 방향 랜덤 결정 (true: 왼쪽에서, false: 오른쪽에서)
        isWindFromLeft = Random.value > 0.5f;

        // 바람 힘 랜덤 결정
        currentWindForce = Random.Range(minWindForce, maxWindForce);

        // 바람 지속 시간 랜덤 결정
        windTimer = Random.Range(minWindDuration, maxWindDuration);

        // 바람 활성화
        isWindActive = true;

        // 바람 파티클 재생
        PlayWindParticles();

        Debug.Log($"새 바람 생성: 방향={(isWindFromLeft ? "왼쪽에서" : "오른쪽에서")}, 힘={currentWindForce:F1}, 지속시간={windTimer:F1}초");
    }

    /// <summary>
    /// 바람 중지
    /// </summary>
    private void StopWind()
    {
        isWindActive = false;
        currentWindForce = 0f;

        // 파티클 중지
        if (leftWindParticle) leftWindParticle.Stop();
        if (rightWindParticle) rightWindParticle.Stop();

        Debug.Log("바람 중지");
    }

    /// <summary>
    /// 바람 파티클 재생
    /// </summary>
    private void PlayWindParticles()
    {
        // 방향에 따라 파티클 재생
        if (isWindFromLeft)
        {
            if (leftWindParticle)
            {
                // 파티클 속도 조정 (바람 힘에 비례)
                var main = leftWindParticle.main;
                main.startSpeed = currentWindForce * 2f;

                leftWindParticle.Play();
            }
            if (rightWindParticle) rightWindParticle.Stop();
        }
        else
        {
            if (rightWindParticle)
            {
                // 파티클 속도 조정 (바람 힘에 비례)
                var main = rightWindParticle.main;
                main.startSpeed = currentWindForce * 2f;

                rightWindParticle.Play();
            }
            if (leftWindParticle) leftWindParticle.Stop();
        }
    }

    /// <summary>
    /// 떨어짐 검사
    /// </summary>
    private void CheckFall()
    {
        // 기울기가 임계값을 넘으면 떨어짐 (X축 회전 확인)
        float currentTilt = transform.rotation.eulerAngles.x;
        if (currentTilt > 180) currentTilt -= 360; // -180 ~ 180 범위로 변환

        if (Mathf.Abs(currentTilt) > fallThreshold && !hasFallen)
        {
            hasFallen = true;
            Debug.Log("균형을 잃고 떨어졌습니다!");

            // 떨어짐 이벤트 발생
            OnFall?.Invoke();
        }
    }

    /// <summary>
    /// 균형 조절 (외부에서 호출)
    /// </summary>
    /// <param name="balanceInput">균형 입력 (-1 ~ 1)</param>
    public void AdjustBalance(float balanceInput)
    {
        if (!isActive || hasFallen) return;

        // 목표 오프셋 조정
        targetPoleOffset += balanceInput * Time.deltaTime * poleMovementSpeed;

        // 오프셋 제한
        targetPoleOffset = Mathf.Clamp(targetPoleOffset, -maxPoleOffset, maxPoleOffset);
    }

    /// <summary>
    /// 균형 막대 초기화
    /// </summary>
    public void ResetPole()
    {
        currentPoleOffset = 0f;
        targetPoleOffset = 0f;
        hasFallen = false;

        // 막대 위치 초기화
        if (horizontalPole != null)
        {
            Vector3 localPos = horizontalPole.localPosition;
            localPos.z = 0f;
            horizontalPole.localPosition = localPos;

            // 막대가 Z축 방향으로 향하도록 회전 (줄은 X축 방향)
            horizontalPole.localRotation = Quaternion.Euler(0, 90, 0);
        }

        // Rigidbody 초기화
        if (characterRigidbody != null)
        {
            characterRigidbody.linearVelocity = Vector3.zero;
            characterRigidbody.angularVelocity = Vector3.zero;
            transform.rotation = Quaternion.identity;
        }

        // 바람 초기화
        StopWind();
        cooldownTimer = 1f; // 시작 후 1초 후에 첫 바람 생성
    }

    /// <summary>
    /// 활성화 상태 설정
    /// </summary>
    public void SetActive(bool active)
    {
        isActive = active;

        // 비활성화 시 바람 중지
        if (!active)
        {
            StopWind();
        }
    }

    /// <summary>
    /// 떨어짐 여부 반환
    /// </summary>
    public bool HasFallen()
    {
        return hasFallen;
    }

    /// <summary>
    /// 현재 바람 정보 반환 (디버깅 및 ML-Agents 관찰용)
    /// </summary>
    public (bool isActive, float force, bool isFromLeft) GetCurrentWindInfo()
    {
        return (isWindActive, currentWindForce, isWindFromLeft);
    }

    /// <summary>
    /// 현재 막대 오프셋 반환
    /// </summary>
    public float GetCurrentPoleOffset()
    {
        return currentPoleOffset;
    }
}
