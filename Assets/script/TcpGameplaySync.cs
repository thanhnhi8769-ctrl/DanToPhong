using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public sealed class TcpGameplaySync : MonoBehaviour
{
    private const float InterpolationDelaySeconds = 0.1f;
    private const float StalePlayerSeconds = 3f;
    private readonly Dictionary<int, PlayerReplica> replicas = new Dictionary<int, PlayerReplica>();
    private readonly IStateChannel stateChannel = new TcpStateChannel();
    private GameObject localCar;
    private int localVehicleIndex;
    private int tickRate = 20;
    private bool configured;
    private float nextWarningTime;
    private int clientTick;
    private bool finishEventRaised;
    private bool streamReady;
    private bool streamStarting;
    private float nextStreamAttempt;

    public static event Action<string> GameFinished;

    public void ConfigureLocalCar(GameObject car, int vehicleIndex, int updatesPerSecond)
    {
        if (configured || car == null)
            return;

        configured = true;
        localCar = car;
        localVehicleIndex = Mathf.Clamp(vehicleIndex, 0, 100);
        tickRate = Mathf.Clamp(updatesPerSecond, 15, 20);
        Rigidbody body = localCar.GetComponent<Rigidbody>();
        if (body != null)
        {
            body.isKinematic = true;
            body.useGravity = false;
        }
        StartCoroutine(SendInputLoop());
    }

    public System.Threading.Tasks.Task<Resp> SendGameEventAsync(Req gameEvent)
    {
        return stateChannel.SendEventAsync(gameEvent);
    }

    private IEnumerator SendInputLoop()
    {
        float interval = 1f / tickRate;
        while (configured && localCar != null)
        {
            if (ServerClient.Ready)
            {
                if (!streamReady && !streamStarting && Time.unscaledTime >= nextStreamAttempt)
                {
                    streamStarting = true;
                    Task startTask = stateChannel.StartReceivingAsync();
                    yield return new WaitUntil(() => startTask.IsCompleted);
                    streamStarting = false;
                    if (startTask.Status == TaskStatus.RanToCompletion)
                        streamReady = true;
                    else
                    {
                        nextStreamAttempt = Time.unscaledTime + 5f;
                        Debug.LogWarning("[TCP gameplay] Snapshot subscription failed; retrying.");
                    }
                }

                scooter controls = localCar.GetComponent<scooter>();
                Vector3 position = localCar.transform.position;
                Resp response = null;
                var request = new Req
                {
                    command = "GAME_INPUT",
                    roomId = LauncherArgs.Room,
                    vehicleIndex = localVehicleIndex,
                    positionX = position.x,
                    positionY = position.y,
                    positionZ = position.z,
                    rotationY = localCar.transform.eulerAngles.y,
                    throttle = controls != null ? controls.NetworkThrottle : 0f,
                    steer = controls != null ? controls.NetworkSteer : 0f,
                    clientTick = ++clientTick,
                    clientTimestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };
                var task = stateChannel.SendStateAsync(request);
                yield return new WaitUntil(() => task.IsCompleted);
                if (task.Status == System.Threading.Tasks.TaskStatus.RanToCompletion)
                    response = task.Result;

                if (response != null && response.success)
                    ApplySnapshot(response);
                else if (Time.unscaledTime >= nextWarningTime)
                {
                    nextWarningTime = Time.unscaledTime + 5f;
                    Debug.LogWarning("[TCP gameplay] No valid authoritative snapshot was received.");
                }
            }

            yield return new WaitForSecondsRealtime(interval);
        }
    }

    private void ApplySnapshot(Resp response)
    {
        if (response.gameFinished && !finishEventRaised)
            RaiseGameFinished(response.winnerUsername);
        if (response.gamePlayers == null)
            return;

        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        foreach (GamePlayerDto state in response.gamePlayers)
        {
            if (state == null || string.IsNullOrWhiteSpace(state.username))
                continue;

            PlayerReplica replica = GetReplica(state.userId, null, false);
            if (state.sequence <= replica.LastSequence)
                continue;
            replica.LastSequence = state.sequence;

            if (string.Equals(state.username, LauncherArgs.User, StringComparison.OrdinalIgnoreCase))
            {
                replica.Object = localCar;
                replica.IsLocal = true;
                AddSample(replica, state, now);
                continue;
            }

            if (replica.Object == null)
            {
                GameObject prefab = Resources.Load<GameObject>("Scooter");
                if (prefab == null)
                {
                    Debug.LogError("[TCP gameplay] Resources/Scooter prefab is missing.");
                    return;
                }

                GameObject remote = Instantiate(prefab);
                remote.name = "XE_REMOTE_" + state.username;
                scooter controller = remote.GetComponent<scooter>();
                if (controller != null)
                {
                    controller.SetRemoteControlled(true);
                    controller.enabled = false;
                    controller.ApDungMauXe(state.vehicleIndex);
                }
                Rigidbody body = remote.GetComponent<Rigidbody>();
                if (body != null)
                {
                    body.isKinematic = true;
                    body.useGravity = false;
                }
                replica.Object = remote;
            }

            AddSample(replica, state, now);
            replica.LastSeenTime = Time.unscaledTime;
        }
    }

    private PlayerReplica GetReplica(int userId, GameObject target, bool isLocal)
    {
        if (!replicas.TryGetValue(userId, out PlayerReplica replica))
        {
            replica = new PlayerReplica { Object = target, IsLocal = isLocal };
            replicas.Add(userId, replica);
        }
        else if (target != null)
        {
            replica.Object = target;
        }
        return replica;
    }

    private static void AddSample(PlayerReplica replica, GamePlayerDto state, long receivedAt)
    {
        long timestamp = state.serverTimestampMs > 0 ? state.serverTimestampMs : receivedAt;
        var sample = new PoseSample(
            timestamp,
            new Vector3(state.positionX, state.positionY, state.positionZ),
            Quaternion.Euler(state.rotationX, state.rotationY, state.rotationZ));

        if (replica.Samples.Count > 0 && replica.Samples[replica.Samples.Count - 1].TimestampMs == timestamp)
            replica.Samples[replica.Samples.Count - 1] = sample;
        else
            replica.Samples.Add(sample);

        while (replica.Samples.Count > 8)
            replica.Samples.RemoveAt(0);
        replica.LastSeenTime = Time.unscaledTime;
    }

    private void Update()
    {
        while (stateChannel.TryDequeueEvent(out Resp gameEvent))
            ApplyGameEvent(gameEvent);
        while (stateChannel.TryDequeueSnapshot(out Resp snapshot))
            ApplySnapshot(snapshot);

        long renderTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() -
                          (long)(InterpolationDelaySeconds * 1000f);
        List<int> remove = null;
        foreach (KeyValuePair<int, PlayerReplica> pair in replicas)
        {
            PlayerReplica replica = pair.Value;
            if (replica.Object == null)
            {
                if (remove == null) remove = new List<int>();
                remove.Add(pair.Key);
                continue;
            }

            if (!replica.IsLocal && Time.unscaledTime - replica.LastSeenTime > StalePlayerSeconds)
            {
                Destroy(replica.Object);
                if (remove == null) remove = new List<int>();
                remove.Add(pair.Key);
                continue;
            }

            ApplyInterpolatedPose(replica, renderTime);
        }

        if (remove != null)
        {
            foreach (int id in remove)
                replicas.Remove(id);
        }
    }

    private void ApplyGameEvent(Resp response)
    {
        if (response.gameEvent != null &&
            string.Equals(response.gameEvent.type, "FINISH", StringComparison.OrdinalIgnoreCase))
            RaiseGameFinished(response.gameEvent.username);
    }

    private void RaiseGameFinished(string username)
    {
        if (finishEventRaised)
            return;
        finishEventRaised = true;
        GameFinished?.Invoke(username ?? string.Empty);
    }

    private void OnDestroy()
    {
        stateChannel.Close();
    }

    private static void ApplyInterpolatedPose(PlayerReplica replica, long renderTime)
    {
        if (replica.Object == null || replica.Samples.Count == 0)
            return;

        PoseSample first = replica.Samples[0];
        PoseSample last = replica.Samples[replica.Samples.Count - 1];
        if (renderTime <= first.TimestampMs)
        {
            replica.Object.transform.SetPositionAndRotation(first.Position, first.Rotation);
            return;
        }

        for (int i = 1; i < replica.Samples.Count; i++)
        {
            PoseSample next = replica.Samples[i];
            if (renderTime > next.TimestampMs)
                continue;

            PoseSample previous = replica.Samples[i - 1];
            float duration = Math.Max(1f, next.TimestampMs - previous.TimestampMs);
            float amount = Mathf.Clamp01((renderTime - previous.TimestampMs) / duration);
            replica.Object.transform.SetPositionAndRotation(
                Vector3.Lerp(previous.Position, next.Position, amount),
                Quaternion.Slerp(previous.Rotation, next.Rotation, amount));
            return;
        }

        replica.Object.transform.SetPositionAndRotation(last.Position, last.Rotation);
    }

    private sealed class PlayerReplica
    {
        public GameObject Object;
        public bool IsLocal;
        public float LastSeenTime;
        public long LastSequence;
        public readonly List<PoseSample> Samples = new List<PoseSample>(8);
    }

    private readonly struct PoseSample
    {
        public PoseSample(long timestampMs, Vector3 position, Quaternion rotation)
        {
            TimestampMs = timestampMs;
            Position = position;
            Rotation = rotation;
        }

        public long TimestampMs { get; }
        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
    }
}
