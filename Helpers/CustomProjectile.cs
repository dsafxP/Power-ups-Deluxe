using SFDGameScriptInterface;

namespace PowerupsDeluxe {
  public partial class GameScript : GameScriptInterface {
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
            IPlayer hitPlayer = (IPlayer)checkedResult.HitObject;

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
        int count = (int)Math.Ceiling(Vector2.Distance(start, end) / pointDistance);

        for (int i = 0; i < count; i++) {
          Vector2 pos = Vector2.Lerp(start, end, (float)i / (count - 1));
          func(pos);
        }
      }
    }
  }
}