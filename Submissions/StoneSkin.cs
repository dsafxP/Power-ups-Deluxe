// STONE SKIN - Danila015
public class StoneSkin : Powerup {
  private const float HEAVY_EXP = 1.034f;

  private static readonly PlayerModifiers _stoneMod = new PlayerModifiers() {
    ImpactDamageTakenModifier = 0,
      ExplosionDamageTakenModifier = 0.25f,
      ProjectileDamageTakenModifier = 0.25f,
      ProjectileCritChanceTakenModifier = 0,
      MeleeStunImmunity = 1,
      MeleeDamageTakenModifier = 0.25f,
      SprintSpeedModifier = 0.75f
  };

  private IProfile _profile;
  private PlayerModifiers _modifiers;
  
  private IProfile StoneProfile {
    get {
      IProfile playerProfile = Player.GetProfile();
    
      playerProfile.Skin = new IProfileClothingItem(string.Format("Normal{0}", 
      playerProfile.Gender == Gender.Male ? string.Empty : "_fem"), "Skin5");
      
      return ColorProfile(playerProfile, "ClothingGray", "ClothingLightGray");
    }
  }

  public override string Name {
    get {
      return "STONE SKIN";
    }
  }

  public override string Author {
    get {
      return "Danila015";
    }
  }

  public StoneSkin(IPlayer player) : base(player) {
    Time = 13000; // 13 s
  }

  protected override void Activate() {
    _profile = Player.GetProfile(); // Store profile

    _modifiers = Player.GetModifiers(); // Store original player modifiers

    _modifiers.CurrentHealth = -1;
    _modifiers.CurrentEnergy = -1;

    Player.SetProfile(StoneProfile);

    Player.SetModifiers(_stoneMod);
  }

  public override void Update(float dlt, float dltSecs) {
    if (Player.IsBurning)
      Player.ClearFire();

    if (!Player.IsOnGround) {
      Vector2 playerLinearVelocity = Player.GetLinearVelocity();

      if (playerLinearVelocity.Y < 0) {
        playerLinearVelocity.Y *= HEAVY_EXP;

        playerLinearVelocity.X /= dlt; // Normalize X

        Player.SetLinearVelocity(playerLinearVelocity);
      }
    }
  }

  public override void TimeOut() {
    Game.PlayEffect("DestroyCloth", Player.GetWorldPosition());
    Game.PlaySound("DestroyStone", Vector2.Zero);
  }

  public override void OnEnabled(bool enabled) {
    if (!enabled) { // Restore player
      Player.SetModifiers(_modifiers);
      Player.SetProfile(_profile);
    }
  }

  public static IProfile ColorProfile(IProfile pr, string col, string colI) {
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