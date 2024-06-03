using SFDGameScriptInterface;

namespace PowerupsDeluxe {
  public partial class GameScript : GameScriptInterface {
    // OVERHEAL - Danila015
    public class Overheal : Powerup {
      private const float REGEN_COOLDOWN = 100;
      private const float REGEN = 5;
      private const float CANCEL_DMG = 15;

      private const int OVERHEAL = 1;

      private static readonly PlayerDamageEventType[] _allowedTypes = {
    PlayerDamageEventType.Melee,
    PlayerDamageEventType.Projectile,
    PlayerDamageEventType.Missile
  };

      private Events.PlayerDamageCallback _damageCallback = null;

      public override string Name {
        get {
          return "OVERHEAL";
        }
      }

      public override string Author {
        get {
          return "Danila015";
        }
      }

      public Overheal(IPlayer player) : base(player) {
        Time = 40000; // 40 s
      }

      protected override void Activate() { }

      public override void Update(float dlt, float dltSecs) {
        if (Time % REGEN_COOLDOWN == 0) {
          float health = Player.GetHealth();

          if (health < Player.GetMaxHealth()) {
            PointShape.Random(Draw, Player.GetAABB(), _rng);

            Game.PlaySound("GetHealthSmall", Vector2.Zero, 0.15f);

            Player.SetHealth(health + REGEN);
          } else {
            // Apply overheal
            PlayerModifiers mod = Player.GetModifiers();

            mod.MaxHealth += OVERHEAL;
            mod.CurrentHealth += OVERHEAL;

            Player.SetModifiers(mod);

            // Effect
            Game.PlaySound("Syringe", Vector2.Zero, 0.25f);
            PointShape.Random(DrawOverheal, Player.GetAABB(), _rng);
          }
        }
      }

      public override void TimeOut() {
        Game.PlaySound("PlayerLeave", Player.GetWorldPosition());
      }

      public override void OnEnabled(bool enabled) {
        if (enabled) {
          _damageCallback = Events.PlayerDamageCallback.Start(OnPlayerDamage);
        } else {
          _damageCallback.Stop();
          _damageCallback = null;
        }
      }

      private void OnPlayerDamage(IPlayer player, PlayerDamageArgs args) {
        if (player != Player)
          return;

        if (_allowedTypes.Any(at => args.DamageType == at)) {
          Player.DealDamage(CANCEL_DMG);

          Time = 0; // Player has been damaged by an opponent, stop
        }
      }

      private static void Draw(Vector2 v) {
        Game.PlayEffect(EffectName.BloodTrail, v);
        Game.PlayEffect(EffectName.Smack, v);
      }

      private static void DrawOverheal(Vector2 v) {
        Game.PlayEffect(EffectName.Blood, v);
      }
    }
  }
}