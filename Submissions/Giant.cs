using SFDGameScriptInterface;

namespace PowerupsDeluxe {
  public partial class GameScript : GameScriptInterface {
    // GIANT - Tomo
    public class Giant : Powerup {
      private const float TIME = 17000;
      private const float EFFECT_COOLDOWN = 100;
      private const float SHAKE_COOLDOWN = 1500;
      private const float SHAKE_INTENSITY = 3;
      private const float SHAKE_EFFECT_DAMP = 2; // Shake / Value
      private const float SHAKE_AREA_SIZE = 100;
      private const float OBJ_DMG_MULT = 5;

      private static readonly PlayerCommand _playerCommand = new PlayerCommand(PlayerCommandType.Fall);

      private static readonly PlayerModifiers _giantMod = new PlayerModifiers() {
        ImpactDamageTakenModifier = 0.44f,
        ExplosionDamageTakenModifier = 0.66f,
        ProjectileDamageTakenModifier = 0.55f,
        ProjectileCritChanceTakenModifier = 0,
        MeleeStunImmunity = 1,
        MeleeDamageTakenModifier = 0.66f,
        SprintSpeedModifier = 0.88f,
        SizeModifier = 2
      };

      private Events.PlayerMeleeActionCallback _meleeActionCallback = null;

      private PlayerModifiers _modifiers; // Stores original player modifiers

      private float _elapsed = 0;

      private float Shake {
        get {
          return SHAKE_INTENSITY * (Player.IsRunning ? 1.5f :
            (Player.IsSprinting ? 2 :
              (Player.IsLayingOnGround || Player.IsRecoveryKneeling ? 3 : 1)));
        }
      }

      private Area ShakeArea {
        get {
          Area playerArea = Player.GetAABB();

          playerArea.SetDimensions(SHAKE_AREA_SIZE, SHAKE_AREA_SIZE);

          return playerArea;
        }
      }

      private IObject[] ObjectsShaking {
        get {
          return Game.GetObjectsByArea(ShakeArea)
            .Where(o => !(o is IPlayer) && o.GetBodyType() == BodyType.Dynamic &&
              o.Destructable)
            .ToArray();
        }
      }

      private IPlayer[] PlayersShaking {
        get {
          return Game.GetObjectsByArea<IPlayer>(ShakeArea)
            .Where(p => (p.GetTeam() == PlayerTeam.Independent || p.GetTeam() != Player.GetTeam()) &&
              !p.IsDisabled && p != Player)
            .ToArray();
        }
      }

      public bool CanShake {
        get {
          return _elapsed <= 0;
        }
      }

      public override string Name {
        get {
          return "GIANT";
        }
      }

      public override string Author {
        get {
          return "Tomo";
        }
      }

      public Giant(IPlayer player) : base(player) {
        Time = TIME;
      }

      protected override void Activate() {
        Player.SetStrengthBoostTime(TIME);

        _modifiers = Player.GetModifiers(); // Store original player modifiers

        _modifiers.CurrentHealth = -1;
        _modifiers.CurrentEnergy = -1;

        Player.SetModifiers(_giantMod);
      }

      public override void Update(float dlt, float dltSecs) {
        _elapsed = Math.Max(_elapsed - dlt, 0);

        if (Player.IsBurningInferno)
          Player.ClearFire();

        if (Player.IsBoostHealthActive)
          Player.Kill();

        float shake = Shake;

        if (shake != SHAKE_INTENSITY) {
          if (Time % EFFECT_COOLDOWN == 0)
            Game.PlayEffect(EffectName.CameraShaker, Vector2.Zero, shake / SHAKE_EFFECT_DAMP,
              EFFECT_COOLDOWN * 2, true);

          if (CanShake) {
            _elapsed = SHAKE_COOLDOWN;

            Game.PlaySound("MuffledExplosion", Vector2.Zero);

            Vector2 shakeVect = new Vector2(0, shake);

            foreach (IPlayer shaking in PlayersShaking) {
              shaking.SetInputEnabled(false);
              shaking.AddCommand(_playerCommand);

              Events.UpdateCallback.Start((float _dlt) => {
                shaking.SetInputEnabled(true);
              }, 1, 1);

              shaking.SetWorldPosition(shaking.GetWorldPosition() + (Vector2Helper.Up * 2)); // Sticky feet

              shaking.SetLinearVelocity(Player.GetLinearVelocity() + shakeVect);

              shaking.SetAngularVelocity(shake);

              shaking.Disarm(shaking.CurrentWeaponDrawn);
            }

            foreach (IObject shaking in ObjectsShaking) {
              shaking.SetLinearVelocity(shaking.GetLinearVelocity() + shakeVect);

              shaking.SetAngularVelocity(shake);

              shaking.TrackAsMissile(true);

              Game.PlayEffect(EffectName.DestroyDefault, shaking.GetWorldPosition());
            }
          }
        }
      }

      public override void OnEnabled(bool enabled) {
        if (enabled) {
          _meleeActionCallback = Events.PlayerMeleeActionCallback.Start(OnPlayerMeleeAction);
        } else {
          _meleeActionCallback.Stop();

          _meleeActionCallback = null;

          // Restore original player modifiers
          Player.SetModifiers(_modifiers);
        }
      }

      private void OnPlayerMeleeAction(IPlayer player, PlayerMeleeHitArg[] args) {
        if (player != Player)
          return;

        foreach (PlayerMeleeHitArg arg in args
          .Where(a => !a.IsPlayer)) {
          IObject hit = arg.HitObject;
          float dmg = arg.HitDamage;

          hit.SetHealth(hit.GetHealth() + dmg); // Null dmg so it isn't dealt twice

          hit.DealDamage(dmg * OBJ_DMG_MULT);
        }
      }
    }
  }
}
