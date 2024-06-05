// OVERCHARGE - dsafxP - Danila015 - Eiga
public class Overcharge : Powerup {
  private const float EFFECT_COOLDOWN = 175;
  private const float CHARGE_INTENSITY = 0.3f; // Charges * Value
  private const float CHARGE_DELAY = 2000;
  private const string CHARGE_TEXT = "+{0}"; // 0 for charges

  private Events.PlayerMeleeActionCallback _meleeActionCallback = null;

  private PlayerModifiers _modifiers; // Stores original player modifiers

  private float _elapsed = 0;
  private int _charges = 0;

  private int Charges {
    get {
      return _charges;
    }
    set {
      _charges = value;

      float charge = _charges * CHARGE_INTENSITY;

      if (_charges > 0) {
        _elapsed += CHARGE_DELAY;

        Game.PlayEffect(EffectName.CustomFloatText, Player.GetWorldPosition(),
          string.Format(CHARGE_TEXT, _charges));

        Game.PlaySound("GetAmmoSmall", Vector2.Zero, Math.Min(charge, 1));
      }

      Player.SetModifiers(new PlayerModifiers() {
        MeleeForceModifier = Math.Max(charge, 1)
      });
    }
  }

  public bool Depleted {
    get {
      return _elapsed <= 0;
    }
  }

  public override string Name {
    get {
      return "OVERCHARGE";
    }
  }

  public override string Author {
    get {
      return "dsafxP - Danila015 - Eiga";
    }
  }

  public Overcharge(IPlayer player) : base(player) {
    Time = 33000; // 33 s
  }

  protected override void Activate() {
    _modifiers = Player.GetModifiers(); // Store original player modifiers

    _modifiers.CurrentHealth = -1;
    _modifiers.CurrentEnergy = -1;
  }

  public override void Update(float dlt, float dltSecs) {
    _elapsed = Math.Max(_elapsed - dlt, 0);

    if (Depleted && Charges != 0) {
      Charges = 0;

      Game.PlaySound("ElectricSparks", Vector2.Zero);
      Game.PlayEffect(EffectName.Electric, Player.GetWorldPosition());
    }
  }

  public override void TimeOut() {
    Game.PlaySound("StrengthBoostStop", Vector2.Zero);
  }

  public override void OnEnabled(bool enabled) {
    if (enabled) {
      _meleeActionCallback = Events.PlayerMeleeActionCallback.Start(OnPlayerMeleeAction);
    } else {
      _meleeActionCallback.Stop();

      _meleeActionCallback = null;

      // Restore original player modifiers
      Player.SetModifiers(_modifiers);
    }
  }

  private void OnPlayerMeleeAction(IPlayer player, PlayerMeleeHitArg[] args) {
    if (player != Player)
      return;

    IEnumerable < IPlayer > stunned = args
      .Where(a => a.IsPlayer)
      .Select(p => (IPlayer) p.HitObject)
      .Where(p => (p.IsFalling || p.IsStaggering) && !p.IsDead);

    if (stunned.Any())
      Charges += stunned.Count();
  }
}