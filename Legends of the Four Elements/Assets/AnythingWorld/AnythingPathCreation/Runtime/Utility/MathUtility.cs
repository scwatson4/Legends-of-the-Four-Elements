using System.Collections.Generic;
using UnityEngine;

namespace AnythingWorld.PathCreation
{
    /// <summary>
    /// This utility class provides methods for performing various mathematical operations related to spline paths,
    /// including transforming points between local and world space, finding intersections between a sphere and a line,
    /// and calculating angles.
    /// </summary>
    public static class MathUtility 
    {
        /// <summary>
        /// Transform point from local to world space.
        /// </summary>
        public static Vector3 TransformPoint(Vector3 p, Transform t, PathSpace space) 
        {
            // Path only works correctly for uniform scales, so average out xyz global scale.
            float scale = Vector3.Dot(t.lossyScale, Vector3.one) / 3;
            Vector3 constrainedPos = t.position;
            Quaternion constrainedRot = t.rotation;
            ConstrainRot(ref constrainedRot, space);
            return constrainedRot * p * scale + constrainedPos;
        }
        
        /// <summary>
        /// Transform point from world to local space.
        /// </summary>
        public static Vector3 InverseTransformPoint(Vector3 p, Transform t, PathSpace space) 
        {
            Vector3 constrainedPos = t.position;
            Quaternion constrainedRot = t.rotation;
            ConstrainRot(ref constrainedRot, space);

            // Path only works correctly for uniform scales, so average out xyz global scale.
            float scale = Vector3.Dot(t.lossyScale, Vector3.one) / 3;
            var offset = p - constrainedPos;

            return Quaternion.Inverse(constrainedRot) * offset / scale;
        }
        
        /// <summary>
        /// Transform vector from world to local space (affected by rotation and scale, but not position).
        /// </summary>
        public static Vector3 InverseTransformVector(Vector3 p, Transform t, PathSpace space) 
        {
            Quaternion constrainedRot = t.rotation;
            ConstrainRot(ref constrainedRot, space);
            // Path only works correctly for uniform scales, so average out xyz global scale.
            float scale = Vector3.Dot(t.lossyScale, Vector3.one) / 3;
            return Quaternion.Inverse(constrainedRot) * p / scale;
        }

        /// <summary>
        /// Transform vector from local to world space (affected by rotation, but not position or scale).
        /// </summary>
        public static Vector3 TransformDirection(Vector3 p, Transform t, PathSpace space) 
        {
            Quaternion constrainedRot = t.rotation;
            ConstrainRot(ref constrainedRot, space);
            return constrainedRot * p;
        }
        
        // Calculate the intersection point(s) of a sphere and a line.
        public static List<Vector3> FindSphereLineIntersections(Vector3 center, float r, Vector3 pointOnLine, 
            Vector3 lineDir)
        {
            // Parameterize the line: L(t) = pointOnLine + t * lineDir.
            // Substitute L(t) into the sphere equation and solve for t.
			
            var pointToLine = pointOnLine - center;
            float b = 2 * Vector3.Dot(lineDir, pointToLine);
            float c = Vector3.Dot(pointToLine, pointToLine) - r * r;
            
            // Solve the quadratic equation a*t^2 + b*t + c = 0.
            float discriminant = b * b - 4 * c;

            List<Vector3> intersectionPoints = new List<Vector3>();
            if (discriminant < 0)
            {
                // No real roots; the line does not intersect the sphere.
                return intersectionPoints;
            }

            // Calculate t values for the intersection points.
            float t1 = (-b + Mathf.Sqrt(discriminant)) / 2;
            float t2 = (-b - Mathf.Sqrt(discriminant)) / 2;

            // Calculate the intersection points using the parameter t.
            Vector3 intersection1 = pointOnLine + t1 * lineDir;
            Vector3 intersection2 = pointOnLine + t2 * lineDir;

            if (discriminant == 0)
            {
                // One intersection point (tangent).
                intersectionPoints.Add(intersection1);
            }
            else
            {
                // Two intersection points.
                intersectionPoints.Add(intersection1);
                intersectionPoints.Add(intersection2);
            }

            return intersectionPoints;
        }
        
        /// <summary>
        /// Finds the closest point on a line segment to a given point by projecting the point onto the line segment and
        /// clamping the result to ensure it lies within the segment's bounds.
        /// </summary>
        public static Vector2 ClosestPointOnLineSegment(Vector2 p, Vector2 a, Vector2 b) 
        {
            Vector2 aB = b - a;
            Vector2 aP = p - a;
            float sqrLenAB = aB.sqrMagnitude;

            if (sqrLenAB == 0)
                return a;

            float t = Mathf.Clamp01(Vector2.Dot(aP, aB) / sqrLenAB);
            return a + aB * t;
        }
        
        /// <summary>
        /// Finds the closest point on a line segment to a given point by projecting the point onto the line segment and
        /// clamping the result to ensure it lies within the segment's bounds.
        /// </summary>
        public static Vector3 ClosestPointOnLineSegment(Vector3 p, Vector3 a, Vector3 b) 
        {
            Vector3 aB = b - a;
            Vector3 aP = p - a;
            float sqrLenAB = aB.sqrMagnitude;

            if (sqrLenAB == 0)
                return a;

            float t = Mathf.Clamp01(Vector3.Dot(aP, aB) / sqrLenAB);
            return a + aB * t;
        }
      
        /// <summary>
        /// Returns the smallest angle between ABC. Never greater than 180.
        /// </summary>
        public static float MinAngle(Vector3 a, Vector3 b, Vector3 c) 
        {
            return Vector3.Angle (a - b, c - b);
        }

        /// <summary>
        /// Constraints a rotation to XZ plane if the curve space is XZ.
        /// </summary>
        private static void ConstrainRot(ref Quaternion rot, PathSpace space)
        {
            if (space != PathSpace.XZ)
            {
                return;
            }
            
            var eulerAngles = rot.eulerAngles;
            if (eulerAngles.x != 0 || eulerAngles.z != 0) 
            {
                rot = Quaternion.AngleAxis(eulerAngles.y, Vector3.up);
            } 
        }
    }
}
