using UnityEngine;
using UnityEngine.Splines;
using System.Linq;
using Unity.Mathematics;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// SplineKnotAnimate 클래스 - 스플라인 경로를 따라 캐릭터 이동 관리
/// BoardEvents 기반 이벤트 시스템을 사용하여 이동, 분기점, 노트 관련 이벤트를 처리합니다.
/// </summary>
public class SplineKnotAnimate : MonoBehaviour
{
    [SerializeField] private SplineContainer splineContainer;

    [Header("Movement Parameters")]
    [SerializeField] private float moveSpeed = 10;
    [SerializeField] private float movementLerp = 10;
    [SerializeField] private float rotationLerp = 10;
    private int remainingSteps;
    public int Step => remainingSteps;

    [Header("Knot Logic")]
    public SplineKnotIndex currentKnot;
    public SplineKnotIndex nextKnot;
    private IReadOnlyList<SplineKnotIndex> connectedKnots;

    [Header("Interpolation")]
    private float currentT;

    [Header("Junction Parameters")]
    public int junctionIndex = 0;
    public List<SplineKnotIndex> walkableKnots = new List<SplineKnotIndex>();

    [Header("States")]
    public bool isMoving = false;
    public bool inJunction = false;
    public bool Paused = false;
    [HideInInspector] public bool SkipStepCount = false;

    // 컴포넌트 참조
    private BaseController baseController;

    /// <summary>
    /// 컴포넌트 초기화
    /// </summary>
    public void Initialize()
    {
        // 컴포넌트 참조 획득
        baseController = GetComponent<BaseController>();

        if (splineContainer == null)
        {
            Debug.LogError("Spline Container not assigned!");
            enabled = false; // Disable component if container is missing
            return;
        }

        // 초기 위치 설정
        currentKnot = new SplineKnotIndex(0, 0);
        currentT = 0;
        
        // 스플라인 인덱스 유효성 검사
        if (currentKnot.Spline < splineContainer.Splines.Count)
        {
            Spline spline = splineContainer.Splines[currentKnot.Spline];
            if (spline.Knots.Count() > 1)
            {
                nextKnot = new SplineKnotIndex(currentKnot.Spline, 1);
            }
            else
            {
                // 단일 노트 스플라인 처리
                nextKnot = currentKnot;
            }
        }
        else
        {
            Debug.LogError("Initial spline index is out of bounds!");
            enabled = false;
            return;
        }
    }

    private void Update()
    {
        // 이동 및 회전 처리
        MoveAndRotate();
    }

    /// <summary>
    /// 지정된 스텝만큼 스플라인을 따라 이동 시작
    /// </summary>
    /// <param name="stepAmount">이동할 스텝 수</param>
    public void Animate(int stepAmount = 1)
    {
        if (isMoving)
        {
            Debug.LogWarning("Animate called while already moving.");
            return;
        }
        if (inJunction)
        {
            Debug.LogWarning("Animate called while in junction.");
            return; // 분기점에서는 이동 시작 불가
        }

        remainingSteps = stepAmount;
        StartCoroutine(MoveAlongSpline());
    }

