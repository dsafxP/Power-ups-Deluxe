using SFDGameScriptInterface;

namespace PowerupsDeluxe {
  public partial class GameScript : GameScriptInterface {
    // SENTRY - dsafxP
    public class Sentry : Powerup {
      private const float SPAWN_OFFSET = 16;
      private const float ROTATE_SPEED = 4;
      private const float RANGE = 200;
      private const float BULLET_SPEED = 14;
      private const float DMG = 4;
      private const float COOLDOWN = 50;

      private static readonly RayCastInput _raycastInput = new RayCastInput(true) {
        IncludeOverlap = false,
        FilterOnMaskBits = true,
        MaskBits = ushort.MaxValue,
        ProjectileHit = RayCastFilterMode.True,
        AbsorbProjectile = RayCastFilterMode.True
      };

      private static readonly CollisionFilter _barrelCollision = new CollisionFilter() {
        CategoryBits = 0,
        MaskBits = 0,
        AboveBits = 0
      };

      private float _elapsed = 0;

      private IObject[] _sentryObjs;
      private IObjectRevoluteJoint _revoluteJoint;

      private Vector2 SpawnOffset {
        get {
          Vector2 v = Player.GetWorldPosition();

          v.X += SPAWN_OFFSET * Player.FacingDirection;

          return v;
        }
      }

      public bool CanFire {
        get {
          return _elapsed <= 0;
        }
      }

      public override string Name {
        get {
          return "SENTRY";
        }
      }

      public override string Author {
        get {
          return "dsafxP";
        }
      }

      public Sentry(IPlayer player) : base(player) {
        Time = 40000; // 40 s
      }

      protected override void Activate() {
        Vector2 spawnOffset = SpawnOffset;

        Game.PlayEffect(EffectName.PlayerLandFull, spawnOffset);

        _sentryObjs = SpawnTurret(spawnOffset);
        _revoluteJoint = (IObjectRevoluteJoint) _sentryObjs
          .First(o => o.Name == "RevoluteJoint");

        _revoluteJoint.SetMotorEnabled(true);
        _revoluteJoint.SetMotorSpeed(ROTATE_SPEED);
      }

      public override void Update(float dlt, float dltSecs) {
        if (_sentryObjs.Any(s => s == null || s.IsRemoved)) {
          Enabled = false;

          return;
        }

        _elapsed = Math.Max(_elapsed - dlt, 0);

        if (CanFire) {
          Vector2 revoluteJointPos = _revoluteJoint.GetWorldPosition();

          Vector2 end = revoluteJointPos + (Vector2Helper.FromAngle(_revoluteJoint
            .GetTargetObjectB()
            .GetAngle() - MathHelper.PIOver2) * RANGE);

          //Game.DrawLine(revoluteJointPos, end);

          RayCastResult rayCastResult = Game.RayCast(revoluteJointPos, end, _raycastInput)[0];

          if (rayCastResult.IsPlayer) {
            IPlayer hit = (IPlayer) rayCastResult.HitObject;

            if ((hit.GetTeam() != Player.GetTeam() ||
              hit.GetTeam() == PlayerTeam.Independent) && !hit.IsDead && hit != Player) {
              _elapsed = COOLDOWN;

              Shoot(rayCastResult.Position);

              _revoluteJoint.SetMotorSpeed(0);
            } else
              _revoluteJoint.SetMotorSpeed(ROTATE_SPEED);
          } else
            _revoluteJoint.SetMotorSpeed(ROTATE_SPEED);
        }
      }

      public override void OnEnabled(bool enabled) {
        if (!enabled) {
          /*Vector2 explosionPos = _sentryObjs
            .First(s => s != null && !s.IsRemoved)
            .GetWorldPosition();

          Game.TriggerExplosion(explosionPos);*/

          foreach (IObject obj in _sentryObjs)
            obj.Destroy();
        }
      }

