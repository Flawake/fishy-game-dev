using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class FishingLine : NetworkBehaviour
{

    [SerializeField] LineRenderer lineRenderer;
    [SerializeField] GameObject linePoint;

    private List<LineSegment> lineSegments = new List<LineSegment>();

    private int lineSegmentsAmount = 30;
    private int currentDrawnSegments = 0;

    private bool isFishing = false;

    Vector2 placeToThrow;

    private float lineSegLength = 0.25f;

    private readonly float lineWidth = 0.02f;

    void Update()
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

    [ClientRpc(includeOwner = false)]
    public void RpcInitThrowFishingLine(Vector2 placeToThrow)
    {
        InitThrowFishingLine(placeToThrow);
    }

    public void InitThrowFishingLine(Vector2 placeToThrow) 
    {
        //sets all line states to their initial value
        lineSegments.Clear();
        lineRenderer.enabled = true;

        this.placeToThrow = placeToThrow;

        //calculates the length of 1 single segment
        float xFactor = Mathf.Pow((transform.position.x - placeToThrow.x), 2);
        float yFactor = Mathf.Pow((transform.position.y - placeToThrow.y), 2);
        lineSegLength = Mathf.Sqrt(xFactor + yFactor) / lineSegmentsAmount;

        //adds the first 2 segments of the line
        lineSegments.Add(new LineSegment(placeToThrow));
        lineSegments.Add(new LineSegment(placeToThrow));
        currentDrawnSegments = 2;
    }

    public void ThrowLine() {
        isFishing = true;
    }

    [ClientRpc(includeOwner = false)]
    public void RpcEndedFishing()
    {
        EndFishing();
    }

    public void EndFishing()
    {
        lineSegments.Clear();
        isFishing = false;
        currentDrawnSegments = 0;
        lineRenderer.positionCount = 0;
        lineRenderer.enabled = false;
        return;
    }

    //Simulates the fishing line
    private void Simulate() 
    {
        //Don't even try to simulate the rod on the server
        if (isServer) {
            return;
        }

        if (currentDrawnSegments < lineSegmentsAmount) {
            lineSegments.Add(new LineSegment(lineSegments[lineSegments.Count - 1].currentPos));
            lineSegments.Add(new LineSegment(lineSegments[lineSegments.Count - 1].currentPos));
            currentDrawnSegments += 2;
        }

        Vector2 lineGravity;

        if (currentDrawnSegments < lineSegmentsAmount)
        {
            lineGravity = new Vector2(0f, 1.0f);
        }
        else {
            lineGravity = new Vector2(0f, -0.75f);
        }

        for (int i = 1; i < currentDrawnSegments; i++) {
            LineSegment currentSegment = lineSegments[i];
            Vector2 velocity = currentSegment.currentPos - currentSegment.previuosPos;
            currentSegment.previuosPos = currentSegment.currentPos;
            currentSegment.currentPos += velocity;
            currentSegment.currentPos += lineGravity * Time.fixedDeltaTime;
            lineSegments[i] = currentSegment;
        }

        for (int i = 0; i < 50; i++) {
            ApplyConstraints();
        }
    }

    //Adds constraints to the fishing line
    //Makes the fishing line not go wibbly-wobbly
    private void ApplyConstraints() 
    {

        //constrain the first and last segment of the line.
        LineSegment firstSegment = lineSegments[0];
        firstSegment.currentPos = linePoint.transform.position;
        lineSegments[0] = firstSegment;

        LineSegment lastSegment = lineSegments[lineSegments.Count - 1];
        //some random math to have the right offsets from the rod tip.
        lastSegment.currentPos = new Vector2(linePoint.transform.position.x + (placeToThrow.x - linePoint.transform.position.x) / lineSegmentsAmount * currentDrawnSegments, linePoint.transform.position.y + (placeToThrow.y - linePoint.transform.position.y) / lineSegmentsAmount * currentDrawnSegments);
        lineSegments[lineSegments.Count - 1] = lastSegment;
            
        for (int i = 0; i < currentDrawnSegments - 1; i++) {
            LineSegment currentSeg = lineSegments[i];
            LineSegment nextSeg = lineSegments[i + 1];

            float distance = (currentSeg.currentPos - nextSeg.currentPos).magnitude;
            float error = Mathf.Abs(distance - lineSegLength);
            Vector2 changeDir = Vector2.zero;

            if (distance > lineSegLength)
            {
                changeDir = (currentSeg.currentPos - nextSeg.currentPos).normalized;
            }
            else if (distance < lineSegLength) {
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
            else {
                nextSeg.currentPos += changeAmount;
                lineSegments[i + 1] = nextSeg;
            }
        }
    }
    private void DrawFishingLine()
    {

        Vector3[] linePositions = new Vector3[currentDrawnSegments];
        for (int i = 0; i < currentDrawnSegments; i++) {
            if (linePositions.Length < i) {
                return;
            }
            linePositions[i] = lineSegments[i].currentPos;
            linePositions[i].z = -5;
        }

        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.positionCount = linePositions.Length;
        lineRenderer.SetPositions(linePositions);
    }

    public struct LineSegment{
        public Vector2 currentPos;
        public Vector2 previuosPos;

        public LineSegment(Vector2 pos) {
            currentPos = pos;
            previuosPos = pos;
        }
    }
}
