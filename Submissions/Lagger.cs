private static readonly Random _rng = new Random();

// LAGGER - dsafxP
public class Lagger : Powerup {
  private const float TP_COOLDOWN = 500;
  private const float SAVE_POS_COOLDOWN = 1000;
  private const float PING_INTENSITY = 0.01f; // Ping * Value = 1

  private static readonly string[] _effectNames = {
    "0",
    "null",
    "NaN",
    "N/A",
    "undefined",
    "nullReference",
    "exception",
    "error",
    "unknown",
    "none",
    "missing",
    "invalid",
    "failure",
    "0x0",
    "false",
    "empty",
    "nil",
    "void"
  };

  private PlayerModifiers _modifiers;
  private Vector2 _lastPos = Vector2.Zero;

  private float PingFactor {
    get {
      float p = Player.GetUser()
        .Ping * PING_INTENSITY;

      return p < 1 ? -2 : p;
    }
  }

  private PlayerModifiers CurrentModifiers {
    get {
      float pingFactor = PingFactor;

      return new PlayerModifiers() {
        EnergyRechargeModifier = pingFactor,
          ProjectileDamageDealtModifier = pingFactor,
          ProjectileCritChanceDealtModifier = pingFactor,
          MeleeDamageDealtModifier = pingFactor,
          MeleeForceModifier = pingFactor,
          RunSpeedModifier = pingFactor,
          SprintSpeedModifier = pingFactor
      };
    }
  }

  public override string Name {
    get {
      return "LAGGER";
    }
  }

  public override string Author {
    get {
      return "dsafxP";
    }
  }

  public Lagger(IPlayer player) : base(player) {
    Time = 16000; // 16 s
  }

  protected override void Activate() {
    _lastPos = Player.GetWorldPosition();
    _modifiers = Player.GetModifiers();

    _modifiers.CurrentHealth = -1;
    _modifiers.CurrentEnergy = -1;
  }

  public override void Update(float dlt, float dltSecs) {
    if (Time % SAVE_POS_COOLDOWN == 0)
      _lastPos = Player.GetWorldPosition();
    else if (Time % TP_COOLDOWN == 0) {
      Player.SetWorldPosition(_lastPos);

      Game.PlayEffect(EffectName.CustomFloatText, _lastPos,
        _effectNames[_rng.Next(_effectNames.Length)]);
    }

    Player.SetModifiers(CurrentModifiers);
  }

  public override void TimeOut() {
    Game.PlaySound("StrengthBoostStop", Vector2.Zero);
  }

  public override void OnEnabled(bool enabled) {
    if (!enabled) {
      Player.SetModifiers(_modifiers);
    }
  }
}