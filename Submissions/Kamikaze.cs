using SFDGameScriptInterface;

namespace PowerupsDeluxe {
  public partial class GameScript : GameScriptInterface {
    // KAMIKAZE - Danila015
    public class Kamikaze : Powerup {
      private const float EFFECT_COOLDOWN = 500;
      private const string TXT_EFFECT = "EXPLODING IN {0}..."; // 0 for current tick

      private static readonly Vector2[] _explosionsOffset = { // +
    Vector2.Zero,
    new Vector2(40, 0),
    new Vector2(-40, 0),
    new Vector2(0, 40),
    new Vector2(0, -40)
  };

      private static readonly PlayerModifiers _explodeMod = new PlayerModifiers() {
        ExplosionDamageTakenModifier = 0.125f
      };

      private PlayerModifiers _modifiers;

      public override string Name {
        get {
          return "KAMIKAZE";
        }
      }

      public override string Author {
        get {
          return "Danila015";
        }
      }

      public Kamikaze(IPlayer player) : base(player) {
        Time = 7000; // 7 s
      }

      protected override void Activate() {
        _modifiers = Player.GetModifiers(); // Store original player modifiers

        _modifiers.CurrentHealth = -1;
        _modifiers.CurrentEnergy = -1;
      }

      public override void Update(float dlt, float dltSecs) {
        if (Time % EFFECT_COOLDOWN == 0) {
          Game.PlayEffect(EffectName.CustomFloatText, Player.GetWorldPosition(),
            string.Format(TXT_EFFECT, (int)(Time / 1000)));

          Game.PlaySound("TimerTick", Vector2.Zero);
        }
      }

      public override void TimeOut() {
        Player.SetModifiers(_explodeMod);

        Events.UpdateCallback.Start(Restart, 50, 1);

        Vector2 playerPos = Player.GetWorldPosition();

        foreach (Vector2 offset in _explosionsOffset) {
          Vector2 pos = playerPos + offset;

          Game.TriggerExplosion(pos);
        }
      }

      private void Restart(float dlt) {
        Player.SetModifiers(_modifiers);
        Player.SetLinearVelocity(Vector2.Zero);
      }
    }
  }
}