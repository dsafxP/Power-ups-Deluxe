using SFDGameScriptInterface;

namespace PowerupsDeluxe {
  public partial class GameScript : GameScriptInterface {
    // THORNS - dsafxP - Motto73
    public class Thorns : Powerup {
      private const float EFFECT_COOLDOWN = 100;
      private const float DMG_MULT = 2.25f;

      private Events.PlayerMeleeActionCallback _playerMeleeActionCallback;

      public override string Name {
        get {
          return "THORNS";
        }
      }

      public override string Author {
        get {
          return "dsafxP - Motto73";
        }
      }

      public Thorns(IPlayer player) : base(player) {
        Time = 17000; // 17 s
      }

      protected override void Activate() {
        _playerMeleeActionCallback = Events.PlayerMeleeActionCallback.Start(OnPlayerMeleeAction);
      }

      public override void Update(float dlt, float dltSecs) {
        if (Time % EFFECT_COOLDOWN == 0) // Effect
          PointShape.Random(Draw, Player.GetAABB(), _rng);
      }

      public override void TimeOut() {
        Game.PlaySound("StrengthBoostStop", Vector2.Zero);
        Game.PlayEffect(EffectName.Gib, Player.GetWorldPosition());
      }

      public override void OnEnabled(bool enabled) {
        if (!enabled) {
          _playerMeleeActionCallback.Stop();

          _playerMeleeActionCallback = null;
        }
      }

      private void OnPlayerMeleeAction(IPlayer attacker, PlayerMeleeHitArg[] args) {
        foreach (PlayerMeleeHitArg arg in args) {
          if (arg.HitObject != Player)
            continue;

          attacker.DealDamage(arg.HitDamage * DMG_MULT); // Damage attacker

          // Effect
          Game.PlayEffect(EffectName.Blood, arg.HitPosition);
          Game.PlaySound("MeleeHitSharp", Vector2.Zero);
        }
      }

      private static void Draw(Vector2 v) {
        Game.PlayEffect(EffectName.BloodTrail, v);
      }
    }
  }
}