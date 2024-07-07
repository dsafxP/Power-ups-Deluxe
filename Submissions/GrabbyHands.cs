using SFDGameScriptInterface;

namespace PowerupsDeluxe {
  public partial class GameScript : GameScriptInterface {
    // GRABBY HANDS - dsafxP - Danila015
    public class GrabbyHands : Powerup {
      private const float EFFECT_DISTANCE = 5;
      private const float GRAB_DISTANCE = 80;

      private WeaponItemType[] EmptyWeaponItemTypes {
        get {
          List<WeaponItemType> weaponItemTypes = new List<WeaponItemType>{
              WeaponItemType.Melee, WeaponItemType.Rifle, WeaponItemType.Handgun,
              WeaponItemType.Powerup, WeaponItemType.Thrown};

          weaponItemTypes.Remove(Player.CurrentMeleeWeapon.WeaponItemType);
          weaponItemTypes.Remove(Player.CurrentPrimaryWeapon.WeaponItemType);
          weaponItemTypes.Remove(Player.CurrentSecondaryWeapon.WeaponItemType);
          weaponItemTypes.Remove(Player.CurrentPowerupItem.WeaponItemType);
          weaponItemTypes.Remove(Player.CurrentThrownItem.WeaponItemType);

          return weaponItemTypes.ToArray();
        }
      }

      private IPlayer ClosestEnemy {
        get {
          List<IPlayer> enemies =
              Game.GetPlayers()
                  .Where(p => (p.GetTeam() != Player.GetTeam() ||
                              p.GetTeam() == PlayerTeam.Independent) &&
                             !p.IsDead)
                  .ToList();

          Vector2 playerPos = Player.GetWorldPosition();

          enemies.Sort((p1, p2) =>
                           Vector2.Distance(p1.GetWorldPosition(), playerPos)
                               .CompareTo(Vector2.Distance(p2.GetWorldPosition(),
                                                           playerPos)));

          return enemies.FirstOrDefault();
        }
      }

      private IPlayer[] ActiveEnemies {
        get {
          return Game.GetPlayers()
              .Where(p => (p.GetTeam() != Player.GetTeam() ||
                          p.GetTeam() == PlayerTeam.Independent) &&
                         !p.IsDead && p.IsInputEnabled)
              .ToArray();
        }
      }

      public override string Name {
        get {
          return "GRABBY HANDS";
        }
      }

      public override string Author {
        get {
          return "dsafxP - Danila015";
        }
      }

      public GrabbyHands(IPlayer player) : base(player) {
        Time = 14000;  // 14 s
      }

      protected override void Activate() {
      }

      public override void Update(float dlt, float dltSecs) {
        WeaponItemType[] emptyWeaponItemTypes = EmptyWeaponItemTypes;
        IPlayer[] enemies = ActiveEnemies;

        if (emptyWeaponItemTypes.Any() && enemies.Any())
          foreach (IPlayer enemy in enemies) {
            WeaponItemType toGrab = emptyWeaponItemTypes.FirstOrDefault(
                s => enemy.CurrentWeaponDrawn == s);

            if (toGrab != null) {
              Vector2 enemyPos = enemy.GetWorldPosition();
              Vector2 playerPos = Player.GetWorldPosition();

              if (Vector2.Distance(enemyPos, playerPos) < GRAB_DISTANCE) {
                IObjectWeaponItem weapon = enemy.Disarm(
                    toGrab, Vector2Helper.DirectionTo(playerPos, enemyPos), true);

                if (weapon != null) {
                  weapon.SetWorldPosition(playerPos);

                  Game.PlayEffect(EffectName.PlayerLandFull, playerPos);
                  Game.PlayEffect(EffectName.PlayerLandFull, enemyPos);
                  PointShape.Trail(Draw, playerPos, enemyPos, EFFECT_DISTANCE);

                  Game.PlaySound("PlayerGrabCatch", Vector2.Zero);
                }
              }
            }
          }
      }

      public override void TimeOut() {
        // Play effects indicating expiration of powerup
        Game.PlaySound("StrengthBoostStop", Vector2.Zero);
      }

      private static void Draw(Vector2 v) {
        Game.PlayEffect(EffectName.ItemGleam, v);
      }
    }
  }
}
