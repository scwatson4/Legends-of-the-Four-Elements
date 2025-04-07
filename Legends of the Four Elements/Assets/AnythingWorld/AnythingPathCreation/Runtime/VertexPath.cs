using UnityEngine;

namespace AnythingWorld.PathCreation 
{
    /// <summary>
    /// A vertex path is a collection of points (vertices) that lie along a bezier path.
    /// This allows one to do things like move at a constant speed along the path,
    /// which is not possible with a bezier path directly due to how they're constructed mathematically.

    /// This class also provides methods for getting the position along the path at a certain distance or time
    /// (where time = 0 is the start of the path, and time = 1 is the end of the path).
    /// Other info about the path (tangents, normals, rotation) can also be retrieved in this manner.
    /// </summary>
    public class VertexPath 
    {
        // A scalar for how many times bezier path is divided when determining vertex positions.
        private const int Accuracy = 10; 
        private const float MinVertexSpacing = .01f;
        
        // Total distance between the vertices of the polyline.
        public float Length { get; private set; }
        
        public readonly bool IsClosedLoop;
        public readonly Vector3[] LocalNormals;

        private readonly PathSpace _space;
        private readonly Vector3[] _localPoints;
        private readonly Vector3[] _localTangents;
        // Percentage along the path at each vertex (0 being start of path, and 1 being the end).
        private readonly float[] _times;
        // Total distance from the first vertex up to each vertex in the polyline.
        private readonly float[] _cumulativeLengthAtEachVertex;
        private readonly float[] _originalCumulativeLength;
        private Transform _transform;
        private readonly float _originalLength;

        
        /// <summary>
        /// Initializes a new instance of the <see cref="VertexPath"/> class by splitting the bezier path into an
        /// array of vertices along the path.
        /// </summary>
        /// <param name="bezierPath">The bezier path to split into vertices.</param>
        /// <param name="transform">The transform to apply to the vertex path.</param>
        /// <param name="maxAngleError">The maximum angle error allowed when splitting the bezier path.</param>
        /// <param name="minVertexDst">The minimum distance between vertices.</param>
        public VertexPath(BezierPath bezierPath, Transform transform, float maxAngleError = 0.3f, float minVertexDst = 0):
            this(bezierPath, VertexPathUtility.SplitBezierPathByAngleError(bezierPath, maxAngleError, minVertexDst, 
                Accuracy), transform){}

        /// <summary>
        /// Initializes a new instance of the <see cref="VertexPath"/> class by splitting the bezier path into an
        /// array of vertices along the path.
        /// </summary>
        /// <param name="bezierPath">The bezier path to split into vertices.</param>
        /// <param name="transform">The transform to apply to the vertex path.</param>
        /// <param name="vertexSpacing">The desired spacing between vertices.</param>
        public VertexPath(BezierPath bezierPath, Transform transform, float vertexSpacing):
            this(bezierPath, VertexPathUtility.SplitBezierPathEvenly(bezierPath, 
                Mathf.Max(vertexSpacing, MinVertexSpacing), Accuracy), transform){}

