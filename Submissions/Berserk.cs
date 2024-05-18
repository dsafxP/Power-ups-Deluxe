private static readonly Random _rng = new Random();

// BERSERK - Danila015
public class Berserk : Powerup {
  private const float EFFECT_COOLDOWN = 100;
  private const float TIME = 35000;

  private static readonly PlayerModifiers _berserkMod = new PlayerModifiers() {
    CurrentHealth = 1,
      ProjectileCritChanceTakenModifier = 0,
      ExplosionDamageTakenModifier = 0,
      ProjectileDamageTakenModifier = 0,
      MeleeDamageTakenModifier = 0,
      ImpactDamageTakenModifier = 0,
      CanBurn = 0
  };

  private static readonly Vector2 _jumpVel = new Vector2(0, 12);

  private PlayerModifiers _modifiers; // Stores original player modifiers

  public override string Name {
    get {
      return "BERSERK";
    }
  }

  public override string Author {
    get {
      return "Danila015";
    }
  }

  public Berserk(IPlayer player) : base(player) {
    Time = TIME; // 35 s
  }

  protected override void Activate() {
    _modifiers = Player.GetModifiers(); // Store original player modifiers

    _modifiers.CurrentHealth = -1;
    _modifiers.CurrentEnergy = -1;

    Player.SetModifiers(_berserkMod);
    Player.SetStrengthBoostTime(TIME);
    Player.SetSpeedBoostTime(TIME);
  }

  public override void Update(float dlt, float dltSecs) {
    if (Player.KeyPressed(VirtualKey.JUMP) &&
      !Player.IsInMidAir && !Player.IsDisabled) {
      Player.SetLinearVelocity(_jumpVel);

      Game.PlaySound("LogoSlam", Vector2.Zero);
      Game.PlayEffect(EffectName.Dig, Player.GetWorldPosition());

      Game.PlayEffect(EffectName.TraceSpawner, Vector2.Zero,
        Player.UniqueID, EffectName.ImpactDefault, 2f);
    }

    if (Time % EFFECT_COOLDOWN == 0) {
      PointShape.Random(Draw, Player.GetAABB(), _rng);
    }
  }

  public override void OnEnabled(bool enabled) {
    if (!enabled) {
      Player.SetModifiers(_modifiers);

      Player.SetHealth(_berserkMod.CurrentHealth);
    }
  }

  private static void Draw(Vector2 v) {
    Game.PlayEffect(EffectName.Blood, v);
    Game.PlayEffect(EffectName.ItemGleam, v);
    Game.PlayEffect(EffectName.WoodParticles, v);
  }
}