using SFDGameScriptInterface;

namespace PowerupsDeluxe {
  public partial class GameScript : GameScriptInterface {
    // SUPER DOVE - Luminous
    public class SuperDove : Powerup {
      private const float SPEED = 5;
      private const float EGG_COOLDOWN = 400;
      private const float EGG_DMG_MULT = 13;

      private static readonly Vector2 _playerPosition = new Vector2(0, 5000);
      private static readonly Vector2 _blockPosition = new Vector2(0, 4984);
      private static readonly PlayerCommand _playerCommand =
          new PlayerCommand(PlayerCommandType.Fall);

      private Events.PlayerDamageCallback _plyDamageCallback;
      private Events.ObjectDamageCallback _objDamageCallback;

      private IPlayer ClosestEnemy {
        get {
          List<IPlayer> enemies =
              Game.GetPlayers()
                  .Where(p => (p.GetTeam() != Player.GetTeam() ||
                              p.GetTeam() == PlayerTeam.Independent) &&
                             !p.IsDead && p != Player)
                  .ToList();

          Vector2 playerPos = Dove.GetWorldPosition();

          enemies.Sort((p1, p2) =>
                           Vector2.Distance(p1.GetWorldPosition(), playerPos)
                               .CompareTo(Vector2.Distance(p2.GetWorldPosition(),
                                                           playerPos)));

          return enemies.FirstOrDefault();
        }
      }

      private Vector2 InputDirection {
        get {
          Vector2 vel = Vector2.Zero;

          if (Player.IsBot) {
            IPlayer closestEnemy = ClosestEnemy;

            if (closestEnemy != null) {
              vel = Vector2Helper.DirectionTo(Dove.GetWorldPosition(),
                                              closestEnemy.GetWorldPosition()) +
                    Vector2Helper.Up;
            }
          } else {
            vel.X += Player.KeyPressed(VirtualKey.AIM_RUN_RIGHT) ? 1 : 0;
            vel.X -= Player.KeyPressed(VirtualKey.AIM_RUN_LEFT) ? 1 : 0;

            vel.Y += Player.KeyPressed(VirtualKey.AIM_CLIMB_UP) ||
                             Player.KeyPressed(VirtualKey.JUMP)
                         ? 1
                         : 0;
            vel.Y -= Player.KeyPressed(VirtualKey.AIM_CLIMB_DOWN) ? 1 : 0;
          }

          return vel;
        }
      }

      private readonly List<IObject> _eggs = new List<IObject>();
      private IDialogue _dialog;

      public IObject Dove {
        get;
        private set;
      }

      public IObject[] Eggs {
        get {
          _eggs.RemoveAll(item => item == null || item.IsRemoved);

          return _eggs.ToArray();
        }
      }

      public Vector2 Velocity {
        get {
          return InputDirection * SPEED;
        }
      }

      public override string Name {
        get {
          return "SUPER DOVE";
        }
      }

      public override string Author {
        get {
          return "Luminous";
        }
      }

      public SuperDove(IPlayer player) : base(player) { Time = 15000; }

      public override void Update(float dlt, float dltSecs) {
        if (Dove == null || Dove.IsRemoved) {
          Enabled = false;

          return;
        }

        // Egg
        if (Time % EGG_COOLDOWN == 0)
          CreateEgg();

        // Apply movement
        Vector2 inputDirection = InputDirection;
        Vector2 vel = inputDirection * SPEED;

        Dove.SetLinearVelocity(vel);
        Dove.SetFaceDirection((int) inputDirection.X);
      }

      protected override void Activate() {
        Game.PlaySound("Wings", Vector2.Zero);  // Effect

        Dove = Game.CreateObject("Dove00",
                                 Player.GetWorldPosition());  // Create dove

        PlayerTeam playerTeam = Player.GetTeam();

        Dove.SetTargetAIData(
            new ObjectAITargetData(500, playerTeam));  // Targetable by bots

        // Hide player
        Game.CreateObject("InvisibleBlockSmall", _blockPosition);
        Player.SetWorldPosition(_playerPosition);

        Player.SetNametagVisible(false);
        Player.SetInputMode(PlayerInputMode.ReadOnly);
        Player.SetCameraSecondaryFocusMode(CameraFocusMode.Ignore);

        // Create tag
        string name = Player.Name;

        if (name.Length > 10) {
          name = name.Substring(0, 10);
          name += "...";
        }

        _dialog = Game.CreateDialogue(name, GetTeamColor(playerTeam), Dove, "",
                                      ushort.MaxValue, false);

        // Callbacks
        _plyDamageCallback = Events.PlayerDamageCallback.Start(OnPlayerDamage);
        _objDamageCallback = Events.ObjectDamageCallback.Start(OnObjectDamage);
      }

      public override void OnEnabled(bool enabled) {
        if (!enabled) {
          Game.PlaySound("StrengthBoostStop", Vector2.Zero);
          Game.PlaySound("Wings", Vector2.Zero);

          // Close dialogs
          if (_dialog != null)
            _dialog.Close();

          // Stop callbacks
          _plyDamageCallback.Stop();

          _plyDamageCallback = null;

          _objDamageCallback.Stop();

          _objDamageCallback = null;

          // Remove dove
          Dove.Destroy();

          // Recover player
          Player.SetWorldPosition(Dove.GetWorldPosition());
          Player.SetLinearVelocity(Vector2.Zero);  // Full stop
          Player.SetInputEnabled(true);
          Player.SetNametagVisible(true);
          Player.SetCameraSecondaryFocusMode(CameraFocusMode.Focus);

          // Clean
          foreach (IObject egg in Eggs)
            egg.Destroy();
        }
      }

      private IObject CreateEgg(bool missile = true) {
        Vector2 dovePos = Dove.GetWorldPosition();

        Game.PlayEffect(EffectName.BulletHitCloth, dovePos);
        Game.PlaySound("Baseball", Vector2.Zero);

        Vector2 vel = Velocity;

        IObject egg =
            Game.CreateObject("CrumpledPaper00", dovePos, 0, vel, vel.Length());

        egg.TrackAsMissile(missile);

        _eggs.Add(egg);

        return egg;
      }

      private void OnPlayerDamage(IPlayer player, PlayerDamageArgs args) {
        if (args.DamageType == PlayerDamageEventType.Missile) {
          IObject attacker = Game.GetObject(args.SourceID);

          if (Eggs.Contains(attacker)) {
            player.DealDamage(args.Damage * EGG_DMG_MULT);

            player.SetInputEnabled(false);
            player.AddCommand(_playerCommand);

            Events.UpdateCallback.Start(
                (float _dlt) => { player.SetInputEnabled(true); }, 1, 1);

            Game.PlayEffect(EffectName.CustomFloatText, attacker.GetWorldPosition(),
                            "*BAM*");

            attacker.Destroy();
          }
        }
      }

      private void OnObjectDamage(IObject obj, ObjectDamageArgs args) {
        if (obj == Dove) {
          Dove.SetHealth(Dove.GetMaxHealth());
          Player.DealDamage(args.Damage);
        }
      }

      private static Color GetTeamColor(PlayerTeam team) {
        switch (team) {
          case PlayerTeam.Team1:
            return Color.Blue;
          case PlayerTeam.Team2:
            return Color.Red;
          case PlayerTeam.Team3:
            return Color.Green;
          case PlayerTeam.Team4:
            return Color.Yellow;
          default:
            return Color.White;
        }
      }
    }
  }
}
