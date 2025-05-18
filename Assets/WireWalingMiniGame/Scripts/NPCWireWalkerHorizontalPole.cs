using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

/// <summary>
/// ML-Agents를 활용한 NPC 줄타기 AI 컨트롤러 (수정 버전)
/// 가로 막대를 좌우로 이동시켜 균형을 맞추는 학습 에이전트
/// 줄은 X축 방향, 균형 막대는 Z축 방향으로 배치됩니다.
/// </summary>
public class NPCWireWalkerHorizontalPole : Agent
{
    [Header("줄타기 설정")]
    [SerializeField] private float moveSpeed = 2.0f;
    [SerializeField] private Transform wireStart;
    [SerializeField] private Transform wireEnd;
    [SerializeField] private float wireLength = 20f;
    
    [Header("균형 막대 설정")]
    [SerializeField] private HorizontalPoleBalanceController poleController;
    
    // 상태 변수
    private bool isActive = false;
    private bool isMoving = false;
    private bool hasFallen = false;
    private bool hasReachedEnd = false;
    private float currentDistance = 0f;
    private float lastDistance = 0f;
    
    // 에피소드 정보
    private int episodeSteps = 0;
    private const int MaxEpisodeSteps = 1000;
    
    // 디버깅 정보
    private string _debugText;
    private Vector3 _debugPosition;
    