    /// <summary>
    /// 스플라인을 따라 이동하는 코루틴
    /// </summary>
    IEnumerator MoveAlongSpline()
    {
        if (Paused)
            yield return new WaitUntil(() => Paused == false);

        isMoving = true; // 이동 시작

        // 스플라인 인덱스 유효성 검사
        if (currentKnot.Spline >= splineContainer.Splines.Count)
        {
            Debug.LogError($"Current spline index {currentKnot.Spline} is out of bounds!");
            isMoving = false;
            yield break;
        }
        Spline spline = splineContainer.Splines[currentKnot.Spline];

        // 다음 노트 계산, 닫힌 스플라인 처리
        int nextKnotIndex = (currentKnot.Knot + 1);
        bool isLooping = false;
        if (nextKnotIndex >= spline.Knots.Count())
        {
            if (spline.Closed)
            {
                nextKnotIndex = 0;
                isLooping = true;
            }
            else
            {
                Debug.LogWarning("Reached end of open spline.");
                isMoving = false;
                
                // 마지막 노트에 착지 이벤트 발생
                if (baseController != null)
                {
                    BoardEvents.OnKnotLand.Invoke(baseController, currentKnot);
                }
                yield break;
            }
        }
        nextKnot = new SplineKnotIndex(currentKnot.Spline, nextKnotIndex);

        // 목표 T값 계산
        currentT = spline.ConvertIndexUnit(currentKnot.Knot, PathIndexUnit.Knot, PathIndexUnit.Normalized);
        float nextT = isLooping ? 1f : spline.ConvertIndexUnit(nextKnot.Knot, PathIndexUnit.Knot, PathIndexUnit.Normalized);

        // 다음 목적지 이벤트 발생
        if (baseController != null)
        {
            BoardEvents.OnDestinationKnot.Invoke(baseController, nextKnot);
        }

        // nextT를 향해 이동
        while (currentT < nextT)
        {
            // NaN 또는 무한대 값 검사
            float step = AdjustedMovementSpeed(spline) * Time.deltaTime;
            if (float.IsNaN(step) || float.IsInfinity(step) || step <= 0)
            {
                Debug.LogError("Invalid movement step calculated. Breaking movement.");
                isMoving = false;
                yield break; // 무한 루프 방지를 위해 코루틴 종료
            }
            currentT = Mathf.MoveTowards(currentT, nextT, step);
            yield return null;
        }

        // 노트에 도달 (currentT >= nextT)
        currentKnot = nextKnot;
        if (isLooping) currentT = 0; // 닫힌 스플라인에서 루프 시 T 리셋

        // 노트 진입 이벤트 발생
        if (baseController != null)
        {
            BoardEvents.OnKnotEnter.Invoke(baseController, currentKnot);
        }

        // 연결 및 분기점 확인
        splineContainer.KnotLinkCollection.TryGetKnotLinks(currentKnot, out connectedKnots);

        if (IsJunctionKnot(currentKnot)) // 분기점 노트인지 확인
        {
            inJunction = true;
            junctionIndex = 0; // 기본 선택
            isMoving = false; // 이동 중지, 선택 대기
            
            // 분기점 UI/시각 효과 트리거
            if (baseController != null)
            {
                BoardEvents.OnEnterJunction.Invoke(baseController, true);
                BoardEvents.OnJunctionSelection.Invoke(baseController, junctionIndex);
            }
            // 코루틴 종료, 이동 재개는 ConfirmJunctionSelection에서 처리
        }
        else // 분기점이 아닌 노트
        {
            // 해당되는 경우 스텝 감소
            if (!SkipStepCount)
                remainingSteps--;
            else
                SkipStepCount = false;

            // 스플라인 간 전환 처리 (IsLastKnot 로직)
            if (IsLastKnot(currentKnot) && connectedKnots != null)
            {
                bool foundNext = false;
                foreach (SplineKnotIndex connKnot in connectedKnots)
                {
                    // 연결된 노트 유효성 검사
                    if (connKnot.Spline < splineContainer.Splines.Count && connKnot.Knot < splineContainer.Splines[connKnot.Spline].Knots.Count())
                    {
                        if (!IsLastKnot(connKnot)) // 끝 노트가 아닌 연결된 노트 찾기
                        {
                            currentKnot = connKnot; // 새 스플라인/노트로 전환
                            currentT = splineContainer.Splines[currentKnot.Spline].ConvertIndexUnit(currentKnot.Knot, PathIndexUnit.Knot, PathIndexUnit.Normalized);
                            foundNext = true;
                            break;
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Invalid connected knot link: {connKnot}");
                    }
                }
                if (!foundNext)
                {
                    remainingSteps = 0; // 끝에 도달하고 유효한 연결이 없으면 강제 정지
                }
            }

            // 이동 계속 여부 확인
            if (remainingSteps > 0)
            {
                // 분기점이 아니므로 즉시 이동 계속
                StartCoroutine(MoveAlongSpline());
            }
            else // 남은 스텝 없음
            {
                isMoving = false;
                
                // 최종 노트 착지 이벤트 발생
                if (baseController != null)
                {
                    BoardEvents.OnKnotLand.Invoke(baseController, currentKnot);
                }
            }
        }
    }

    /// <summary>
    /// 분기점 선택 확인 및 이동 재개
    /// </summary>
    public void ConfirmJunctionSelection()
    {
        if (!inJunction) return;

        inJunction = false;
        
        // 분기점 UI/시각 효과 숨김
        if (baseController != null)
        {
            BoardEvents.OnEnterJunction.Invoke(baseController, false);
        }
        
        // 선택된 경로 적용
        SelectJunctionPath(junctionIndex);

        // 이동 재개 여부 확인
        if (remainingSteps > 0)
        {
            if (!isMoving) // 이미 이동 중이 아닌지 확인
            {
                // 시스템 업데이트를 위한 짧은 지연 후 이동 재개
                StartCoroutine(ResumeMoveAfterDelay(0.05f));
            }
            else
            {
                Debug.LogWarning("ConfirmJunctionSelection called but already moving?");
            }
        }
        else // 분기점 직후 남은 스텝 없음
        {
            isMoving = false;
            
            // 최종 노트 착지 이벤트 발생
            if (baseController != null)
            {
                BoardEvents.OnKnotLand.Invoke(baseController, currentKnot);
            }
        }
    }

    /// <summary>
    /// 지연 후 이동 재개 코루틴
    /// </summary>
    private IEnumerator ResumeMoveAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (!isMoving && remainingSteps > 0) // 상태 재확인
        {
            StartCoroutine(MoveAlongSpline()); // 이동 재개
        }
    }

