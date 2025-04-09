using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace AnythingWorld.PathCreation
{
	/// <summary>
	/// Static class for handling path handles in the editor.
	/// </summary>
	public static class PathHandle
	{
		/// <summary>
		/// Struct representing handle colors for different states.
		/// </summary>
		public struct HandleColours
		{
			public Color DefaultColour;
			public Color HighlightedColour;
			public Color SelectedColour;
			public Color DisabledColour;

			public HandleColours(Color defaultColour, Color highlightedColour, Color selectedColour, Color disabledColour)
			{
				DefaultColour = defaultColour;
				HighlightedColour = highlightedColour;
				SelectedColour = selectedColour;
				DisabledColour = disabledColour;
			}
		}
		
		/// <summary>
		/// Enum representing the type of input received by a handle.
		/// </summary>
		public enum HandleInputType
		{
			None,
			LMBPress,
			LMBClick,
			LMBDrag,
			LMBRelease,
		}

		private const float ExtraInputRadius = .005f;

		private static Vector2 _handleDragMouseStart;
		private static Vector2 _handleDragMouseEnd;
		private static Vector3 _handleDragWorldStart;

		private static int _selectedHandleID;
		private static bool _mouseIsOverAHandle;
		private static float _dstMouseToDragPointStart;

		private static readonly List<int> Ids;
		private static readonly HashSet<int> IDHash;

		static PathHandle()
		{
			Ids = new List<int>();
			IDHash = new HashSet<int>();

			_dstMouseToDragPointStart = float.MaxValue;
		}
		
        /// <summary>
		/// Draws a handle at the specified position and returns the updated position based on user input.
		/// Handles interactive editing of the handle position.
        /// </summary>
		public static Vector3 DrawHandle(Vector3 position, bool isInteractive, bool isSnappedToSurface, PathSpace space, 
			float handleDiameter, Handles.CapFunction capFunc, HandleColours colours, out HandleInputType inputType, 
			int handleIndex, float pathHeight)
		{
			int id = GetID(handleIndex);
			Vector3 screenPosition = Handles.matrix.MultiplyPoint(position);
			Matrix4x4 cachedMatrix = Handles.matrix;

			inputType = HandleInputType.None;

			EventType eventType = Event.current.GetTypeForControl(id);
			float handleRadius = handleDiameter / 2f;
			float dstToHandle = HandleUtility.DistanceToCircle(position, handleRadius + ExtraInputRadius);
			float dstToMouse = HandleUtility.DistanceToCircle(position, 0);

			// Handle input events.
			if (isInteractive)
			{
				// Repaint if mouse is entering/exiting handle (for highlight colour).
				if (dstToHandle == 0)
				{
					if (!_mouseIsOverAHandle)
					{
						HandleUtility.Repaint();
						_mouseIsOverAHandle = true;
					}
				}
				else
				{
					if (_mouseIsOverAHandle)
					{
						HandleUtility.Repaint();
						_mouseIsOverAHandle = false;
					}
				}
				switch (eventType)
				{
					case EventType.MouseDown:
						if (Event.current.button == 0 && Event.current.modifiers != EventModifiers.Alt)
						{
							if (dstToHandle == 0 && dstToMouse < _dstMouseToDragPointStart)
							{
								_dstMouseToDragPointStart = dstToMouse;
								GUIUtility.hotControl = id;
								_handleDragMouseEnd = _handleDragMouseStart = Event.current.mousePosition;
								_handleDragWorldStart = position;
								_selectedHandleID = id;
								inputType = HandleInputType.LMBPress;
							}
						}
						break;

					case EventType.MouseUp:
						_dstMouseToDragPointStart = float.MaxValue;
						if (GUIUtility.hotControl == id && Event.current.button == 0)
						{
							GUIUtility.hotControl = 0;
							_selectedHandleID = -1;
							Event.current.Use();

							inputType = HandleInputType.LMBRelease;

							if (Event.current.mousePosition == _handleDragMouseStart)
							{
								inputType = HandleInputType.LMBClick;
							}
						}
						break;

					case EventType.MouseDrag:
						if (GUIUtility.hotControl == id && Event.current.button == 0)
						{
							_handleDragMouseEnd += new Vector2(Event.current.delta.x, -Event.current.delta.y);
							Vector3 position2 = 
								Camera.current.WorldToScreenPoint(Handles.matrix.MultiplyPoint(_handleDragWorldStart))
								+ (Vector3)(_handleDragMouseEnd - _handleDragMouseStart);
							inputType = HandleInputType.LMBDrag;
							
							if ((space == PathSpace.XYZ && isSnappedToSurface) || space == PathSpace.XZ)
							{
								position = GetMouseWorldPosition(space, pathHeight);
							}
							else if (space == PathSpace.XYZ)
							{
								position = Handles.matrix.inverse.MultiplyPoint(Camera.current.ScreenToWorldPoint(position2));
							}

							GUI.changed = true;
							Event.current.Use();
						}
						break;
				}
			}

			switch (eventType)
			{
				case EventType.Repaint:
					Color originalColour = Handles.color;
					Handles.color = (isInteractive) ? colours.DefaultColour : colours.DisabledColour;

					if (id == GUIUtility.hotControl)
					{
						Handles.color = colours.SelectedColour;
					}
					else if (dstToHandle == 0 && _selectedHandleID == -1 && isInteractive)
					{
						Handles.color = colours.HighlightedColour;
					}
					
					Handles.matrix = Matrix4x4.identity;
					Vector3 lookForward = Vector3.zero;
					Camera cam = Camera.current;
					if (cam != null)
					{
						if (cam.orthographic)
						{
							lookForward = -cam.transform.forward;
						}
						else
						{
							lookForward = (cam.transform.position - position).normalized;
						}
					}
					
					if (lookForward == Vector3.zero) {
						lookForward = Vector3.forward;
					}

					capFunc(id, screenPosition, Quaternion.LookRotation(lookForward), handleDiameter, EventType.Repaint);
					Handles.matrix = cachedMatrix;

					Handles.color = originalColour;
					break;

				case EventType.Layout:
					Handles.matrix = Matrix4x4.identity;
					HandleUtility.AddControl(id, HandleUtility.DistanceToCircle(screenPosition, handleDiameter / 2f));
					Handles.matrix = cachedMatrix;
					break;
			}
			return position;
		}
		

        /// <summary>
		/// Determines mouse position in world. Will attempt to raycast to a reasonably close object,
		/// or return the position at depthFor3DSpace distance from the current view.
        /// </summary>
		public static Vector3 GetMouseWorldPosition(PathSpace space, float pathHeight = 0, float depthFor3DSpace = 10)
		{
			var mouseRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

			if (space == PathSpace.XYZ)
			{
				return Physics.Raycast(mouseRay, out var hitInfo, depthFor3DSpace * 2f) ? 
					hitInfo.point : mouseRay.GetPoint(depthFor3DSpace);
			}
			
			var plane = new Plane(Vector3.up, Vector3.up * pathHeight);
			var ray = new Ray(mouseRay.origin, mouseRay.direction);
			if (plane.Raycast(ray, out float distance))
			{
				return mouseRay.GetPoint(distance);
			}
			
			return Vector3.up * pathHeight;
		}
		
        /// <summary>
		/// Adds new handle IDs to the Ids list up to the specified index.
		/// Ensures unique IDs are generated and added to the IDHash set.
        /// </summary>
		private static void AddIDs(int upToIndex)
		{
			int numIDAtStart = Ids.Count;
			int numToAdd = (upToIndex - numIDAtStart) + 1;
			for (int i = 0; i < numToAdd; i++)
			{
				string hashString = string.Format("pathhandle({0})", numIDAtStart + i);
				int hash = hashString.GetHashCode();

				int id = GUIUtility.GetControlID(hash, FocusType.Passive);
				int numIts = 0;
				
				while (IDHash.Contains(id))
				{
					numIts++;
					id += numIts * numIts;
					if (numIts > 100)
					{
						Debug.LogError("Failed to generate unique handle id.");
						break;
					}
				}

				IDHash.Add(id);
				Ids.Add(id);
			}
		}

        /// <summary>
		/// Retrieves the handle ID for the specified handle index.
		/// If the index is out of range, new IDs are added up to that index.
        /// </summary>
		private static int GetID(int handleIndex)
		{
			if (handleIndex >= Ids.Count)
			{
				AddIDs(handleIndex);
			}
			
			return Ids[handleIndex];
		}
	}
}