      private void Shoot(Vector2 target) {
        Game.PlaySound("SMG", Vector2.Zero, 0.65f);

        Vector2 revoluteJointPos = _revoluteJoint.GetWorldPosition();

        new CustomProjectile(revoluteJointPos,
        Vector2Helper.DirectionTo(revoluteJointPos, target), _raycastInput) {
          Effect = EffectName.ItemGleam,
          Speed = BULLET_SPEED,
          Piercing = true,
          MaxDistanceTravelled = RANGE,
          OnPlayerHit = _OnPlayerHit,
          OnObjectHit = _OnObjectHit
        };
      }

      private void _OnPlayerHit(IPlayer hit, Vector2 pos) {
        if (hit == Player)
          return;

        hit.DealDamage(DMG);

        Game.PlayEffect(EffectName.BulletHit, pos);
        Game.PlaySound("BulletHitFlesh", Vector2.Zero);
      }

      private void _OnObjectHit(IObject hit, Vector2 pos) {
        if (_sentryObjs.Any(o => o == hit))
          return;

        hit.DealDamage(DMG);

        Game.PlayEffect(EffectName.BulletHitDefault, pos);
        Game.PlaySound("BulletHitDefault", Vector2.Zero);
      }

      private static IObject[] SpawnTurret(Vector2 pos) {
        const float MASS_MULT = 0.02f;

        IObjectAlterCollisionTile alt1 = (IObjectAlterCollisionTile) Game.CreateObject("AlterCollisionTile");

        alt1.SetDisableCollisionTargetObjects(true);

        IObjectWeldJoint weld1 = (IObjectWeldJoint) Game.CreateObject("WeldJoint", pos);

        IObject metal = Game.CreateObject("Metal02E", pos);

        metal.SetBodyType(BodyType.Dynamic);

        metal.SetMass(metal.GetMass() * MASS_MULT);

        weld1.AddTargetObject(metal);

        alt1.AddTargetObject(metal);

        IObject metal2 = Game.CreateObject("Metal03A", pos);

        metal2.SetBodyType(BodyType.Dynamic);

        metal2.SetMass(metal2.GetMass() * MASS_MULT);

        weld1.AddTargetObject(metal2);

        alt1.AddTargetObject(metal2);

        IObject pulley = Game.CreateObject("Pulley00", pos);

        pulley.SetMass(pulley.GetMass() * MASS_MULT);

        weld1.AddTargetObject(pulley);

        alt1.AddTargetObject(pulley);

        IObjectWeldJoint weld2 = (IObjectWeldJoint) Game.CreateObject("WeldJoint", pos);

        IObjectRevoluteJoint revoluteJoint = (IObjectRevoluteJoint) Game.CreateObject("RevoluteJoint", pos);

        IObjectAlterCollisionTile alt2 = (IObjectAlterCollisionTile) Game.CreateObject("AlterCollisionTile");

        alt2.SetDisablePlayerMelee(true);
        alt2.SetDisableProjectileHit(true);

        IObject rail = Game.CreateObject("MetalRailing00", pos + new Vector2(8, 0), MathHelper.PIOver2);

        rail.SetBodyType(BodyType.Dynamic);

        rail.SetMass(rail.GetMass() * MASS_MULT);

        rail.SetCollisionFilter(_barrelCollision);

        weld2.AddTargetObject(rail);

        revoluteJoint.SetTargetObjectA(pulley);
        revoluteJoint.SetTargetObjectB(rail);

        alt1.AddTargetObject(rail);
        alt2.AddTargetObject(rail);

        IObject hatch = Game.CreateObject("MetalHatch00A", pos + new Vector2(18, 0));

        hatch.SetBodyType(BodyType.Dynamic);

        hatch.SetMass(hatch.GetMass() * MASS_MULT);

        hatch.SetCollisionFilter(_barrelCollision);

        weld2.AddTargetObject(hatch);

        alt1.AddTargetObject(hatch);
        alt2.AddTargetObject(hatch);

        return new IObject[] {
          alt1,
          weld1,
          metal,
          metal2,
          pulley,
          weld2,
          revoluteJoint,
          alt2,
          rail,
          hatch
        };
      }
    }
  }
}