using SFDGameScriptInterface;

namespace PowerupsDeluxe {
  public partial class GameScript : GameScriptInterface {
    // MAGNETIC FIELD - Tomo
    public class Magnet : Powerup {
      private const float EFFECT_COOLDOWN = 100;
      private const float EFFECT_SEPARATION = 10;
      private const float MAGNET_AREA_SIZE = 250;
      private const float MAGNET_FORCE = 2;
      private const float MAGNET_ANGULAR_SPEED = 1.5f;

      private const float MAGNET_RADIUS = MAGNET_AREA_SIZE / 2;

      private static readonly Type[] _objTypes = {
    typeof (IObjectSupplyCrate),
    typeof (IObjectStreetsweeperCrate),
    typeof (IObjectWeaponItem)
  };

      private static readonly string[] _explosiveNames = {
    "WpnGrenadesThrown",
    "WpnMolotovsThrown",
    "WpnC4Thrown",
    "WpnMineThrown",
    "WpnShurikenThrown"
  };

      private Area MagnetArea {
        get {
          Area playerArea = Player.GetAABB();

          playerArea.SetDimensions(MAGNET_AREA_SIZE, MAGNET_AREA_SIZE);

          return playerArea;
        }
      }

      private IObject[] ObjectsInMagnet {
        get {
          return Game.GetObjectsByArea(MagnetArea)
            .Where(o => _objTypes.Any(t => t.IsAssignableFrom(o.GetType())))
            .ToArray();
        }
      }

      private IObject[] ExplosivesInMagnet {
        get {
          return Game.GetObjectsByArea(MagnetArea)
            .Where(o => _explosiveNames.Any(e => o.Name == e))
            .ToArray();
        }
      }

      private IObjectStreetsweeper[] TargetStreetsweepersInMagnet {
        get {
          return Game.GetObjectsByArea<IObjectStreetsweeper>(MagnetArea)
            .Where(s => s.GetAttackTarget() == Player)
            .ToArray();
        }
      }

      public override string Name {
        get {
          return "MAGNETIC FIELD";
        }
      }

      public override string Author {
        get {
          return "Tomo";
        }
      }

      public Magnet(IPlayer player) : base(player) {
        Time = 31000; // 31 s
      }

      protected override void Activate() { }

      public override void Update(float dlt, float dltSecs) {
        bool effect = Time % EFFECT_COOLDOWN == 0;

        foreach (IObject affected in ObjectsInMagnet) {
          affected.SetLinearVelocity(Vector2Helper.DirectionTo(affected.GetWorldPosition(),
            Player.GetWorldPosition()) * MAGNET_FORCE);

          affected.SetAngularVelocity(MAGNET_ANGULAR_SPEED);

          if (effect)
            Game.PlayEffect(EffectName.ItemGleam, affected.GetWorldPosition());
        }

        foreach (IObject explosives in ExplosivesInMagnet) {
          explosives.SetLinearVelocity(Vector2Helper.DirectionTo(Player.GetWorldPosition(),
            explosives.GetWorldPosition()) * MAGNET_FORCE);

          explosives.SetAngularVelocity(MAGNET_ANGULAR_SPEED);

          if (effect)
            Game.PlayEffect(EffectName.ItemGleam, explosives.GetWorldPosition());
        }

        foreach (IObjectStreetsweeper sweepers in TargetStreetsweepersInMagnet) {
          Game.PlayEffect(EffectName.Electric, sweepers.GetWorldPosition());
          Game.PlaySound("ElectricSparks", Vector2.Zero);

          sweepers.Destroy();
        }

        if (effect) {
          PointShape.Circle(Draw, Player.GetWorldPosition(),
            MAGNET_RADIUS, EFFECT_SEPARATION);
        }
      }

      public override void TimeOut() {
        // Play sound effect indicating expiration of powerup
        Game.PlaySound("StrengthBoostStop", Vector2.Zero);
      }

      private static void Draw(Vector2 v) {
        Game.PlayEffect(EffectName.Electric, v);
      }
    }
  }
}