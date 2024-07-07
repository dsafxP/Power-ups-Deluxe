using SFDGameScriptInterface;

namespace PowerupsDeluxe {
  public partial class GameScript : GameScriptInterface {
    // TELEKINESIS - dsafxP
    public class Telekinesis : Powerup {
      private const float EFFECT_COOLDOWN = 50;
      private const float VORTEX_FORCE = 5;
      private const float THROW_FORCE = 9;
      private const float BULLET_FORCE = 200;
      private const float LAUNCH_FORCE = 37;
      private const float TRACK_THROW_SIZE = 30;

      private static readonly Vector2 _vortexOffset = new Vector2(0, 32);
      private static readonly Vector2 _rayCastEndOffset = new Vector2(56, -1);
      private static readonly Vector2 _rayCastStartOffset = new Vector2(0, -1);

      private static readonly RayCastInput _rayCastInput = new RayCastInput(true) {
        AbsorbProjectile = RayCastFilterMode.Any,
        ProjectileHit = RayCastFilterMode.True
      };

      private readonly List<IObject> _thrown = new List<IObject>();

      private Events.PlayerKeyInputCallback _keyCallback = null;
      private Events.ObjectCreatedCallback _objectCreatedCallback = null;

      private IObject _sticky = null;

      private Vector2 InputDirection {
        get {
          Vector2 vel = Vector2.Zero;

          if (Player.IsBot) {
            IPlayer closestEnemy = ClosestEnemy;

            if (closestEnemy != null) {
              vel = Vector2Helper.DirectionTo(Player.GetWorldPosition(),
              closestEnemy.GetWorldPosition());
            }
          } else {
            vel.X += Player.KeyPressed(VirtualKey.AIM_RUN_RIGHT) ? 1 : 0;
            vel.X -= Player.KeyPressed(VirtualKey.AIM_RUN_LEFT) ? 1 : 0;

            vel.Y += Player.KeyPressed(VirtualKey.AIM_CLIMB_UP) ||
            Player.KeyPressed(VirtualKey.JUMP) ? 1 : 0;
            vel.Y -= Player.KeyPressed(VirtualKey.AIM_CLIMB_DOWN) ? 1 : 0;
          }

          return vel;
        }
      }

      private Vector2 RayCastEndOffset {
        get {
          Vector2 v = _rayCastEndOffset;
          v.X *= Player.FacingDirection;

          return v;
        }
      }

      private Vector2 RayCastStartOffset {
        get {
          Vector2 v = _rayCastStartOffset;
          v.X *= Player.FacingDirection;

          return v;
        }
      }

      private RayCastResult RayCast {
        get {
          Vector2 playerPos = Player.GetWorldPosition();

          Vector2 rayCastStart = playerPos + RayCastStartOffset;
          Vector2 rayCastEnd = playerPos + RayCastEndOffset;

          Game.DrawLine(rayCastStart, rayCastEnd, Color.Red);

          return Game.RayCast(rayCastStart, rayCastEnd, _rayCastInput)[0];
        }
      }

      private IObject FrontObject {
        get {
          RayCastResult result = RayCast;
          IObject hit = result.HitObject;

          if (hit == null)
            return null;

          if (hit.GetBodyType() == BodyType.Dynamic && hit.Destructable)
            return hit;

          return null;
        }
      }

      private IPlayer ClosestEnemy {
        get {
          List<IPlayer> enemies = Game.GetPlayers()
            .Where(p => (p.GetTeam() != Player.GetTeam() ||
              p.GetTeam() == PlayerTeam.Independent) && !p.IsDead &&
              p != _sticky && p != Player)
            .ToList();

          Vector2 playerPos = Player.GetWorldPosition();

          enemies.Sort((p1, p2) => Vector2.Distance(p1.GetWorldPosition(), playerPos)
            .CompareTo(Vector2.Distance(p2.GetWorldPosition(), playerPos)));

          return enemies.FirstOrDefault();
        }
      }

