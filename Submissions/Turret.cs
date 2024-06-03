using SFDGameScriptInterface;

namespace PowerupsDeluxe {
  public partial class GameScript : GameScriptInterface {
    // BLOOD TURRET - dsafxP
    public class Turret : Powerup {
      private const bool PIERCING = true;
      private const float SPEED = 11;
      private const float DMG = 11;

      private static readonly RayCastInput _raycastInput = new RayCastInput(true) {
        FilterOnMaskBits = true,
        AbsorbProjectile = RayCastFilterMode.True,
        MaskBits = ushort.MaxValue
      };

      private static readonly Vector2 _offset = new Vector2(0, 24);

      private Wisp _wisp;

      public override string Name {
        get {
          return "BLOOD TURRET";
        }
      }

      public override string Author {
        get {
          return "dsafxP";
        }
      }

      public Turret(IPlayer player) : base(player) {
        Time = 17000;
      }

      protected override void Activate() {
        _wisp = new Wisp(Player) {
          Offset = _offset,
          Effect = EffectName.Blood,
          Cooldown = 750,
          OnShoot = Shoot
        };

        Game.PlaySound("Flamethrower", Vector2.Zero);
      }

      private void Shoot(Vector2 target) {
        Game.PlaySound("ImpactFlesh", Vector2.Zero);
        Game.PlaySound("ImpactFlesh", Vector2.Zero);
        Game.PlaySound("Heartbeat", Vector2.Zero);

        new CustomProjectile(_wisp.Position,
        Vector2Helper.DirectionTo(_wisp.Position, target), _raycastInput) {
          Effect = EffectName.BloodTrail,
          Speed = SPEED,
          Piercing = PIERCING,
          OnPlayerHit = _OnPlayerHit,
          OnObjectHit = _OnObjectHit
        };
      }

      private void _OnPlayerHit(IPlayer hit, Vector2 pos) {
        if (hit == Player)
          return;

        hit.DealDamage(DMG * hit.GetModifiers()
        .ProjectileDamageTakenModifier);

        Game.PlayEffect(EffectName.Blood, pos);
        Game.PlaySound("ImpactFlesh", Vector2.Zero);
      }

      private static void _OnObjectHit(IObject hit, Vector2 pos) {
        hit.DealDamage(DMG);

        Game.PlayEffect(EffectName.Blood, pos);
        Game.PlaySound("ImpactFlesh", Vector2.Zero);
      }

      public override void TimeOut() {
        // Play sound effect indicating expiration of powerup
        Game.PlaySound("StrengthBoostStop", Vector2.Zero);
        Game.PlaySound("PlayerGib", Vector2.Zero);
        Game.PlayEffect(EffectName.Gib, _wisp.Position);
      }

      public override void OnEnabled(bool enabled) {
        if (_wisp != null)
          _wisp.Enabled = enabled;
      }

      private class Wisp {
        private const uint EFFECT_COOLDOWN = 50;

        private static readonly RayCastInput _raycastInput = new RayCastInput(true) {
          IncludeOverlap = true,
          FilterOnMaskBits = true,
          MaskBits = ushort.MaxValue,
          ProjectileHit = RayCastFilterMode.True,
          AbsorbProjectile = RayCastFilterMode.True
        };

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

        private float _elapsed = 0;
        private Events.UpdateCallback _updateCallback = null;

        public IPlayer Player;
        public Vector2 Offset = Vector2.Zero;
        public string Effect = string.Empty;
        public float Cooldown = 1000;

        public Vector2 Position {
          get {
            return Player.GetWorldPosition() + Offset;
          }
        }

        public bool Enabled {
          get {
            return _updateCallback != null;
          }
          set {
            if (value != Enabled)
              if (value) {
                _updateCallback = Events.UpdateCallback.Start(Update);
              } else {
                _updateCallback.Stop();

                _updateCallback = null;
              }
          }
        }

        public bool CanFire {
          get {
            return _elapsed <= 0;
          }
        }

        public delegate void OnShootCallback(Vector2 target);
        public OnShootCallback OnShoot;

        public Wisp(IPlayer player) {
          Player = player;
          Enabled = true;
        }

        private void Update(float dlt) {
          if (Player == null) {
            Enabled = false;

            return;
          }

          if (Player.IsDead || Player.IsRemoved) {
            Enabled = false;

            return;
          }

          _elapsed = Math.Max(_elapsed - dlt, 0);

          Vector2 position = Position;

          if (_elapsed % EFFECT_COOLDOWN == 0)
            Game.PlayEffect(Effect, position);

          if (OnShoot != null && CanFire) {
            _elapsed = Cooldown;

            IPlayer closestPlayer = ClosestEnemy;

            if (closestPlayer != null) {
              Vector2 closestTarget = closestPlayer.GetWorldPosition();
              RayCastResult rayCastResult = Game.RayCast(position, closestTarget, _raycastInput)[0];

              if (rayCastResult.IsPlayer)
                OnShoot.Invoke(closestTarget);
            }
          }
        }
      }
    }
  }
}