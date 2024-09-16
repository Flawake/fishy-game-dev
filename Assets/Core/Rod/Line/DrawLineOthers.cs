using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class DrawLineOthers : NetworkBehaviour
{
    private List<LineSegment> lineSegments = new List<LineSegment>();

    //script classes
    [SerializeField] private LineRenderer lineRendererOther;

    //global variables
    [SyncVar] public Vector2 placeToThrow;
    [SyncVar] public float lineSegLength = 0.25f;
    [SyncVar(hook = nameof(ThrowLineStart)), SerializeField] 
    public bool isFishing = false;

    public int lineSegmentsAmount = 30;
    private int currentDrawnSegments = 0;
    private float lineWidth = 0.02f;

    public void ThrowLineStart(bool oldValue, bool newValue)
    {
        if (!newValue || isLocalPlayer)
        {
            return;
        }
        startedFishing();
    }

    private void Update()
    {
        if (isFishing)
        {
           DrawFishingLine();
        }
    }

    private void FixedUpdate()
    {
        if (isFishing)
        {
            Simulate();
        }
    }

    public void startedFishing()
    {
        if (isLocalPlayer || isServer)
            return;

        lineSegments.Clear();
        lineSegments.Add(new LineSegment(placeToThrow));
        lineSegments.Add(new LineSegment(placeToThrow));
        currentDrawnSegments = 2;
        lineRendererOther.enabled = true;
    }

    [ClientRpc]
    public void RpcEndedFishing() {
        if (isLocalPlayer)
            return;
        lineSegments.Clear();
        currentDrawnSegments = 0;
        lineRendererOther.positionCount = 0;
        lineRendererOther.enabled = false;
        return;
    }

    private void DrawFishingLine()
    {
        if (isLocalPlayer || isServer)
            return;

        Vector3[] linePositions = new Vector3[currentDrawnSegments];
        for (int i = 0; i < currentDrawnSegments; i++)
        {
            linePositions[i] = lineSegments[i].currentPos;
            linePositions[i].z = -5;
        }

        lineRendererOther.startWidth = lineWidth;
        lineRendererOther.endWidth = lineWidth;
        lineRendererOther.positionCount = linePositions.Length;
        lineRendererOther.SetPositions(linePositions);
    }

    private void Simulate()
    {
        if (isLocalPlayer || isServer) 
            return;
        if (lineSegments.Count < 2)
            return;

        for (int i = 0; i < 2; i++)
        {
            if (currentDrawnSegments < lineSegmentsAmount)
            {
                lineSegments.Add(new LineSegment(lineSegments[lineSegments.Count - 1].currentPos));
                currentDrawnSegments++;
            }
        }

        Vector2 lineGravity;

        if (currentDrawnSegments < lineSegmentsAmount)
        {
            lineGravity = new Vector2(0f, 1.0f);
        }
        else
        {
            lineGravity = new Vector2(0f, -0.75f);
        }

        for (int i = 1; i < currentDrawnSegments; i++)
        {
            LineSegment currentSegment = lineSegments[i];
            Vector2 velocity = currentSegment.currentPos - currentSegment.previuosPos;
            currentSegment.previuosPos = currentSegment.currentPos;
            currentSegment.currentPos += velocity;
            currentSegment.currentPos += lineGravity * Time.fixedDeltaTime;
            lineSegments[i] = currentSegment;
        }

        for (int i = 0; i < 50; i++)
        {
            ApplyConstraints();
        }
    }

    private void ApplyConstraints()
    {
        LineSegment firstSegment = lineSegments[0];
        firstSegment.currentPos = transform.position;
        lineSegments[0] = firstSegment;

        LineSegment lastSegment = lineSegments[lineSegments.Count - 1];
        //some random math to have the right offsets from the player.
        lastSegment.currentPos = new Vector2(transform.position.x + (placeToThrow.x - transform.position.x) / lineSegmentsAmount * currentDrawnSegments, transform.position.y + (placeToThrow.y - transform.position.y) / lineSegmentsAmount * currentDrawnSegments);
        lineSegments[lineSegments.Count - 1] = lastSegment;

        for (int i = 0; i < currentDrawnSegments - 1; i++)
        {
            LineSegment currentSeg = lineSegments[i];
            LineSegment nextSeg = lineSegments[i + 1];

            float distance = (currentSeg.currentPos - nextSeg.currentPos).magnitude;
            float error = Mathf.Abs(distance - lineSegLength);
            Vector2 changeDir = Vector2.zero;

            if (distance > lineSegLength)
            {
                changeDir = (currentSeg.currentPos - nextSeg.currentPos).normalized;
            }
            else if (distance < lineSegLength)
            {
                changeDir = (nextSeg.currentPos - currentSeg.currentPos).normalized;
            }

            Vector2 changeAmount = changeDir * error;
            if (i != 0)
            {
                currentSeg.currentPos -= changeAmount * 0.5f;
                lineSegments[i] = currentSeg;
                nextSeg.currentPos += changeAmount * 0.5f;
                lineSegments[i + 1] = nextSeg;
            }
            else
            {
                nextSeg.currentPos += changeAmount;
                lineSegments[i + 1] = nextSeg;
            }
        }
    }

    public struct LineSegment
    {
        public Vector2 currentPos;
        public Vector2 previuosPos;

        public LineSegment(Vector2 pos)
        {
            currentPos = pos;
            previuosPos = pos;
        }
    }
}