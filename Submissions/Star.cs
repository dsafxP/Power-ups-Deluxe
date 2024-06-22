private static readonly Random _rng = new Random();

// STARRED - Tomo
public class Star : Powerup {
  private const float EFFECT_COOLDOWN = 50;
  private const float THROW_COOLDOWN = 100;
  private const float PUSH_FORCE = 7;
  private const float PUSH_DMG = 16;

  private static readonly PlayerCommand _playerCommand = new PlayerCommand(PlayerCommandType.Fall);
  private static readonly PlayerModifiers _starMod = new PlayerModifiers() {
    EnergyConsumptionModifier = 0,
      ProjectileCritChanceTakenModifier = 0,
      ExplosionDamageTakenModifier = 0,
      ProjectileDamageTakenModifier = 0,
      MeleeDamageTakenModifier = 0,
      ImpactDamageTakenModifier = 0,
      MeleeStunImmunity = 1,
      CanBurn = 0,
      RunSpeedModifier = 2,
      SprintSpeedModifier = 2
  };

  private static readonly string[] _colors = {
    "ClothingRed",
    "ClothingOrange",
    "ClothingYellow",
    "ClothingGreen",
    "ClothingBlue",
    "ClothingPurple"
  };

  private static readonly string[] _lightColors = {
    "ClothingLightRed",
    "ClothingLightOrange",
    "ClothingLightYellow",
    "ClothingLightGreen",
    "ClothingLightBlue",
    "ClothingLightPurple"
  };

  private int _rainbowIndex = 0;

  private IProfile _profile;
  private PlayerModifiers _modifiers;

  private int RainbowIndex {
    get {
      _rainbowIndex = (_rainbowIndex + 1) % _colors.Length;

      return _rainbowIndex;
    }
  }

  private IPlayer[] PlayersToPush {
    get {
      return Game.GetObjectsByArea < IPlayer > (Player.GetAABB())
        .Where(p => !p.IsDead && p != Player &&
          (p.GetTeam() != Player.GetTeam() || p.GetTeam() == PlayerTeam.Independent))
        .ToArray();
    }
  }

  public override string Name {
    get {
      return "STARRED";
    }
  }

  public override string Author {
    get {
      return "Tomo";
    }
  }

  public Star(IPlayer player) : base(player) {
    Time = 9000; // 9 s
  }

  protected override void Activate() {
    _profile = Player.GetProfile(); // Store profile

    _modifiers = Player.GetModifiers(); // Store original player modifiers

    _modifiers.CurrentHealth = -1;
    _modifiers.CurrentEnergy = -1;

    Player.SetModifiers(_starMod);
  }

  public override void Update(float dlt, float dltSecs) {
    if (Time % EFFECT_COOLDOWN == 0) {
      PointShape.Random(Draw, Player.GetAABB(), _rng);

      Player.SetProfile(ColorProfile(Player.GetProfile(),
        _colors[RainbowIndex], _lightColors[_rainbowIndex]));
    }

    if (Time % THROW_COOLDOWN == 0)
      foreach(IPlayer toPush in PlayersToPush) {
        toPush.SetInputEnabled(false);
        toPush.AddCommand(_playerCommand);

        Events.UpdateCallback.Start((float _dlt) => {
          toPush.SetInputEnabled(true);
        }, 1, 1);

        Vector2 toPushPos = toPush.GetWorldPosition();

        toPush.SetWorldPosition(toPushPos + (Vector2Helper.Up * 2)); // Sticky feet

        toPush.SetLinearVelocity(new Vector2(-toPush.FacingDirection * PUSH_FORCE, PUSH_FORCE));

        toPush.DealDamage(PUSH_DMG, Player.UniqueID);

        Game.PlaySound("PlayerDiveCatch", Vector2.Zero);
        Game.PlayEffect(EffectName.Smack, toPushPos);
      }
  }

  public override void TimeOut() {
    // Play effects indicating expiration of powerup
    Game.PlaySound("StrengthBoostStop", Vector2.Zero);
    Game.PlayEffect(EffectName.PlayerLandFull, Player.GetWorldPosition());
  }

  public override void OnEnabled(bool enabled) {
    if (!enabled) { // Restore player
      Player.SetModifiers(_modifiers);
      Player.SetProfile(_profile);
    }
  }

  private static void Draw(Vector2 v) {
    Game.PlayEffect(EffectName.ItemGleam, v);
  }

  private static IProfile ColorProfile(IProfile pr, string col, string colI) {
    if (pr.Accesory != null)
      pr.Accesory = new IProfileClothingItem(pr.Accessory.Name, col, colI);

    if (pr.ChestOver != null)
      pr.ChestOver = new IProfileClothingItem(pr.ChestOver.Name, col, colI);

    if (pr.ChestUnder != null)
      pr.ChestUnder = new IProfileClothingItem(pr.ChestUnder.Name, col, colI);

    if (pr.Feet != null)
      pr.Feet = new IProfileClothingItem(pr.Feet.Name, col, colI);

    if (pr.Hands != null)
      pr.Hands = new IProfileClothingItem(pr.Hands.Name, col, colI);

    if (pr.Head != null)
      pr.Head = new IProfileClothingItem(pr.Head.Name, col, colI);

    if (pr.Legs != null)
      pr.Legs = new IProfileClothingItem(pr.Legs.Name, col, colI);

    if (pr.Waist != null)
      pr.Waist = new IProfileClothingItem(pr.Waist.Name, col, colI);

    return pr;
  }
}