    /// <summary>
    /// 분기점 경로 선택 및 업데이트
    /// </summary>
    public void SelectJunctionPath(int index)
    {
        if (walkableKnots == null || walkableKnots.Count == 0)
        {
            Debug.LogError("SelectJunctionPath called with no walkable knots available.");
            return;
        }
        if (index < 0 || index >= walkableKnots.Count)
        {
            Debug.LogError($"Invalid junction index {index} selected. Clamping to 0.");
            index = 0; // 유효한 인덱스로 제한
        }

        SplineKnotIndex selectedKnot = walkableKnots[index];

        // 선택된 노트 인덱스 유효성 검사
        if (selectedKnot.Spline >= splineContainer.Splines.Count || selectedKnot.Knot >= splineContainer.Splines[selectedKnot.Spline].Knots.Count())
        {
            Debug.LogError($"Selected walkable knot {selectedKnot} is invalid!");
            return;
        }

        currentKnot = selectedKnot; // 현재 노트를 선택된 경로의 시작으로 업데이트

        // 새 스플라인/노트 기반으로 nextKnot 및 currentT 업데이트
        Spline spline = splineContainer.Splines[currentKnot.Spline];
        int nextKnotIndex = (currentKnot.Knot + 1);
        if (nextKnotIndex >= spline.Knots.Count())
        {
            if (spline.Closed)
            {
                nextKnotIndex = 0;
            }
            else
            {
                Debug.LogWarning("Selected path leads immediately to the end of an open spline.");
                // 끝이면 선택된 노트에 착지
                nextKnot = currentKnot;
                currentT = spline.ConvertIndexUnit(currentKnot.Knot, PathIndexUnit.Knot, PathIndexUnit.Normalized);
                walkableKnots.Clear();
                return;
            }
        }
        nextKnot = new SplineKnotIndex(currentKnot.Spline, nextKnotIndex);
        currentT = spline.ConvertIndexUnit(currentKnot.Knot, PathIndexUnit.Knot, PathIndexUnit.Normalized);

        walkableKnots.Clear(); // 선택 후 옵션 정리
    }

    /// <summary>
    /// 분기점 선택 인덱스 변경
    /// </summary>
    public void AddToJunctionIndex(int amount)
    {
        if (!inJunction || walkableKnots == null || walkableKnots.Count == 0)
            return;
        junctionIndex = (int)Mathf.Repeat(junctionIndex + amount, walkableKnots.Count);
        
        // 분기점 선택 이벤트 발생
        if (baseController != null)
        {
            BoardEvents.OnJunctionSelection.Invoke(baseController, junctionIndex);
        }
    }

