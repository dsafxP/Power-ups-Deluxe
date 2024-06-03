using SFDGameScriptInterface;

namespace PowerupsDeluxe {

  public partial class GameScript : GameScriptInterface {
    /// <summary>
    /// A helper class for performing various operations on Vector2 objects.
    /// </summary>
    public static class Vector2Helper {
      private static readonly Vector2 _up = new Vector2(0, 1);
      private static readonly Vector2 _down = new Vector2(0, -1);
      private static readonly Vector2 _right = new Vector2(1, 0);
      private static readonly Vector2 _left = new Vector2(-1, 0);

      /// <summary>
      /// Gets the Vector2 representing upward direction.
      /// </summary>
      public static Vector2 Up {
        get { return _up; }
      }

      /// <summary>
      /// Gets the Vector2 representing downward direction.
      /// </summary>
      public static Vector2 Down {
        get { return _down; }
      }

      /// <summary>
      /// Gets the Vector2 representing rightward direction.
      /// </summary>
      public static Vector2 Right {
        get { return _right; }
      }

      /// <summary>
      /// Gets the Vector2 representing leftward direction.
      /// </summary>
      public static Vector2 Left {
        get { return _left; }
      }

      /// <summary>
      /// Returns the absolute value of each component of the specified vector.
      /// </summary>
      public static Vector2 Abs(Vector2 v) {
        return new Vector2(Math.Abs(v.X), Math.Abs(v.Y));
      }

      /// <summary>
      /// Returns the angle (in radians) of the specified vector.
      /// </summary>
      public static float Angle(Vector2 v) { return (float)Math.Atan2(v.Y, v.X); }

      /// <summary>
      /// Returns the angle (in radians) between two vectors.
      /// </summary>
      public static float AngleTo(Vector2 v, Vector2 to) {
        return (float)Math.Atan2(Cross(v, to), Vector2.Dot(to, v));
      }

      /// <summary>
      /// Returns the angle (in radians) from one vector to another point.
      /// </summary>
      public static float AngleToPoint(Vector2 v, Vector2 to) {
        return (float)Math.Atan2(to.Y - v.Y, to.X - v.X);
      }

      /// <summary>
      /// Returns the aspect ratio of the specified vector (X / Y).
      /// </summary>
      public static float Aspect(Vector2 v) { return v.X / v.Y; }

      /// <summary>
      /// Reflects a vector off the specified normal vector.
      /// </summary>
      public static Vector2 Bounce(Vector2 v, Vector2 normal) {
        return -Reflect(v, normal);
      }

      /// <summary>
      /// Returns the ceiling of each component of the specified vector.
      /// </summary>
      public static Vector2 Ceiling(Vector2 v) {
        return new Vector2((float)Math.Ceiling(v.X), (float)Math.Ceiling(v.Y));
      }

      /// <summary>
      /// Restricts each component of the specified vector to the specified range.
      /// </summary>
      public static Vector2 Clamp(Vector2 v, Vector2 min, Vector2 max) {
        return new Vector2(MathHelper.Clamp(v.X, min.X, max.X),
            MathHelper.Clamp(v.Y, min.Y, min.Y));
      }

      /// <summary>
      /// Calculates the cross product of two vectors.
      /// </summary>
      public static float Cross(Vector2 v, Vector2 with) {
        return (v.X * with.Y) - (v.Y * with.X);
      }

      /// <summary>
      /// Returns a unit vector pointing from one vector to another.
      /// </summary>
      public static Vector2 DirectionTo(Vector2 v, Vector2 to) {
        return Vector2.Normalize(new Vector2(to.X - v.X, to.Y - v.Y));
      }

      /// <summary>
      /// Returns the floor of each component of the specified vector.
      /// </summary>
      public static Vector2 Floor(Vector2 v) {
        return new Vector2((float)Math.Floor(v.X), (float)Math.Floor(v.Y));
      }

      /// <summary>
      /// Returns the inverse of each component of the specified vector.
      /// </summary>
      public static Vector2 Inverse(Vector2 v) {
        return new Vector2(1 / v.X, 1 / v.Y);
      }

      /// <summary>
      /// Determines whether the specified vector is normalized.
      /// </summary>
      public static bool IsNormalized(Vector2 v) {
        return Math.Abs(v.LengthSquared() - 1) < float.Epsilon;
      }

      /// <summary>
      /// Restricts the length of the specified vector to a maximum value.
      /// </summary>
      public static Vector2 LimitLength(Vector2 v, float length = 1) {
        float l = v.Length();

        if (l > 0 && length < l) {
          v /= l;
          v *= length;
        }

        return v;
      }

      /// <summary>
      /// Moves a vector towards a target vector by a specified delta.
      /// </summary>
      public static Vector2 MoveToward(Vector2 v, Vector2 to, float delta) {
        Vector2 vd = to - v;
        float len = vd.Length();

        if (len <= delta || len < float.Epsilon)
          return to;

        return v + (vd / len * delta);
      }

      /// <summary>
      /// Projects a vector onto a specified normal vector.
      /// </summary>
      public static Vector2 Project(Vector2 v, Vector2 onNormal) {
        return onNormal * (Vector2.Dot(onNormal, v) / onNormal.LengthSquared());
      }

      /// <summary>
      /// Reflects a vector off the specified normal vector.
      /// </summary>
      public static Vector2 Reflect(Vector2 v, Vector2 normal) {
        normal.Normalize();

        return 2 * normal * Vector2.Dot(normal, v) - v;
      }

      /// <summary>
      /// Rotates the specified vector by the specified angle (in radians).
      /// </summary>
      public static Vector2 Rotated(Vector2 v, float angle) {
        float sin = (float)Math.Sin(angle);
        float cos = (float)Math.Cos(angle);

        return new Vector2(v.X * cos - v.Y * sin, v.X * sin + v.Y * cos);
      }

      /// <summary>
      /// Returns the rounded value of each component of the specified vector.
      /// </summary>
      public static Vector2 Round(Vector2 v) {
        return new Vector2((float)Math.Round(v.X), (float)Math.Round(v.Y));
      }

      /// <summary>
      /// Returns a vector with the sign of each component of the specified vector.
      /// </summary>
      public static Vector2 Sign(Vector2 v) {
        v.X = Math.Sign(v.X);
        v.Y = Math.Sign(v.Y);

        return v;
      }

      /// <summary>
      /// Slides a vector along the specified normal vector.
      /// </summary>
      public static Vector2 Slide(Vector2 v, Vector2 normal) {
        return v - (normal * Vector2.Dot(normal, v));
      }

      /// <summary>
      /// Returns an orthogonal vector to the specified vector.
      /// </summary>
      public static Vector2 Orthogonal(Vector2 v) { return new Vector2(v.Y, -v.X); }

      /// <summary>
      /// Returns a unit vector rotated by the specified angle (in radians).
      /// </summary>
      public static Vector2 FromAngle(float angle) {
        float sin = (float)Math.Sin(angle);
        float cos = (float)Math.Cos(angle);

        return new Vector2(cos, sin);
      }
    }
  }
}