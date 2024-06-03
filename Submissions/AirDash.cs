using SFDGameScriptInterface;

namespace PowerupsDeluxe {
  public partial class GameScript : GameScriptInterface {
    // AIR DASH - Danila015
    public class AirDash : Powerup {
      private static readonly Vector2 _velocity = new Vector2(17, 5);

      private Vector2 Velocity {
        get {
          Vector2 v = _velocity;
          v.X *= Player.FacingDirection;

          return v;
        }
      }

      public override string Name {
        get {
          return "AIR DASH";
        }
      }

      public override string Author {
        get {
          return "Danila015";
        }
      }

      public bool Dashing {
        get;
        private set;
      }

      public AirDash(IPlayer player) : base(player) {
        Time = 27000; // 27 s
        Dashing = false;
      }

      protected override void Activate() { }

      public override void Update(float dlt, float dltSecs) {
        EmptyUppercutCheck(0);

        if (Dashing) {
          if (!Player.IsMeleeAttacking && Player.IsOnGround) {
            Dashing = false;

            return;
          }
        }
      }

      public override void TimeOut() {
        Game.PlaySound("StrengthBoostStop", Vector2.Zero);
      }

      private void OnEmptyUppercut() {
        if (Dashing)
          return;

        Game.PlaySound("Sawblade", Vector2.Zero);

        Dashing = true;

        Game.PlayEffect(EffectName.TraceSpawner, Vector2.Zero, Player.UniqueID, EffectName.DustTrail, 1.5f);

        Player.SetWorldPosition(Player.GetWorldPosition() + Vector2Helper.Up * 2); // Sticky feet

        Player.SetLinearVelocity(Velocity);
      }

      private void EmptyUppercutCheck(float dlt) {
        float playerXVelocityAbs = Math.Abs(Player.GetLinearVelocity().X);

        bool[] checks = {
          Player.IsMeleeAttacking,
          playerXVelocityAbs >= 0.4f,
          playerXVelocityAbs < 1
        };

        if (checks.All(c => c)) {
          OnEmptyUppercut();
        }
      }
    }
  }
}