    /// <summary>
    /// 분기점 경로 위치 계산 (시각 효과용)
    /// </summary>
    public Vector3 GetJunctionPathPosition(int index)
    {
        if (walkableKnots == null || walkableKnots.Count <= index || index < 0)
            return transform.position; // 유효하지 않으면 현재 위치 반환

        SplineKnotIndex walkableKnotIndex = walkableKnots[index];

        // 인덱스 유효성 검사
        if (walkableKnotIndex.Spline >= splineContainer.Splines.Count)
            return transform.position;
        Spline walkableSpline = splineContainer.Splines[walkableKnotIndex.Spline];
        if (walkableKnotIndex.Knot >= walkableSpline.Knots.Count())
            return transform.position;

        // 선택된 경로의 *다음* 노트 위치 (방향용)
        int nextWalkableKnotNum = (walkableKnotIndex.Knot + 1);
        if (nextWalkableKnotNum >= walkableSpline.Knots.Count())
        {
            if (walkableSpline.Closed)
                nextWalkableKnotNum = 0;
            else
                // 경로가 즉시 끝나면 현재 위치에서 노트 자체를 향해 지정
                return (Vector3)splineContainer.EvaluatePosition(walkableKnotIndex.Spline, splineContainer.Splines[walkableKnotIndex.Spline].ConvertIndexUnit(walkableKnotIndex.Knot, PathIndexUnit.Knot, PathIndexUnit.Normalized));
        }

        SplineKnotIndex nextWalkableKnotIndex = new SplineKnotIndex(walkableKnotIndex.Spline, nextWalkableKnotNum);
        // 더 나은 방향을 위해 경로를 따라 약간 앞쪽 위치 평가
        float targetT = walkableSpline.ConvertIndexUnit(walkableKnotIndex.Knot, PathIndexUnit.Knot, PathIndexUnit.Normalized);
        float nextTargetT = walkableSpline.ConvertIndexUnit(nextWalkableKnotIndex.Knot, PathIndexUnit.Knot, PathIndexUnit.Normalized);
        if (nextTargetT < targetT) nextTargetT = 1f; // 루프 랩 처리
        float sampleT = Mathf.Lerp(targetT, nextTargetT, 0.1f); // 세그먼트의 10% 지점 샘플링

        return (Vector3)splineContainer.EvaluatePosition(walkableKnotIndex.Spline, sampleT);
    }

    /// <summary>
    /// 노트가 분기점인지 확인
    /// </summary>
    bool IsJunctionKnot(SplineKnotIndex knotIndex)
    {
        walkableKnots.Clear();

        if (connectedKnots == null || connectedKnots.Count == 0)
            return false;

        int divergingPaths = 0;
        foreach (SplineKnotIndex connectedKnot in connectedKnots)
        {
            // 연결된 노트 유효성 검사
            if (connectedKnot.Spline < splineContainer.Splines.Count && 
                connectedKnot.Knot < splineContainer.Splines[connectedKnot.Spline].Knots.Count())
            {
                // 현재 노트가 끝 노트이고 연결된 노트가 시작 노트인 경우
                if (IsLastKnot(knotIndex) && IsFirstKnot(connectedKnot))
                {
                    walkableKnots.Add(connectedKnot);
                    divergingPaths++;
                }
                // 현재 노트가 시작 노트이고 연결된 노트가 끝 노트인 경우
                else if (IsFirstKnot(knotIndex) && IsLastKnot(connectedKnot))
                {
                    walkableKnots.Add(connectedKnot);
                    divergingPaths++;
                }
            }
        }

        // 현재 스플라인의 다음 노트도 경로 옵션에 추가
        if (!IsLastKnot(knotIndex))
        {
            int nextKnotIndex = knotIndex.Knot + 1;
            if (nextKnotIndex < splineContainer.Splines[knotIndex.Spline].Knots.Count())
            {
                SplineKnotIndex nextKnot = new SplineKnotIndex(knotIndex.Spline, nextKnotIndex);
                walkableKnots.Add(nextKnot);
                divergingPaths++;
            }
        }

        // 현재 스플라인의 이전 노트도 경로 옵션에 추가 (닫힌 스플라인 또는 첫 노트가 아닌 경우)
        Spline currentSpline = splineContainer.Splines[knotIndex.Spline];
        if (currentSpline.Closed || knotIndex.Knot > 0)
        {
            int prevKnotIndex = knotIndex.Knot - 1;
            if (prevKnotIndex < 0) prevKnotIndex = currentSpline.Knots.Count() - 1; // 닫힌 스플라인에서 랩
            
            if (prevKnotIndex >= 0 && prevKnotIndex < currentSpline.Knots.Count())
            {
                SplineKnotIndex prevKnot = new SplineKnotIndex(knotIndex.Spline, prevKnotIndex);
                
                // 이미 추가된 노트와 중복되지 않는지 확인
                bool alreadyAdded = false;
                foreach (SplineKnotIndex knot in walkableKnots)
                {
                    if (knot.Spline == prevKnot.Spline && knot.Knot == prevKnot.Knot)
                    {
                        alreadyAdded = true;
                        break;
                    }
                }
                
                if (!alreadyAdded)
                {
                    walkableKnots.Add(prevKnot);
                    divergingPaths++;
                }
            }
        }

        // 2개 이상의 경로 옵션이 있으면 분기점으로 간주
        return divergingPaths >= 2;
    }

