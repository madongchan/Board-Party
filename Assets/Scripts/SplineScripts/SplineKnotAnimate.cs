using UnityEngine;
using UnityEngine.Splines;
using System.Linq;
using Unity.Mathematics;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Events;

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

    // Removed resumeAfterJunction flag

    [Header("Events")]
    [HideInInspector] public UnityEvent<bool> OnEnterJunction;
    [HideInInspector] public UnityEvent<int> OnJunctionSelection;
    [HideInInspector] public UnityEvent<SplineKnotIndex> OnDestinationKnot;
    [HideInInspector] public UnityEvent<SplineKnotIndex> OnKnotEnter;
    [HideInInspector] public UnityEvent<SplineKnotIndex> OnKnotLand;

    void Start()
    {
        if (splineContainer == null)
        {
            Debug.LogError("Spline Container not assigned!");
            enabled = false; // Disable component if container is missing
            return;
        }

        // Initialize position at first knot
        currentKnot = new SplineKnotIndex(0, 0);
        currentT = 0;
        // Ensure spline index is valid before accessing
        if (currentKnot.Spline < splineContainer.Splines.Count)
        {
            Spline spline = splineContainer.Splines[currentKnot.Spline];
            if (spline.Knots.Count() > 1)
            {
                nextKnot = new SplineKnotIndex(currentKnot.Spline, 1);
            }
            else
            {
                // Handle single-knot spline case if necessary
                nextKnot = currentKnot;
            }
        }
        else
        {
            Debug.LogError("Initial spline index is out of bounds!");
            enabled = false;
            return;
        }

        // Register events with managers (with null checks)
        if (VisualEffectsManager.Instance != null)
        {
            OnEnterJunction.AddListener(VisualEffectsManager.Instance.OnEnterJunction);
            OnJunctionSelection.AddListener(VisualEffectsManager.Instance.OnJunctionSelection);
            OnKnotEnter.AddListener(VisualEffectsManager.Instance.OnKnotEnter);
            OnKnotLand.AddListener(VisualEffectsManager.Instance.OnKnotLand);
        }

        if (UIManager.Instance != null)
        {
            OnEnterJunction.AddListener(UIManager.Instance.OnEnterJunction);
        }
    }

    private void OnDestroy()
    {
        // Unregister events (with null checks)
        if (VisualEffectsManager.Instance != null)
        {
            OnEnterJunction.RemoveListener(VisualEffectsManager.Instance.OnEnterJunction);
            OnJunctionSelection.RemoveListener(VisualEffectsManager.Instance.OnJunctionSelection);
            OnKnotEnter.RemoveListener(VisualEffectsManager.Instance.OnKnotEnter);
            OnKnotLand.RemoveListener(VisualEffectsManager.Instance.OnKnotLand);
        }

        if (UIManager.Instance != null)
        {
            OnEnterJunction.RemoveListener(UIManager.Instance.OnEnterJunction);
        }
    }

    private void Update()
    {
        // Only call MoveAndRotate if needed (e.g., if moving or interpolating)
        // This check might be optional depending on performance needs
        // if (isMoving || transform.position != (Vector3)splineContainer.EvaluatePosition(currentKnot.Spline, currentT))
        // {
        MoveAndRotate();
        // }
        // Removed Update logic for restarting movement after junction
    }

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
            return; // Don't start animating if waiting at a junction
        }

        remainingSteps = stepAmount;
        StartCoroutine(MoveAlongSpline());
    }

    IEnumerator MoveAlongSpline()
    {
        // Removed junction check at the start

        if (Paused)
            yield return new WaitUntil(() => Paused == false);

        isMoving = true; // Start moving

        // Ensure spline index is valid
        if (currentKnot.Spline >= splineContainer.Splines.Count)
        {
            Debug.LogError($"Current spline index {currentKnot.Spline} is out of bounds!");
            isMoving = false;
            yield break;
        }
        Spline spline = splineContainer.Splines[currentKnot.Spline];

        // Calculate next knot, handle closed splines correctly
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
                OnKnotLand.Invoke(currentKnot); // Land at the last knot
                yield break;
            }
        }
        nextKnot = new SplineKnotIndex(currentKnot.Spline, nextKnotIndex);

        // Calculate target T value
        currentT = spline.ConvertIndexUnit(currentKnot.Knot, PathIndexUnit.Knot, PathIndexUnit.Normalized);
        float nextT = isLooping ? 1f : spline.ConvertIndexUnit(nextKnot.Knot, PathIndexUnit.Knot, PathIndexUnit.Normalized);

        OnDestinationKnot.Invoke(nextKnot); // Event for next destination

        // Move towards nextT
        while (currentT < nextT)
        {
            // Check for potential issues like NaN or infinite values
            float step = AdjustedMovementSpeed(spline) * Time.deltaTime;
            if (float.IsNaN(step) || float.IsInfinity(step) || step <= 0)
            {
                Debug.LogError("Invalid movement step calculated. Breaking movement.");
                isMoving = false;
                yield break; // Exit coroutine to prevent infinite loop
            }
            currentT = Mathf.MoveTowards(currentT, nextT, step);
            yield return null;
        }

        // Reached the knot (currentT >= nextT)
        currentKnot = nextKnot;
        if (isLooping) currentT = 0; // Reset T if looped on closed spline

        OnKnotEnter.Invoke(currentKnot); // Event for entering the knot

        // Check for connections and junctions
        splineContainer.KnotLinkCollection.TryGetKnotLinks(currentKnot, out connectedKnots);

        if (IsJunctionKnot(currentKnot)) // Check if the arrived knot is a junction
        {
            inJunction = true;
            junctionIndex = 0; // Default selection
            isMoving = false; // Stop movement, wait for selection
            OnEnterJunction.Invoke(true); // Trigger junction UI/visuals
            OnJunctionSelection.Invoke(junctionIndex); // Update visuals for default selection
            // Coroutine ends here, movement restart handled by ConfirmJunctionSelection
        }
        else // Not a junction knot
        {
            // Decrement steps if applicable
            if (!SkipStepCount)
                remainingSteps--;
            else
                SkipStepCount = false;

            // Handle transitions between splines if necessary (IsLastKnot logic)
            if (IsLastKnot(currentKnot) && connectedKnots != null)
            {
                bool foundNext = false;
                foreach (SplineKnotIndex connKnot in connectedKnots)
                {
                    // Ensure connected knot is valid
                    if (connKnot.Spline < splineContainer.Splines.Count && connKnot.Knot < splineContainer.Splines[connKnot.Spline].Knots.Count())
                    {
                        if (!IsLastKnot(connKnot)) // Find a connected knot that isn't an end knot
                        {
                            currentKnot = connKnot; // Switch to the new spline/knot
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
                    remainingSteps = 0; // Force stop if at end and no valid continuation found
                }
            }

            // Check if movement should continue
            if (remainingSteps > 0)
            {
                // Continue moving immediately since it's not a junction
                StartCoroutine(MoveAlongSpline());
            }
            else // No steps remaining
            {
                isMoving = false;
                OnKnotLand.Invoke(currentKnot); // Landed on the final knot
            }
        }
    }

    // Centralized method to handle junction confirmation and movement restart
    public void ConfirmJunctionSelection()
    {
        if (!inJunction) return;

        inJunction = false;
        OnEnterJunction.Invoke(false); // Hide junction UI/visuals
        SelectJunctionPath(junctionIndex); // Apply the selected path (updates currentKnot, nextKnot, currentT)

        // Check if movement should resume
        if (remainingSteps > 0)
        {
            if (!isMoving) // Ensure we are not somehow already moving
            {
                // Add a small delay before restarting movement to allow systems to update
                StartCoroutine(ResumeMoveAfterDelay(0.05f));
            }
            else
            {
                Debug.LogWarning("ConfirmJunctionSelection called but already moving?");
            }
        }
        else // No steps remaining after junction (e.g., landed exactly on junction)
        {
            isMoving = false;
            OnKnotLand.Invoke(currentKnot);
        }
    }

    private IEnumerator ResumeMoveAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (!isMoving && remainingSteps > 0) // Double check state before starting
        {
            StartCoroutine(MoveAlongSpline()); // Resume movement
        }
    }

    // SelectJunctionPath: Updates currentKnot, nextKnot, currentT based on index
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
            index = 0; // Clamp to valid index
        }

        SplineKnotIndex selectedKnot = walkableKnots[index];

        // Validate selected knot index before using
        if (selectedKnot.Spline >= splineContainer.Splines.Count || selectedKnot.Knot >= splineContainer.Splines[selectedKnot.Spline].Knots.Count())
        {
            Debug.LogError($"Selected walkable knot {selectedKnot} is invalid!");
            return;
        }

        currentKnot = selectedKnot; // Update current knot to the start of the selected path

        // Update nextKnot and currentT based on the new spline/knot
        Spline spline = splineContainer.Splines[currentKnot.Spline];
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
                Debug.LogWarning("Selected path leads immediately to the end of an open spline.");
                // Land at the selected knot if it's the end
                nextKnot = currentKnot;
                currentT = spline.ConvertIndexUnit(currentKnot.Knot, PathIndexUnit.Knot, PathIndexUnit.Normalized);
                walkableKnots.Clear();
                // Consider landing immediately if remainingSteps was 1?
                // For now, let ConfirmJunctionSelection handle landing if remainingSteps is 0.
                return;
            }
        }
        nextKnot = new SplineKnotIndex(currentKnot.Spline, nextKnotIndex);
        currentT = spline.ConvertIndexUnit(currentKnot.Knot, PathIndexUnit.Knot, PathIndexUnit.Normalized);

        walkableKnots.Clear(); // Clear options after selection
    }

    // AddToJunctionIndex: Updates selection index and invokes event
    public void AddToJunctionIndex(int amount)
    {
        if (!inJunction || walkableKnots == null || walkableKnots.Count == 0)
            return;
        junctionIndex = (int)Mathf.Repeat(junctionIndex + amount, walkableKnots.Count);
        OnJunctionSelection.Invoke(junctionIndex);
    }

    // GetJunctionPathPosition: Calculates position for visuals
    public Vector3 GetJunctionPathPosition(int index)
    {
        if (walkableKnots == null || walkableKnots.Count <= index || index < 0)
            return transform.position; // Return current position if invalid

        SplineKnotIndex walkableKnotIndex = walkableKnots[index];

        // Validate index
        if (walkableKnotIndex.Spline >= splineContainer.Splines.Count)
            return transform.position;
        Spline walkableSpline = splineContainer.Splines[walkableKnotIndex.Spline];
        if (walkableKnotIndex.Knot >= walkableSpline.Knots.Count())
            return transform.position;

        // Get position of the *next* knot on the selected path for direction
        int nextWalkableKnotNum = (walkableKnotIndex.Knot + 1);
        if (nextWalkableKnotNum >= walkableSpline.Knots.Count())
        {
            if (walkableSpline.Closed)
                nextWalkableKnotNum = 0;
            else
                // If path ends immediately, point towards the knot itself from current pos?
                return (Vector3)splineContainer.EvaluatePosition(walkableKnotIndex.Spline, splineContainer.Splines[walkableKnotIndex.Spline].ConvertIndexUnit(walkableKnotIndex.Knot, PathIndexUnit.Knot, PathIndexUnit.Normalized));
        }

        SplineKnotIndex nextWalkableKnotIndex = new SplineKnotIndex(walkableKnotIndex.Spline, nextWalkableKnotNum);
        // Evaluate position slightly along the path for better direction
        float targetT = walkableSpline.ConvertIndexUnit(walkableKnotIndex.Knot, PathIndexUnit.Knot, PathIndexUnit.Normalized);
        float nextTargetT = walkableSpline.ConvertIndexUnit(nextWalkableKnotIndex.Knot, PathIndexUnit.Knot, PathIndexUnit.Normalized);
        if (nextTargetT < targetT) nextTargetT = 1f; // Handle loop wrap
        float sampleT = Mathf.Lerp(targetT, nextTargetT, 0.1f); // Sample 10% along the segment

        return (Vector3)splineContainer.EvaluatePosition(walkableKnotIndex.Spline, sampleT);
    }

    // IsJunctionKnot: Determines if a knot is a junction
    bool IsJunctionKnot(SplineKnotIndex knotIndex)
    {
        walkableKnots.Clear();

        if (connectedKnots == null || connectedKnots.Count == 0)
            return false;

        int divergingPaths = 0;

        // Check each connected spline
        foreach (SplineKnotIndex connection in connectedKnots)
        {
            // Validate connection index
            if (connection.Spline >= splineContainer.Splines.Count)
            {
                Debug.LogWarning($"Invalid connection spline index {connection.Spline}");
                continue;
            }
            var spline = splineContainer.Splines[connection.Spline];
            if (connection.Knot >= spline.Knots.Count())
            {
                Debug.LogWarning($"Invalid connection knot index {connection.Knot} on spline {connection.Spline}");
                continue;
            }

            if (!IsLastKnot(connection))
            {
                divergingPaths++;
                walkableKnots.Add(connection);
            }
        }

        // Sort walkableKnots by spline index number (optional, but consistent)
        walkableKnots.Sort((knot1, knot2) => knot1.Spline.CompareTo(knot2.Spline));

        if (divergingPaths <= 1)
        {
            walkableKnots.Clear(); // Not a junction if 0 or 1 path forward
            return false;
        }

        return true; // It's a junction if more than one path forward
    }

    // IsLastKnot: Checks if a knot is the last on an open spline
    bool IsLastKnot(SplineKnotIndex knotIndex)
    {
        // Validate index
        if (knotIndex.Spline >= splineContainer.Splines.Count)
            return true; // Treat invalid index as end
        var spline = splineContainer.Splines[knotIndex.Spline];
        if (knotIndex.Knot >= spline.Knots.Count())
            return true; // Treat invalid index as end

        return knotIndex.Knot >= spline.Knots.Count() - 1 && !spline.Closed;
    }

    // AdjustedMovementSpeed: Calculates speed relative to spline length
    float AdjustedMovementSpeed(Spline spline)
    {
        if (spline == null) return 0;
        float splineLength = spline.GetLength();
        // Avoid division by zero or very small lengths
        if (splineLength < 0.001f) return moveSpeed; // Return base speed or a large number
        return moveSpeed / splineLength;
    }

    // MoveAndRotate: Updates transform position and rotation based on currentT
    void MoveAndRotate()
    {
        if (splineContainer == null || currentKnot.Spline >= splineContainer.Splines.Count)
            return;

        // Lerp position for smoothness
        float movementBlend = 1f - Mathf.Pow(0.5f, Time.deltaTime * movementLerp);
        Vector3 targetPosition = (Vector3)splineContainer.EvaluatePosition(currentKnot.Spline, currentT);
        transform.position = Vector3.Lerp(transform.position, targetPosition, movementBlend);

        // Lerp rotation for smoothness
        splineContainer.Splines[currentKnot.Spline].Evaluate(currentT, out float3 position, out float3 direction, out float3 up);
        Vector3 worldDirection = splineContainer.transform.TransformDirection(direction);

        if (worldDirection.sqrMagnitude > 0.0001f) // Check for valid direction
        {
            Quaternion targetRotation = Quaternion.LookRotation(worldDirection, splineContainer.transform.TransformDirection(up));
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotationLerp * Time.deltaTime);
        }
    }

    // OnDrawGizmos: For debugging junction path selection
    private void OnDrawGizmos()
    {
        if (inJunction && splineContainer != null)
        {
            Gizmos.color = Color.red;
            Vector3 targetVisPos = GetJunctionPathPosition(junctionIndex);
            // Draw sphere slightly above ground
            Gizmos.DrawSphere(targetVisPos + Vector3.up * 0.1f, 0.5f);
        }
    }
}
