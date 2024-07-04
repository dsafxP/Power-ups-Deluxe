public static class Template {
  //==================================================================//
  //======================< YOUR SUBMISSION FORM >====================//
  //=====================< EDIT THE SECTIONS BELOW >==================//
  //==================================================================//

  /// <summary>
  /// Represents a base power-up that can be activated and updated over time.
  /// </summary>
  public class MY_POWERUP_CHANGE_THIS : Powerup { //1: CHANGE YOUR CLASS' NAMESPACE

    public override string Name {
      get {
        return "My Powerup Name";
      } //2: CHANGE THE TEXT TO YOUR POWERUP NAME
    }

    public override string Author {
      get {
        return "My Name";
      } //3: CHANGE THE TEXT TO YOUR NAME
    }

    // Interval for the main update callback event

    public MY_POWERUP_CHANGE_THIS(IPlayer player) : base(player) { //4: CHANGE YOUR CONSTRUCTOR TO MATCH THE CLASS NAME
      Time = 12000; //5: CHANGE THE DURATION OF THE EFFECT TO WHAT YOU WANT
    }

    /// <summary>
    /// Virtual method for actions upon activating the power-up.
    /// </summary>
    protected override void Activate() {
      //6: ADD CODE ON ACTIVATION

      Game.RunCommand(string.Format("/msg {0} power activated on {1}", Name, Player.Name));

      // Implement in derived classes
    }

    /// <summary>
    /// Virtual method for updating the power-up.
    /// </summary>
    /// <param name="dlt">The time delta since the last update.</param>
    /// <param name="dltSecs">The time delta in seconds since the last update.</param>
    public override void Update(float dlt, float dltSecs) {
      //7: ADD CODE FOR THE EFFECT DURING ITS INFLUENCE

      // Implement in derived classes

      //EXAMPLE EFFECT
      if (Time % 50 == 0)
        PointShape.Swirl((v => Game.PlayEffect("GLM", Vector2Helper.Rotated(v - Player.GetWorldPosition(),
        (float) (Time % 1500 * (MathHelper.TwoPI / 1500))) + Player.GetWorldPosition() + new Vector2(0, 4))),
          Player.GetWorldPosition(), //Center Position
          5f, //Initial Radius
          50f, //End Radius
          2 //Rotations
        );
    }

    /// <summary>
    /// Virtual method called when the power-up times out.
    /// </summary>
    public override void TimeOut() {
      //8: ADD CODE ON EFFECT END
      Game.RunCommand(string.Format("/msg {0} power deactivated on {1}", Name, Player.Name));
      // Implement in derived classes
    }

    /// <summary>
    /// Virtual method called when the power-up is enabled or disabled. Called by the constructor and on timeout.
    /// </summary>
    public override void OnEnabled(bool enabled) {
      // Implement in derived classes
    }
  }

  //==================================================================//
  //==================< DO NOT CHANGE ANYTHING BELOW >================//
  //==============< IF YOU'RE NOT SURE WHAT YOU'RE DOING >============//
  //==================================================================//
}

public void OnStartup() {
  Events.UserMessageCallback.Start(OnUserMessage);
}

//		   USE  >>>> /test <player>  <<<< TO TEST YOUR SUBMISSION

private void OnUserMessage(UserMessageCallbackArgs args) {
  if (args.IsCommand && args.User.IsModerator) {
    if (args.Command == "TEST") {
      string[] argsPieces = args.CommandArguments.ToLower()
      .Split(' ');

      IUser reciever;

      if (argsPieces.Length > 0 && argsPieces[0].Length > 0) {
        reciever = GetUser(argsPieces[0]);
      } else {
        reciever = args.User;
      }

      if (reciever != null && reciever.GetPlayer() != null) {
        IPlayer ply = reciever.GetPlayer();

        Activator.CreateInstance(GetPowerup(), ply);

        Game.ShowChatMessage(string.Format("{0} recieved ability", ply.Name), new Color(34, 134, 34));
      }
    }
  }
}

//==================================================================//
//============================< HELPERS >===========================//
//==================================================================//

