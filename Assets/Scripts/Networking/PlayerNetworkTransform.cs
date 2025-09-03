using Mirror;
using System.Runtime.CompilerServices;
using UnityEngine;

public class PlayerNetworkTransform : NetworkTransformBase
{
    [SerializeField] 
    PlayerController playerController;

    uint sendIntervalCounter = 0;
    double lastSendIntervalTime = double.MinValue;

    [Tooltip("If we only sync on change, then we need to correct old snapshots if more time than sendInterval * multiplier has elapsed.\n\nOtherwise the first move will always start interpolating from the last move sequence's time, which will make it stutter when starting every time.")]
    public float onlySyncOnChangeCorrectionMultiplier = 2;

    [Tooltip("Position is rounded in order to drastically minimize bandwidth.\n\nFor example, a precision of 0.01 rounds to a centimeter. In other words, sub-centimeter movements aren't synced until they eventually exceeded an actual centimeter.\n\nDepending on how important the object is, a precision of 0.01-0.10 (1-10 cm) is recommended.\n\nFor example, even a 1cm precision combined with delta compression cuts the Benchmark demo's bandwidth in half, compared to sending every tiny change.")]
    [Range(0.00_01f, 1f)]                   // disallow 0 division. 1mm to 1m precision is enough range.
    public float positionPrecision = 0.01f; // 1 cm

    // delta compression needs to remember 'last' to compress against
    protected Vector3Long lastSerializedPosition = Vector3Long.zero;
    protected Vector3Long lastDeserializedPosition = Vector3Long.zero;

    protected Vector2 targetPosition = Vector2.zero;

    // Used to store last sent snapshots
    protected TransformSnapshot last;
    // Start is called before the first frame update

    protected int lastClientCount = 1;

    void Start()
    {
        if(syncRotation)
        {
            Debug.LogWarning("Sync rotation is not supported by the custom networkTransform");
        }
        if (syncScale)
        {
            Debug.LogWarning("Sync scale is not supported by the custom networkTransform");
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (isClient) UpdateClient(false);
    }

    void LateUpdate()
    {
        // set dirty to trigger OnSerialize. either always, or only if changed.
        // It has to be checked in LateUpdate() for onlySyncOnChange to avoid
        // the possibility of Update() running first before the object's movement
        // script's Update(), which then causes NT to send every alternate frame
        // instead.
        if (isServer || (IsClientWithAuthority && NetworkClient.ready))
        {
            if (sendIntervalCounter == sendIntervalMultiplier && (!onlySyncOnChange || Changed(Construct())))
                SetDirty();

            CheckLastSendTime();
        }
    }

    protected virtual void UpdateClient(bool force)
    {
        if(isLocalPlayer && !force)
        {
            return;
        }

        //target.position is the current position of the transform
        Vector2 dir = new Vector2(
            Mathf.Abs(targetPosition.x - target.position.x) > positionPrecision ? targetPosition.x - target.position.x : 0,
            Mathf.Abs(targetPosition.x - target.position.y) > positionPrecision ? targetPosition.y - target.position.y : 0
        ).normalized;

        playerController.MoveOtherPlayerLocally(dir, targetPosition);
    }

    protected virtual void CheckLastSendTime()
    {
        // timeAsDouble not available in older Unity versions.
        if (AccurateInterval.Elapsed(NetworkTime.localTime, NetworkServer.sendInterval, ref lastSendIntervalTime))
        {
            if (sendIntervalCounter == sendIntervalMultiplier)
                sendIntervalCounter = 0;
            sendIntervalCounter++;
        }
    }

    protected virtual bool Changed(TransformSnapshot current)
    {
        //We only care for the position, not rotation and scale
        // position is quantized and delta compressed.
        // only consider it changed if the quantized representation is changed.
        // careful: don't use 'serialized / deserialized last'. as it depends on sync mode etc.
        return QuantizedChanged(last.position, current.position, positionPrecision);
    }

    // helper function to compare quantized representations of a Vector3
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool QuantizedChanged(Vector3 u, Vector3 v, float precision)
    {
        Compression.ScaleToLong(u, precision, out Vector3Long uQuantized);
        Compression.ScaleToLong(v, precision, out Vector3Long vQuantized);
        return uQuantized != vQuantized;
    }

    public override void OnSerialize(NetworkWriter writer, bool initialState)
    {
        // get current snapshot for broadcasting.
        TransformSnapshot snapshot = Construct();

        // ClientToServer optimization:
        // for interpolated client owned identities,
        // always broadcast the latest known snapshot so other clients can
        // interpolate immediately instead of catching up too

        // TODO dirty mask? [compression is very good w/o it already]
        // each vector's component is delta compressed.
        // an unchanged component would still require 1 byte.
        // let's use a dirty bit mask to filter those out as well.

        // initial
        if(initialState)
        {
            // If there is a last serialized snapshot, we use it.
            // This prevents the new client getting a snapshot that is different
            // from what the older clients last got. If this happens, and on the next
            // regular serialisation the delta compression will get wrong values.
            // Notes:
            // 1. Interestingly only the older clients have it wrong, because at the end
            //    of this function, last = snapshot which is the initial state's snapshot
            // 2. Regular NTR gets by this bug because it sends every frame anyway so initialstate
            //    snapshot constructed would have been the same as the last anyway.
            if (last.remoteTime > 0) snapshot = last;
            if (syncPosition) writer.WriteVector3(snapshot.position);
        }
        // delta
        else
        {
            // int before = writer.Position;
            if (syncPosition)
            {
                // quantize -> delta -> varint
                Compression.ScaleToLong(snapshot.position, positionPrecision, out Vector3Long quantized);
                DeltaCompression.Compress(writer, lastSerializedPosition, quantized);
            }
        }

        // save serialized as 'last' for next delta compression
        if (syncPosition) Compression.ScaleToLong(snapshot.position, positionPrecision, out lastSerializedPosition);
        // set 'last'
        last = snapshot;
    }

    //We're serializing and deserializing vector3's this is unneccessary since the z coordinate doesn't change and only costs extra bandwith
    public override void OnDeserialize(NetworkReader reader, bool initialState)
    {
        Vector3? position = null;

        // initial
        if (initialState)
        {
            if (syncPosition) position = reader.ReadVector3();
        }
        // delta
        else
        {
            // varint -> delta -> quantize
            if (syncPosition)
            {
                Vector3Long quantized = DeltaCompression.Decompress(reader, lastDeserializedPosition);
                position = Compression.ScaleToFloat(quantized, positionPrecision);
            }
        }

        if (isClient) OnServerToClientSync(position);

        // save deserialized as 'last' for next delta compression
        if (syncPosition) Compression.ScaleToLong(position.Value, positionPrecision, out lastDeserializedPosition);
    }

    protected virtual void OnServerToClientSync(Vector3? position)
    {
        // don't apply for local player with authority
        if (IsClientWithAuthority) return;
        targetPosition = (Vector2)position;
    }

    // reset state for next session.
    // do not ever call this during a session (i.e. after teleport).
    // calling this will break delta compression.
    public override void ResetState()
    {
        base.ResetState();

        // reset delta
        lastSerializedPosition = Vector3Long.zero;
        lastDeserializedPosition = Vector3Long.zero;

        // reset 'last' for delta too
        last = new TransformSnapshot(0, 0, Vector3.zero, Quaternion.identity, Vector3.zero);
    }

}
