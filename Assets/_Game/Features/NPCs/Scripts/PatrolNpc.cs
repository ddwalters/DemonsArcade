using System.Collections.Generic;
using NodeTree;
using Pathfinding;
using UnityEngine;

[RequireComponent(typeof(CharacterMotor))]
[RequireComponent(typeof(Seeker))]
[RequireComponent(typeof(NpcConversationFacing))]
public class PatrolNpc : InteractableBase
{
    [Header("Patrol")]
    [SerializeField] private Transform[] waypoints = new Transform[0];
    [SerializeField, Min(0.05f)] private float waypointReachDistance = 0.45f;
    [SerializeField, Min(0f)] private float waypointVerticalReachDistance = 0.75f;
    [SerializeField, Min(0.05f)] private float pathPointReachDistance = 0.1f;
    [SerializeField, Min(0.05f)] private float repathInterval = 0.75f;
    [SerializeField, Min(0.05f)] private float failedPathRetryDelay = 1f;

    [Header("Dialogue")]
    public NodeTreeGraphSO dialogue;
    public DialogRunner runner;

    private readonly List<Vector3> pathPoints = new List<Vector3>();
    private CharacterMotor motor;
    private CharacterController characterController;
    private Seeker seeker;
    private NpcConversationFacing conversationFacing;
    private int currentWaypointIndex;
    private int currentPathPointIndex;
    private int pathRequestVersion;
    private bool hasPath;
    private bool pathRequestPending;
    private bool wasDialogActive;
    private float nextPathRequestTime;

    public override InteractionWorldSpaceNameType WorldSpaceNameType =>
        InteractionWorldSpaceNameType.Npc;

    private void Awake()
    {
        motor = GetComponent<CharacterMotor>();
        characterController = GetComponent<CharacterController>();
        seeker = GetComponent<Seeker>();
        conversationFacing = GetComponent<NpcConversationFacing>();
        if (conversationFacing == null)
            conversationFacing = gameObject.AddComponent<NpcConversationFacing>();
        ResolveDialogRunner();
        SetToolTip("Talk");
        UpdateInteractableState();
    }

    private void OnEnable()
    {
        if (motor == null)
            motor = GetComponent<CharacterMotor>();
        if (characterController == null)
            characterController = GetComponent<CharacterController>();
        if (seeker == null)
            seeker = GetComponent<Seeker>();

        nextPathRequestTime = Time.time;
    }

    private void OnDisable()
    {
        InvalidateCurrentPathRequest();
        motor?.Stop();
        conversationFacing?.EndConversation();
    }

    private void OnValidate()
    {
        SetToolTip("Talk");
    }

    private void Update()
    {
        if (motor == null)
            return;

        ResolveDialogRunner();
        UpdateInteractableState();
        bool dialogIsActive = runner != null && runner.IsDialogActive;
        if (dialogIsActive)
        {
            if (!wasDialogActive)
                PausePatrol();

            wasDialogActive = true;
            TickIdleMotor();
            return;
        }

        if (wasDialogActive)
        {
            wasDialogActive = false;
            conversationFacing?.EndConversation();
            nextPathRequestTime = Time.time;
        }

        if (!TryGetCurrentWaypoint(out Transform targetWaypoint))
        {
            hasPath = false;
            pathPoints.Clear();
            TickIdleMotor();
            return;
        }

        if (waypoints.Length > 1 && IsAtWaypoint(targetWaypoint.position))
        {
            AdvanceWaypoint();
            if (!TryGetCurrentWaypoint(out targetWaypoint))
            {
                TickIdleMotor();
                return;
            }
        }

        if (!pathRequestPending && Time.time >= nextPathRequestTime)
            RequestPath(targetWaypoint);

        Vector3 movementDirection = GetPathDirection();
        Vector2 motorInput = movementDirection.sqrMagnitude > 0.0001f
            ? Vector2.up
            : Vector2.zero;
        motor.Tick(motorInput, false, true, Time.deltaTime, movementDirection);
    }

    public void Configure(Transform[] route, NodeTreeGraphSO graph, DialogRunner dialogRunner)
    {
        waypoints = route ?? new Transform[0];
        dialogue = graph;
        runner = dialogRunner;
        if (motor == null)
            motor = GetComponent<CharacterMotor>();
        currentWaypointIndex = 0;
        ResolveDialogRunner();
        UpdateInteractableState();
        InvalidateCurrentPathRequest();
        hasPath = false;
        pathPoints.Clear();
        motor?.Stop();
        nextPathRequestTime = Time.time;
    }

    public override void OnInteract()
    {
        base.OnInteract();
        ResolveDialogRunner();
        if (dialogue == null || runner == null || runner.IsDialogActive)
            return;

        runner.StartDialog(dialogue);
        if (runner.IsDialogActive)
        {
            PausePatrol();
            wasDialogActive = true;
            conversationFacing?.BeginConversation(FindConversationParticipant());
        }
    }

    private static Transform FindConversationParticipant()
    {
        PlayerController player = FindAnyObjectByType<PlayerController>();
        if (player != null)
            return player.transform;

        InteractionController interactor = FindAnyObjectByType<InteractionController>();
        return interactor != null ? interactor.transform : null;
    }

    private void ResolveDialogRunner()
    {
        if (runner != null)
            return;

        runner = DialogRunner.Instance;
        if (runner == null)
            runner = FindAnyObjectByType<DialogRunner>();
    }

    private void UpdateInteractableState()
    {
        SetInteractable(dialogue != null && runner != null);
        SetToolTip("Talk");
    }

    private void PausePatrol()
    {
        InvalidateCurrentPathRequest();
        hasPath = false;
        pathPoints.Clear();
        motor.Stop();
    }