    private void Start()
    {
        // 균형 막대 컨트롤러 이벤트 구독
        if (poleController != null)
        {
            poleController.OnFall += OnPoleFall;
        }
        
        // 초기 위치 설정
        ResetPosition();
    }
    
    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (poleController != null)
        {
            poleController.OnFall -= OnPoleFall;
        }
    }
    
    /// <summary>
    /// 막대가 떨어졌을 때 호출되는 이벤트 핸들러
    /// </summary>
    private void OnPoleFall()
    {
        hasFallen = true;
    }
    
    /// <summary>
    /// 에이전트 초기화 (ML-Agents)
    /// </summary>
    public override void OnEpisodeBegin()
    {
        // 위치 및 상태 초기화
        ResetPosition();
        
        // 균형 막대 초기화
        if (poleController != null)
        {
            poleController.ResetPole();
        }
        
        // 상태 변수 초기화
        hasFallen = false;
        hasReachedEnd = false;
        currentDistance = 0f;
        lastDistance = 0f;
        episodeSteps = 0;
        
        // 활성화
        isActive = true;
        if (poleController != null)
        {
            poleController.SetActive(true);
        }
    }
    
    /// <summary>
    /// 환경 관찰 수집 (ML-Agents)
    /// </summary>
    public override void CollectObservations(VectorSensor sensor)
    {
        // 현재 위치 정보 (정규화된 진행 거리)
        sensor.AddObservation(currentDistance / wireLength);
        
        // 현재 회전 상태 (x축 기울기 - 줄 방향으로 기울어짐)
        float currentTilt = transform.rotation.eulerAngles.x;
        if (currentTilt > 180) currentTilt -= 360; // -180 ~ 180 범위로 변환
        sensor.AddObservation(currentTilt / 45f); // -1 ~ 1 범위로 정규화
        
        // 현재 막대 오프셋 (Z축 방향)
        float poleOffset = poleController != null ? poleController.GetCurrentPoleOffset() : 0f;
        sensor.AddObservation(poleOffset / 1.5f); // -1 ~ 1 범위로 정규화 (maxPoleOffset 기준)
        
        // 바람 정보
        var windInfo = poleController != null ? poleController.GetCurrentWindInfo() : (false, 0f, false);
        sensor.AddObservation(windInfo.Item1 ? 1f : 0f); // 바람 활성화 여부
        sensor.AddObservation(windInfo.Item2 / 5f); // 바람 힘 (0 ~ 1 범위로 정규화)
        sensor.AddObservation(windInfo.Item3 ? 1f : 0f); // 바람 방향
        
        // 떨어짐 여부
        sensor.AddObservation(hasFallen ? 1f : 0f);
        
        // 도착 여부
        sensor.AddObservation(hasReachedEnd ? 1f : 0f);
    }
    
    /// <summary>
    /// 행동 수행 (ML-Agents)
    /// </summary>
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        if (!isActive) return;
        
        // 에피소드 단계 증가
        episodeSteps++;
        
        // 행동 값 가져오기
        float moveAction = actionBuffers.ContinuousActions[0]; // -1 ~ 1
        float balanceAction = actionBuffers.ContinuousActions[1]; // -1 ~ 1
        
        // 이동 여부 결정 (0.5 이상이면 이동)
        isMoving = moveAction > 0.5f;
        
        // 균형 조절
        if (poleController != null)
        {
            poleController.AdjustBalance(balanceAction);
        }
        
        // 이동 처리
        MoveForward();
        
        // 보상 계산
        CalculateReward();
        
        // 에피소드 종료 조건 확인
        CheckEpisodeEnd();
        
        // 디버깅 정보 업데이트
        UpdateDebugInfo();
    }
    
    /// <summary>
    /// 휴리스틱 행동 (테스트용)
    /// </summary>
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        
        // 전진 입력 (W 키)
        continuousActionsOut[0] = Input.GetKey(KeyCode.W) ? 1.0f : 0.0f;
        
        // 균형 조절 입력 (A, D 키)
        float balanceInput = 0f;
        if (Input.GetKey(KeyCode.A)) balanceInput -= 1f;
        if (Input.GetKey(KeyCode.D)) balanceInput += 1f;
        continuousActionsOut[1] = balanceInput;
    }
    
    private void Update()
    {
        if (!isActive) return;
        
        // 떨어짐 상태 확인
        if (poleController != null && poleController.HasFallen())
        {
            hasFallen = true;
        }
        
        // 디버깅 정보 업데이트
        UpdateDebugInfo();
    }
    
    /// <summary>
    /// 전진 이동 처리
    /// </summary>
    private void MoveForward()
    {
        if (isMoving && !hasFallen && !hasReachedEnd)
        {
            // 줄을 따라 전진
            currentDistance += moveSpeed * Time.deltaTime;
            
            // 위치 업데이트 (X축 방향으로 이동)
            float t = currentDistance / wireLength;
            transform.position = Vector3.Lerp(wireStart.position, wireEnd.position, t);
            
            // 도착 확인
            if (currentDistance >= wireLength)
            {
                hasReachedEnd = true;
                Debug.Log($"{gameObject.name}이(가) 줄 끝에 도달했습니다!");
            }
        }
    }
    
    /// <summary>
    /// 위치 초기화
    /// </summary>
    public void ResetPosition()
    {
        if (wireStart != null)
        {
            transform.position = wireStart.position;
        }
        transform.rotation = Quaternion.identity;
        currentDistance = 0f;
    }
    
    /// <summary>
    /// 보상 계산
    /// </summary>
    private void CalculateReward()
    {
        // 기본 보상 (생존 보상)
        AddReward(0.01f);
        
        // 진행 보상 (앞으로 나아갈수록 보상)
        float distanceDelta = currentDistance - lastDistance;
        if (distanceDelta > 0)
        {
            AddReward(0.1f * distanceDelta);
        }
        lastDistance = currentDistance;
        
        // 균형 유지 보상 (기울기가 작을수록 보상)
        float currentTilt = transform.rotation.eulerAngles.x;
        if (currentTilt > 180) currentTilt -= 360; // -180 ~ 180 범위로 변환
        float balanceReward = 1.0f - Mathf.Abs(currentTilt) / 45.0f; // 0 ~ 1 범위
        AddReward(0.05f * balanceReward);
        
        // 바람에 대응하는 보상
        if (poleController != null)
        {
            var windInfo = poleController.GetCurrentWindInfo();
            if (windInfo.isActive)
            {
                // 바람이 있을 때 균형을 유지하면 추가 보상
                if (Mathf.Abs(currentTilt) < 20.0f) // 바람이 있어도 균형을 잘 유지하고 있다면
                {
                    AddReward(0.05f * windInfo.force); // 바람 힘에 비례한 보상
                }
            }
        }
        
        // 떨어짐 패널티
        if (hasFallen)
        {
            AddReward(-1.0f);
            EndEpisode();
        }
        
        // 도착 보상
        if (hasReachedEnd)
        {
            AddReward(10.0f);
            EndEpisode();
        }
    }
    
    /// <summary>
    /// 에피소드 종료 조건 확인
    /// </summary>
    private void CheckEpisodeEnd()
    {
        // 최대 스텝 수 초과 시 종료
        if (episodeSteps >= MaxEpisodeSteps)
        {
            EndEpisode();
        }
    }
    
    /// <summary>
    /// 활성화 상태 설정
    /// </summary>
    public void SetActive(bool active)
    {
        isActive = active;
        
        if (poleController != null)
        {
            poleController.SetActive(active);
        }
    }
    
    /// <summary>
    /// 줄 끝에 도달했는지 확인
    /// </summary>
    public bool HasReachedEnd()
    {
        return hasReachedEnd;
    }
    
    /// <summary>
    /// 떨어졌는지 확인
    /// </summary>
    public bool HasFallen()
    {
        return hasFallen;
    }
    
    /// <summary>
    /// 디버깅 정보 업데이트
    /// </summary>
    private void UpdateDebugInfo()
    {
        // 현재 보상 값 표시
        float currentReward = GetCumulativeReward();
        
        // 바람 정보 가져오기
        var windInfo = poleController != null ? poleController.GetCurrentWindInfo() : (false, 0f, false);
        
        // 텍스트 정보 생성
        string debugText = $"NPC: {gameObject.name}\n" +
                          $"보상: {currentReward:F2}\n" +
                          $"진행: {(currentDistance / wireLength * 100):F0}%\n" +
                          $"바람: {(windInfo.Item1 ? "활성" : "비활성")}\n" +
                          $"방향: {(windInfo.Item3 ? "왼쪽" : "오른쪽")}\n" +
                          $"힘: {windInfo.Item2:F1}";
        
        // 월드 스페이스에 텍스트 표시 위치
        Vector3 textPosition = transform.position + Vector3.up * 2.5f;
        
        // 디버깅 정보 저장
        _debugText = debugText;
        _debugPosition = textPosition;
    }
    
    /// <summary>
    /// GUI에 디버깅 정보 표시
    /// </summary>
    private void OnGUI()
    {
        if (string.IsNullOrEmpty(_debugText) || !isActive) return;
        
        // 월드 좌표를 스크린 좌표로 변환
        if (Camera.main == null) return;
        Vector3 screenPos = Camera.main.WorldToScreenPoint(_debugPosition);
        
        // 화면에 보이는 경우에만 텍스트 표시
        if (screenPos.z > 0)
        {
            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.black;
            style.fontSize = 14;
            style.alignment = TextAnchor.UpperCenter;
            style.fontStyle = FontStyle.Bold;
            
            // 배경 그리기
            Rect rect = new Rect(screenPos.x - 100, Screen.height - screenPos.y - 100, 200, 100);
            GUI.Box(rect, "");
            
            // 텍스트 그리기
            GUI.Label(rect, _debugText, style);
        }
    }
}