//              Use these to help you create your powerups

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
  /// <param name="pointDistance">The distance between each point on the
  /// trail.</param>
  public static void Trail(Action<Vector2> func, Vector2 start, Vector2 end,
    float pointDistance = 0.1f) {
    int count
      = (int) Math.Ceiling(Vector2.Distance(start, end) / pointDistance);

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
  /// <param name="separationAngle">The angle between each point on the
  /// circle.</param>
  public static void Circle(Action<Vector2> func, Vector2 centerPoint,
    float radius, float separationAngle = 1) {
    int pointCount = (int) Math.Ceiling(360f / separationAngle);

    for (int i = 0; i < pointCount; i++) {
      float angle = DegreesToRadians(i * separationAngle);
      Vector2 pos
        = new Vector2(centerPoint.X + radius * (float) Math.Cos(angle),
          centerPoint.Y + radius * (float) Math.Sin(angle));
      func(pos);
    }
  }

  /// <summary>
  /// Creates a square of points within a specified area.
  /// </summary>
  /// <param name="func">The action to perform on each point.</param>
  /// <param name="area">The area defining the square.</param>
  /// <param name="pointDistance">The distance between each point on the
  /// square.</param>
  public static void Square(
    Action<Vector2> func, Area area, float pointDistance = 0.1f) {
    Vector2[] vertices = new Vector2[] {
      area.BottomLeft, area.BottomRight,
        area.TopRight, area.TopLeft
    };

    Polygon(func, vertices, pointDistance);
  }

  /// <summary>
  /// Creates a polygon of points using the provided vertices.
  /// </summary>
  /// <param name="func">The action to perform on each point.</param>
  /// <param name="points">The vertices of the polygon.</param>
  /// <param name="pointDistance">The distance between each point on the
  /// polygon.</param>
  public static void Polygon(
    Action<Vector2> func, Vector2[] points, float pointDistance = 0.1f) {
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
  /// <param name="pointsPerRevolution">The number of points per
  /// revolution.</param>
  public static void Swirl(Action<Vector2> func, Vector2 centerPoint,
    float startRadius, float endRadius, int revolutions = 1,
    int pointsPerRevolution = 360) {
    int totalPoints = revolutions * pointsPerRevolution;

    float angleIncrement = 360f / pointsPerRevolution;
    float radiusIncrement = (endRadius - startRadius) / totalPoints;

    for (int i = 0; i < totalPoints; i++) {
      float angle = DegreesToRadians(i * angleIncrement);
      float radius = startRadius + i * radiusIncrement;
      Vector2 pos
        = new Vector2(centerPoint.X + radius * (float) Math.Cos(angle),
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
  /// <param name="pointDistance">The distance between each point on the
  /// wave.</param>
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
  /// <param name="func">Function to be called with the generated random Vector2
  /// point.</param> <param name="area">The Area in which to generate the random
  /// point.</param> <param name="random">A Random instance for generating
  /// random numbers.</param> <returns>The generated random Vector2
  /// point.</returns>
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

/// <summary>
/// A helper class for performing various operations on Vector2 objects.
/// </summary>
public static class Vector2Helper {
  public static readonly Vector2 Up = new Vector2(0, 1);
  public static readonly Vector2 Down = new Vector2(0, -1);
  public static readonly Vector2 Right = new Vector2(1, 0);
  public static readonly Vector2 Left = new Vector2(-1, 0);

  /// <summary>
  /// Returns the absolute value of each component of the specified vector.
  /// </summary>
  public static Vector2 Abs(Vector2 v) {
    return new Vector2(Math.Abs(v.X), Math.Abs(v.Y));
  }

  /// <summary>
  /// Returns the angle (in radians) of the specified vector.
  /// </summary>
  public static float Angle(Vector2 v) {
    return (float) Math.Atan2(v.Y, v.X);
  }

  /// <summary>
  /// Returns the angle (in radians) between two vectors.
  /// </summary>
  public static float AngleTo(Vector2 v, Vector2 to) {
    return (float) Math.Atan2(Cross(v, to), Vector2.Dot(to, v));
  }

  /// <summary>
  /// Returns the angle (in radians) from one vector to another point.
  /// </summary>
  public static float AngleToPoint(Vector2 v, Vector2 to) {
    return (float) Math.Atan2(to.Y - v.Y, to.X - v.X);
  }

  /// <summary>
  /// Returns the aspect ratio of the specified vector (X / Y).
  /// </summary>
  public static float Aspect(Vector2 v) {
    return v.X / v.Y;
  }

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
    return new Vector2((float) Math.Ceiling(v.X), (float) Math.Ceiling(v.Y));
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
    return new Vector2((float) Math.Floor(v.X), (float) Math.Floor(v.Y));
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
    float sin = (float) Math.Sin(angle);
    float cos = (float) Math.Cos(angle);

    return new Vector2(v.X * cos - v.Y * sin, v.X * sin + v.Y * cos);
  }

  /// <summary>
  /// Returns the rounded value of each component of the specified vector.
  /// </summary>
  public static Vector2 Round(Vector2 v) {
    return new Vector2((float) Math.Round(v.X), (float) Math.Round(v.Y));
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
  public static Vector2 Orthogonal(Vector2 v) {
    return new Vector2(v.Y, -v.X);
  }

  /// <summary>
  /// Returns a unit vector rotated by the specified angle (in radians).
  /// </summary>
  public static Vector2 FromAngle(float angle) {
    float sin = (float) Math.Sin(angle);
    float cos = (float) Math.Cos(angle);

    return new Vector2(cos, sin);
  }
}

/// <summary>
/// Represents a custom projectile with customizable behavior and collision handling.
/// </summary>
public class CustomProjectile {
  private const uint COOLDOWN = 0;

  private Vector2 _direction;
  private Vector2 _position;
  private Vector2 _subPosition;

  private RayCastInput _rayCastCollision;

  private Events.UpdateCallback _updateCallback = null;

  private float _distanceTravelled = 0;

  /// <summary>
  /// Indicates whether the projectile pierces through objects or not.
  /// </summary>
  public bool Piercing = false;

  /// <summary>
  /// Indicates whether the projectile hits rolling players or not.
  /// </summary>
  public bool IgnoreRolling = false;

  /// <summary>
  /// Speed of the projectile.
  /// </summary>
  public float Speed = 1;

  /// <summary>
  /// The maximum distance the projectile can travel before being disabled.
  /// </summary>
  public float MaxDistanceTravelled = 1000;

  /// <summary>
  /// Effect to be played on movement.
  /// </summary>
  public string Effect = string.Empty;

  /// <summary>
  /// Information about the collision to be used for ray casting.
  /// </summary>
  public RayCastInput RayCastCollision {
    get {
      return _rayCastCollision;
    }
    set {
      _rayCastCollision = value;
      _rayCastCollision.ClosestHitOnly = true;
    }
  }

  /// <summary>
  /// Current position of the projectile.
  /// </summary>
  public Vector2 Position {
    get {
      return _position;
    }
    set {
      _position = value;
      _subPosition = _position;
    }
  }

  /// <summary>
  /// Direction of the projectile.
  /// </summary>
  public Vector2 Direction {
    get {
      return _direction;
    }
    set {
      _direction = Vector2.Normalize(value);
    }
  }

  /// <summary>
  /// Velocity of the projectile.
  /// </summary>
  public Vector2 Velocity {
    get {
      return Direction * Speed;
    }
    set {
      Speed = value.Length();
      Direction = value;
    }
  }

  /// <summary>
  /// Indicates whether the projectile is enabled or not.
  /// </summary>
  public bool Enabled {
    get {
      return _updateCallback != null;
    }
    set {
      if (value != Enabled) {
        if (value) {
          _updateCallback = Events.UpdateCallback.Start(Update, COOLDOWN);
        } else {
          _updateCallback.Stop();
          _updateCallback = null;
        }
      }
    }
  }

  /// <summary>
  /// The distance that the projectile has travelled.
  /// </summary>
  public float DistanceTravelled {
    get {
      return _distanceTravelled;
    }
    set {
      _distanceTravelled = value;

      Enabled = value <= MaxDistanceTravelled;
    }
  }

  /// <summary>
  /// Delegate for handling when the projectile hits a player.
  /// </summary>
  /// <param name="hitPlayer">The player hit by the projectile.</param>
  /// <param name="hitPosition">The position at which the projectile hit.</param>
  public delegate void OnPlayerHitCallback(IPlayer hitPlayer, Vector2 hitPosition);
  public OnPlayerHitCallback OnPlayerHit;

  /// <summary>
  /// Delegate for handling when the projectile hits an object.
  /// </summary>
  /// <param name="hitObject">The object hit by the projectile.</param>
  /// <param name="hitPosition">The position at which the projectile hit.</param>
  public delegate void OnObjectHitCallback(IObject hitObject, Vector2 hitPosition);
  public OnObjectHitCallback OnObjectHit;

  /// <summary>
  /// Initializes a new instance of the CustomProjectile class.
  /// </summary>
  /// <param name="pos">Initial position of the projectile.</param>
  /// <param name="direction">Initial direction of the projectile.</param>
  /// <param name="rayCastCollision">Information about the collision to be used for ray casting.</param>
  public CustomProjectile(Vector2 pos, Vector2 direction, RayCastInput rayCastCollision) {
    Position = pos;
    Direction = direction;
    RayCastCollision = rayCastCollision;
    Enabled = true;
  }

  private void Update(float dlt) {
    Vector2 vel = Velocity;

    DistanceTravelled += vel.Length();

    _position += vel;

    Game.DrawLine(_subPosition, Position, Color.Yellow);

    RayCastResult checkedResult = Game.RayCast(_subPosition, Position, RayCastCollision)[0];

    bool dodged = false;

    if (checkedResult.Hit) {
      if (checkedResult.IsPlayer && OnPlayerHit != null) {
        IPlayer hitPlayer = (IPlayer) checkedResult.HitObject;

        dodged = !IgnoreRolling && (hitPlayer.IsRolling || hitPlayer.IsDiving);

        if (!dodged)
          OnPlayerHit.Invoke(hitPlayer, checkedResult.Position);
      }

      if (!checkedResult.IsPlayer && OnObjectHit != null)
        OnObjectHit.Invoke(checkedResult.HitObject, checkedResult.Position);

      Enabled = !(!Piercing && !dodged || !checkedResult.IsPlayer && !checkedResult.HitObject.Destructable);
    }

    Game.PlayEffect(Effect, _subPosition);

    Vector2 trailEnd = checkedResult.Hit && !dodged && !Piercing ? checkedResult.Position : Position;

    Trail(Draw, _subPosition, trailEnd, 5);

    _subPosition += vel;
  }

  private void Draw(Vector2 pos) {
    Game.PlayEffect(Effect, pos);
  }

  private static void Trail(Action<Vector2> func, Vector2 start, Vector2 end, float pointDistance = 0.1f) {
    int count = (int) Math.Ceiling(Vector2.Distance(start, end) / pointDistance);

    for (int i = 0; i < count; i++) {
      Vector2 pos = Vector2.Lerp(start, end, (float) i / (count - 1));
      func(pos);
    }
  }
}

public IUser GetUser(string arg) {
  return Game.GetActiveUsers().FirstOrDefault(u => u.AccountName == arg ||
    u.Name == arg ||
    (arg.All(char.IsDigit) ? u.GameSlotIndex == int.Parse(arg) :
      false));
}

public static Type GetPowerup() {

  Type[] nestedPowerups = typeof(Template).GetNestedTypes();

  Type[] instantiableTypes = nestedPowerups
    .Where(t =>
      // t.BaseType == typeof(Powerup) &&
      t.GetConstructors().Any(c => c.GetParameters().Length == 1 &&
        c.GetParameters()[0].ParameterType ==
        typeof(IPlayer)))
    .ToArray();

  if (instantiableTypes.Length == 0)
    throw new InvalidOperationException("No instantiable types found.");

  return instantiableTypes[0];
}

//=================================================================//
//==========================< BASE CLASS >=========================//
//=================================================================//

/// <summary>
/// Represents a base power-up that can be activated and updated over time.
/// </summary>
public abstract class Powerup {
  // Interval for the main update callback event
  private const uint COOLDOWN = 0;

  // Main update callback event
  private Events.UpdateCallback _updateCallback = null;

  public abstract string Name {
    get;
  }

  public abstract string Author {
    get;
  }

  // Time left for the power-up to be active
  public float Time = 1000;

  // The player associated with this power-up
  public IPlayer Player;

  /// <summary>
  /// Gets or sets whether the power-up is enabled.
  /// </summary>
  public bool Enabled {
    get {
      return _updateCallback != null;
    }
    set {
      if (value != Enabled) {
        if (value) {
          _updateCallback = Events.UpdateCallback.Start(Update, COOLDOWN);

          OnEnabled(true);
        } else {
          _updateCallback.Stop();
          _updateCallback = null;

          OnEnabled(false);
        }
      }
    }
  }

  /// <summary>
  /// Initializes a new instance of the <see cref="Powerup"/> class.
  /// </summary>
  /// <param name="player">The player associated with this power-up.</param>
  public Powerup(IPlayer player) {
    Player = player;
    Enabled = true;
    Activate();
  }

  /// <summary>
  /// Updates the power-up with the specified time delta.
  /// </summary>
  /// <param name="dlt">The time delta since the last update.</param>
  private void Update(float dlt) {
    // Check if the player is still valid
    if (Player == null || Player.IsRemoved || Player.IsDead) {
      Enabled = false;

      return;
    }

    // Check if the power-up has timed out
    if (Time <= 0) {
      TimeOut();

      Enabled = false;

      return;
    }

    // Update the time left for the power-up
    Time -= dlt;

    // Invoke the virtual Update method
    Update(dlt, dlt / 1000);
  }

  /// <summary>
  /// Virtual method for actions upon activating the power-up.
  /// </summary>
  protected abstract void Activate();

  /// <summary>
  /// Virtual method for updating the power-up.
  /// </summary>
  /// <param name="dlt">The time delta since the last update.</param>
  /// <param name="dltSecs">The time delta in seconds since the last
  /// update.</param>
  public virtual void Update(float dlt, float dltSecs) {
    // Implement in derived classes
  }

  /// <summary>
  /// Virtual method called when the power-up times out.
  /// </summary>
  public virtual void TimeOut() {
    // Implement in derived classes
  }

  /// <summary>
  /// Virtual method called when the power-up is enabled or disabled. Called by
  /// the constructor.
  /// </summary>
  public virtual void OnEnabled(bool enabled) {
    // Implement in derived classes
  }
}