    /// <summary>
    /// 노트가 스플라인의 첫 번째 노트인지 확인
    /// </summary>
    bool IsFirstKnot(SplineKnotIndex knotIndex)
    {
        return knotIndex.Knot == 0;
    }

    /// <summary>
    /// 노트가 스플라인의 마지막 노트인지 확인
    /// </summary>
    bool IsLastKnot(SplineKnotIndex knotIndex)
    {
        if (knotIndex.Spline >= splineContainer.Splines.Count)
            return false;
            
        Spline spline = splineContainer.Splines[knotIndex.Spline];
        return knotIndex.Knot == spline.Knots.Count() - 1;
    }

    /// <summary>
    /// 스플라인 길이에 따른 이동 속도 조정
    /// </summary>
    float AdjustedMovementSpeed(Spline spline)
    {
        if (spline == null) return moveSpeed;
        
        // 스플라인 길이에 따라 속도 조정
        float splineLength = spline.GetLength();
        if (splineLength <= 0) return moveSpeed;
        
        // 기본 속도를 스플라인 길이로 정규화
        return moveSpeed / splineLength;
    }

    /// <summary>
    /// 캐릭터 이동 및 회전 처리
    /// </summary>
    void MoveAndRotate()
    {
        if (splineContainer == null) return;
        
        // 현재 스플라인 인덱스 유효성 검사
        if (currentKnot.Spline >= splineContainer.Splines.Count) return;
        
        // 현재 위치 계산
        Vector3 targetPosition = (Vector3)splineContainer.EvaluatePosition(currentKnot.Spline, currentT);
        
        // 위치 오프셋 적용 (필요시)
        // targetPosition += positionOffset;
        
        // 부드러운 이동
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * movementLerp);
        
        // 이동 방향 계산
        if (isMoving)
        {
            // 다음 위치 계산
            float nextT = Mathf.Min(currentT + 0.01f, 1f);
            Vector3 nextPosition = (Vector3)splineContainer.EvaluatePosition(currentKnot.Spline, nextT);
            
            // 이동 방향 계산
            Vector3 moveDirection = nextPosition - transform.position;
            
            // 수평 방향만 고려
            moveDirection.y = 0;
            
            // 방향이 유효한 경우에만 회전
            if (moveDirection.sqrMagnitude > 0.001f)
            {
                // 목표 회전 계산
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                
                // 부드러운 회전
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationLerp);
            }
        }
    }

    /// <summary>
    /// 현재 노트 인덱스 반환
    /// </summary>
    public SplineKnotIndex CurrentKnot => currentKnot;
}
