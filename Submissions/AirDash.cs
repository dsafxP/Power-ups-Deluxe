public class AirDash : Powerup {
  private const uint TRAIL_COOLDOWN = 5;

  private static readonly Vector2 _velocity = new Vector2(18, 5);

  private Vector2 Velocity {
    get {
      Vector2 v = _velocity;
      v.X *= Player.FacingDirection;

      return v;
    }
  }

  public override string Name {
    get {
      return "AIR DASH";
    }
  }

  public override string Author {
    get {
      return "Danila015";
    }
  }

  public bool Dashing {
    get;
    private set;
  }

  public AirDash(IPlayer player) : base(player) {
    Time = 24000; // 24 s
    Dashing = false;
  }

  protected override void Activate() {}

  public override void Update(float dlt, float dltSecs) {
    EmptyUppercutCheck(0);

    Game.WriteToConsole(Dashing);

    if (Dashing) {
      if (!Player.IsMeleeAttacking && Player.IsOnGround) {
        Dashing = false;

        return;
      }

      if (Time % TRAIL_COOLDOWN == 0) {
        Game.PlayEffect("ImpactDefault", Player.GetWorldPosition());
      }
    }
  }

  public override void TimeOut() {
    Game.PlaySound("StrengthBoostStop", Vector2.Zero);
  }

  private void OnEmptyUppercut() {
    if (Dashing)
      return;

    Game.PlaySound("Sawblade", Vector2.Zero);

    Dashing = true;

    Player.SetWorldPosition(Player.GetWorldPosition() + Vector2Helper.Up * 2); // Sticky feet

    Player.SetLinearVelocity(Velocity);
  }

  private void EmptyUppercutCheck(float dlt) {
    float playerXVelocityAbs = Math.Abs(Player.GetLinearVelocity().X);

    bool[] checks = {
      Player.IsMeleeAttacking,
      playerXVelocityAbs >= 0.4f,
      playerXVelocityAbs < 1,
      //Player.CurrentWeaponDrawn == WeaponItemType.NONE
    };

    if (checks.All(c => c)) {
      OnEmptyUppercut();
    }
  }
}