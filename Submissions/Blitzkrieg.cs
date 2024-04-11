// BLITZKRIEG - dsafxP
public class Blitzkrieg : Powerup {
  private const float ATTACK_COOLDOWN = 250;
  private const float THROW_ANGULAR_VELOCITY = 50;
  private const float THROW_SPEED = 10;

  private static readonly Vector2 _offset = new Vector2(0, 24);
  private static readonly PlayerModifiers _setMod = new PlayerModifiers() {
    ExplosionDamageTakenModifier = 0,
      ImpactDamageTakenModifier = 0.25f
  };
  private static readonly string[] _throwableIDs = {
    "WpnGrenadesThrown",
    "WpnMolotovsThrown",
    //"WpnC4Thrown",
    "WpnMineThrown"
  };

  private PlayerModifiers _modifiers;

  private static string RandomThrowableID {
    get {
      return _throwableIDs[_rng.Next(_throwableIDs.Length)];
    }
  }

  private IPlayer ClosestEnemy {
    get {
      List < IPlayer > enemies = Game.GetPlayers()
        .Where(p => (p.GetTeam() != Player.GetTeam() ||
            p.GetTeam() == PlayerTeam.Independent) && !p.IsDead &&
          p != Player)
        .ToList();

      Vector2 playerPos = Player.GetWorldPosition();

      enemies.Sort((p1, p2) => Vector2.Distance(p1.GetWorldPosition(), playerPos)
        .CompareTo(Vector2.Distance(p2.GetWorldPosition(), playerPos)));

      return enemies.FirstOrDefault();
    }
  }

  public override string Name {
    get {
      return "BLITZKRIEG";
    }
  }

  public override string Author {
    get {
      return "dsafxP";
    }
  }

  public Blitzkrieg(IPlayer player) : base(player) {
    Time = 19000; // 19 s
  }

  protected override void Activate() {
    _modifiers = Player.GetModifiers(); // Store original player modifiers

    _modifiers.CurrentHealth = -1;
    _modifiers.CurrentEnergy = -1;

    Player.SetModifiers(_setMod);
  }

  public override void Update(float dlt, float dltSecs) {
    if (Player.IsBurning)
      Player.ClearFire();

    if (Time % ATTACK_COOLDOWN == 0) {
      Throw(true);
    }
  }

  public override void OnEnabled(bool enabled) {
    if (!enabled)
      Player.SetModifiers(_modifiers);
  }

  private IObject Throw(bool missile = false) {
    IPlayer closestEnemy = ClosestEnemy;

    if (closestEnemy == null)
      return null;

    Vector2 playerPos = Player.GetWorldPosition() + _offset;
    Vector2 throwVel = Vector2Helper.DirectionTo(playerPos, closestEnemy
      .GetWorldPosition()) * THROW_SPEED;

    IObject thrown = Game.CreateObject(RandomThrowableID, playerPos, 0,
      throwVel, THROW_ANGULAR_VELOCITY, Player.FacingDirection);

    thrown.TrackAsMissile(missile);

    Game.PlayEffect(EffectName.TraceSpawner, Vector2.Zero, thrown.UniqueID,
      EffectName.ItemGleam, 2f);

    return thrown;
  }
}