        /// <summary>
        /// Internal constructor for creating a vertex path from a bezier path and path split data.
        /// </summary>
        /// <param name="bezierPath">The bezier path to create the vertex path from.</param>
        /// <param name="pathSplitData">The path split data containing vertex and tangent information.</param>
        /// <param name="transform">The transform to apply to the vertex path.</param>
        private VertexPath(BezierPath bezierPath, VertexPathUtility.PathSplitData pathSplitData, Transform transform) 
        {
            _transform = transform;
            _space = bezierPath.Space;
            IsClosedLoop = bezierPath.IsClosed;
            int numVerts = pathSplitData.vertices.Count;
            _originalLength = pathSplitData.cumulativeLength[numVerts - 1];
            
            var scale = Vector3.Dot(_transform.lossyScale, Vector3.one) / 3;
            Length = _originalLength * scale;

            _localPoints = new Vector3[numVerts];
            LocalNormals = new Vector3[numVerts];
            _localTangents = new Vector3[numVerts];
            _cumulativeLengthAtEachVertex = new float[numVerts];
            _originalCumulativeLength = new float[numVerts];
            _times = new float[numVerts];
            var bounds = new Bounds((pathSplitData.minMax.Min + pathSplitData.minMax.Max) / 2, 
                pathSplitData.minMax.Max - pathSplitData.minMax.Min);

            // Figure out up direction for path.
            var up = bounds.size.z > bounds.size.y ? Vector3.up : -Vector3.forward;
            Vector3 lastRotationAxis = up;

            // Loop through the data and assign to arrays.
            for (int i = 0; i < _localPoints.Length; i++) 
            {
                _localPoints[i] = pathSplitData.vertices[i];
                _localTangents[i] = pathSplitData.tangents[i];
                _originalCumulativeLength[i] = pathSplitData.cumulativeLength[i];
                _cumulativeLengthAtEachVertex[i] = pathSplitData.cumulativeLength[i] * scale;
                if (_originalLength == 0)
                {
                    Debug.LogError("Path data provided to VertexPath constructor has a length of zero, check if " +
                                   "anchor points are not too close to each other");
                }
                else
                {
                    _times[i] = _cumulativeLengthAtEachVertex[i] / Length;
                }

                // Calculate normals.
                if (_space == PathSpace.XYZ) 
                {
                    if (i == 0) 
                    {
                        LocalNormals[0] = Vector3.Cross(lastRotationAxis, pathSplitData.tangents[0]).normalized;
                    } 
                    else 
                    {
                        Vector3 tangent = _localTangents[i].normalized;
                        Vector3 prevTangent = _localTangents[i - 1].normalized; 
                        Vector3 prevNormal = LocalNormals[i - 1]; 
                        
                        // Calculate rotation from previous to current tangent.
                        Quaternion rotationFromPrevToCurrent = Quaternion.FromToRotation(prevTangent, tangent);
                        
                        // Apply this rotation to the previous normal to "transport" it along the path.
                        Vector3 currentNormal = rotationFromPrevToCurrent * prevNormal;
                        
                        // Ensure the normal is orthogonal to the tangent.
                        // This step corrects any minor deviations introduced by floating-point inaccuracies.
                        Vector3 binormal = Vector3.Cross(tangent, currentNormal).normalized;
                        currentNormal = Vector3.Cross(binormal, tangent).normalized;
                        
                        // Assign the calculated normal to the current point.
                        LocalNormals[i] = currentNormal;
                    }
                } 
                else 
                {
                    LocalNormals[i] = Vector3.up * (bezierPath.FlipNormals ? -1 : 1);
                }
            }

            // Apply correction for 3d normals along a closed path.
            if (_space == PathSpace.XYZ && IsClosedLoop) {
                // Get angle between first and last normal (if zero, they're already lined up,
                // otherwise we need to correct).
                float normalsAngleErrorAcrossJoin = 
                    Vector3.SignedAngle(LocalNormals[LocalNormals.Length - 1], LocalNormals[0], _localTangents[0]);
                // Gradually rotate the normals along the path to ensure start and end normals line up correctly.
                // Don't bother correcting if very nearly correct.
                if (Mathf.Abs(normalsAngleErrorAcrossJoin) > 0.1f) 
                {
                    for (int i = 1; i < LocalNormals.Length; i++) 
                    {
                        float t = i / (LocalNormals.Length - 1f);
                        float angle = normalsAngleErrorAcrossJoin * t;
                        Quaternion rot = Quaternion.AngleAxis (angle, _localTangents[i]);
                        LocalNormals[i] = rot * LocalNormals[i] * (bezierPath.FlipNormals ? -1 : 1);
                    }
                }
            }

            // Rotate normals to match up with user-defined anchor angles.
            if (_space == PathSpace.XYZ) 
            {
                for (int anchorIndex = 0; anchorIndex < pathSplitData.anchorVertexMap.Count - 1; anchorIndex++) 
                {
                    int nextAnchorIndex = IsClosedLoop ? (anchorIndex + 1) % bezierPath.NumSegments : anchorIndex + 1;

                    float startAngle = bezierPath.GetAnchorNormalAngle (anchorIndex) + bezierPath.GlobalNormalsAngle;
                    float endAngle = bezierPath.GetAnchorNormalAngle (nextAnchorIndex) + bezierPath.GlobalNormalsAngle;
                    float deltaAngle = Mathf.DeltaAngle (startAngle, endAngle);

                    int startVertIndex = pathSplitData.anchorVertexMap[anchorIndex];
                    int endVertIndex = pathSplitData.anchorVertexMap[anchorIndex + 1];

                    int num = endVertIndex - startVertIndex;
                    if (anchorIndex == pathSplitData.anchorVertexMap.Count - 2) 
                    {
                        num += 1;
                    }
                    for (int i = 0; i < num; i++) 
                    {
                        int vertIndex = startVertIndex + i;
                        float t = num == 1 ? 1f : i / (num - 1f);
                        float angle = startAngle + deltaAngle * t;
                        Quaternion rot = Quaternion.AngleAxis(angle, _localTangents[vertIndex]);
                        LocalNormals[vertIndex] = (rot * LocalNormals[vertIndex]) * ((bezierPath.FlipNormals) ? -1 : 1);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the number of points in the vertex path.
        /// </summary>
        public int NumPoints => _localPoints.Length;
        
        /// <summary>
        /// Scales the length of the vertex path.
        /// </summary>
        public void ScalePathLength()
        {
            var scale = Vector3.Dot(_transform.lossyScale, Vector3.one) / 3;
            Length = _originalLength * scale;
            
            for (int i = 0; i < _originalCumulativeLength.Length; i++)
            {
                _cumulativeLengthAtEachVertex[i] = _originalCumulativeLength[i] * scale;
            }
        }
        
        /// <summary>
        /// Updates the transform of the vertex path.
        /// </summary>
        /// <param name="transform">The new transform to apply to the vertex path.</param>
        public void UpdateTransform(Transform transform)
        {
            _transform = transform;
        }

        /// <summary>
        /// Gets the tangent vector at the specified index in the vertex path.
        /// </summary>
        /// <param name="index">The index of the point to get the tangent for.</param>
        public Vector3 GetTangent(int index) 
        {
            return MathUtility.TransformDirection(_localTangents[index], _transform, _space);
        }

        /// <summary>
        /// Gets the normal vector at the specified index in the vertex path.
        /// </summary>
        /// <param name="index">The index of the point to get the normal for.</param>
        public Vector3 GetNormal(int index) 
        {
            return MathUtility.TransformDirection(LocalNormals[index], _transform, _space);
        }

        /// <summary>
        /// Gets the point at the specified index in the vertex path.
        /// </summary>
        /// <param name="index">The index of the point to get.</param>
        public Vector3 GetPoint(int index) 
        {
            return MathUtility.TransformPoint(_localPoints[index], _transform, _space);
        }

        /// <summary>
        /// Gets the point on the path based on the distance travelled.
        /// </summary>
        /// <param name="dst">The distance travelled along the path.</param>
        /// <param name="endOfPathInstruction">The instruction for handling the end of the path.</param>
        public Vector3 GetPointAtDistance(float dst, 
            EndOfPathInstruction endOfPathInstruction = EndOfPathInstruction.Loop) 
        {
            float t = dst / Length;
            return GetPointAtTime(t, endOfPathInstruction);
        }

        /// <summary>
        /// Gets the direction on the path based on the distance travelled.
        /// </summary>
        /// <param name="dst">The distance travelled along the path.</param>
        /// <param name="endOfPathInstruction">The instruction for handling the end of the path.</param>
        public Vector3 GetDirectionAtDistance(float dst, 
            EndOfPathInstruction endOfPathInstruction = EndOfPathInstruction.Loop) 
        {
            float t = dst / Length;
            return GetDirection(t, endOfPathInstruction);
        }

        /// <summary>
        /// Gets the normal vector on the path based on the distance travelled.
        /// </summary>
        /// <param name="dst">The distance travelled along the path.</param>
        /// <param name="endOfPathInstruction">The instruction for handling the end of the path.</param>
        public Vector3 GetNormalAtDistance(float dst, 
            EndOfPathInstruction endOfPathInstruction = EndOfPathInstruction.Loop) 
        {
            float t = dst / Length;
            return GetNormal(t, endOfPathInstruction);
        }

        /// <summary>
        /// Gets a rotation that will orient an object in the direction of the path at the specified distance,
        /// with the local up direction along the path's normal.
        /// </summary>
        /// <param name="dst">The distance travelled along the path.</param>
        /// <param name="endOfPathInstruction">The instruction for handling the end of the path.</param>
        public Quaternion GetRotationAtDistance(float dst, 
            EndOfPathInstruction endOfPathInstruction = EndOfPathInstruction.Loop) 
        {
            float t = dst / Length;
            return GetRotation(t, endOfPathInstruction);
        }
        
        /// <summary>
        /// Finds the closest point on the path from any point in the world.
        /// </summary>
        /// <param name="worldPoint">The world point to find the closest point on the path for.</param>
        public Vector3 GetClosestPointOnPath(Vector3 worldPoint) 
        {
            // Transform the provided worldPoint into VertexPath local-space.
            // This allows to do math on the localPoint's, thus avoiding the need to
            // transform each local vertexpath point into world space via GetPoint.
            Vector3 localPoint = MathUtility.InverseTransformPoint(worldPoint, _transform, _space);

            TimeOnPathData data = CalculateClosestPointOnPathData(localPoint);
            Vector3 localResult = Vector3.Lerp(_localPoints[data.PreviousIndex], _localPoints[data.NextIndex], 
                data.PercentBetweenIndices);

            // Transform local result into world space.
            return MathUtility.TransformPoint(localResult, _transform, _space);
        }

        /// <summary>
        /// Gets the point on the path based on the normalized time (0 is start, 1 is end of path).
        /// </summary>
        /// <param name="t">The normalized time along the path (0 to 1).</param>
        /// <param name="endOfPathInstruction">The instruction for handling the end of the path.</param>
        private Vector3 GetPointAtTime(float t, EndOfPathInstruction endOfPathInstruction = EndOfPathInstruction.Loop) 
        {
            var data = CalculatePercentOnPathData(t, endOfPathInstruction);
            return Vector3.Lerp(GetPoint(data.PreviousIndex), GetPoint (data.NextIndex), data.PercentBetweenIndices);
        }

        /// <summary>
        /// Gets the direction on the path based on the normalized time (0 is start, 1 is end of path).
        /// </summary>
        /// <param name="t">The normalized time along the path (0 to 1).</param>
        /// <param name="endOfPathInstruction">The instruction for handling the end of the path.</param>
        private Vector3 GetDirection(float t, EndOfPathInstruction endOfPathInstruction = EndOfPathInstruction.Loop)
        {
            var data = CalculatePercentOnPathData(t, endOfPathInstruction);
            Vector3 dir = Vector3.Lerp(_localTangents[data.PreviousIndex], _localTangents[data.NextIndex], 
                data.PercentBetweenIndices);
            return MathUtility.TransformDirection(dir, _transform, _space);
        }

        /// <summary>
        /// Gets the normal vector on the path based on the normalized time (0 is start, 1 is end of path).
        /// </summary>
        /// <param name="t">The normalized time along the path (0 to 1).</param>
        /// <param name="endOfPathInstruction">The instruction for handling the end of the path.</param>
        private Vector3 GetNormal(float t, EndOfPathInstruction endOfPathInstruction = EndOfPathInstruction.Loop) 
        {
            var data = CalculatePercentOnPathData(t, endOfPathInstruction);
            Vector3 normal = Vector3.Lerp(LocalNormals[data.PreviousIndex], LocalNormals[data.NextIndex], 
                data.PercentBetweenIndices);
            return MathUtility.TransformDirection(normal, _transform, _space);
        }

        /// <summary>
        /// Gets a rotation that will orient an object in the direction of the path at the specified time,
        /// with the local up direction along the path's normal.
        /// </summary>
        /// <param name="t">The normalized time along the path (0 to 1).</param>
        /// <param name="endOfPathInstruction">The instruction for handling the end of the path.</param>
        private Quaternion GetRotation(float t, EndOfPathInstruction endOfPathInstruction = EndOfPathInstruction.Loop) 
        {
            var data = CalculatePercentOnPathData(t, endOfPathInstruction);
            Vector3 direction = Vector3.Lerp(_localTangents[data.PreviousIndex], _localTangents[data.NextIndex], 
                data.PercentBetweenIndices);
            Vector3 normal = Vector3.Lerp(LocalNormals[data.PreviousIndex], LocalNormals[data.NextIndex], 
                data.PercentBetweenIndices);
            return Quaternion.LookRotation(MathUtility.TransformDirection (direction, _transform, _space), 
                MathUtility.TransformDirection (normal, _transform, _space));
        }
        
        /// <summary>
        /// Finds the normalized time (0=start of path, 1=end of path) along the path that is closest to the given point.
        /// </summary>
        /// <param name="worldPoint">The world point to find the closest time on the path for.</param>
        public float GetClosestTimeOnPath(Vector3 worldPoint) 
        {
            Vector3 localPoint = MathUtility.InverseTransformPoint(worldPoint, _transform, _space);
            TimeOnPathData data = CalculateClosestPointOnPathData(localPoint);
            return Mathf.Lerp(_times[data.PreviousIndex], _times[data.NextIndex], data.PercentBetweenIndices);
        }

        /// <summary>
        /// Finds the distance along the path that is closest to the given point.
        /// </summary>
        /// <param name="worldPoint">The world point to find the closest distance along the path for.</param>
        public float GetClosestDistanceAlongPath(Vector3 worldPoint) 
        {
            Vector3 localPoint = MathUtility.InverseTransformPoint(worldPoint, _transform, _space);
            TimeOnPathData data = CalculateClosestPointOnPathData(localPoint);
            return Mathf.Lerp(_cumulativeLengthAtEachVertex[data.PreviousIndex], 
                _cumulativeLengthAtEachVertex[data.NextIndex], data.PercentBetweenIndices);
        }
        
        /// <summary>
        /// For a given value 't' between 0 and 1, calculates the indices of the two vertices before and after t,
        /// and how far t is between those two vertices as a percentage between 0 and 1.
        /// </summary>
        /// <param name="t">The normalized time along the path (0 to 1).</param>
        /// <param name="endOfPathInstruction">The instruction for handling the end of the path.</param>
        private TimeOnPathData CalculatePercentOnPathData(float t, EndOfPathInstruction endOfPathInstruction) 
        {
            // Constrain t based on the end of path instruction.
            switch (endOfPathInstruction) 
            {
                case EndOfPathInstruction.Loop:
                    // If t is negative, make it the equivalent value between 0 and 1.
                    if (t < 0) 
                    {
                        t += Mathf.CeilToInt(Mathf.Abs(t));
                    }
                    t %= 1;
                    break;
                case EndOfPathInstruction.Reverse:
                    t = Mathf.PingPong(t, 1);
                    break;
                case EndOfPathInstruction.Stop:
                    t = Mathf.Clamp01(t);
                    break;
            }

            int prevIndex = 0;
            int nextIndex = NumPoints - 1;
            int i = Mathf.RoundToInt(t * (NumPoints - 1)); // starting guess

            // Starts by looking at middle vertex and determines if t lies to the left or to the right of that vertex.
            // Continues dividing in half until closest surrounding vertices have been found.
            while (true) 
            {
                // t lies to left.
                if (t <= _times[i]) 
                {
                    nextIndex = i;
                }
                // t lies to right.
                else 
                {
                    prevIndex = i;
                }
                i = (nextIndex + prevIndex) / 2;

                if (nextIndex - prevIndex <= 1) 
                {
                    break;
                }
            }

            float abPercent = Mathf.InverseLerp(_times[prevIndex], _times[nextIndex], t);
            return new TimeOnPathData(prevIndex, nextIndex, abPercent);
        }

        /// <summary>
        /// Calculates the time data for the closest point on the path from the given local point.
        /// </summary>
        /// <param name="localPoint">The local point to find the closest point on the path for.</param>
        private TimeOnPathData CalculateClosestPointOnPathData(Vector3 localPoint) 
        {
            float minSqrDst = float.MaxValue;
            Vector3 closestPoint = Vector3.zero;
            int closestSegmentIndexA = 0;
            int closestSegmentIndexB = 0;

            for (int i = 0; i < _localPoints.Length; i++) 
            {
                int nextI = i + 1;
                if (nextI >= _localPoints.Length) 
                {
                    if (IsClosedLoop) 
                    {
                        nextI %= _localPoints.Length;
                    } 
                    else 
                    {
                        break;
                    }
                }

                Vector3 closestPointOnSegment = MathUtility.ClosestPointOnLineSegment(localPoint, _localPoints[i], 
                    _localPoints[nextI]);
                float sqrDst = (localPoint - closestPointOnSegment).sqrMagnitude;
                if (sqrDst < minSqrDst) 
                {
                    minSqrDst = sqrDst;
                    closestPoint = closestPointOnSegment;
                    closestSegmentIndexA = i;
                    closestSegmentIndexB = nextI;
                }
            }
            float closestSegmentLength = (_localPoints[closestSegmentIndexA] - _localPoints[closestSegmentIndexB]).magnitude;
            float t = (closestPoint - _localPoints[closestSegmentIndexA]).magnitude / closestSegmentLength;
            return new TimeOnPathData(closestSegmentIndexA, closestSegmentIndexB, t);
        }

        /// <summary>
        /// Represents the time data on the path, containing the indices of the previous and next vertices,
        /// and the percentage between those indices.
        /// </summary>
        private struct TimeOnPathData 
        {
            public readonly int PreviousIndex;
            public readonly int NextIndex;
            public readonly float PercentBetweenIndices;

            public TimeOnPathData(int prev, int next, float percentBetweenIndices) 
            {
                PreviousIndex = prev;
                NextIndex = next;
                PercentBetweenIndices = percentBetweenIndices;
            }
        }
    }
}