    private void TickIdleMotor()
    {
        motor.Tick(Vector2.zero, false, true, Time.deltaTime);
    }

    private bool TryGetCurrentWaypoint(out Transform targetWaypoint)
    {
        targetWaypoint = null;
        if (waypoints == null || waypoints.Length == 0)
            return false;

        int checkedWaypoints = 0;
        while (checkedWaypoints < waypoints.Length && waypoints[currentWaypointIndex] == null)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
            checkedWaypoints++;
        }

        targetWaypoint = waypoints[currentWaypointIndex];
        return targetWaypoint != null;
    }

    private bool IsAtWaypoint(Vector3 position)
    {
        Vector3 offset = position - transform.position;
        offset.y = 0f;
        float reachDistance = Mathf.Max(0.05f, waypointReachDistance);
        if (offset.sqrMagnitude > reachDistance * reachDistance)
            return false;

        float feetHeight = characterController != null
            ? characterController.bounds.min.y
            : transform.position.y;
        return Mathf.Abs(feetHeight - position.y) <= waypointVerticalReachDistance;
    }

    private void AdvanceWaypoint()
    {
        currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
        hasPath = false;
        pathPoints.Clear();
        currentPathPointIndex = 0;
        nextPathRequestTime = Time.time;
    }

    private void RequestPath(Transform targetWaypoint)
    {
        if (seeker == null || targetWaypoint == null || AstarPath.active == null)
        {
            nextPathRequestTime = Time.time + Mathf.Max(0.05f, failedPathRetryDelay);
            return;
        }

        int waypointIndex = currentWaypointIndex;
        int requestVersion = ++pathRequestVersion;
        Vector3 destination = targetWaypoint.position;
        Vector3 startPosition = GetFeetPosition();
        pathRequestPending = true;

        seeker.StartPath(startPosition, destination,
            path => OnPathCalculated(path, waypointIndex, requestVersion));
    }

    private void OnPathCalculated(Path calculatedPath, int waypointIndex, int requestVersion)
    {
        if (this == null || requestVersion != pathRequestVersion)
            return;

        pathRequestPending = false;

        if (waypointIndex != currentWaypointIndex || (runner != null && runner.IsDialogActive))
        {
            hasPath = false;
            pathPoints.Clear();
            nextPathRequestTime = Time.time;
            return;
        }

        if (calculatedPath == null || calculatedPath.error ||
            calculatedPath.vectorPath == null || calculatedPath.vectorPath.Count == 0)
        {
            hasPath = false;
            pathPoints.Clear();
            nextPathRequestTime = Time.time + Mathf.Max(0.05f, failedPathRetryDelay);
            return;
        }

        pathPoints.Clear();
        pathPoints.AddRange(calculatedPath.vectorPath);
        currentPathPointIndex = FindNextPathPointIndex(pathPoints, GetFeetPosition());
        hasPath = true;
        nextPathRequestTime = Time.time + Mathf.Max(0.05f, repathInterval);
    }

    private Vector3 GetFeetPosition()
    {
        Vector3 feetPosition = transform.position;
        if (characterController != null)
            feetPosition.y = characterController.bounds.min.y;

        return feetPosition;
    }

    private static int FindNextPathPointIndex(List<Vector3> points, Vector3 position)
    {
        if (points.Count < 2)
            return 0;

        Vector2 flatPosition = new Vector2(position.x, position.z);
        float nearestDistanceSquared = float.PositiveInfinity;
        int nearestSegmentIndex = 0;

        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector2 segmentStart = new Vector2(points[i].x, points[i].z);
            Vector2 segment = new Vector2(points[i + 1].x - points[i].x,
                points[i + 1].z - points[i].z);
            float segmentLengthSquared = segment.sqrMagnitude;
            float segmentProgress = segmentLengthSquared > 0.000001f
                ? Mathf.Clamp01(Vector2.Dot(flatPosition - segmentStart, segment) /
                    segmentLengthSquared)
                : 0f;
            Vector2 nearestPoint = segmentStart + segment * segmentProgress;
            float distanceSquared = (flatPosition - nearestPoint).sqrMagnitude;

            if (distanceSquared < nearestDistanceSquared)
            {
                nearestDistanceSquared = distanceSquared;
                nearestSegmentIndex = i;
            }
        }

        // Continue toward the end of the nearest segment instead of returning to its start node.
        return Mathf.Min(nearestSegmentIndex + 1, points.Count - 1);
    }

    private Vector3 GetPathDirection()
    {
        if (!hasPath)
            return Vector3.zero;

        float reachDistance = Mathf.Max(0.05f, pathPointReachDistance);
        float reachDistanceSquared = reachDistance * reachDistance;
        while (currentPathPointIndex < pathPoints.Count)
        {
            Vector3 offset = pathPoints[currentPathPointIndex] - transform.position;
            offset.y = 0f;
            if (offset.sqrMagnitude > reachDistanceSquared)
                return offset.normalized;

            currentPathPointIndex++;
        }

        return Vector3.zero;
    }

    private void InvalidateCurrentPathRequest()
    {
        pathRequestVersion++;
        if (pathRequestPending && seeker != null)
            seeker.CancelCurrentPathRequest();
        pathRequestPending = false;
    }

    private void OnDrawGizmosSelected()
    {
        if (waypoints == null || waypoints.Length == 0)
            return;

        Gizmos.color = Color.red;
        for (int i = 0; i < waypoints.Length; i++)
        {
            Transform waypoint = waypoints[i];
            if (waypoint == null)
                continue;

            Gizmos.DrawSphere(waypoint.position, 0.12f);
            Transform nextWaypoint = waypoints[(i + 1) % waypoints.Length];
            if (nextWaypoint != null)
                Gizmos.DrawLine(waypoint.position, nextWaypoint.position);
        }
    }
}
