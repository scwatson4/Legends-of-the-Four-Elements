using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace AnythingWorld.PathCreation
{
	/// <summary>
	/// Represents screen-space polyline based on a Bezier path.
	/// </summary>
	public class ScreenSpacePolyLine
	{
		private const int AccuracyMultiplier = 10;
		// Don't allow vertices to be spaced too far apart, as screenspace-worldspace conversion can then be
		// noticeably off.
		private const float IntermediaryThreshold = .2f;

		private readonly List<Vector3> _verticesWorld;
		
		private readonly List<int> _vertexToPathSegmentMap;
		private readonly int[] _segmentStartIndices;

		private readonly float _pathLengthWorld;
		private readonly float[] _cumulativeLengthWorld;

		private Vector2[] _points;

		private Vector3 _prevCamPos;
		private Quaternion _prevCamRot;
		private bool _prevCamIsOrtho;

		private readonly Transform _transform;
		private readonly Vector3 _transformPosition;
		private readonly Quaternion _transformRotation;
		private readonly Vector3 _transformScale;

		public ScreenSpacePolyLine(BezierPath bezierPath, Transform transform, float maxAngleError, float minVertexDst, 
			float accuracy = 1)
		{
			_transform = transform;
			_transformPosition = transform.position;
			_transformRotation = transform.rotation;
			_transformScale = transform.localScale;

			// Split path in vertices based on angle error.
			_verticesWorld = new List<Vector3>();
			_vertexToPathSegmentMap = new List<int>();
			_segmentStartIndices = new int[bezierPath.NumSegments + 1];

			_verticesWorld.Add(bezierPath[0]);
			_vertexToPathSegmentMap.Add(0);

			for (int segmentIndex = 0; segmentIndex < bezierPath.NumSegments; segmentIndex++)
			{
				Vector3[] segmentPoints = bezierPath.GetPointsInSegment(segmentIndex);
				_verticesWorld.Add(segmentPoints[0]);
				_vertexToPathSegmentMap.Add(segmentIndex);
				_segmentStartIndices[segmentIndex] = _verticesWorld.Count - 1;

				var prevPointOnPath = segmentPoints[0];
				var lastAddedPoint = prevPointOnPath;
				float dstSinceLastVertex = 0;
				float dstSinceLastIntermediary = 0;

				float estimatedSegmentLength = CubicBezierUtility.EstimateCurveLength(segmentPoints[0], 
					segmentPoints[1], segmentPoints[2], segmentPoints[3]);
				int divisions = Mathf.CeilToInt(estimatedSegmentLength * accuracy * AccuracyMultiplier);
				float increment = 1f / divisions;

				for (float t = increment; t <= 1; t += increment)
				{
					Vector3 pointOnPath = CubicBezierUtility.EvaluateCurve(segmentPoints[0], segmentPoints[1], 
						segmentPoints[2], segmentPoints[3], t);
					Vector3 nextPointOnPath = CubicBezierUtility.EvaluateCurve(segmentPoints[0], segmentPoints[1], 
						segmentPoints[2], segmentPoints[3], t + increment);

					// Angle at current point on path.
					float localAngle = 180 - MathUtility.MinAngle(prevPointOnPath, pointOnPath, nextPointOnPath);
					// Angle between the last added vertex, the current point on the path, and the next point on the path.
					float angleFromPrevVertex = 180 - MathUtility.MinAngle(lastAddedPoint, pointOnPath, nextPointOnPath);
					float angleError = Mathf.Max(localAngle, angleFromPrevVertex);


					if (angleError > maxAngleError && dstSinceLastVertex >= minVertexDst)
					{
						dstSinceLastVertex = 0;
						dstSinceLastIntermediary = 0;
						_verticesWorld.Add(pointOnPath);
						_vertexToPathSegmentMap.Add(segmentIndex);
						lastAddedPoint = pointOnPath;
					}
					else
					{
						if (dstSinceLastIntermediary > IntermediaryThreshold)
						{
							_verticesWorld.Add(pointOnPath);
							_vertexToPathSegmentMap.Add(segmentIndex);
							dstSinceLastIntermediary = 0;
						}
						else
						{
							dstSinceLastIntermediary += (pointOnPath - prevPointOnPath).magnitude;
						}
						dstSinceLastVertex += (pointOnPath - prevPointOnPath).magnitude;
					}
					prevPointOnPath = pointOnPath;
				}
			}

			_segmentStartIndices[bezierPath.NumSegments] = _verticesWorld.Count;

			// Ensure final point gets added (unless path is closed loop).
			if (!bezierPath.IsClosed)
			{
				_verticesWorld.Add(bezierPath[bezierPath.NumPoints - 1]);
			}
			else
			{
				_verticesWorld.Add(bezierPath[0]);
			}

			// Calculate length.
			_cumulativeLengthWorld = new float[_verticesWorld.Count];
			for (int i = 0; i < _verticesWorld.Count; i++)
			{
				_verticesWorld[i] = MathUtility.TransformPoint(_verticesWorld[i], transform, bezierPath.Space);
				if (i > 0)
				{
					_pathLengthWorld += (_verticesWorld[i - 1] - _verticesWorld[i]).magnitude;
					_cumulativeLengthWorld[i] = _pathLengthWorld;
				}
			}

		}

        /// <summary>
		/// Gets information about the mouse position relative to the polyline.
        /// </summary>
		public MouseInfo CalculateMouseInfo()
		{
			ComputeScreenSpace();

			Vector2 mousePos = Event.current.mousePosition;
			float minDst = float.MaxValue;
			int closestPolyLineSegmentIndex = 0;
			int closestBezierSegmentIndex = 0;

			for (int i = 0; i < _points.Length - 1; i++)
			{
				float dst = HandleUtility.DistancePointToLineSegment(mousePos, _points[i], _points[i + 1]);

				if (dst < minDst)
				{
					minDst = dst;
					closestPolyLineSegmentIndex = i;
					closestBezierSegmentIndex = _vertexToPathSegmentMap[i];
				}
			}

			Vector2 closestPointOnLine = MathUtility.ClosestPointOnLineSegment(mousePos, 
				_points[closestPolyLineSegmentIndex], _points[closestPolyLineSegmentIndex + 1]);
			float dstToPointOnLine = (_points[closestPolyLineSegmentIndex] - closestPointOnLine).magnitude;

			float d = (_points[closestPolyLineSegmentIndex] - _points[closestPolyLineSegmentIndex + 1]).magnitude;
			float percentBetweenVertices = (d == 0) ? 0 : dstToPointOnLine / d;
			Vector3 closestPoint3D = Vector3.Lerp(_verticesWorld[closestPolyLineSegmentIndex], 
				_verticesWorld[closestPolyLineSegmentIndex + 1], percentBetweenVertices);

			float distanceAlongPathWorld = _cumulativeLengthWorld[closestPolyLineSegmentIndex] + 
			                               Vector3.Distance(_verticesWorld[closestPolyLineSegmentIndex], closestPoint3D);
			float timeAlongPath = distanceAlongPathWorld / _pathLengthWorld;

			// Calculate how far between the current bezier segment the closest point on the line is.
			int bezierSegmentStartIndex = _segmentStartIndices[closestBezierSegmentIndex];
			int bezierSegmentEndIndex = _segmentStartIndices[closestBezierSegmentIndex + 1];
			float bezierSegmentLength = _cumulativeLengthWorld[bezierSegmentEndIndex] - 
			                            _cumulativeLengthWorld[bezierSegmentStartIndex];
			float distanceAlongBezierSegment = distanceAlongPathWorld - _cumulativeLengthWorld[bezierSegmentStartIndex];
			float timeAlongBezierSegment = distanceAlongBezierSegment / bezierSegmentLength;

			return new MouseInfo(minDst, closestPoint3D, distanceAlongPathWorld, timeAlongPath, timeAlongBezierSegment,
				closestBezierSegmentIndex);
		}

        /// <summary>
		/// Is stored transform data misaligned with current transform? 
        /// </summary>
		public bool TransformIsOutOfDate()
		{
			return _transform.position != _transformPosition || _transform.rotation != _transformRotation || 
			       _transform.localScale != _transformScale;
		}

        /// <summary>
		/// Update the screen space coordinates of the polyline vertices if the camera has moved or changed orientation.
        /// </summary>
		private void ComputeScreenSpace()
		{
			if (Camera.current.transform.position != _prevCamPos || Camera.current.transform.rotation != _prevCamRot || 
			    Camera.current.orthographic != _prevCamIsOrtho)
			{
				_points = new Vector2[_verticesWorld.Count];
				for (int i = 0; i < _verticesWorld.Count; i++)
				{
					_points[i] = HandleUtility.WorldToGUIPoint(_verticesWorld[i]);
				}

				_prevCamPos = Camera.current.transform.position;
				_prevCamRot = Camera.current.transform.rotation;
				_prevCamIsOrtho = Camera.current.orthographic;
			}
		}

		// Struct to hold mouse information.
		public struct MouseInfo
		{
			public readonly float MouseDstToLine;
			public readonly Vector3 ClosestWorldPointToMouse;
			public readonly float DistanceAlongPathWorld;
			public readonly float TimeOnPath;
			public readonly float TimeOnBezierSegment;
			public readonly int ClosestSegmentIndex;


			public MouseInfo(float mouseDstToLine, Vector3 closestWorldPointToMouse, float distanceAlongPathWorld, float timeOnPath, float timeOnBezierSegment, int closestSegmentIndex)
			{
				MouseDstToLine = mouseDstToLine;
				ClosestWorldPointToMouse = closestWorldPointToMouse;
				DistanceAlongPathWorld = distanceAlongPathWorld;
				TimeOnPath = timeOnPath;
				TimeOnBezierSegment = timeOnBezierSegment;
				ClosestSegmentIndex = closestSegmentIndex;
			}
		}
	}
}
