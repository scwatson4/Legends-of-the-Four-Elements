using System;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace AnythingWorld.PathCreation
{
	/// A bezier path is a path made by stitching together any number of (cubic) bezier curves.
	/// A single cubic bezier curve is defined by 4 points: anchor1, control1, control2, anchor2
	/// The curve moves between the 2 anchors, and the shape of the curve is affected by the positions of the 2 control points

	/// When two curves are stitched together, they share an anchor point (end anchor of curve 1 = start anchor of curve 2).
	/// So while one curve alone consists of 4 points, two curves are defined by 7 unique points.

	/// Apart from storing the points, this class also provides methods for working with the path.
	/// For example, adding, inserting, and deleting points.
	
	/// <summary>
	/// Class to hold the state and manage all the changes made to anchor and control points of Bezier curve.
	/// </summary>
	[Serializable]
	public class BezierPath
	{
		public event Action OnModified;
		
		public enum ControlMode
		{
			Aligned, 
			Mirrored, 
			Automatic,
			Free
		}

		public enum SurfaceOrientation
		{
			Automatic,
			SameAsPathTransform,
			Up,
			Down,
			Left,
			Right,
			Forward,
			Backward
		}

		[SerializeField, HideInInspector]
		public Transform pathTransform;
		[SerializeField, HideInInspector]
		public bool isSnappedToSurface;
		[SerializeField, HideInInspector]
		public SurfaceOrientation surfaceOrientation;
		[SerializeField, HideInInspector]
		private List<Vector3> localPoints;
		[SerializeField, HideInInspector]
		private bool isClosed;
		[SerializeField, HideInInspector]
		private PathSpace space;
		[SerializeField, HideInInspector]
		private ControlMode controlMode;
		[SerializeField, HideInInspector]
		private float autoControlLength = .3f;

		// Normals settings.
		[SerializeField, HideInInspector]
		private List<float> perAnchorNormalsAngle;
		[SerializeField, HideInInspector]
		private float globalNormalsAngle;
		[SerializeField, HideInInspector]
		private bool flipNormals;
		
		private Vector3 _vectorUp;
		private float _defaultGlobalNormalsAngle;

        /// <summary>
        /// Creates a two-anchor path centred around the given centre point.
        /// </summary>
        public BezierPath(Vector3 centre, bool isPathSnappedToSurface, 
	        SurfaceOrientation surfaceOrientation, Transform pathTransform = null, float normalsAngle = 0, 
	        bool isClosed = false, PathSpace space = PathSpace.XYZ)
		{
			Vector3 dir = Vector3.forward;
			float width = 2;
			float controlHeight = .5f;
			float controlWidth = 1f;
			localPoints = new List<Vector3> 
			{
 				centre + Vector3.left * width,
 				centre + Vector3.left * controlWidth + dir * controlHeight,
 				centre + Vector3.right * controlWidth - dir * controlHeight,
 				centre + Vector3.right * width
			};

			globalNormalsAngle = _defaultGlobalNormalsAngle = normalsAngle;
			perAnchorNormalsAngle = new List<float> { 0, 0 };
			IsClosed = isClosed;
			Space = space;
			isSnappedToSurface = isPathSnappedToSurface;
			this.surfaceOrientation = surfaceOrientation;
			if (pathTransform)
			{
				this.pathTransform = pathTransform;
			}
		}

        /// <summary>
		/// Creates a path from the supplied 3D points.
        /// </summary>
		public BezierPath(IEnumerable<Vector3> points, bool isClosed = false, PathSpace space = PathSpace.XYZ)
		{
			Vector3[] pointsArray = points.ToArray();

			if (pointsArray.Length < 2)
			{
				Debug.LogError("Path requires at least 2 anchor points.");
			}
			else
			{
				controlMode = ControlMode.Automatic;
				localPoints = new List<Vector3> { pointsArray[0], Vector3.zero, Vector3.zero, pointsArray[1] };
				perAnchorNormalsAngle = new List<float>(new float[] { 0, 0 });

#if UNITY_EDITOR
				for (int i = 2; i < pointsArray.Length; i++)
				{
					AddSegmentToEnd(pointsArray[i]);
					perAnchorNormalsAngle.Add(0);
				}
#endif
			}

			Space = space;
			IsClosed = isClosed;
		}

        /// <summary>
		/// Creates a path from the positions of the supplied 2D points.
        /// </summary>
		public BezierPath(IEnumerable<Vector2> transforms, bool isClosed = false, PathSpace space = PathSpace.XZ) :
			this(transforms.Select(p => new Vector3(p.x, p.y)), isClosed, space){}

        /// <summary>
		/// Creates a path from the positions of the supplied transforms.
        /// </summary>
		public BezierPath(IEnumerable<Transform> transforms, bool isClosed = false, PathSpace space = PathSpace.XZ) :
			this(transforms.Select(t => t.position), isClosed, space){}

        /// <summary>
		/// Creates a path from the supplied 2D points.
        /// </summary>
		public BezierPath(IEnumerable<Vector2> points, PathSpace space = PathSpace.XYZ, bool isClosed = false) :
			this(points.Select(p => new Vector3(p.x, p.y)), isClosed, space){}

		// Get local space position of point.
		public Vector3 this[int i] => GetPoint(i);

        /// <summary>
		/// Get local space position of point.
        /// </summary>
		public Vector3 GetPoint(int i) => localPoints[i];

        /// <summary>
		/// Set local space position of point.
        /// </summary>
		public void SetPoint(int i, Vector3 localPosition, bool suppressPathModifiedEvent = false)
		{
			localPoints[i] = localPosition;
			if (!suppressPathModifiedEvent)
			{
				NotifyPathModified();
			}
		}

        public bool IsInitialized => localPoints != null;
	        
		// Total number of points in the path (anchors and controls).
		public int NumPoints => localPoints.Count;

		// Number of anchor points making up the path.
		public int NumAnchorPoints => IsClosed ? localPoints.Count / 3 : (localPoints.Count + 2) / 3;

		// Number of bezier curves making up this path.
		public int NumSegments => localPoints.Count / 3;
		
		// Gets the current space of the curve, updates curve to new 2d space if setting was changed from 3d to 2d.
		public PathSpace Space
		{
			get => space;

			set
			{
				if (value != space)
				{
					PathSpace previousSpace = space;
					space = value;
					UpdateToNewPathSpace(previousSpace);
				}
			}
		}

		// If closed, path will loop back from end point to start point.
		public bool IsClosed
		{
			get => isClosed;
			set
			{
				if (isClosed != value)
				{
					isClosed = value;
					UpdateClosedState();
				}
			}
		}

		// The control mode determines the behaviour of control points.
		// Possible modes are:
		// Aligned = controls stay in straight line around their anchor.
		// Mirrored = controls stay in straight, equidistant line around their anchor.
		// Free = no constraints (use this if sharp corners are needed).
		// Automatic = controls placed automatically to try make the path smooth.
		public ControlMode ControlPointMode
		{
			get => controlMode;
			set
			{
				if (controlMode != value)
				{
					controlMode = value;
					if (controlMode == ControlMode.Automatic)
					{
						AutoSetAllControlPoints();
						NotifyPathModified();
					}
				}
			}
		}

		// When using automatic control point placement, this value scales how far apart controls are placed.
		public float AutoControlLength
		{
			get => autoControlLength;
			set
			{
				value = Mathf.Max(value, .01f);
				if (autoControlLength != value)
				{
					autoControlLength = value;
					AutoSetAllControlPoints();
					NotifyPathModified();
				}
			}
		}
		
		// Returns an array of the 4 points making up the segment (anchor1, control1, control2, anchor2).
		public Vector3[] GetPointsInSegment(int segmentIndex)
		{
			segmentIndex = Mathf.Clamp(segmentIndex, 0, NumSegments - 1);
			return new Vector3[] { this[segmentIndex * 3], this[segmentIndex * 3 + 1], this[segmentIndex * 3 + 2], 
				this[LoopIndex(segmentIndex * 3 + 3)] };
		}

#if UNITY_EDITOR
		/// <summary>
		/// Add new anchor point to end of the path.
        /// </summary>
		public void AddSegmentToEnd(Vector3 anchorPos)
		{
			if (isClosed)
			{
				return;
			}

			int lastAnchorIndex = localPoints.Count - 1;
			// Set position for new control to be mirror of its counterpart.
			Vector3 secondControlForOldLastAnchorOffset = localPoints[lastAnchorIndex] - localPoints[lastAnchorIndex - 1];
			if (controlMode != ControlMode.Mirrored && controlMode != ControlMode.Automatic)
			{
				// Set position for new control to be aligned with its counterpart, but with a length of half
				// the distance from prev to new anchor.
				float dstPrevToNewAnchor = (localPoints[lastAnchorIndex] - anchorPos).magnitude;
				secondControlForOldLastAnchorOffset = (localPoints[lastAnchorIndex] - 
				                                       localPoints[lastAnchorIndex - 1]).normalized * 
				                                      (dstPrevToNewAnchor * .5f);
			}
			Vector3 secondControlForOldLastAnchor = localPoints[lastAnchorIndex] + secondControlForOldLastAnchorOffset;
			Vector3 controlForNewAnchor = (anchorPos + secondControlForOldLastAnchor) * .5f;

			localPoints.Add(secondControlForOldLastAnchor);
			localPoints.Add(controlForNewAnchor);
			localPoints.Add(anchorPos);
			
			if (isSnappedToSurface)
			{
				TrySnapControlPointToSurface(lastAnchorIndex, lastAnchorIndex + 1, true, true);
				TrySnapControlPointToSurface(lastAnchorIndex + 3, lastAnchorIndex + 2, true, true);
			}
			
			perAnchorNormalsAngle.Add(perAnchorNormalsAngle[perAnchorNormalsAngle.Count - 1]);

			if (controlMode == ControlMode.Automatic)
			{
				AutoSetAllAffectedControlPoints(localPoints.Count - 1);
			}

			NotifyPathModified();
		}

        /// <summary>
		/// Add new anchor point to start of the path.
        /// </summary>
		public void AddSegmentToStart(Vector3 anchorPos)
		{
			if (isClosed)
			{
				return;
			}

			// Set position for new control to be mirror of its counterpart.
			Vector3 secondControlForOldFirstAnchorOffset = localPoints[0] - localPoints[1];
			if (controlMode != ControlMode.Mirrored && controlMode != ControlMode.Automatic)
			{
				// Set position for new control to be aligned with its counterpart, but with a length of half
				// the distance from prev to new anchor.
				float dstPrevToNewAnchor = (localPoints[0] - anchorPos).magnitude;
				secondControlForOldFirstAnchorOffset = secondControlForOldFirstAnchorOffset.normalized * 
				                                       (dstPrevToNewAnchor * .5f);
			}

			Vector3 secondControlForOldFirstAnchor = localPoints[0] + secondControlForOldFirstAnchorOffset;
			Vector3 controlForNewAnchor = (anchorPos + secondControlForOldFirstAnchor) * .5f;
			localPoints.Insert(0, anchorPos);
			localPoints.Insert(1, controlForNewAnchor);
			localPoints.Insert(2, secondControlForOldFirstAnchor);
			
			if (isSnappedToSurface)
			{
				TrySnapControlPointToSurface(0, 1, true, true);
				TrySnapControlPointToSurface(3, 2, true, true);
			}
			
			perAnchorNormalsAngle.Insert(0, perAnchorNormalsAngle[0]);

			if (controlMode == ControlMode.Automatic)
			{
				AutoSetAllAffectedControlPoints(0);
			}
			NotifyPathModified();
		}

        /// <summary>
		/// Insert new anchor point at given position. Automatically place control points around it so as to
		/// keep shape of curve the same.
        /// </summary>
		public void SplitSegment(Vector3 anchorPos, int segmentIndex, float splitTime)
		{
			if (float.IsNaN(splitTime))
			{
				Debug.Log("Trying to split segment, but given value was invalid");
				return;
			}

			splitTime = Mathf.Clamp01(splitTime);

			if (controlMode == ControlMode.Automatic)
			{
				localPoints.InsertRange(segmentIndex * 3 + 2, new Vector3[] { Vector3.zero, anchorPos, Vector3.zero });
				AutoSetAllAffectedControlPoints(segmentIndex * 3 + 3);
			}
			else
			{
				// Split the curve to find where control points can be inserted to least affect shape of curve.
				// Curve will probably be deformed slightly since splitTime is only an estimate
				// (for performance reasons, and so doesn't correspond exactly with anchorPos).
				Vector3[][] splitSegment = CubicBezierUtility.SplitCurve(GetPointsInSegment(segmentIndex), splitTime);
				localPoints.InsertRange(segmentIndex * 3 + 2, new Vector3[] { splitSegment[0][2], splitSegment[1][0], 
					splitSegment[1][1] });
				int newAnchorIndex = segmentIndex * 3 + 3;
				MovePoint(newAnchorIndex - 2, splitSegment[0][1], true);
				MovePoint(newAnchorIndex + 2, splitSegment[1][2], true);
				MovePoint(newAnchorIndex, anchorPos, true);

				if (isSnappedToSurface)
				{
					TrySnapControlPointToSurface(newAnchorIndex - 3, newAnchorIndex - 2, true, true);
				}
				
				if (controlMode == ControlMode.Mirrored)
				{
					float avgDst = ((splitSegment[0][2] - anchorPos).magnitude + 
					                (splitSegment[1][1] - anchorPos).magnitude) / 2;
					MovePoint(newAnchorIndex + 1, anchorPos + 
					                              (splitSegment[1][1] - anchorPos).normalized * avgDst, true);
				}
			}

			// Insert angle for new anchor (value should be set in between neighbour anchor angles).
			int newAnchorAngleIndex = (segmentIndex + 1) % perAnchorNormalsAngle.Count;
			float anglePrev = perAnchorNormalsAngle[segmentIndex];
			float angleNext = perAnchorNormalsAngle[newAnchorAngleIndex];
			float splitAngle = Mathf.LerpAngle(anglePrev, angleNext, splitTime);
			perAnchorNormalsAngle.Insert(newAnchorAngleIndex, splitAngle);

			NotifyPathModified();
		}

        /// <summary>
		/// Delete the anchor point at given index, as well as its associated control points.
        /// </summary>
		public void DeleteSegment(int anchorIndex)
		{
			// Don't delete segment if its the last one remaining (or if only two segments in a closed path).
			if (NumSegments > 2 || !isClosed && NumSegments > 1)
			{
				if (anchorIndex == 0)
				{
					if (isClosed)
					{
						localPoints[localPoints.Count - 1] = localPoints[2];
					}
					localPoints.RemoveRange(0, 3);
				}
				else if (anchorIndex == localPoints.Count - 1 && !isClosed)
				{
					localPoints.RemoveRange(anchorIndex - 2, 3);
				}
				else
				{
					localPoints.RemoveRange(anchorIndex - 1, 3);
				}

				perAnchorNormalsAngle.RemoveAt(anchorIndex / 3);

				if (controlMode == ControlMode.Automatic)
				{
					AutoSetAllControlPoints();
				}

				NotifyPathModified();
			}
		}
		
        /// <summary>
		/// Move an existing point to a new position.
        /// </summary>
		public void MovePoint(int i, Vector3 pointPos, bool suppressPathModifiedEvent = false)
		{
			var needSnapPointToSurface = isSnappedToSurface;
			if (space == PathSpace.XZ)
			{
				needSnapPointToSurface = false;
			}
			
			if (needSnapPointToSurface)
			{
				SetSurfaceNormal();
			}
			
			Vector3 deltaMove = pointPos - localPoints[i];
			bool isAnchorPoint = i % 3 == 0;
				
			// Don't process control point if control mode is set to automatic.
			if (isAnchorPoint || controlMode != ControlMode.Automatic)
			{
				localPoints[i] = pointPos;

				if (controlMode == ControlMode.Automatic)
				{
					AutoSetAllAffectedControlPoints(i);
				}
				else
				{
					// Move control points with anchor point.
					if (isAnchorPoint)
					{
						bool isFirstSnapped = false, isSecondSnapped = false;
						
						var isFirstMoved = i + 1 < localPoints.Count || isClosed;
						var isSecondMoved =  i - 1 >= 0 || isClosed;
						
						if (isFirstMoved)
						{
							localPoints[LoopIndex(i + 1)] += deltaMove;
							if (needSnapPointToSurface)
							{
								isFirstSnapped = TrySnapControlPointToSurface(i, LoopIndex(i + 1));
							}
						}
						if (isSecondMoved)
						{
							localPoints[LoopIndex(i - 1)] += deltaMove;
							if (needSnapPointToSurface)
							{
								isSecondSnapped = TrySnapControlPointToSurface(i, LoopIndex(i - 1));
							}
						}
						
						if (needSnapPointToSurface && ControlPointMode != ControlMode.Free)
						{
							if (isFirstMoved && !isFirstSnapped && isSecondSnapped)
							{
								PlaceAttachedControl(i, LoopIndex(i + 1), LoopIndex(i - 1), true);
							}
							else if (isFirstMoved && !isFirstSnapped)
							{
								isFirstSnapped = TrySnapFloatingControl(i, LoopIndex(i + 1));
							}

							if (isSecondMoved && !isSecondSnapped && isFirstSnapped)
							{
								PlaceAttachedControl(i, LoopIndex(i - 1), LoopIndex(i + 1), true);
							}
							else if (isSecondMoved && !isSecondSnapped)
							{
								TrySnapFloatingControl(i, LoopIndex(i - 1));
							}
						}
					}
					else
					{
						var nextPointIsAnchor = (i + 1) % 3 == 0;
						var attachedControlIndex = nextPointIsAnchor ? i + 2 : i - 2;
						var anchorIndex = nextPointIsAnchor ? i + 1 : i - 1;

						if (attachedControlIndex >= 0 && attachedControlIndex < localPoints.Count || isClosed)
						{
							PlaceAttachedControl(anchorIndex, attachedControlIndex, i, needSnapPointToSurface);
						}
					}
				}

				if (!suppressPathModifiedEvent)
				{
					NotifyPathModified();
				}
			}
		}
		
        /// <summary>
		/// If not in free control mode, then move attached control point to
		/// be aligned/mirrored (depending on mode).
        /// </summary>
		private void PlaceAttachedControl(int anchorIndex, int attachedControlIndex, int oppositeControlIndex, 
			bool needSnapPointToSurface)
		{
			if (controlMode == ControlMode.Free)
			{
				return;
			}
						
			var anchorLoopedIdx = LoopIndex(anchorIndex);
			var anchorPos = localPoints[anchorLoopedIdx];
			var attachedControlLoopedIdx = LoopIndex(attachedControlIndex);
			var attachedControlPos = localPoints[attachedControlLoopedIdx];
			var oppositeControlLoopedIdx = LoopIndex(oppositeControlIndex);
			var oppositeControlPos = localPoints[oppositeControlLoopedIdx];
	                    
			float distanceFromAnchor = 0;
						
			// If in aligned mode, then attached control's current distance from anchor point
			// should be maintained.
			if (controlMode == ControlMode.Aligned)
			{
				distanceFromAnchor = (anchorPos - attachedControlPos).magnitude;
			}
			// If in mirrored mode, then both control points should have the same distance
			// from the anchor point.
			else if (controlMode == ControlMode.Mirrored)
			{
				distanceFromAnchor = (anchorPos - oppositeControlPos).magnitude;
			}
			
			var dir = (anchorPos - oppositeControlPos).normalized;
			localPoints[attachedControlLoopedIdx] = anchorPos + 
			                                        dir * distanceFromAnchor;
			if (needSnapPointToSurface)
			{
				TrySnapControlPointToSurface(anchorLoopedIdx, attachedControlLoopedIdx);
			}
		}
        
		/// <summary>
		/// Try to find a point on the surface to snap the control point to.
        /// </summary>
		private bool TrySnapFloatingControl(int anchorIdx, int attachedControlIdx)
		{
			var isUpVectorSet = _vectorUp != Vector3.zero;
			var anchorGlobal = MathUtility.TransformPoint(localPoints[anchorIdx], pathTransform, PathSpace.XYZ);
			var editorCameraPosition = SceneView.lastActiveSceneView.camera.transform.position;
			var distToCam = Vector3.Distance(editorCameraPosition, anchorGlobal);
			var dirFromCam = (anchorGlobal - editorCameraPosition) / distToCam;
			var nudgeLength = 0.01f;

			if (!Physics.Raycast(editorCameraPosition, dirFromCam, out var anchorHit, distToCam + nudgeLength))
			{
				return false;
			}

			localPoints[anchorIdx] = MathUtility.InverseTransformPoint(anchorHit.point, pathTransform, PathSpace.XYZ);
			anchorGlobal = anchorHit.point;
			var anchorNormal = anchorHit.normal;
			var isAnchorSurfaceFlat = IsSurfaceFlat(anchorNormal);
			var attachedControlGlobal = MathUtility.TransformPoint(localPoints[attachedControlIdx], pathTransform, 
				PathSpace.XYZ);

			var distanceFromAnchor = Vector3.Distance(anchorGlobal, attachedControlGlobal);
			
			distToCam = Vector3.Distance(editorCameraPosition, attachedControlGlobal);
			dirFromCam = (attachedControlGlobal - editorCameraPosition) / distToCam;
			if (Physics.Raycast(editorCameraPosition, dirFromCam, distToCam + nudgeLength))
			{
				return TrySnapControlPointToSurface(anchorIdx, attachedControlIdx);
			}
			
			var nudgeDir = isAnchorSurfaceFlat || !isUpVectorSet ? anchorNormal : _vectorUp; 
			if (!Physics.Raycast(attachedControlGlobal, -nudgeDir, out var surfaceHit, distanceFromAnchor))
			{
				return false;
			}
			
			var dirToSurfaceHit = (surfaceHit.point - anchorGlobal).normalized;
			var newPointPos = anchorGlobal + dirToSurfaceHit * distanceFromAnchor;
			localPoints[attachedControlIdx] = 
				MathUtility.InverseTransformPoint(newPointPos, pathTransform, PathSpace.XYZ);
			return TrySnapControlPointToSurface(anchorIdx, attachedControlIdx);
		}

        /// <summary>
		/// Sets the surface normal based on the surface orientation setting.
        /// </summary>
		private void SetSurfaceNormal()
		{
			switch (surfaceOrientation)
			{
				case SurfaceOrientation.Automatic:
					_vectorUp = Vector3.zero;
					break;
				case SurfaceOrientation.SameAsPathTransform:
					_vectorUp = pathTransform.up;
					break;
				case SurfaceOrientation.Up:
					_vectorUp = Vector3.up;
					break;
				case SurfaceOrientation.Down:
					_vectorUp = Vector3.down;
					break;
				case SurfaceOrientation.Left:
					_vectorUp = Vector3.left;
					break;
				case SurfaceOrientation.Right:
					_vectorUp = Vector3.right;
					break;
				case SurfaceOrientation.Forward:
					_vectorUp = Vector3.forward;
					break;
				case SurfaceOrientation.Backward:
					_vectorUp = Vector3.back;
					break;
			}
		}

        /// <summary>
		/// Try to snap control point to surface.
        /// </summary>
		private bool TrySnapControlPointToSurface(int anchorIdx, int attachedControlIdx, bool snapFloating = false, 
	        bool snapAnchor = false)
		{
			var isUpVectorSet = _vectorUp != Vector3.zero;
			var anchorGlobal = MathUtility.TransformPoint(localPoints[anchorIdx], pathTransform, PathSpace.XYZ);
			var editorCameraPosition = SceneView.lastActiveSceneView.camera.transform.position;
			var distToCam = Vector3.Distance(editorCameraPosition, anchorGlobal);
			var dirFromCam = (anchorGlobal - editorCameraPosition) / distToCam;
			var nudgeLength = 0.01f;
			
			if (!Physics.Raycast(editorCameraPosition, dirFromCam, out var anchorHit, distToCam + nudgeLength))
			{
				if (!snapAnchor)
				{
					return false;
				}
				
				var neighbourAnchorIdx = anchorIdx == 0 ? 3 : anchorIdx - 3;

				if (neighbourAnchorIdx >= 0 || isClosed)
				{
					Vector3 dirToGround;
					var loopedIdx = LoopIndex(neighbourAnchorIdx);
					var neighbourAnchorGlobal = MathUtility.TransformPoint(localPoints[loopedIdx], pathTransform, 
						PathSpace.XYZ);
					if (isUpVectorSet)
					{
						dirToGround = -_vectorUp;
					}
					else
					{
						editorCameraPosition = SceneView.lastActiveSceneView.camera.transform.position;
						distToCam = Vector3.Distance(editorCameraPosition, neighbourAnchorGlobal);
						dirFromCam = (neighbourAnchorGlobal - editorCameraPosition) / distToCam;
						if (!Physics.Raycast(editorCameraPosition, dirFromCam, out var neighbourAnchorHit, distToCam + 
							    nudgeLength))
						{
							return false;
						}

						dirToGround = -neighbourAnchorHit.normal;
					}
					if (!Physics.Raycast(anchorGlobal, dirToGround, out anchorHit, Vector3.Distance(neighbourAnchorGlobal, 
						    anchorGlobal)))
					{
						return false;
					}
				}
				else
				{
					return false;
				}
			}

			localPoints[anchorIdx] = MathUtility.InverseTransformPoint(anchorHit.point, pathTransform, PathSpace.XYZ);
			anchorGlobal = anchorHit.point;
			var anchorNormal = anchorHit.normal;

			var isAnchorSurfaceFlat = IsSurfaceFlat(anchorNormal);
			var attachedControlGlobal = MathUtility.TransformPoint(localPoints[attachedControlIdx], pathTransform, 
				PathSpace.XYZ);
			
			var distanceFromAnchor = Vector3.Distance(anchorGlobal, attachedControlGlobal);
			var dirToControl = (attachedControlGlobal - anchorGlobal).normalized;
			
			var nudgeDir = isAnchorSurfaceFlat || !isUpVectorSet ? anchorNormal : _vectorUp; 
			var anchorNudged = anchorGlobal + nudgeDir * (nudgeLength / 2);
			
			if (!Physics.Raycast(anchorNudged, dirToControl, out var surfaceHit, distanceFromAnchor))
			{
			 	distToCam = Vector3.Distance(editorCameraPosition, attachedControlGlobal);
				dirFromCam = (attachedControlGlobal - editorCameraPosition) / distToCam;
				var isControlSnapped = Physics.Raycast(editorCameraPosition, dirFromCam, distToCam + nudgeLength);
				if (snapFloating)
				{
					return TrySnapFloatingControl(anchorIdx, attachedControlIdx);
				}
				return isControlSnapped;
			}

			if (surfaceHit.normal == anchorNormal)
			{
				dirToControl = (surfaceHit.point - anchorGlobal).normalized;
				if (!Physics.Raycast(anchorNudged, dirToControl, out surfaceHit, distanceFromAnchor))
				{ 
					var newControlPointGlobal = anchorGlobal + dirToControl * distanceFromAnchor;
					          Vector3.Distance(anchorGlobal, attachedControlGlobal);
					localPoints[attachedControlIdx] = MathUtility.InverseTransformPoint(newControlPointGlobal, 
						pathTransform, PathSpace.XYZ);
					return true;
				}
			}
			
			if (isAnchorSurfaceFlat)
			{
				attachedControlGlobal = surfaceHit.point + anchorNormal * (nudgeLength * 2);
			}
			else
			{
				var dirToSurface = (surfaceHit.point - attachedControlGlobal).normalized;
				attachedControlGlobal = surfaceHit.point + dirToSurface * (nudgeLength * 2);
			}

			dirToControl = (attachedControlGlobal - anchorGlobal).normalized;
			if (!Physics.Raycast(anchorNudged, dirToControl, out var hit2, distanceFromAnchor * 2))
			{
				var oppositeIdx = anchorIdx + 1;
				if (oppositeIdx == attachedControlIdx)
				{
					oppositeIdx = anchorIdx - 1;
				}

				if ((oppositeIdx < 0 || oppositeIdx > localPoints.Count) && !isClosed)
				{
					return false;
				}
				
				var oppositeControlGlobal =
					MathUtility.TransformPoint(localPoints[LoopIndex(oppositeIdx)], pathTransform, PathSpace.XYZ);
				distToCam = Vector3.Distance(editorCameraPosition, oppositeControlGlobal);
				dirFromCam = (oppositeControlGlobal - editorCameraPosition) / distToCam;
				if (!Physics.Raycast(editorCameraPosition, dirFromCam, out var controlHit, distToCam + nudgeLength))
				{
					return false;
				}
				var oppositeNudged = oppositeControlGlobal + controlHit.normal * nudgeLength;
				
				var distToControl = Vector3.Distance(anchorGlobal, oppositeControlGlobal) + distanceFromAnchor;
				dirToControl = (attachedControlGlobal - oppositeControlGlobal).normalized;

				if (!Physics.Raycast(oppositeNudged, dirToControl, out hit2, distToControl * 2))
				{
					return false;
				}
			}

			var dirAlongSurface = (hit2.point - surfaceHit.point).normalized;
			var intersections = MathUtility.FindSphereLineIntersections(anchorGlobal, distanceFromAnchor, 
				hit2.point, dirAlongSurface);
			if (intersections.Count == 0)
			{
				return false;
			}
			
			attachedControlGlobal = intersections[0];
			distToCam = Vector3.Distance(editorCameraPosition, attachedControlGlobal);
			dirFromCam = (attachedControlGlobal - editorCameraPosition) / distToCam;
			if (!Physics.Raycast(editorCameraPosition, dirFromCam, out var camHit, distToCam + nudgeLength))
			{
				return false;
			}

			if (!(Vector3.Distance(attachedControlGlobal, camHit.point) > nudgeLength))
			{
				localPoints[attachedControlIdx] = MathUtility.InverseTransformPoint(attachedControlGlobal, pathTransform,
					PathSpace.XYZ);
				return true;
			}

			dirToControl = (attachedControlGlobal - hit2.point).normalized;
			var hit2Nudged = hit2.point + hit2.normal * nudgeLength;

			if (!Physics.Raycast(hit2Nudged, dirToControl, out surfaceHit, distanceFromAnchor))
			{
				return false;
			}

			if (IsSurfaceFlat(hit2.normal))
			{
				attachedControlGlobal += hit2.normal * (nudgeLength * 2);
			}
			else
			{
				var dirToSurface = (surfaceHit.point - attachedControlGlobal).normalized;
				attachedControlGlobal = surfaceHit.point + dirToSurface * (nudgeLength * 2);
			}
			
			dirToControl = (attachedControlGlobal - hit2Nudged).normalized;

			if (!Physics.Raycast(hit2Nudged, dirToControl, out hit2, distanceFromAnchor * 2))
			{
				return false;
			}

			dirAlongSurface = (hit2.point - surfaceHit.point).normalized;
			intersections = MathUtility.FindSphereLineIntersections(anchorGlobal, distanceFromAnchor, 
				hit2.point, dirAlongSurface);
			if (intersections.Count == 0)
			{
				return false;
			}

			attachedControlGlobal = intersections[0];
			localPoints[attachedControlIdx] = MathUtility.InverseTransformPoint(attachedControlGlobal, pathTransform,
				PathSpace.XYZ);
			
			return true;
		}
#endif
        /// <summary>
		/// Update the bounding box of the path.
        /// </summary>
		public Bounds CalculateBoundsWithTransform(Transform transform)
		{
			// Loop through all segments and keep track of the minmax points of all their bounding boxes.
			MinMax3D minMax = new MinMax3D();

			for (int i = 0; i < NumSegments; i++)
			{
				Vector3[] p = GetPointsInSegment(i);
				for (int j = 0; j < p.Length; j++)
				{
					p[j] = MathUtility.TransformPoint(p[j], transform, space);
				}

				minMax.UpdateValues(p[0]);
				minMax.UpdateValues(p[3]);

				List<float> extremePointTimes = CubicBezierUtility.ExtremePointTimes(p[0], p[1], p[2], p[3]);
				foreach (float t in extremePointTimes)
				{
					minMax.UpdateValues(CubicBezierUtility.EvaluateCurve(p, t));
				}
			}

			return new Bounds((minMax.Min + minMax.Max) / 2, minMax.Max - minMax.Min);
		}

		// Flip the normal vectors 180 degrees.
		public bool FlipNormals
		{
			get => flipNormals;
			set
			{
				if (flipNormals != value)
				{
					flipNormals = value;
					NotifyPathModified();
				}
			}
		}

		// Global angle that all normal vectors are rotated by (only relevant for paths in 3D space).
		public float GlobalNormalsAngle
		{
			get => globalNormalsAngle;
			set
			{
				if (!Mathf.Approximately(value, globalNormalsAngle))
				{
					globalNormalsAngle = value;
					NotifyPathModified();
				}
			}
		}

        /// <summary>
		/// Get the desired angle of the normal vector at a particular anchor (only relevant for paths in 3D space).
        /// </summary>
		public float GetAnchorNormalAngle(int anchorIndex)
		{
			return perAnchorNormalsAngle[anchorIndex] % 360;
		}

        /// <summary>
		/// Set the desired angle of the normal vector at a particular anchor (only relevant for paths in 3D space).
        /// </summary>
		public void SetAnchorNormalAngle(int anchorIndex, float angle)
		{
			angle = (angle + 360) % 360;
			if (perAnchorNormalsAngle[anchorIndex] != angle)
			{
				perAnchorNormalsAngle[anchorIndex] = angle;
				NotifyPathModified();
			}
		}

        /// <summary>
		/// Reset global and anchor normal angles to 0.
        /// </summary>
		public void ResetNormalAngles()
		{
			for (int i = 0; i < perAnchorNormalsAngle.Count; i++)
			{
				perAnchorNormalsAngle[i] = 0;
			}
			globalNormalsAngle = _defaultGlobalNormalsAngle;
			NotifyPathModified();
		}
		
        /// <summary>
		/// Function to check if a surface is flat.
        /// </summary>
		private bool IsSurfaceFlat(Vector3 normal)
		{
			var flatnessThreshold = 0.001f;

			var up = Vector3.up;
			var forward = Vector3.forward;
			var right = Vector3.right;

			// Calculate the dot products between the normal and the three principal axes.
			var dotProductUp = Vector3.Dot(normal, up);
			var dotProductForward = Vector3.Dot(normal, forward);
			var dotProductRight = Vector3.Dot(normal, right);
			var dotProductDown = Vector3.Dot(normal, -up);
			var dotProductBackward = Vector3.Dot(normal, -forward);
			var dotProductLeft = Vector3.Dot(normal, -right);

			// Check if the dot products are close to zero (within the threshold) for all directions.
			var isFlatUp = CheckIfFlat(dotProductUp);
			var isFlatForward = CheckIfFlat(dotProductForward);
			var isFlatRight = CheckIfFlat(dotProductRight);
			var isFlatDown = CheckIfFlat(dotProductDown);
			var isFlatBackward = CheckIfFlat(dotProductBackward);
			var isFlatLeft = CheckIfFlat(dotProductLeft);

			// Return true only if the surface is flat in all directions.
			return isFlatUp && isFlatForward && isFlatRight && isFlatDown && isFlatBackward && isFlatLeft;

			bool CheckIfFlat(float dotProduct)
			{
				return Mathf.Abs(dotProduct) < flatnessThreshold || Mathf.Abs(Mathf.Abs(dotProduct) - 1) < flatnessThreshold;
			}
		}

        /// <summary>
		/// Determines good positions (for a smooth path) for the control points affected by
		/// a moved/inserted anchor point.
        /// </summary>
		private void AutoSetAllAffectedControlPoints(int updatedAnchorIndex)
		{
			for (int i = updatedAnchorIndex - 3; i <= updatedAnchorIndex + 3; i += 3)
			{
				if (i >= 0 && i < localPoints.Count || isClosed)
				{
					AutoSetAnchorControlPoints(LoopIndex(i));
				}
			}

			AutoSetStartAndEndControls();
		}

		// Determines good positions (for a smooth path) for all control points.
		void AutoSetAllControlPoints()
		{
			if (NumAnchorPoints > 2)
			{
				for (int i = 0; i < localPoints.Count; i += 3)
				{
					AutoSetAnchorControlPoints(i);
				}
			}

			AutoSetStartAndEndControls();
		}

        /// <summary>
		/// Calculates good positions (to result in smooth path) for the controls around specified anchor.
        /// </summary>
		private void AutoSetAnchorControlPoints(int anchorIndex)
		{
			// Calculate a vector that is perpendicular to the vector bisecting the angle between this anchor and
			// its two immediate neighbours.
			// The control points will be placed along that vector.
			Vector3 anchorPos = localPoints[anchorIndex];
			Vector3 dir = Vector3.zero;
			float[] neighbourDistances = new float[2];

			if (anchorIndex - 3 >= 0 || isClosed)
			{
				Vector3 offset = localPoints[LoopIndex(anchorIndex - 3)] - anchorPos;
				dir += offset.normalized;
				neighbourDistances[0] = offset.magnitude;
			}
			if (anchorIndex + 3 >= 0 || isClosed)
			{
				Vector3 offset = localPoints[LoopIndex(anchorIndex + 3)] - anchorPos;
				dir -= offset.normalized;
				neighbourDistances[1] = -offset.magnitude;
			}

			dir.Normalize();

			// Set the control points along the calculated direction, with a distance proportional to
			// the distance to the neighbouring control point.
			for (int i = 0; i < 2; i++)
			{
				int controlIndex = anchorIndex + i * 2 - 1;
				if (controlIndex >= 0 && controlIndex < localPoints.Count || isClosed)
				{
					localPoints[LoopIndex(controlIndex)] = anchorPos + dir * (neighbourDistances[i] * autoControlLength);
				}
			}
		}

        /// <summary>
		/// Determines good positions (for a smooth path) for the control points at the start and end of a path.
        /// </summary>
		private void AutoSetStartAndEndControls()
		{
			if (isClosed)
			{
				// Handle case with only 2 anchor points separately, as will otherwise result in straight line.
				if (NumAnchorPoints == 2)
				{
					Vector3 dirAnchorAToB = (localPoints[3] - localPoints[0]).normalized;
					float dstBetweenAnchors = (localPoints[0] - localPoints[3]).magnitude;
					Vector3 perp = Vector3.Cross(dirAnchorAToB, Vector3.up);
					localPoints[1] = localPoints[0] + perp * dstBetweenAnchors / 2f;
					localPoints[5] = localPoints[0] - perp * dstBetweenAnchors / 2f;
					localPoints[2] = localPoints[3] + perp * dstBetweenAnchors / 2f;
					localPoints[4] = localPoints[3] - perp * dstBetweenAnchors / 2f;

				}
				else
				{
					AutoSetAnchorControlPoints(0);
					AutoSetAnchorControlPoints(localPoints.Count - 3);
				}
			}
			else
			{
				// Handle case with 2 anchor points separately, as otherwise minor adjustments cause path to
				// constantly flip.
				if (NumAnchorPoints == 2)
				{
					localPoints[1] = localPoints[0] + (localPoints[3] - localPoints[0]) * .25f;
					localPoints[2] = localPoints[3] + (localPoints[0] - localPoints[3]) * .25f;
				}
				else
				{
					localPoints[1] = (localPoints[0] + localPoints[2]) * .5f;
					localPoints[localPoints.Count - 2] = (localPoints[localPoints.Count - 1] + 
					                                      localPoints[localPoints.Count - 3]) * .5f;
				}
			}
		}

		/// Update point positions for new path space.
		void UpdateToNewPathSpace(PathSpace previousSpace)
		{
			if (previousSpace == PathSpace.XYZ)
			{
				for (int i = 0; i < NumPoints; i++)
				{
					var newPos = localPoints[i];
					newPos.y = 0;
					localPoints[i] = newPos;
				}
			}

			NotifyPathModified();
		}

        /// <summary>
		/// Add/remove the extra 2 controls required for a closed path.
        /// </summary>
		private void UpdateClosedState()
		{
			if (isClosed)
			{
				// Set positions for new controls to mirror their counterparts.
				Vector3 lastAnchorSecondControl = localPoints[localPoints.Count - 1] * 2 - 
				                                  localPoints[localPoints.Count - 2];
				Vector3 firstAnchorSecondControl = localPoints[0] * 2 - localPoints[1];
				if (controlMode != ControlMode.Mirrored && controlMode != ControlMode.Automatic)
				{
					// Set positions for new controls to be aligned with their counterparts, but with a
					// length of half the distance between start/end anchor.
					float dstBetweenStartAndEndAnchors = (localPoints[localPoints.Count - 1] - localPoints[0]).magnitude;
					lastAnchorSecondControl = localPoints[localPoints.Count - 1] + (localPoints[localPoints.Count - 1] - 
					                                                      localPoints[localPoints.Count - 2]).normalized * 
																	      (dstBetweenStartAndEndAnchors * .5f);
					firstAnchorSecondControl = localPoints[0] + (localPoints[0] - localPoints[1]).normalized * 
						(dstBetweenStartAndEndAnchors * .5f);
				}
				localPoints.Add(lastAnchorSecondControl);
				localPoints.Add(firstAnchorSecondControl);
			}
			else
			{
				localPoints.RemoveRange(localPoints.Count - 2, 2);

			}

			if (controlMode == ControlMode.Automatic)
			{
				AutoSetStartAndEndControls();
			}

			OnModified?.Invoke();
		}

		// Loop index around to start/end of points array if out of bounds (useful when working with closed paths).
		int LoopIndex(int i)
		{
			return (i + localPoints.Count) % localPoints.Count;
		}

        /// <summary>
		/// Called when the path is modified.
        /// </summary>
		public void NotifyPathModified()
		{
			OnModified?.Invoke();
		}
	}
}
