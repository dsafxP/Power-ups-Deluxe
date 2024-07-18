using SFDGameScriptInterface;

namespace PowerupsDeluxe {
  public partial class GameScript : GameScriptInterface {
    // DRONE - dsafxP
    public class Drone : Powerup {
      private const float SPEED = 4;

      private Events.ObjectDamageCallback _objDmgCallback;

      private Vector2 InputDirection {
        get {
          Vector2 vel = Vector2.Zero;

          if (Player.IsBot) {
            IPlayer closestEnemy = ClosestEnemy;

            if (closestEnemy != null) {
              vel = Vector2Helper.DirectionTo(Player.GetWorldPosition(),
              closestEnemy.GetWorldPosition());
            }
          } else {
            vel.X += Player.KeyPressed(VirtualKey.AIM_RUN_RIGHT) ? 1 : 0;
            vel.X -= Player.KeyPressed(VirtualKey.AIM_RUN_LEFT) ? 1 : 0;

            vel.Y += Player.KeyPressed(VirtualKey.AIM_CLIMB_UP) ||
            Player.KeyPressed(VirtualKey.JUMP) ? 1 : 0;
            vel.Y -= Player.KeyPressed(VirtualKey.AIM_CLIMB_DOWN) ? 1 : 0;
          }

          return vel;
        }
      }

      private IPlayer ClosestEnemy {
        get {
          List<IPlayer> enemies = Game.GetPlayers()
            .Where(p => (p.GetTeam() != Player.GetTeam() ||
              p.GetTeam() == PlayerTeam.Independent) && !p.IsDead &&
              p != Player)
            .ToList();

          Vector2 playerPos = Streetsweeper.GetWorldPosition();

          enemies.Sort((p1, p2) => Vector2.Distance(p1.GetWorldPosition(), playerPos)
            .CompareTo(Vector2.Distance(p2.GetWorldPosition(), playerPos)));

          return enemies.FirstOrDefault();
        }
      }

      public IObjectStreetsweeper Streetsweeper {
        get;
        private set;
      }

      public override string Name {
        get {
          return "DRONE";
        }
      }

      public override string Author {
        get {
          return "dsafxP";
        }
      }

      public Vector2 Velocity {
        get {
          return InputDirection * SPEED;
        }
      }

      public Drone(IPlayer player) : base(player) {
        Time = 25000; // 25 s
      }

      protected override void Activate() {
        Streetsweeper = (IObjectStreetsweeper) Game.CreateObject("Streetsweeper", Player.GetWorldPosition());

        Streetsweeper.SetOwnerPlayer(Player);
        Streetsweeper.SetMovementType(StreetsweeperMovementType.Stationary);
        Streetsweeper.SetWeaponType(StreetsweeperWeaponType.None);
      }

      public override void Update(float dlt, float dltSecs) {
        if (Streetsweeper == null || Streetsweeper.IsRemoved) {
          Enabled = false;

          return;
        }

        Streetsweeper.SetLinearVelocity(Velocity);

        IPlayer enemy = ClosestEnemy;
        float angle = enemy != null ? Vector2Helper.AngleToPoint(Streetsweeper.GetWorldPosition(),
          enemy.GetWorldPosition()) - MathHelper.PIOver2 : 0;

        Streetsweeper.SetAngle(angle);
      }

      public override void OnEnabled(bool enabled) {
        if (enabled) {
          _objDmgCallback = Events.ObjectDamageCallback.Start(OnObjectDamage);
        } else {
          _objDmgCallback.Stop();

          _objDmgCallback = null;

          Streetsweeper.Destroy();
        }
      }

      private void OnObjectDamage(IObject obj, ObjectDamageArgs args) {
        if (obj != Streetsweeper)
          return;

        Streetsweeper.SetHealth(Streetsweeper.GetMaxHealth()); // Immortality
      }
    }
  }
}
