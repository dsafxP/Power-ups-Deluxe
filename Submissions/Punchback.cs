using SFDGameScriptInterface;

namespace PowerupsDeluxe {
  public partial class GameScript : GameScriptInterface {
    // PUNCHBACK - dsafxP
    public class Punchback : Powerup {
      private const string TXT_EFFECT = "BULLETS LEFT: {0}"; // 0 for bullets left
      private const ProjectileItem PROJ_ITEM = ProjectileItem.PISTOL45;
      private const ProjectilePowerup PROJ_POWERUP = ProjectilePowerup.Bouncing;

      private static readonly Vector2 _muzzleOffset = new Vector2(8, 4);

      private Events.PlayerMeleeActionCallback _meleeActionCallback = null;
      private Events.ProjectileHitCallback _projHitCallback = null;

      private ushort _bulletsAbsorbed = 0;

      private Vector2 MuzzleOffset {
        get {
          Vector2 v = _muzzleOffset;

          v.X *= Player.FacingDirection;

          return v;
        }
      }

      private IPlayer ClosestEnemy {
        get {
          List<IPlayer> enemies = Game.GetPlayers()
            .Where(p => (p.GetTeam() != Player.GetTeam() ||
                p.GetTeam() == PlayerTeam.Independent) && !p.IsDead &&
              p != Player)
            .ToList();

          Vector2 playerPos = Player.GetWorldPosition();

          enemies.Sort((p1, p2) => Vector2.Distance(p1.GetWorldPosition(), playerPos)
            .CompareTo(Vector2.Distance(p2.GetWorldPosition(), playerPos)));

          return enemies.FirstOrDefault();
        }
      }

      public override string Name {
        get {
          return "PUNCHBACK";
        }
      }

      public override string Author {
        get {
          return "dsafxP";
        }
      }

      public Punchback(IPlayer player) : base(player) {
        Time = 25000; // 25 s
      }

      protected override void Activate() { }

      public override void TimeOut() {
        Game.PlaySound("DestroyMetal", Vector2.Zero, 1);
        Game.PlayEffect(EffectName.Sparks, Player.GetWorldPosition());
      }

      public override void OnEnabled(bool enabled) {
        if (enabled) {
          _meleeActionCallback = Events.PlayerMeleeActionCallback.Start(OnPlayerMeleeAction);
          _projHitCallback = Events.ProjectileHitCallback.Start(OnProjectileHit);
        } else {
          _meleeActionCallback.Stop();
          _meleeActionCallback = null;

          _projHitCallback.Stop();
          _projHitCallback = null;
        }
      }

      private void OnPlayerMeleeAction(IPlayer player, PlayerMeleeHitArg[] args) {
        if (player != Player)
          return;

        IPlayer closestEnemy = ClosestEnemy;

        if (_bulletsAbsorbed > 0 && closestEnemy != null) {
          _bulletsAbsorbed--;

          Game.PlayEffect(EffectName.CustomFloatText, Player.GetWorldPosition(),
            string.Format(TXT_EFFECT, _bulletsAbsorbed));

          Game.PlaySound("Pistol45", Vector2.Zero);

          Vector2 bulletPos = Player.GetWorldPosition() + MuzzleOffset;

          Game.SpawnProjectile(PROJ_ITEM, bulletPos,
            Vector2Helper.DirectionTo(bulletPos, closestEnemy.GetWorldPosition()), PROJ_POWERUP);
        }
      }

      private void OnProjectileHit(IProjectile projectile, ProjectileHitArgs args) {
        if (args.HitObjectID == Player.UniqueID) {
          _bulletsAbsorbed++;

          Player.SetHealth(Player.GetHealth() + args.Damage); // Heal

          Game.PlayEffect(EffectName.Block, args.HitPosition);

          Game.PlaySound("MeleeDrawMetal", Vector2.Zero);
        }
      }
    }
  }
}