      private Area TrackThrowArea {
        get {
          Area playerArea = Player.GetAABB();

          playerArea.SetDimensions(TRACK_THROW_SIZE, TRACK_THROW_SIZE);

          return playerArea;
        }
      }

      public IObject[] Thrown {
        get {
          _thrown.RemoveAll(item => item == null || item.IsRemoved ||
          !item.IsMissile);

          return _thrown.ToArray();
        }
      }

      public override string Name {
        get {
          return "TELEKINESIS";
        }
      }

      public override string Author {
        get {
          return "dsafxP";
        }
      }

      public Telekinesis(IPlayer player) : base(player) {
        Time = 27000; // 27 s
      }

      protected override void Activate() {
      }

      public override void Update(float dlt, float dltSecs) {
        if (_sticky != null) {
          Vector2 pos = Player.GetWorldPosition() + _vortexOffset;
          Vector2 stickyPos = _sticky.GetWorldPosition();

          _sticky.SetLinearVelocity(Vector2Helper.DirectionTo(stickyPos, pos) * VORTEX_FORCE);

          _sticky.SetAngularVelocity(1);

          if (Time % EFFECT_COOLDOWN == 0)
            Game.PlayEffect(EffectName.ImpactDefault, stickyPos);
        }

        Vector2 inputDirection = InputDirection;

        foreach (IObject thrown in Thrown) {
          thrown.SetLinearVelocity(inputDirection * THROW_FORCE);
          thrown.SetAngularVelocity(THROW_FORCE);
        }

        foreach (IProjectile fired in Game.GetProjectiles()) {
          if (fired.OwnerPlayerID != Player.UniqueID)
            continue;

          fired.Direction = inputDirection;
          fired.Velocity = inputDirection != Vector2.Zero ? inputDirection * BULLET_FORCE :
          fired.Direction * BULLET_FORCE;
        }
      }

      public override void TimeOut() {
        Game.PlaySound("StrengthBoostStop", Vector2.Zero);
      }

      public override void OnEnabled(bool enabled) {
        if (enabled) {
          _keyCallback = Events.PlayerKeyInputCallback.Start(OnPlayerKeyInput);
          _objectCreatedCallback = Events.ObjectCreatedCallback.Start(OnObjectCreated);
        } else {
          _keyCallback.Stop();
          _keyCallback = null;

          _objectCreatedCallback.Stop();
          _objectCreatedCallback = null;
        }
      }

      private void OnPlayerKeyInput(IPlayer player, VirtualKeyInfo[] keyEvents) {
        if (player != Player)
          return;

        foreach (VirtualKeyInfo pressed in keyEvents) {
          if (pressed.Event != VirtualKeyEvent.Pressed && pressed.Key != VirtualKey.ACTIVATE)
            continue;

          if (_sticky != null) {
            _sticky.TrackAsMissile(true);

            IPlayer closestEnemy = ClosestEnemy;

            _sticky.SetLinearVelocity(closestEnemy != null ?
              Vector2Helper.DirectionTo(_sticky.GetWorldPosition(),
                closestEnemy.GetWorldPosition()) * LAUNCH_FORCE :
              Vector2.Zero);

            _sticky.SetAngularVelocity(LAUNCH_FORCE);

            Game.PlayEffect(EffectName.Smack, _sticky.GetWorldPosition());
            Game.PlaySound("MeleeSwing", Vector2.Zero);

            _sticky = null;
          } else {
            _sticky = FrontObject;

            if (_sticky != null) {
              Game.PlayEffect(EffectName.BulletHit, _sticky.GetWorldPosition());
              Game.PlaySound("Draw1", Vector2.Zero);
            }
          }
        }
      }

      private void OnObjectCreated(IObject[] objs) {
        _thrown.AddRange(objs
        .Where(o => o.IsMissile && TrackThrowArea
        .Intersects(o.GetAABB())));
      }
    }
  }
}