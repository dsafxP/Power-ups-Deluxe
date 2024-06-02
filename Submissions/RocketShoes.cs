// ROCKET SHOES - Ebomb09
public class RocketShoes : Powerup {
  private const uint EFFECT_COOLDOWN = 25;
  private const float IMPULSE = 0.2f;

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
  
  private bool Rocketing {
    get {
      return PlayerValid && Player.KeyPressed(VirtualKey.JUMP);
    }
  }
  
  private Vector2 Impulse {
    get {
      Vector2 impulse = new Vector2(0, Player.GetLinearVelocity().Y + IMPULSE);
      
      impulse.X += Player.KeyPressed(VirtualKey.AIM_RUN_RIGHT) ? 1 : 
      (Player.KeyPressed(VirtualKey.AIM_RUN_LEFT) ? -1 : 0);
      
      impulse.X *= Player.KeyPressed(VirtualKey.SPRINT) ? 2 : 
      (Player.KeyPressed(VirtualKey.WALKING) ? 0.5f : 1);
      
      return impulse;
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
    Time = 20000; // 20 s
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
    Vector2 playerPos = Player.GetWorldPosition();

    foreach(IObject obj in _feet) {
      obj.SetLinearVelocity(Player.GetLinearVelocity());

      obj.SetAngle(
        Vector2Helper.AngleToPoint(
          playerPos,
          playerPos - new Vector2(Player.GetLinearVelocity().X, Math.Abs(Player.GetLinearVelocity().Y))
        )
      );
    }

    _feet[0].SetWorldPosition(playerPos + new Vector2(-5, -2));
    _feet[1].SetWorldPosition(playerPos + new Vector2(5, -2));

    if (Rocketing) {
      Player.SetLinearVelocity(Impulse);
      
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