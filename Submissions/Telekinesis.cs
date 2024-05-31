// TELEKINESIS - dsafxP
public class Telekinesis : Powerup {
  private const float EFFECT_COOLDOWN = 50;
  private const float FORCE = 5;
  private const float LAUNCH_FORCE = 37;

  private static readonly Vector2 _vortexOffset = new Vector2(0, 32);
  private static readonly Vector2 _rayCastEndOffset = new Vector2(56, 0);
  private static readonly Vector2 _rayCastStartOffset = Vector2.Zero;

  private static readonly RayCastInput _rayCastInput = new RayCastInput(true) {
    AbsorbProjectile = RayCastFilterMode.Any,
      ProjectileHit = RayCastFilterMode.True
  };

  private Events.PlayerKeyInputCallback _keyCallback = null;

  private IObject _sticky = null;

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

  private IObject FrontObject {
    get {
      Vector2 playerPos = Player.GetWorldPosition();

      Vector2 rayCastStart = playerPos + RayCastStartOffset;
      Vector2 rayCastEnd = playerPos + RayCastEndOffset;

      Game.DrawLine(rayCastStart, rayCastEnd, Color.Red);

      RayCastResult result = Game.RayCast(rayCastStart, rayCastEnd, _rayCastInput)[0];
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
      List < IPlayer > enemies = Game.GetPlayers()
        .Where(p => (p.GetTeam() != Player.GetTeam() ||
          p.GetTeam() == PlayerTeam.Independent) && !p.IsDead && p != _sticky)
        .ToList();

      Vector2 playerPos = Player.GetWorldPosition();

      enemies.Sort((p1, p2) => Vector2.Distance(p1.GetWorldPosition(), playerPos)
        .CompareTo(Vector2.Distance(p2.GetWorldPosition(), playerPos)));

      return enemies.FirstOrDefault();
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
    Time = 24000;
  }

  protected override void Activate() {}

  public override void Update(float dlt, float dltSecs) {
    if (_sticky != null) {
      Vector2 pos = Player.GetWorldPosition() + _vortexOffset;
      Vector2 stickyPos = _sticky.GetWorldPosition();

      _sticky.SetLinearVelocity(Vector2Helper.DirectionTo(stickyPos, pos) * FORCE);

      _sticky.SetAngularVelocity(1);

      if (Time % EFFECT_COOLDOWN == 0)
        Game.PlayEffect(EffectName.ImpactDefault, stickyPos);
    }
  }

  public override void TimeOut() {
    Game.PlaySound("StrengthBoostStop", Vector2.Zero);
  }

  public override void OnEnabled(bool enabled) {
    if (enabled) {
      _keyCallback = Events.PlayerKeyInputCallback.Start(OnPlayerKeyInput);
    } else {
      _keyCallback.Stop();
      _keyCallback = null;
    }
  }

  private void OnPlayerKeyInput(IPlayer player, VirtualKeyInfo[] keyEvents) {
    if (player != Player)
      return;

    foreach(VirtualKeyInfo pressed in keyEvents
      .Where(k => k.Event == VirtualKeyEvent.Pressed && k.Key == VirtualKey.ACTIVATE)) {
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
}