using SFDGameScriptInterface;

namespace PowerupsDeluxe {
  public partial class GameScript : GameScriptInterface {
    // ADRENALINE - dsafxP
    public class Adrenaline : Powerup {
      private const uint EFFECT_COOLDOWN = 50; // Cooldown between each effect
      private const float SPEED_MULT = 0.75f; // Moving while punching speed multiplier
      private const float BOUNCE_SPEED = 9;

      private static readonly Vector2 _jumpAttackSpeed = new Vector2(0, 2);

      private static readonly VirtualKey[] _inputKeys = { // Keys that will trigger movement
        VirtualKey.AIM_RUN_LEFT,
        VirtualKey.AIM_RUN_RIGHT
      };

      private bool PlayerValid {
        get {
          return Player.IsOnGround &&
          !Player.IsDiving &&
          !Player.IsManualAiming &&
          !Player.IsDisabled;
        }
      }

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

      public Adrenaline(IPlayer player) : base(player) {
        Time = 18000; // Set duration of powerup (18 seconds)
      }

      public override void Update(float dlt, float dltSecs) {
        // Moving while attacking
        if ((_inputKeys.Any(k => Player.KeyPressed(k)) || Player.IsBot) &&
          (Player.IsMeleeAttacking || Player.IsKicking)) {
          // Calculate offset
          Vector2 offset = new Vector2((SPEED_MULT * Player.GetModifiers().RunSpeedModifier) *
            Player.FacingDirection, 0);

          // Apply offset
          Player.SetWorldPosition(Player.GetWorldPosition() + offset);
        }

        // Bounce
        if (Player.KeyPressed(VirtualKey.JUMP) && PlayerValid) {
          Vector2 vel = Player.GetLinearVelocity();

          vel.Y = BOUNCE_SPEED;

          Player.SetLinearVelocity(vel);
        }

        // Jump attack spam
        if ((Player.IsJumpAttacking || Player.IsJumpKicking) &&
        Player.GetLinearVelocity().Y > _jumpAttackSpeed.Y)
          Player.SetLinearVelocity(_jumpAttackSpeed);

        // Play effect
        if (Time % EFFECT_COOLDOWN == 0)
          Game.PlayEffect(EffectName.ImpactDefault, Player.GetWorldPosition());
      }

      protected override void Activate() {
      }

      public override void TimeOut() {
        // Play effects indicating expiration of powerup
        Game.PlaySound("StrengthBoostStop", Vector2.Zero);
        Game.PlayEffect(EffectName.PlayerLandFull, Player.GetWorldPosition());
      }
    }
  }
}