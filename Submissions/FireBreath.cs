private static readonly Random _rng = new Random();

// FIRE BREATH - Danila015
public class FireBreath : Powerup {
  private const float EFFECT_COOLDOWN = 175;
  private const float FIRE_RATE = 100;
  private const float TIME_DECREASE = 250;

  private const float RANDOM_SPEED_EXP = 2;
  private const float SPEED = 0.1f;

  private const FireNodeType FIRE_TYPE = FireNodeType.Flamethrower;

  private static readonly Vector2 _effectOffset = new Vector2(0, 8);
  private static readonly Vector2 _fireOffset = new Vector2(8, 0);

  private static readonly Vector2 _rayCastEndOffset = new Vector2(48, 8);
  private static readonly Vector2 _rayCastStartOffset = new Vector2(0, 8);

  private static readonly Type[] _types = {
    typeof (IPlayer)
  };

  private static readonly RayCastInput _rayCastInput = new RayCastInput(true) {
    Types = _types
  };

  private bool EnemiesInRange {
    get {
      Vector2 playerPos = Player.GetWorldPosition();

      Vector2 rayCastStartOffset = _rayCastStartOffset;
      rayCastStartOffset.X *= Player.FacingDirection;

      Vector2 rayCastEndOffset = _rayCastEndOffset;
      rayCastEndOffset.X *= Player.FacingDirection;

      Vector2 rayCastStart = playerPos + rayCastStartOffset;
      Vector2 rayCastEnd = playerPos + rayCastEndOffset;

      Game.DrawLine(rayCastStart, rayCastEnd, Color.Red);

      RayCastResult result = Game.RayCast(rayCastStart, rayCastEnd, _rayCastInput)[0];

      if (result.IsPlayer) {
        IPlayer hit = (IPlayer) result.HitObject;

        return (hit.GetTeam() == PlayerTeam.Independent || hit.GetTeam() != Player.GetTeam()) &&
          !hit.IsDead;
      }

      return false;
    }
  }

  public override string Name {
    get {
      return "FIRE BREATH";
    }
  }

  public override string Author {
    get {
      return "Danila015";
    }
  }

  public FireBreath(IPlayer player) : base(player) {
    Time = 25000; // 25 s
  }

  protected override void Activate() {}

  public override void Update(float dlt, float dltSecs) {
    if (Player.IsBurningInferno) // Fire resistance
      Player.ClearFire();
    if (Time % EFFECT_COOLDOWN == 0) // Effect
      Game.PlayEffect("TR_F", Player.GetWorldPosition() + _effectOffset);

    if (Time % FIRE_RATE == 0) // Attack
      if (EnemiesInRange) {
        Time -= TIME_DECREASE; // Decrease time

        //Game.WriteToConsole(Time);

        Game.PlaySound("Flamethrower", Vector2.Zero);

        // Calculate offset
        Vector2 fireOffset = _fireOffset;
        fireOffset.X *= Player.FacingDirection;

        Game.SpawnFireNode(Player.GetWorldPosition() + fireOffset,
          GetRandomFireVelocity(_rng) * SPEED,
          FIRE_TYPE);
      }
  }

  private Vector2 GetRandomFireVelocity(Random random) {
    float x = (_rayCastEndOffset.X * Player.FacingDirection) * (float)(RANDOM_SPEED_EXP * random.NextDouble());
    float y = _rayCastEndOffset.Y * (float)(RANDOM_SPEED_EXP * random.NextDouble());

    return new Vector2(x, y);
  }
}