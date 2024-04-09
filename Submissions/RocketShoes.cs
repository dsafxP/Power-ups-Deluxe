// ROCKET SHOES - Ebomb09
public class RocketShoes : Powerup {
  private const uint EFFECT_COOLDOWN = 25;

  private IObject[] _feet;

  private bool PlayerValid {
    get {
      return !Player.IsDisabled &&
        !Player.IsLedgeGrabbing &&
        !Player.IsClimbing &&
        !Player.IsDiving &&
        !Player.IsGrabbing &&
        Player.IsInputEnabled;
    }
  }

  public override string Name {
    get {
      return "ROCKET SHOES";
    }
  }

  public override string Author {
    get {
      return "Ebomb09";
    }
  }

  public RocketShoes(IPlayer player): base(player) {
    Time = 15000;
  }

  protected override void Activate() {
    _feet = new IObject[] {
      Game.CreateObject("InvisibleBlockNoCollision", Vector2.Zero, 3 / 2 * MathHelper.PI),
        Game.CreateObject("InvisibleBlockNoCollision", Vector2.Zero, 3 / 2 * MathHelper.PI)
    };

    _feet[0].SetBodyType(BodyType.Dynamic);
    _feet[1].SetBodyType(BodyType.Dynamic);
  }

  public override void Update(float dlt, float dltSecs) {
    bool rocketing = PlayerValid && Player.KeyPressed(VirtualKey.JUMP);

    if (rocketing) {
      Vector2 impulse = new Vector2(0, Player.GetLinearVelocity().Y + 0.2f);

      if (Player.KeyPressed(VirtualKey.AIM_RUN_RIGHT))
        impulse.X += 1;

      if (Player.KeyPressed(VirtualKey.AIM_RUN_LEFT))
        impulse.X -= 1;

      if (Player.KeyPressed(VirtualKey.SPRINT))
        impulse.X *= 2;

      if (Player.KeyPressed(VirtualKey.WALKING))
        impulse.X /= 2;

      Player.SetLinearVelocity(impulse);
    }

    foreach(IObject obj in _feet) {
      obj.SetLinearVelocity(Player.GetLinearVelocity());

      obj.SetAngle(
        Vector2Helper.AngleToPoint(
          Player.GetWorldPosition(),
          Player.GetWorldPosition() - new Vector2(Player.GetLinearVelocity().X, Math.Abs(Player.GetLinearVelocity().Y))
        )
      );
    }

    _feet[0].SetWorldPosition(Player.GetWorldPosition() + new Vector2(-5, -2));
    _feet[1].SetWorldPosition(Player.GetWorldPosition() + new Vector2(5, -2));

    if (rocketing) {
      if (Time % EFFECT_COOLDOWN == 0) {
        Game.PlayEffect("MZLED", Vector2.Zero, _feet[0].UniqueID, "MuzzleFlashS");
        Game.PlayEffect("MZLED", Vector2.Zero, _feet[1].UniqueID, "MuzzleFlashS");
      }

      if (Time % (EFFECT_COOLDOWN * 2) == 0)
        Game.PlaySound("BarrelExplode", Vector2.Zero, 0.5f);
    }
  }

  public override void TimeOut() {
    Game.PlaySound("DestroyMetal", Vector2.Zero, 1);
    Game.PlayEffect(EffectName.Sparks, Player.GetWorldPosition());
  }

  public override void OnEnabled(bool enabled) {
    if (!enabled) {
      _feet[0].Remove();
      _feet[1].Remove();
    }
  }
}