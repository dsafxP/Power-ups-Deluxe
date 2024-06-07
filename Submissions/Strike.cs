using SFDGameScriptInterface;

namespace PowerupsDeluxe {
  public partial class GameScript : GameScriptInterface {
    public class Strike : Powerup {
      private const float EFFECT_COOLDOWN = 50;
      private const float EFFECT_SEPARATION = 5;
      private const float STAND_DMG_MULT = 0.33f;
      private const float STAND_FORCE = 2;

      private static readonly PlayerCommand _playerCommand = new PlayerCommand(PlayerCommandType.Fall);

      private static readonly Vector2[] _offsets = {
        new Vector2(-10, 0),
        new Vector2(10, 0),
        new Vector2(0, 20),
      };

      private Events.PlayerMeleeActionCallback _meleeActionCallback = null;

      private Vector2[] EffectPositions {
        get {
          return _offsets
            .Select(o => o += Player.GetWorldPosition())
            .ToArray();
        }
      }

      public override string Name {
        get {
          return "TRI-STRIKE";
        }
      }

      public override string Author {
        get {
          return "dsafxP - Eiga";
        }
      }

      public Strike(IPlayer player) : base(player) {
        Time = 17000;
      }

      protected override void Activate() { }

      public override void Update(float dlt, float dltSecs) {
        if (Time % EFFECT_COOLDOWN == 0) {
          PointShape.Polygon(Draw, EffectPositions, EFFECT_SEPARATION);
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
        }
      }

      private void OnPlayerMeleeAction(IPlayer player, PlayerMeleeHitArg[] args) {
        if (player != Player)
          return;

        foreach (PlayerMeleeHitArg arg in args) {
          if (!arg.IsPlayer)
            continue;

          IPlayer hit = (IPlayer)arg.HitObject;

          hit.SetInputEnabled(false);
          hit.AddCommand(_playerCommand);

          Events.UpdateCallback.Start((float _dlt) => {
            hit.SetInputEnabled(true);
          }, 1, 1);

          if (!hit.IsBlocking) {
            hit.DealDamage(arg.HitDamage * STAND_DMG_MULT);

            hit.SetWorldPosition(hit.GetWorldPosition() + (Vector2Helper.Up * 2)); // Sticky feet

            hit.SetLinearVelocity(hit.GetLinearVelocity() +
              new Vector2(STAND_FORCE * -hit.FacingDirection, STAND_FORCE));

            Game.PlaySound("PlayerDive", Vector2.Zero);

            PointShape.Polygon(Draw2, EffectPositions, EFFECT_SEPARATION);
          }
        }
      }

      private static void Draw(Vector2 pos) {
        Game.PlayEffect(EffectName.ItemGleam, pos);
      }

      private static void Draw2(Vector2 pos) {
        Game.PlayEffect(EffectName.Smack, pos);
      }
    }
  }
}