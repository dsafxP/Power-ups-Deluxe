// ADRENALINE - dsafxP
public class Adrenaline : Powerup {
  public override string Name {
    get {
      return "ADRENALINE";
    }
  }

  public override string Author {
    get {
      return "dsafxP";
    }
  }

  private const uint EFFECT_COOLDOWN = 50; // Cooldown between each effect
  private const float SPEED_MULT = 0.75f; // Moving while punching speed multiplier

  private static readonly VirtualKey[] _inputKeys = { // Keys that will trigger movement
    VirtualKey.AIM_RUN_LEFT,
    VirtualKey.AIM_RUN_RIGHT
  };

  public Adrenaline(IPlayer player): base(player) {
    Time = 16000; // Set duration of powerup (16 seconds)
  }

  public override void Update(float dlt, float dltSecs) {
    // Verify keys are pressed and the player is attacking or kicking
    if ((_inputKeys.Any(k => Player.KeyPressed(k)) || Player.IsBot) &&
      (Player.IsMeleeAttacking || Player.IsKicking)) {
      // Calculate offset
      Vector2 offset = new Vector2((SPEED_MULT * Player.GetModifiers().RunSpeedModifier) *
        Player.FacingDirection, 0);

      // Apply offset
      Player.SetWorldPosition(Player.GetWorldPosition() + offset);
    }

    // Play effect
    if (Time % EFFECT_COOLDOWN == 0)
      Game.PlayEffect("ImpactDefault", Player.GetWorldPosition());
  }

  protected override void Activate() {}

  public override void TimeOut() {
    // Play sound effect indicating expiration of powerup
    Game.PlaySound("StrengthBoostStop", Vector2.Zero);
  }
}