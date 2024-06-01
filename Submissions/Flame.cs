// FLAME - dsafxP
public class Flame : Powerup {
  private static readonly PlayerModifiers _fireMod = new PlayerModifiers() {
    FireDamageTakenModifier = 0
  };

  private BotBehaviorSet _set = null;

  private PlayerModifiers _modifiers; // Stores original player modifiers

  public override string Name {
    get {
      return "FLAME";
    }
  }

  public override string Author {
    get {
      return "dsafxP";
    }
  }

  public Flame(IPlayer player): base(player) {
    Time = 20000; // Set duration of powerup (20 seconds)
  }

  public override void Update(float dlt, float dltSecs) {
    Player.SetMaxFire(); // Ensure player has maximum fire level while powerup is active
  }

  protected override void Activate() {
    // Play visual effect at player's position indicating start of powerup
    Game.PlayEffect("PLRB", Player.GetWorldPosition());

    _modifiers = Player.GetModifiers(); // Store original player modifiers

    _modifiers.CurrentHealth = -1;
    _modifiers.CurrentEnergy = -1;
    
    Player.SetModifiers(_fireMod);

    if (Player.IsBot) {
      BotBehaviorSet botSet = Player.GetBotBehaviorSet();

      _set = botSet;

      botSet.DefensiveRollFireLevel = 0;

      Player.SetBotBehaviorSet(botSet);
    }
  }

  public override void TimeOut() {
    // Play effects indicating expiration of powerup
    Game.PlaySound("StrengthBoostStop", Vector2.Zero);
    Game.PlayEffect("PLRB", Player.GetWorldPosition());
  }

  public override void OnEnabled(bool enabled) {
    if (!enabled) {
      Player.ClearFire(); // Clear any fire effects on the player

      // Restore original player modifiers
      Player.SetModifiers(_modifiers);

      // Restore behavior set
      if (Player.IsBot && _set != null)
        Player.SetBotBehaviorSet(_set);
    }
  }
}