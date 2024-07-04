/// <summary>
/// Contains methods for creating various point shapes.
/// </summary>
public static class PointShape {
  /// <summary>
  /// Creates a trail of points between two points.
  /// </summary>
  /// <param name="func">The action to perform on each point.</param>
  /// <param name="start">The starting point of the trail.</param>
  /// <param name="end">The ending point of the trail.</param>
  /// <param name="pointDistance">The distance between each point on the trail.</param>
  public static void Trail(Action<Vector2> func, Vector2 start, Vector2 end, float pointDistance = 0.1f) {
    int count = (int) Math.Ceiling(Vector2.Distance(start, end) / pointDistance);

    for (int i = 0; i < count; i++) {
      Vector2 pos = Vector2.Lerp(start, end, (float) i / (count - 1));
      func(pos);
    }
  }

  /// <summary>
  /// Creates a circle of points around a center point.
  /// </summary>
  /// <param name="func">The action to perform on each point.</param>
  /// <param name="centerPoint">The center point of the circle.</param>
  /// <param name="radius">The radius of the circle.</param>
  /// <param name="separationAngle">The angle between each point on the circle.</param>
  public static void Circle(Action<Vector2> func, Vector2 centerPoint, float radius, float separationAngle = 1) {
    int pointCount = (int) Math.Ceiling(360f / separationAngle);

    for (int i = 0; i < pointCount; i++) {
      float angle = DegreesToRadians(i * separationAngle);
      Vector2 pos = new Vector2(centerPoint.X + radius * (float) Math.Cos(angle),
                                centerPoint.Y + radius * (float) Math.Sin(angle));
      func(pos);
    }
  }

  /// <summary>
  /// Creates a square of points within a specified area.
  /// </summary>
  /// <param name="func">The action to perform on each point.</param>
  /// <param name="area">The area defining the square.</param>
  /// <param name="pointDistance">The distance between each point on the square.</param>
  public static void Square(Action<Vector2> func, Area area, float pointDistance = 0.1f) {
    Vector2[] vertices = new Vector2[]
    {
            area.BottomLeft,
            area.BottomRight,
            area.TopRight,
            area.TopLeft
    };

    Polygon(func, vertices, pointDistance);
  }

  /// <summary>
  /// Creates a polygon of points using the provided vertices.
  /// </summary>
  /// <param name="func">The action to perform on each point.</param>
  /// <param name="points">The vertices of the polygon.</param>
  /// <param name="pointDistance">The distance between each point on the polygon.</param>
  public static void Polygon(Action<Vector2> func, Vector2[] points, float pointDistance = 0.1f) {
    for (int i = 0; i < points.Length - 1; i++) {
      Trail(func, points[i], points[i + 1], pointDistance);
    }

    Trail(func, points[points.Length - 1], points[0], pointDistance);
  }

  /// <summary>
  /// Creates a swirl of points around a center point.
  /// </summary>
  /// <param name="func">The action to perform on each point.</param>
  /// <param name="centerPoint">The center point of the swirl.</param>
  /// <param name="startRadius">The starting radius of the swirl.</param>
  /// <param name="endRadius">The ending radius of the swirl.</param>
  /// <param name="revolutions">The number of revolutions for the swirl.</param>
  /// <param name="pointsPerRevolution">The number of points per revolution.</param>
  public static void Swirl(Action<Vector2> func, Vector2 centerPoint, float startRadius,
      float endRadius, int revolutions = 1, int pointsPerRevolution = 360) {
    int totalPoints = revolutions * pointsPerRevolution;

    float angleIncrement = 360f / pointsPerRevolution;
    float radiusIncrement = (endRadius - startRadius) / totalPoints;

    for (int i = 0; i < totalPoints; i++) {
      float angle = DegreesToRadians(i * angleIncrement);
      float radius = startRadius + i * radiusIncrement;
      Vector2 pos = new Vector2(centerPoint.X + radius * (float) Math.Cos(angle),
                                centerPoint.Y + radius * (float) Math.Sin(angle));
      func(pos);
    }
  }

  /// <summary>
  /// Creates a wave of points between two points.
  /// </summary>
  /// <param name="func">The action to perform on each point.</param>
  /// <param name="start">The starting point of the wave.</param>
  /// <param name="end">The ending point of the wave.</param>
  /// <param name="amplitude">The amplitude of the wave.</param>
  /// <param name="frequency">The frequency of the wave.</param>
  /// <param name="pointDistance">The distance between each point on the wave.</param>
  public static void Wave(Action<Vector2> func, Vector2 start, Vector2 end,
      float amplitude = 1, float frequency = 1, float pointDistance = 0.1f) {
    float totalDistance = Vector2.Distance(start, end);
    int count = (int) Math.Ceiling(totalDistance / pointDistance);
    float adjustedFrequency = frequency * (totalDistance / count);

    for (int i = 0; i < count; i++) {
      Vector2 pos = Vector2.Lerp(start, end, (float) i / (count - 1));
      float offsetY = amplitude * ((float) Math.Sin(adjustedFrequency * pos.X));
      func(pos + new Vector2(0, offsetY));
    }
  }

  /// <summary>
  /// Generates a random Vector2 point inside the specified Area.
  /// </summary>
  /// <param name="func">Function to be called with the generated random Vector2 point.</param>
  /// <param name="area">The Area in which to generate the random point.</param>
  /// <param name="random">A Random instance for generating random numbers.</param>
  /// <returns>The generated random Vector2 point.</returns>
  public static Vector2 Random(Action<Vector2> func, Area area, Random random) {
    // Generate random coordinates within the bounds of the area
    float randomX = (float) random.NextDouble() * area.Width + area.Left;
    float randomY = (float) random.NextDouble() * area.Height + area.Bottom;

    Vector2 randomV = new Vector2(randomX, randomY);

    // Return the random point as a tuple
    func(randomV);

    return randomV;
  }

  private static float DegreesToRadians(float degrees) {
    return degrees * MathHelper.PI / 180f;
  }
}