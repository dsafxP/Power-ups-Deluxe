using SFDGameScriptInterface;

namespace PowerupsDeluxe {
  public partial class GameScript : GameScriptInterface {
    public GameScript() : base(null) { }

    private static readonly Random _rng = new Random();

    public void OnStartup() {
      Powerups.Enabled = true;

      Events.UserMessageCallback.Start(HandleCommand);

      Config.Update();
    }

    public void HandleCommand(UserMessageCallbackArgs args) {
      IUser user = args.User;

      if (!args.IsCommand)
        return;

      switch (args.Command) {
        case "PD_HELP": {
            int uid = user.UserIdentifier;

            Game.ShowChatMessage("Available commands:",
            Color.Green, uid);
            Game.ShowChatMessage("PD_HELP - Shows command help.",
            Color.Green, uid);
            Game.ShowChatMessage("PD_POWERUPS - Displays all the power-ups with their codenames.",
            Color.Green, uid);
            Game.ShowChatMessage("PD_CRATE_CHANCE [chance] - Sets or gets the spawn chance of a power-up crate.",
            Color.Green, uid);
            Game.ShowChatMessage("PD_SYRINGE [player] - Gives a player a power-up syringe.",
            Color.Green, uid);
            Game.ShowChatMessage("PD_POWERUP <powerup> [player] - Gives a player a power-up.",
            Color.Green, uid);
            Game.ShowChatMessage("Required options are shown with <>, optional parameters are shown with [].",
            Color.Yellow, uid);
          }
          break;

        case "PD_POWERUPS": {
            int uid = user.UserIdentifier;

            Game.ShowChatMessage("Available power-ups:",
            Color.Green, uid);

            foreach (string powerUpName in typeof(Powerups.AvailablePowerups)
            .GetNestedTypes()
            .Select(t => t.Name)
            .OrderBy(n => n))
              Game.ShowChatMessage(powerUpName,
              Color.Green, uid);
          }
          break;

        case "PD_CRATE_CHANCE": {
            if (!user.IsModerator && !user.IsHost) {
              Game.ShowChatMessage("You don't have enough perms to execute this command.",
              Color.Red, user.UserIdentifier);

              break;
            }

            string arg = args.CommandArguments.Trim();

            if (string.IsNullOrEmpty(arg)) {
              Game.ShowChatMessage(string.Format("Special crate chance is set to {0}.", Config.SpecialCrateChance),
              Color.Green, user.UserIdentifier);

              break;
            }

            float crateChance;

            if (float.TryParse(arg, out crateChance)) {
              Config.SpecialCrateChance = crateChance;

              Game.ShowChatMessage(string.Format("Set special crate chance to {0}.", Config.SpecialCrateChance),
              Color.Green, user.UserIdentifier);
            } else {
              Game.ShowChatMessage("Specify a valid number.",
              Color.Red, user.UserIdentifier);
            }
          }
          break;

        case "PD_SYRINGE": {
            if (!user.IsModerator && !user.IsHost) {
              Game.ShowChatMessage("You don't have enough perms to execute this command.",
              Color.Red, user.UserIdentifier);

              break;
            }

            IUser target = GetUser(args.CommandArguments.Trim());
            IPlayer targetPlayer = target != null ? target.GetPlayer() : user.GetPlayer();

            if (targetPlayer != null) {
              OnPowerupSyringe(new TriggerArgs(null, targetPlayer, false));
            } else {
              Game.ShowChatMessage("Invalid player.",
              Color.Red, user.UserIdentifier);
            }
          }
          break;

        case "PD_POWERUP": {
            if (!user.IsModerator && !user.IsHost) {
              Game.ShowChatMessage("You don't have enough perms to execute this command.",
              Color.Red, user.UserIdentifier);

              break;
            }

            string[] arg = args.CommandArguments.Split(' ');

            Type powerUpType = GetPowerup(arg[0]);

            if (powerUpType != null) {
              IUser target = GetUser(arg.ElementAtOrDefault(1));
              IPlayer targetPlayer = target != null ? target.GetPlayer() : user.GetPlayer();

              if (targetPlayer != null) {
                Powerup powerUp = (Powerup)Activator.CreateInstance(powerUpType, targetPlayer);

                Game.ShowChatMessage(string.Format("{0} - {1}", powerUp.Name, powerUp.Author),
                Color.Yellow, targetPlayer.UserIdentifier);

                PlayPowerupEffect(targetPlayer.GetWorldPosition());
              } else {
                Game.ShowChatMessage("Invalid player.",
                Color.Red, user.UserIdentifier);
              }
            } else {
              Game.ShowChatMessage("Invalid power-up.",
              Color.Red, user.UserIdentifier);

              break;
            }
          }
          break;
      }
    }

    public static class Config {
      private const string SPECIAL_CRATE_KEY = "SpecialCrateChance";

      public static float SpecialCrateChance {
        get {
          return Powerups.SpawnChance;
        }
        set {
          float val = MathHelper.Clamp(value, 0, 100);
          Powerups.SpawnChance = val;

          Game.LocalStorage.SetItem(SPECIAL_CRATE_KEY, val);
        }
      }

      public static void Update() {
        float specialCrateChance;

        if (Game.LocalStorage.TryGetItemFloat(SPECIAL_CRATE_KEY, out specialCrateChance)) {
          Powerups.SpawnChance = specialCrateChance;
        }
      }
    }

    public IUser GetUser(string arg) {
      return string.IsNullOrEmpty(arg) ? null :
      Game.GetActiveUsers()
      .FirstOrDefault(u => u.AccountName == arg || u.Name == arg ||
      (arg.All(char.IsDigit) ? u.GameSlotIndex == int.Parse(arg) : false));
    }

    public Type GetPowerup(string arg) {
      string nest = "SFDScript.GameScript+Powerups+AvailablePowerups+" + arg;
      System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();

      return assembly.GetTypes()
      .FirstOrDefault(t => t.FullName.Equals(nest, StringComparison.OrdinalIgnoreCase));
    }

    private static void PlayPowerupEffect(Vector2 pos) {
      Game.PlaySound("LogoSlam", pos, 1);
      Game.PlaySound("MuffledExplosion", pos, 1);

      Game.PlayEffect(EffectName.Explosion, pos);

      Game.PlayEffect(EffectName.CameraShaker, pos, 1f, 1000f, true);
    }

    public static void OnPowerupSyringe(TriggerArgs args) {
      const WeaponItem POWERUP_WEAPONITEM = WeaponItem.STRENGTHBOOST;

      IObjectActivateTrigger caller = args.Caller as IObjectActivateTrigger;
      IPlayer sender = args.Sender as IPlayer;

      if (sender == null) { // Get
        sender = Game.GetObjectsByArea<IPlayer>(caller.GetAABB())
        .FirstOrDefault(p => !p.IsDead && p.IsInputEnabled && p.IsBot);
      }

      Vector2 offset = new Vector2(0, 26);

      if (sender.CurrentPowerupItem.WeaponItem == POWERUP_WEAPONITEM) {
        Game.PlayEffect(EffectName.CustomFloatText, sender.GetWorldPosition() + offset, "CAN'T PICKUP");

        return;
      }

      // Remove syringe
      if (caller != null) {
        caller.GetHighlightObject()
        .Remove();
      }

      sender.GiveWeaponItem(POWERUP_WEAPONITEM);

      Game.PlayEffect(EffectName.CustomFloatText, sender.GetWorldPosition() + offset, "POWER-UP BOOST");

      Events.PlayerWeaponRemovedActionCallback weaponRemovedActionCallback = null;

      weaponRemovedActionCallback = Events.PlayerWeaponRemovedActionCallback.Start(
        (IPlayer player, PlayerWeaponRemovedArg arg) => {
          if (player == sender && arg.WeaponItem == POWERUP_WEAPONITEM) {
            IObject item = Game.GetObject(arg.TargetObjectID);

            if (item != null) { // Powerup item dropped
              IObject syringe = Powerups.CreatePowerupSyringe(sender.GetWorldPosition());

              //Game.WriteToConsole("Created syringe", syringe.UniqueID);

              // Set syringe
              syringe.SetAngle(item.GetAngle());
              syringe.SetLinearVelocity(item.GetLinearVelocity());
              syringe.SetAngularVelocity(item.GetAngularVelocity());

              item.Remove(); // Remove original item

              weaponRemovedActionCallback.Stop();

              weaponRemovedActionCallback = null;
            } else { // Powerup item used
              sender.SetStrengthBoostTime(0);
              sender.SetSpeedBoostTime(0);

              Type powerUpType = Powerups.GetRandomPowerupType();

              Powerup powerUp = (Powerup)Activator.CreateInstance(powerUpType, sender); // Activate random powerup

              Game.ShowChatMessage(string.Format("{0} - {1}", powerUp.Name, powerUp.Author), Color.Yellow, sender.UserIdentifier);

              PlayPowerupEffect(sender.GetWorldPosition());

              Game.PlayEffect(EffectName.CustomFloatText, sender.GetWorldPosition() + offset, powerUp.Name);

              weaponRemovedActionCallback.Stop();

              weaponRemovedActionCallback = null;
            }
          }
        });
    }

    public static class Powerups {
      private static readonly ObjectAITargetData _boxTargetData = new ObjectAITargetData(500, ObjectAITargetMode.MeleeOnly);

      private static Events.ObjectCreatedCallback _objectCreatedCallback = null;

      public static bool Enabled {
        get {
          return _objectCreatedCallback != null;
        }
        set {
          if (value != Enabled)
            if (value)
              _objectCreatedCallback = Events.ObjectCreatedCallback.Start(OnObjectCreated);
            else {
              _objectCreatedCallback.Stop();

              _objectCreatedCallback = null;
            }
        }
      }

      public static float SpawnChance = 33;

      public static SupplyBox CreatePowerupBox(Vector2 pos) {
        // Create box
        IObject box = Game.CreateObject("CardboardBox00", pos);

        // Create helmet
        Vector2 helmOffset = new Vector2(2, -0.5f);

        IObject helm = Game.CreateObject("Helmet00", pos + helmOffset);

        // Create weld joint
        IObjectWeldJoint weldJoint = (IObjectWeldJoint)Game.CreateObject("WeldJoint", pos);

        // Set weld joint targets
        weldJoint.AddTargetObject(box);
        weldJoint.AddTargetObject(helm);

        // Create destroy targets
        IObjectDestroyTargets destroyTargets = (IObjectDestroyTargets)Game.CreateObject("DestroyTargets", pos);

        // Set destroy targets
        destroyTargets.AddTriggerDestroyObject(box);

        destroyTargets.AddObjectToDestroy(helm);
        destroyTargets.AddObjectToDestroy(weldJoint);

        // Bot support
        box.SetTargetAIData(_boxTargetData);

        // Instance
        SupplyBox supply = new SupplyBox(box) {
          Effect = "ImpactDefault",
          EffectCooldown = 300,
          SlowFallMultiplier = 0.77f,
          Destroyed = OnPowerupBoxDestroyed
        };

        return supply;
      }

      public static IObject CreatePowerupSyringe(Vector2 pos) {
        // Create syringe
        IObject syringe = Game.CreateObject("ItemStrengthBoostEmpty", pos);

        // Create ActivateTrigger
        IObjectActivateTrigger activateTrigger = (IObjectActivateTrigger)Game.CreateObject("ActivateTrigger", pos);

        // Set ActivateTrigger
        activateTrigger.SetBodyType(BodyType.Dynamic);
        activateTrigger.SetHighlightObject(syringe);
        activateTrigger.SetScriptMethod("OnPowerupSyringe");

        // Create weld joint
        IObjectWeldJoint weldJoint = (IObjectWeldJoint)Game.CreateObject("WeldJoint", pos);

        // Set weld joint targets
        weldJoint.AddTargetObject(syringe);
        weldJoint.AddTargetObject(activateTrigger);

        // Create destroy targets
        IObjectDestroyTargets destroyTargets = (IObjectDestroyTargets)Game.CreateObject("DestroyTargets", pos);

        // Set destroy targets
        destroyTargets.AddTriggerDestroyObject(syringe);

        destroyTargets.AddObjectToDestroy(activateTrigger);
        destroyTargets.AddObjectToDestroy(weldJoint);

        // Bot support
        syringe.SetTargetAIData(_boxTargetData);

        // Make pickupable for bots
        new ActivateTriggerBot(activateTrigger);

        return syringe;
      }

      public static Type GetRandomPowerupType(Random random = null) {
        if (random == null)
          random = _rng;

        Type[] nestedPowerups = typeof(AvailablePowerups).GetNestedTypes();

        Type[] instantiableTypes = nestedPowerups.Where(t =>
          //t.BaseType == typeof(Powerup) &&
          t.GetConstructors().Any(c =>
            c.GetParameters().Length == 1 &&
            c.GetParameters()[0].ParameterType == typeof(IPlayer)
          )
        ).ToArray();

        if (instantiableTypes.Length == 0)
          throw new InvalidOperationException("No instantiable types found.");

        Type randomType = instantiableTypes[random.Next(instantiableTypes.Length)];

        return randomType;
      }

      private static void OnPowerupBoxDestroyed(IObject destroyed) {
        CreatePowerupSyringe(destroyed.GetWorldPosition());

        Game.PlaySound("DestroyWood", Vector2.Zero);
      }

      private static void OnObjectCreated(IObject[] objs) {
        // Get random supply crates
        foreach (IObject supplyCrate in objs
          .Where(o => o.Name == "SupplyCrate00" && _rng.Next(101) < SpawnChance)) {
          CreatePowerupBox(supplyCrate.GetWorldPosition());

          supplyCrate.Remove();
        }
      }

      public class SupplyBox {
        private const float RAYCAST_COOLDOWN = 300;
        private const float ANGULAR = 0;

        private static readonly RayCastInput _collision = new RayCastInput(true) {
          ProjectileHit = RayCastFilterMode.True,
          BlockFire = RayCastFilterMode.True,
          FilterOnMaskBits = true,
          MaskBits = ushort.MaxValue
        };
        private static readonly Vector2 _rayCastOffset = new Vector2(0, -48);

        private Events.UpdateCallback _updateCallback = null;
        private Events.ObjectTerminatedCallback _objTerminatedCallback = null;

        private float _elapsed = 0;
        private bool _slowFall = true;

        public IObject Box;

        public string Effect = string.Empty;
        public float EffectCooldown = 1000;
        public float SlowFallMultiplier = 1;

        public bool Enabled {
          get {
            return _updateCallback != null && _objTerminatedCallback != null;
          }
          set {
            if (value != Enabled)
              if (value) {
                _updateCallback = Events.UpdateCallback.Start(Update);
                _objTerminatedCallback = Events.ObjectTerminatedCallback.Start(OnObjectTerminated);
              } else {
                _updateCallback.Stop();

                _updateCallback = null;

                _objTerminatedCallback.Stop();

                _objTerminatedCallback = null;
              }
          }
        }

        public delegate void DestroyedCallback(IObject destroyed);
        public DestroyedCallback Destroyed;

        public SupplyBox(IObject box) {
          Box = box;
          Enabled = true;
        }

        private void Update(float dlt) {
          if (Box == null) {
            Enabled = false;

            return;
          }

          if (Box.IsRemoved) {
            Enabled = false;

            return;
          }

          _elapsed += dlt;

          Vector2 vel = Box.GetLinearVelocity();

          if (_slowFall) {
            if (vel.Y < 0) {
              vel.Y *= SlowFallMultiplier;

              Box.SetLinearVelocity(vel);

              Box.SetAngle(ANGULAR);
              Box.SetAngularVelocity(ANGULAR);

              Box.SetHealth(Box.GetMaxHealth());

              if (_elapsed % EffectCooldown == 0)
                Game.PlayEffect(Effect, Box.GetWorldPosition());
            }

            if (_elapsed % RAYCAST_COOLDOWN == 0) {
              Vector2 rayCastStart = Box.GetWorldPosition();
              Vector2 rayCastEnd = rayCastStart + _rayCastOffset;

              Game.DrawLine(rayCastStart, rayCastEnd, Color.Yellow);

              RayCastResult result = Game.RayCast(rayCastStart, rayCastEnd, _collision)[0];

              _slowFall = !result.Hit;

              if (!_slowFall)
                Box.SetLinearVelocity(Vector2.Zero);
            }
          }
        }

        private void OnObjectTerminated(IObject[] objs) {
          if (objs.Any(o => o == Box)) {
            Enabled = false;

            if (Destroyed != null)
              Destroyed.Invoke(Box);
          }
        }
      }

      public class ActivateTriggerBot {
        private const uint UPDATE_DELAY = 50;

        private readonly List<IPlayer> _activators = new List<IPlayer>();
        private Events.UpdateCallback _updateCallback = null;

        private IPlayer Activator {
          get {
            return Game.GetObjectsByArea<IPlayer>(Trigger.GetAABB())
              .FirstOrDefault(p => p.IsBot && !p.IsDead && p.IsInputEnabled &&
                !_activators.Contains(p) && (Trigger.GetUseType() == ActivateTriggerUseType.Individual || !_activators.Any()));
          }
        }

        public IObjectActivateTrigger Trigger;

        public bool Enabled {
          get {
            return _updateCallback != null;
          }
          set {
            if (value != Enabled)
              if (value)
                _updateCallback = Events.UpdateCallback.Start(Update, UPDATE_DELAY);
              else {
                _updateCallback.Stop();

                _updateCallback = null;
              }
          }
        }

        public ActivateTriggerBot(IObjectActivateTrigger trigger) {
          Trigger = trigger;
          Enabled = true;
        }

        private void Update(float delta) {
          if (Trigger == null) {
            Enabled = false;

            return;
          }

          if (!Trigger.IsEnabled)
            return;

          IPlayer activator = Activator;

          if (activator != null) {
            Trigger.Trigger();

            // List handling
            _activators.Add(activator);

            Events.UpdateCallback.Start(activatorRemovalElapsed => {
              _activators.Remove(activator);
            }, (uint)Trigger.GetCooldown(), 1);
          }
        }
      }

      public static class AvailablePowerups {
        // FLAME - dsafxP
        public class Flame : Powerup {
          private static readonly PlayerModifiers _fireMod = new PlayerModifiers() {
            FireDamageTakenModifier = 0
          };

          private BotBehaviorSet _set = null;

          private PlayerModifiers _modifiers; // Stores original player modifiers

          public override string Name {
            get {
              return "FLAME";
            }
          }

          public override string Author {
            get {
              return "dsafxP";
            }
          }

          public Flame(IPlayer player) : base(player) {
            Time = 20000; // Set duration of powerup (20 seconds)
          }

          public override void Update(float dlt, float dltSecs) {
            Player.SetMaxFire(); // Ensure player has maximum fire level while powerup is active
          }

          protected override void Activate() {
            // Play visual effect at player's position indicating start of powerup
            Game.PlayEffect("PLRB", Player.GetWorldPosition());

            _modifiers = Player.GetModifiers(); // Store original player modifiers

            _modifiers.CurrentHealth = -1;
            _modifiers.CurrentEnergy = -1;

            Player.SetModifiers(_fireMod);

            if (Player.IsBot) {
              BotBehaviorSet botSet = Player.GetBotBehaviorSet();

              _set = botSet;

              botSet.DefensiveRollFireLevel = 0;

              Player.SetBotBehaviorSet(botSet);
            }
          }

          public override void TimeOut() {
            // Play effects indicating expiration of powerup
            Game.PlaySound("StrengthBoostStop", Vector2.Zero);
            Game.PlayEffect("PLRB", Player.GetWorldPosition());
          }

          public override void OnEnabled(bool enabled) {
            if (!enabled) {
              Player.ClearFire(); // Clear any fire effects on the player

              // Restore original player modifiers
              Player.SetModifiers(_modifiers);

              // Restore behavior set
              if (Player.IsBot && _set != null)
                Player.SetBotBehaviorSet(_set);
            }
          }
        }

        // ADRENALINE - dsafxP
        public class Adrenaline : Powerup {
          private const uint EFFECT_COOLDOWN = 50; // Cooldown between each effect
          private const float SPEED_MULT = 0.75f; // Moving while punching speed multiplier
          private const float BOUNCE_SPEED = 9;

          private static readonly Vector2 _jumpAttackSpeed = new Vector2(0, 2);

          private static readonly VirtualKey[] _inputKeys = { // Keys that will trigger movement
        VirtualKey.AIM_RUN_LEFT,
        VirtualKey.AIM_RUN_RIGHT
      };

          private bool PlayerValid {
            get {
              return Player.IsOnGround &&
                !Player.IsDiving &&
                !Player.IsManualAiming &&
                !Player.IsDisabled;
            }
          }

          public override string Name {
            get {
              return "ADRENALINE";
            }
          }

          public override string Author {
            get {
              return "dsafxP";
            }
          }

          public Adrenaline(IPlayer player) : base(player) {
            Time = 18000; // Set duration of powerup (18 seconds)
          }

          public override void Update(float dlt, float dltSecs) {
            // Moving while attacking
            if ((_inputKeys.Any(k => Player.KeyPressed(k)) || Player.IsBot) &&
              (Player.IsMeleeAttacking || Player.IsKicking)) {
              // Calculate offset
              Vector2 offset = new Vector2((SPEED_MULT * Player.GetModifiers().RunSpeedModifier) *
                Player.FacingDirection, 0);

              // Apply offset
              Player.SetWorldPosition(Player.GetWorldPosition() + offset);
            }

            // Bounce
            if (Player.KeyPressed(VirtualKey.JUMP) && PlayerValid) {
              Vector2 vel = Player.GetLinearVelocity();

              vel.Y = BOUNCE_SPEED;

              Player.SetLinearVelocity(vel);
            }

            // Jump attack spam
            if ((Player.IsJumpAttacking || Player.IsJumpKicking) &&
              Player.GetLinearVelocity().Y > _jumpAttackSpeed.Y)
              Player.SetLinearVelocity(_jumpAttackSpeed);

            // Play effect
            if (Time % EFFECT_COOLDOWN == 0)
              Game.PlayEffect(EffectName.ImpactDefault, Player.GetWorldPosition());
          }

          protected override void Activate() { }

          public override void TimeOut() {
            // Play effects indicating expiration of powerup
            Game.PlaySound("StrengthBoostStop", Vector2.Zero);
            Game.PlayEffect(EffectName.PlayerLandFull, Player.GetWorldPosition());
          }
        }

        // VORTEX - dsafxP
        public class Vortex : Powerup {
          private const uint VORTEX_COOLDOWN = 250;
          private const float VORTEX_AREA_SIZE = 100;
          private const float VORTEX_FORCE = 5;

          private static readonly PlayerCommand _playerCommand = new PlayerCommand(PlayerCommandType.Fall);
          private static readonly Type[] _objTypes = {
        typeof(IObjectSupplyCrate),
        typeof(IObjectStreetsweeperCrate),
        typeof(IObjectWeaponItem)
      };

          private Area VortexArea {
            get {
              Area playerArea = Player.GetAABB();

              playerArea.SetDimensions(VORTEX_AREA_SIZE, VORTEX_AREA_SIZE);

              return playerArea;
            }
          }

          private IPlayer[] PlayersInVortex {
            get {
              return Game.GetObjectsByArea<IPlayer>(VortexArea)
              .Where(p => (p.GetTeam() == PlayerTeam.Independent || p.GetTeam() != Player.GetTeam())
              && !p.IsDisabled && p != Player)
              .ToArray();
            }
          }

          private IObject[] ObjectsInVortex {
            get {
              return Game.GetObjectsByArea(VortexArea)
              .Where(o => _objTypes.Any(t => t.IsAssignableFrom(o.GetType())))
              .ToArray();
            }
          }

          public override string Name {
            get {
              return "VORTEX";
            }
          }

          public override string Author {
            get {
              return "dsafxP";
            }
          }

          public Vortex(IPlayer player) : base(player) {
            Time = 17000; // 17 s
          }

          public override void Update(float dlt, float dltSecs) {
            if (Time % 50 == 0) // every 50ms
              Draw(Player.GetWorldPosition());

            if (Time % VORTEX_COOLDOWN == 0) { // every 250ms
              Game.DrawArea(VortexArea, Color.Red);

              foreach (IPlayer pulled in PlayersInVortex) {
                pulled.SetInputEnabled(false);
                pulled.AddCommand(_playerCommand);

                Events.UpdateCallback.Start((float _dlt) => {
                  pulled.SetInputEnabled(true);
                }, 1, 1);

                Vector2 pulledPos = pulled.GetWorldPosition();

                pulled.SetWorldPosition(pulledPos + (Vector2Helper.Up * 2)); // Sticky feet

                pulled.SetLinearVelocity(Vector2Helper.DirectionTo(pulledPos,
                Player.GetWorldPosition()) * VORTEX_FORCE);

                pulled.Disarm(pulled.CurrentWeaponDrawn);

                Game.PlaySound("PlayerDive", Vector2.Zero);
              }

              foreach (IObject pulled in ObjectsInVortex) {
                pulled.SetLinearVelocity(Vector2Helper.DirectionTo(pulled.GetWorldPosition(),
                Player.GetWorldPosition()) * VORTEX_FORCE);

                Game.PlaySound("PlayerDive", Vector2.Zero);
              }
            }
          }

          protected override void Activate() { }

          public override void TimeOut() {
            // Play sound effect indicating expiration of powerup
            Game.PlaySound("StrengthBoostStop", Vector2.Zero);
          }

          // This cool effect was made by Danger Ross!
          private void Draw(Vector2 pos) {
            PointShape.Swirl(
              (v => Game.PlayEffect(EffectName.ItemGleam,
                   Vector2Helper.Rotated(v - pos,
                       (float)(Time % 1500 * (MathHelper.TwoPI / 1500)))
                       + pos)),
              pos, // Center Position
              5, // Initial Radius
              VORTEX_AREA_SIZE / 2, // End Radius
              2, // Rotations
              45 // Point count
            );
          }
        }

        // SPHERE - dsafxP
        public class Sphere : Powerup {
          private const uint EFFECT_COOLDOWN = 50;
          private const float EFFECT_SEPARATION = 45;
          private const float SPHERE_SIZE = 100;

          private const float SPHERE_RADIUS = SPHERE_SIZE / 2;

          private Area SphereArea {
            get {
              Area playerArea = Player.GetAABB();

              playerArea.SetDimensions(SPHERE_SIZE, SPHERE_SIZE);

              return playerArea;
            }
          }

          private IProjectile[] ProjectilesInSphere {
            get {
              return Game.GetProjectiles()
                .Where(pr => SphereArea.Contains(pr.Position) && pr.InitialOwnerPlayerID != Player.UniqueID &&
                  (GetTeamOrDefault(Game.GetPlayer(pr.InitialOwnerPlayerID)) != Player.GetTeam() ||
                    Player.GetTeam() == PlayerTeam.Independent) &&
                  !pr.PowerupBounceActive)
                .ToArray();
            }
          }

          public override string Name {
            get {
              return "SPHERE";
            }
          }

          public override string Author {
            get {
              return "dsafxP";
            }
          }

          public Sphere(IPlayer player) : base(player) {
            Time = 24000; // 24 s
          }

          public override void Update(float dlt, float dltSecs) {
            if (Time % EFFECT_COOLDOWN == 0) {
              Draw(Player.GetWorldPosition());

              Game.DrawArea(SphereArea, Color.Red);
            }

            foreach (IProjectile projs in ProjectilesInSphere) {
              projs.Direction *= -1;
              projs.CritChanceDealtModifier = 100;
              projs.PowerupBounceActive = true;

              Game.PlayEffect(EffectName.Electric, projs.Position);
              Game.PlaySound("ShellBounce", Vector2.Zero, 1);
              Game.PlaySound("ElectricSparks", Vector2.Zero, 1);
            }
          }

          public override void TimeOut() {
            // Play sound effect indicating expiration of powerup
            Game.PlaySound("StrengthBoostStop", Vector2.Zero);
          }

          protected override void Activate() { }

          private void Draw(Vector2 pos) {
            PointShape.Circle(v => {
              Game.PlayEffect(EffectName.ItemGleam, Vector2Helper.Rotated(v - pos,
                  (float)(Time % 1500 * (MathHelper.TwoPI / 1500))) +
                pos);
            }, pos, SPHERE_RADIUS, EFFECT_SEPARATION);
          }

          private PlayerTeam GetTeamOrDefault(IPlayer player,
            PlayerTeam defaultTeam = PlayerTeam.Independent) {
            return player != null ? player.GetTeam() : defaultTeam;
          }
        }

        // ROCKET SHOES - Ebomb09
        public class RocketShoes : Powerup {
          private const uint EFFECT_COOLDOWN = 25;
          private const float IMPULSE = 0.2f;

          private IObject[] _feet;

          private bool PlayerValid {
            get {
              return !Player.IsDisabled &&
                !Player.IsLedgeGrabbing &&
                !Player.IsClimbing &&
                !Player.IsDiving &&
                !Player.IsGrabbing &&
                Player.IsInputEnabled;
            }
          }

          private bool Rocketing {
            get {
              return PlayerValid && Player.KeyPressed(VirtualKey.JUMP);
            }
          }

          private Vector2 Impulse {
            get {
              Vector2 impulse = new Vector2(0, Player.GetLinearVelocity().Y + IMPULSE);

              impulse.X += Player.KeyPressed(VirtualKey.AIM_RUN_RIGHT) ? 1 :
                (Player.KeyPressed(VirtualKey.AIM_RUN_LEFT) ? -1 : 0);

              impulse.X *= Player.KeyPressed(VirtualKey.SPRINT) ? 2 :
                (Player.KeyPressed(VirtualKey.WALKING) ? 0.5f : 1);

              return impulse;
            }
          }

          public override string Name {
            get {
              return "ROCKET SHOES";
            }
          }

          public override string Author {
            get {
              return "Ebomb09";
            }
          }

          public RocketShoes(IPlayer player) : base(player) {
            Time = 20000; // 20 s
          }

          protected override void Activate() {
            _feet = new IObject[] {
          Game.CreateObject("InvisibleBlockNoCollision", Vector2.Zero, 3 / 2 * MathHelper.PI),
            Game.CreateObject("InvisibleBlockNoCollision", Vector2.Zero, 3 / 2 * MathHelper.PI)
        };

            _feet[0].SetBodyType(BodyType.Dynamic);
            _feet[1].SetBodyType(BodyType.Dynamic);
          }

          public override void Update(float dlt, float dltSecs) {
            Vector2 playerPos = Player.GetWorldPosition();

            foreach (IObject obj in _feet) {
              obj.SetLinearVelocity(Player.GetLinearVelocity());

              obj.SetAngle(
                Vector2Helper.AngleToPoint(
                  playerPos,
                  playerPos - new Vector2(Player.GetLinearVelocity().X, Math.Abs(Player.GetLinearVelocity().Y))
                )
              );
            }

            _feet[0].SetWorldPosition(playerPos + new Vector2(-5, -2));
            _feet[1].SetWorldPosition(playerPos + new Vector2(5, -2));

            if (Rocketing) {
              Player.SetLinearVelocity(Impulse);

              if (Time % EFFECT_COOLDOWN == 0) {
                Game.PlayEffect("MZLED", Vector2.Zero, _feet[0].UniqueID, "MuzzleFlashS");
                Game.PlayEffect("MZLED", Vector2.Zero, _feet[1].UniqueID, "MuzzleFlashS");
              }

              if (Time % (EFFECT_COOLDOWN * 2) == 0)
                Game.PlaySound("BarrelExplode", Vector2.Zero, 0.5f);
            }
          }

          public override void TimeOut() {
            Game.PlaySound("DestroyMetal", Vector2.Zero, 1);
            Game.PlayEffect(EffectName.Sparks, Player.GetWorldPosition());
          }

          public override void OnEnabled(bool enabled) {
            if (!enabled) {
              _feet[0].Remove();
              _feet[1].Remove();
            }
          }
        }

        // CLONE-O-MATIC - Ebomb09
        public class Clone : Powerup {
          private readonly float _healthPerMilSec;

          private IPlayer _clonePlayer;
          private float _accumulatedDamage = 0;

          public override string Name {
            get {
              return "CLONE-O-MATIC";
            }
          }

          public override string Author {
            get {
              return "Ebomb09";
            }
          }

          public Clone(IPlayer player) : base(player) {
            Time = 24000;
            _healthPerMilSec = player.GetHealth() / Time;
          }

          /// <summary>
          /// Virtual method for actions upon activating the power-up.
          /// </summary>
          protected override void Activate() {
            // Copy attributes
            _clonePlayer = Game.CreatePlayer(Player.GetWorldPosition());
            _clonePlayer.SetProfile(Player.GetProfile());
            _clonePlayer.SetModifiers(Player.GetModifiers());
            _clonePlayer.SetBotName(Player.Name);
            _clonePlayer.SetTeam(Player.GetTeam());

            // If no team try to find the first available team
            if (_clonePlayer.GetTeam() == PlayerTeam.Independent) {
              List<PlayerTeam> AvailableTeams = new List<PlayerTeam> {
            PlayerTeam.Team1,
            PlayerTeam.Team2,
            PlayerTeam.Team3,
            PlayerTeam.Team4
          };

              foreach (IPlayer player in Game.GetPlayers()) {
                if (!player.IsDead)
                  AvailableTeams.Remove(player.GetTeam());
              }

              if (AvailableTeams.Count > 0) {
                _clonePlayer.SetTeam(AvailableTeams[0]);
                Player.SetTeam(AvailableTeams[0]);
              }
            }

            // Copy weapons over
            _clonePlayer.GiveWeaponItem(Player.CurrentMeleeWeapon.WeaponItem);

            _clonePlayer.SetCurrentMeleeDurability(Player.CurrentMeleeWeapon.Durability);

            _clonePlayer.GiveWeaponItem(Player.CurrentPrimaryRangedWeapon.WeaponItem);

            _clonePlayer.SetCurrentPrimaryWeaponAmmo(Player.CurrentPrimaryRangedWeapon.CurrentAmmo,
              Player.CurrentPrimaryRangedWeapon.SpareMags);

            _clonePlayer.GiveWeaponItem(Player.CurrentSecondaryRangedWeapon.WeaponItem);

            _clonePlayer.SetCurrentPrimaryWeaponAmmo(Player.CurrentSecondaryRangedWeapon.CurrentAmmo,
              Player.CurrentSecondaryRangedWeapon.SpareMags);

            _clonePlayer.GiveWeaponItem(Player.CurrentThrownItem.WeaponItem);

            _clonePlayer.SetCurrentThrownItemAmmo(Player.CurrentThrownItem.CurrentAmmo);

            // Create a hard bot
            _clonePlayer.SetBotBehavior(new BotBehavior(true, PredefinedAIType.CompanionB));
            _clonePlayer.SetGuardTarget(Player);
          }

          public override void Update(float dlt, float dltSecs) {
            // Calculate damage taken
            _accumulatedDamage += dlt * _healthPerMilSec;

            // Wait till accumulation is high enough to avoid red damage indicators
            if (_accumulatedDamage > 5 && _clonePlayer != null) {
              _accumulatedDamage = 0;

              PlayerModifiers mods = _clonePlayer.GetModifiers();

              if (Time * _healthPerMilSec < mods.CurrentHealth)
                mods.CurrentHealth = Time * _healthPerMilSec;

              _clonePlayer.SetModifiers(mods);

              // Kill if game doesn't trigger it
              if (mods.CurrentHealth <= 0)
                _clonePlayer.Kill();

              // Show accelerated aging at half health
              if (mods.CurrentHealth < mods.MaxHealth * 1 / 2) {
                IProfile prof = _clonePlayer.GetProfile();
                prof.Accessory = new IProfileClothingItem("SantaMask", string.Empty);

                _clonePlayer.SetProfile(prof);
              }
            }
          }

          public override void OnEnabled(bool enabled) {
            if (!enabled && _clonePlayer != null)
              _clonePlayer.Kill();
          }
        }

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
            private
            const uint EFFECT_COOLDOWN = 50;

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

        // SUPER DOVE - Luminous
        public class SuperDove : Powerup {
          private const float SPEED = 5;
          private const float EGG_COOLDOWN = 400;
          private const float EGG_DMG_MULT = 23;

          private static readonly Vector2 _playerPosition = new Vector2(0, 5000);
          private static readonly Vector2 _blockPosition = new Vector2(0, 4984);
          private static readonly PlayerCommand _playerCommand = new PlayerCommand(PlayerCommandType.Fall);

          private Events.PlayerDamageCallback _plyDamageCallback;
          private Events.ObjectDamageCallback _objDamageCallback;

          private IPlayer ClosestEnemy {
            get {
              List<IPlayer> enemies = Game.GetPlayers()
                .Where(p => (p.GetTeam() != Player.GetTeam() ||
                    p.GetTeam() == PlayerTeam.Independent) && !p.IsDead &&
                  p != Player)
                .ToList();

              Vector2 playerPos = Dove.GetWorldPosition();

              enemies.Sort((p1, p2) => Vector2.Distance(p1.GetWorldPosition(), playerPos)
                .CompareTo(Vector2.Distance(p2.GetWorldPosition(), playerPos)));

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
                    closestEnemy.GetWorldPosition()) + Vector2Helper.Up;
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

          public SuperDove(IPlayer player) : base(player) {
            Time = 15000;
          }

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
            Dove.SetFaceDirection((int)inputDirection.X);
          }

          protected override void Activate() {
            Game.PlaySound("Wings", Vector2.Zero); // Effect

            Dove = Game.CreateObject("Dove00", Player.GetWorldPosition()); // Create dove

            PlayerTeam playerTeam = Player.GetTeam();

            Dove.SetTargetAIData(new ObjectAITargetData(500, playerTeam)); // Targetable by bots

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

            _dialog = Game.CreateDialogue(name, GetTeamColor(playerTeam), Dove, "", ushort.MaxValue, false);

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
              Player.SetLinearVelocity(Vector2.Zero); // Full stop
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

            IObject egg = Game.CreateObject("CrumpledPaper00", dovePos, 0, vel, vel.Length());

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

                Events.UpdateCallback.Start((float _dlt) => {
                  player.SetInputEnabled(true);
                }, 1, 1);

                Game.PlayEffect(EffectName.CustomFloatText, attacker.GetWorldPosition(), "*BAM*");

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

        // STONE SKIN - Danila015
        public class StoneSkin : Powerup {
          private
          const float HEAVY_EXP = 1.034f;

          private static readonly PlayerModifiers _stoneMod = new PlayerModifiers() {
            ImpactDamageTakenModifier = 0,
            ExplosionDamageTakenModifier = 0.25f,
            ProjectileDamageTakenModifier = 0.25f,
            ProjectileCritChanceTakenModifier = 0,
            MeleeStunImmunity = 1,
            MeleeDamageTakenModifier = 0.25f,
            SprintSpeedModifier = 0.75f
          };

          private IProfile _profile;
          private PlayerModifiers _modifiers;

          private IProfile StoneProfile {
            get {
              IProfile playerProfile = Player.GetProfile();

              playerProfile.Skin = new IProfileClothingItem(string.Format("Normal{0}",
              playerProfile.Gender == Gender.Male ? string.Empty : "_fem"), "Skin5");

              return ColorProfile(playerProfile, "ClothingGray", "ClothingLightGray");
            }
          }

          public override string Name {
            get {
              return "STONE SKIN";
            }
          }

          public override string Author {
            get {
              return "Danila015";
            }
          }

          public StoneSkin(IPlayer player) : base(player) {
            Time = 13000; // 13 s
          }

          protected override void Activate() {
            _profile = Player.GetProfile(); // Store profile

            _modifiers = Player.GetModifiers(); // Store original player modifiers

            _modifiers.CurrentHealth = -1;
            _modifiers.CurrentEnergy = -1;

            Player.SetProfile(StoneProfile);

            Player.SetModifiers(_stoneMod);
          }

          public override void Update(float dlt, float dltSecs) {
            if (Player.IsBurning)
              Player.ClearFire();

            if (!Player.IsOnGround) {
              Vector2 playerLinearVelocity = Player.GetLinearVelocity();

              if (playerLinearVelocity.Y < 0) {
                playerLinearVelocity.Y *= HEAVY_EXP;

                playerLinearVelocity.X /= dlt; // Normalize X

                Player.SetLinearVelocity(playerLinearVelocity);
              }
            }
          }

          public override void TimeOut() {
            Game.PlayEffect(EffectName.DestroyCloth, Player.GetWorldPosition());
            Game.PlaySound("DestroyStone", Vector2.Zero);
          }

          public override void OnEnabled(bool enabled) {
            if (!enabled) { // Restore player
              Player.SetModifiers(_modifiers);
              Player.SetProfile(_profile);
            }
          }

          private static IProfile ColorProfile(IProfile pr, string col, string colI) {
            if (pr.Accesory != null)
              pr.Accesory = new IProfileClothingItem(pr.Accessory.Name, col, colI);

            if (pr.ChestOver != null)
              pr.ChestOver = new IProfileClothingItem(pr.ChestOver.Name, col, colI);

            if (pr.ChestUnder != null)
              pr.ChestUnder = new IProfileClothingItem(pr.ChestUnder.Name, col, colI);

            if (pr.Feet != null)
              pr.Feet = new IProfileClothingItem(pr.Feet.Name, col, colI);

            if (pr.Hands != null)
              pr.Hands = new IProfileClothingItem(pr.Hands.Name, col, colI);

            if (pr.Head != null)
              pr.Head = new IProfileClothingItem(pr.Head.Name, col, colI);

            if (pr.Legs != null)
              pr.Legs = new IProfileClothingItem(pr.Legs.Name, col, colI);

            if (pr.Waist != null)
              pr.Waist = new IProfileClothingItem(pr.Waist.Name, col, colI);

            return pr;
          }
        }

        // FIRE BREATH - Danila015
        public class FireBreath : Powerup {
          private const float EFFECT_COOLDOWN = 175;
          private const float FIRE_RATE = 100;
          private const float TIME_DECREASE = 250;

          private const float RANDOM_SPEED_EXP = 2;
          private const float SPEED = 0.1f;

          private const FireNodeType FIRE_TYPE = FireNodeType.Flamethrower;

          private static readonly Vector2 _effectOffset = new Vector2(0, 8);
          private static readonly Vector2 _fireOffset = new Vector2(8, 0);

          private static readonly Vector2 _rayCastEndOffset = new Vector2(48, 8);
          private static readonly Vector2 _rayCastStartOffset = new Vector2(0, 4);

          private static readonly Type[] _types = {
        typeof (IPlayer)
      };

          private static readonly RayCastInput _rayCastInput = new RayCastInput(true) {
            Types = _types
          };

          private Vector2 RayCastEndOffset {
            get {
              Vector2 v = _rayCastEndOffset;
              v.X *= Player.FacingDirection;

              return v;
            }
          }

          private Vector2 RayCastStartOffset {
            get {
              Vector2 v = _rayCastStartOffset;
              v.X *= Player.FacingDirection;

              return v;
            }
          }

          private RayCastResult RayCast {
            get {
              Vector2 playerPos = Player.GetWorldPosition();

              Vector2 rayCastStart = playerPos + RayCastStartOffset;
              Vector2 rayCastEnd = playerPos + RayCastEndOffset;

              Game.DrawLine(rayCastStart, rayCastEnd, Color.Red);

              return Game.RayCast(rayCastStart, rayCastEnd, _rayCastInput)[0];
            }
          }

          private bool EnemiesInRange {
            get {
              RayCastResult result = RayCast;

              if (result.IsPlayer) {
                IPlayer hit = (IPlayer)result.HitObject;

                return (hit.GetTeam() == PlayerTeam.Independent || hit.GetTeam() != Player.GetTeam()) &&
                  !hit.IsDead;
              }

              return false;
            }
          }

          public override string Name {
            get {
              return "FIRE BREATH";
            }
          }

          public override string Author {
            get {
              return "Danila015";
            }
          }

          public FireBreath(IPlayer player) : base(player) {
            Time = 25000; // 25 s
          }

          protected override void Activate() { }

          public override void Update(float dlt, float dltSecs) {
            if (Player.IsBurning) // Fire resistance
              Player.ClearFire();

            if (Time % EFFECT_COOLDOWN == 0) // Effect
              Game.PlayEffect(EffectName.FireTrail, Player.GetWorldPosition() + _effectOffset);

            if (Time % FIRE_RATE == 0) // Attack
              if (EnemiesInRange) {
                Time -= TIME_DECREASE; // Decrease time

                //Game.WriteToConsole(Time);

                Game.PlaySound("Flamethrower", Vector2.Zero);

                // Calculate offset
                Vector2 fireOffset = _fireOffset;
                fireOffset.X *= Player.FacingDirection;

                Game.SpawnFireNode(Player.GetWorldPosition() + fireOffset,
                  GetRandomFireVelocity(_rng) * SPEED,
                  FIRE_TYPE);
              }
          }

          public override void TimeOut() {
            // Play effects indicating expiration of powerup
            Game.PlaySound("StrengthBoostStop", Vector2.Zero);
            Game.PlayEffect(EffectName.PlayerBurned, Player.GetWorldPosition());
          }

          private Vector2 GetRandomFireVelocity(Random random) {
            float x = (_rayCastEndOffset.X * Player.FacingDirection) * (float)(RANDOM_SPEED_EXP * random.NextDouble());
            float y = _rayCastEndOffset.Y * (float)(RANDOM_SPEED_EXP * random.NextDouble());

            return new Vector2(x, y);
          }
        }

        public class ManaShield : Powerup {
          private const string centerobj = "InvisibleBlockNoCollision";

          private const int MAX_TIME = 25000;
          private const int RADIUS = 25;
          private const int X_OFFSET = 0;
          private const int Y_OFFSET = 10;

          private const byte COLOR_R = 123;
          private const byte COLOR_G = 244;
          private const byte COLOR_B = 244;

          private readonly List<IObject> allItems = new List<IObject>();

          private readonly IObjectText[] effects = new IObjectText[8];

          private readonly Events.CallbackDelegate[] handlers = new Events.CallbackDelegate[2];

          private IObject bird;

          private Vector2 offset;

          private float health = 100;
          private float preservedHealth;

          private bool queueDisable = false;
          private bool delayUpdate = true;

          public override string Name {
            get {
              return "MANA SHIELD";
            }
          }

          public override string Author {
            get {
              return "Danger Ross";
            }
          }

          public ManaShield(IPlayer player) : base(player) {
            Time = MAX_TIME;
          }

          protected override void Activate() {
            offset = new Vector2(X_OFFSET, Y_OFFSET);

            Game.PlaySound("StrengthBoostStart", Player.GetWorldPosition(), 5);

            PlayerModifiers modify = Player.GetModifiers();

            modify.MeleeStunImmunity = 1;

            preservedHealth = modify.CurrentHealth;

            Player.SetModifiers(modify);

            IObjectWeldJoint weld1 = (IObjectWeldJoint)Game.CreateObject("WeldJoint", Player.GetWorldPosition() + offset); //Direct attachment to player by center1
            IObjectWeldJoint weld2 = (IObjectWeldJoint)Game.CreateObject("WeldJoint", Player.GetWorldPosition() + offset); //Rotating attachments around center1, by center2
            IObjectWeldJoint weld3 = (IObjectWeldJoint)Game.CreateObject("WeldJoint", Player.GetWorldPosition() + offset); //attached to player by proxy through center1

            allItems.Add(weld1);
            allItems.Add(weld2);
            allItems.Add(weld3);

            IObject center1 = (IObject)Game.CreateObject(centerobj, Player.GetWorldPosition() + offset); //HINGE FOR ROTATING PART TO ATTACH TO, WELDED ONTO PLAYER
            center1.SetBodyType(BodyType.Dynamic);
            center1.SetMass(0.0001f);
            weld1.AddTargetObject(center1);
            allItems.Add(center1);

            IObjectPullJoint force = (IObjectPullJoint)Game.CreateObject("PullJoint", center1.GetWorldPosition() + new Vector2(0, 200));
            //force.SetLineVisual(LineVisual.DJRope);
            force.SetForcePerDistance(0.01f);
            allItems.Add(force);

            bird = Game.CreateObject(centerobj, center1.GetWorldPosition() + new Vector2(0, 200));
            force.SetTargetObject(bird);
            allItems.Add(bird);

            IObjectTargetObjectJoint target = (IObjectTargetObjectJoint)Game.CreateObject("TargetObjectJoint", center1.GetWorldPosition());
            target.SetTargetObject(center1);
            force.SetTargetObjectJoint(target);
            allItems.Add(target);

            IObject center2 = (IObject)Game.CreateObject(centerobj, Player.GetWorldPosition() + offset);
            center2.SetBodyType(BodyType.Dynamic);
            center2.SetMass(0.001f);
            weld2.AddTargetObject(center2);
            weld2.AddTargetObject(Player);
            allItems.Add(center2);

            IObjectRevoluteJoint revolute = (IObjectRevoluteJoint)Game.CreateObject("RevoluteJoint", Player.GetWorldPosition() + offset);
            revolute.SetTargetObjectA(center2);
            revolute.SetTargetObjectB(center1);
            revolute.SetMotorEnabled(true);
            revolute.SetMotorSpeed(0.7f);
            allItems.Add(revolute);

            //revolute.SetBodyType(BodyType.Dynamic);
            //revolute.SetMass(0.0001f);

            for (int i = 0; i < 4; i++) {
              IObjectText obj = (IObjectText)Game.CreateObject("Text", center1.GetWorldPosition() + Vector2Helper.Rotated(new Vector2(-22, 2), MathHelper.PIOver2 * i));
              obj.SetTextColor(new Color(COLOR_R, COLOR_G, COLOR_B));
              obj.SetTextScale(4);
              obj.SetText("(");
              obj.CustomID = "(";
              obj.SetAngle(MathHelper.PIOver2 * i);
              obj.SetBodyType(BodyType.Dynamic);
              obj.SetMass(0.000001f);
              weld1.AddTargetObject(obj);
              allItems.Add(obj);
              effects[i] = obj;
            }

            for (int i = 0; i < 4; i++) {
              IObjectText obj = (IObjectText)Game.CreateObject("Text", center1.GetWorldPosition() + Vector2Helper.Rotated(new Vector2(-22, 2), MathHelper.PIOver2 * i));
              obj.SetTextColor(Color.White);
              obj.SetTextScale(4);
              obj.SetText("{");
              obj.CustomID = "{";
              obj.SetAngle(MathHelper.PIOver2 * i);
              obj.SetBodyType(BodyType.Dynamic);
              obj.SetMass(0.000001f);
              weld1.AddTargetObject(obj);
              allItems.Add(obj);
              effects[i + 4] = obj;
            }

            CollisionFilter filter = new CollisionFilter {
              ProjectileHit = true,
              AbsorbProjectile = false,
              BlockFire = true
            };

            IObject deflector = Game.CreateObject("InvisibleBlockNoCollision", Player.GetWorldPosition() + new Vector2(-17, 2.3f));

            deflector.CustomID = "deflector";
            deflector.SetBodyType(BodyType.Dynamic);
            deflector.SetCollisionFilter(filter);
            deflector.SetAngle(MathHelper.PIOver4);
            deflector.SetSizeFactor(new Point(4, 4)); //setmass doesnt come into effect if called too early
            deflector.SetMass(0.000001f);
            weld2.AddTargetObject(deflector);
            allItems.Add(deflector);

            IObjectText shine = (IObjectText)Game.CreateObject("Text", new Vector2(-5, -1) + center1.GetWorldPosition());
            shine.SetTextColor(Color.White);
            shine.SetTextScale(3);
            shine.SetText(",");
            shine.SetAngle(MathHelper.PIOver2);
            shine.SetBodyType(BodyType.Dynamic);
            shine.SetMass(0.000001f);
            weld2.AddTargetObject(shine);
            allItems.Add(shine);

            IObjectText crack1 = (IObjectText)Game.CreateObject("Text", center1.GetWorldPosition() + new Vector2(-8.6f, 14.5f));
            crack1.SetTextColor(Color.White);
            crack1.SetTextScale(3);
            crack1.SetText("");
            crack1.SetAngle(5.22f);
            crack1.SetBodyType(BodyType.Dynamic);
            crack1.SetMass(0.000001f);
            weld2.AddTargetObject(crack1);
            allItems.Add(crack1);

            IObjectText crack2 = (IObjectText)Game.CreateObject("Text", center1.GetWorldPosition() + new Vector2(12.8f, 6.9f));
            crack2.SetTextColor(Color.White);
            crack2.SetTextScale(3);
            crack2.SetText("");
            crack2.SetAngle(4.45f);
            crack2.SetBodyType(BodyType.Dynamic);
            crack2.SetMass(0.000001f);
            weld2.AddTargetObject(crack2);
            allItems.Add(crack2);

            IObjectText crack3 = (IObjectText)Game.CreateObject("Text", center1.GetWorldPosition() + new Vector2(6f, -5.3f));
            crack3.SetTextColor(Color.White);
            crack3.SetTextScale(3);
            crack3.SetText("");
            crack3.SetAngle(3.93f);
            crack3.SetBodyType(BodyType.Dynamic);
            crack3.SetMass(0.000001f);
            weld2.AddTargetObject(crack3);
            allItems.Add(crack3);

            //Player.SetCollisionFilter(filter);

            Events.ProjectileHitCallback onHit = null;

            onHit = Events.ProjectileHitCallback.Start((projectile, args) => {
              if (Game.GetObject(args.HitObjectID).CustomID == "deflector") { //remove getobject
                Vector2 normal = Vector2Helper.Rotated(Vector2.Normalize(projectile.Position - center1.GetWorldPosition()), MathHelper.PI);

                double angleDifference = Math.Abs(Vector2Helper.AngleTo(normal, projectile.Velocity)); //Math.Abs(Vector2Helper.Angle(normal) - Vector2Helper.Angle(projectile.Velocity));

                if (angleDifference < MathHelper.PIOver2) {

                  if (projectile.ProjectileItem == ProjectileItem.GRENADE_LAUNCHER || projectile.ProjectileItem == ProjectileItem.BAZOOKA || projectile.ProjectileItem == ProjectileItem.FLAKCANNON) {
                    Game.TriggerExplosion(projectile.Position);
                    projectile.FlagForRemoval();
                    return;
                  }

                  Game.PlaySound("GrenadeBounce", projectile.Position);
                  Game.PlayEffect(EffectName.Sparks, projectile.Position);

                  projectile.Velocity = Vector2Helper.Bounce(projectile.Velocity, normal);
                  projectile.Position += normal * 2;

                  health -= (projectile.GetProperties().ObjectDamage * (float)(angleDifference / MathHelper.PI)) + (projectile.GetProperties().ObjectDamage) / 3;

                  if (health > 50 && health < 75) {
                    crack1.SetText("X");
                    Game.PlaySound("ImpactGlass", crack1.GetWorldPosition(), 5);
                  } else if (health > 25 && health < 50) {
                    crack2.SetText("X");
                    Game.PlaySound("ImpactGlass", crack2.GetWorldPosition(), 5);
                  } else if (health > 0 && health < 25) {
                    crack3.SetText("X");
                    Game.PlaySound("ImpactGlass", crack3.GetWorldPosition(), 5);
                  } else if (health <= 0) {
                    queueDisable = true;
                  }
                  //onHeadshot.Stop();
                  //return;
                }

              }
            });

            handlers[0] = onHit;

            Events.PlayerDamageCallback onDamage = null;

            onDamage = Events.PlayerDamageCallback.Start((IPlayer hitPlayer, PlayerDamageArgs args) => {
              if (args.DamageType == PlayerDamageEventType.Fire) {
                preservedHealth = Player.GetModifiers().CurrentHealth;
                if (preservedHealth > 0) return;
              }

              if (hitPlayer.UniqueID == Player.UniqueID) {
                PlayerModifiers modhp = Player.GetModifiers();
                if (modhp.CurrentHealth == 0)
                  modhp.CurrentHealth = preservedHealth;
                else
                  modhp.CurrentHealth += args.Damage; //THIS DOESNT BLOCK ALL DAMAGE
                Player.SetModifiers(modhp);
                queueDisable = true;
              }
            });
            handlers[1] = onDamage;

            weld2.AddTargetObject(Player);
          }

          public override void Update(float dlt, float dltSecs) {

            if (queueDisable) {
              if (!delayUpdate) {
                Enabled = false;
                return;
              }
              delayUpdate = false;
            }

            if (Time > MAX_TIME - 1200 || Time < 2000) {
              if ((Time < MAX_TIME - 1000 && Time > 2000)) {
                TurnEffect(); //setting to effect at the last sec
              } else {
                ToggleEffect(); //blinking
              }
            }

            if (Time % 50 == 0) {
              if (_rng.Next(0, 6) == 1) {
                Game.PlayEffect(EffectName.ItemGleam, RandomPoint(RADIUS - 6) + Player.GetWorldPosition() + offset);
              }
            }

            bird.SetWorldPosition(Player.GetWorldPosition() + new Vector2(0, 202));
          }

          public override void OnEnabled(bool enabled) {
            PlayerModifiers modify = Player.GetModifiers();
            modify.MeleeStunImmunity = 0;
            Player.SetModifiers(modify);

            if (!enabled) {
              foreach (IObject obj in allItems) {
                obj.Remove();
              }

              for (int i = 0; i < handlers.Length; i++) {
                handlers[i].Stop();
              }

              if (Time > 0) {
                BreakShield();
              }
            }
          }

          public void ToggleEffect() {
            for (int i = 0; i < effects.Length; i++) {
              if (effects[i].GetText() == "") {
                effects[i].SetText(effects[i].CustomID);
              } else {
                effects[i].SetText("");
              }
            }
          }

          public void TurnEffect() {
            for (int i = 0; i < effects.Length; i++) {
              effects[i].SetText(effects[i].CustomID); //setting to effect at the last sec
            }
          }

          private void BreakShield() {
            List<IObject> toFade = new List<IObject>();

            Game.PlaySound("BreakGlass", Player.GetWorldPosition(), 5);
            Game.PlaySound("BreakGlass", Player.GetWorldPosition(), 5);
            Game.PlaySound("BreakGlass", Player.GetWorldPosition(), 5);

            for (int i = 0; i < 50; i++) {
              Vector2 dir = RandomPoint(RADIUS);

              if (_rng.Next(0, 2) == 0) {
                IObject debris = Game.CreateObject("GlassShard00A", Player.GetWorldPosition() + new Vector2(X_OFFSET, Y_OFFSET) + dir);
                debris.SetHealth(1);
                debris.SetLinearVelocity(dir * 0.3f + new Vector2(0, 4));
                debris.SetAngle((float)(_rng.NextDouble() * MathHelper.TwoPI));
                debris.SetAngularVelocity(((float)_rng.NextDouble() - 0.5f) * 20);
                toFade.Add(debris);
              } else {
                Game.PlayEffect(EffectName.DestroyGlass, dir + Player.GetWorldPosition());
              }
            }

            Events.UpdateCallback cleanUp = null;

            cleanUp = Events.UpdateCallback.Start(_dlt => {
              if (toFade.Count() > 0) {
                Game.PlaySound("GlassShard", toFade[toFade.Count() - 1].GetWorldPosition(), 5);
                toFade[toFade.Count() - 1].Remove();
                toFade.RemoveAt(toFade.Count() - 1);
              } else {
                cleanUp.Stop();
              }
            }, 100);
          }

          private Vector2 RandomPoint(float radius) {
            float distance = (float)Math.Pow(_rng.NextDouble(), 0.25) * radius;

            return Vector2Helper.Rotated(new Vector2(distance, 0), (float)(_rng.NextDouble() * MathHelper.TwoPI));
          }
        }

        // AIR DASH - Danila015
        public class AirDash : Powerup {
          private static readonly Vector2 _velocity = new Vector2(17, 5);

          private Vector2 Velocity {
            get {
              Vector2 v = _velocity;
              v.X *= Player.FacingDirection;

              return v;
            }
          }

          public override string Name {
            get {
              return "AIR DASH";
            }
          }

          public override string Author {
            get {
              return "Danila015";
            }
          }

          public bool Dashing {
            get;
            private set;
          }

          public AirDash(IPlayer player) : base(player) {
            Time = 27000; // 27 s
            Dashing = false;
          }

          protected override void Activate() { }

          public override void Update(float dlt, float dltSecs) {
            EmptyUppercutCheck(0);

            if (Dashing) {
              if (!Player.IsMeleeAttacking && Player.IsOnGround) {
                Dashing = false;

                return;
              }
            }
          }

          public override void TimeOut() {
            Game.PlaySound("StrengthBoostStop", Vector2.Zero);
          }

          private void OnEmptyUppercut() {
            if (Dashing)
              return;

            Game.PlaySound("Sawblade", Vector2.Zero);

            Dashing = true;

            Game.PlayEffect(EffectName.TraceSpawner, Vector2.Zero, Player.UniqueID, EffectName.DustTrail, 1.5f);

            Player.SetWorldPosition(Player.GetWorldPosition() + Vector2Helper.Up * 2); // Sticky feet

            Player.SetLinearVelocity(Velocity);
          }

          private void EmptyUppercutCheck(float dlt) {
            float playerXVelocityAbs = Math.Abs(Player.GetLinearVelocity().X);

            bool[] checks = {
          Player.IsMeleeAttacking,
          playerXVelocityAbs >= 0.4f,
          playerXVelocityAbs < 1,
          //Player.CurrentWeaponDrawn == WeaponItemType.NONE
        };

            if (checks.All(c => c)) {
              OnEmptyUppercut();
            }
          }
        }

        // THORNS - dsafxP - Motto73
        public class Thorns : Powerup {
          private const float EFFECT_COOLDOWN = 100;
          private const float DMG_MULT = 2.25f;

          private Events.PlayerMeleeActionCallback _playerMeleeActionCallback;

          public override string Name {
            get {
              return "THORNS";
            }
          }

          public override string Author {
            get {
              return "dsafxP - Motto73";
            }
          }

          public Thorns(IPlayer player) : base(player) {
            Time = 17000; // 17 s
          }

          protected override void Activate() {
            _playerMeleeActionCallback = Events.PlayerMeleeActionCallback.Start(OnPlayerMeleeAction);
          }

          public override void Update(float dlt, float dltSecs) {
            if (Time % EFFECT_COOLDOWN == 0) // Effect
              PointShape.Random(Draw, Player.GetAABB(), _rng);
          }

          public override void TimeOut() {
            Game.PlaySound("StrengthBoostStop", Vector2.Zero);
            Game.PlayEffect(EffectName.Gib, Player.GetWorldPosition());
          }

          public override void OnEnabled(bool enabled) {
            if (!enabled) {
              _playerMeleeActionCallback.Stop();

              _playerMeleeActionCallback = null;
            }
          }

          private void OnPlayerMeleeAction(IPlayer attacker, PlayerMeleeHitArg[] args) {
            foreach (PlayerMeleeHitArg arg in args
              .Where(a => a.HitObject == Player)) {
              attacker.DealDamage(arg.HitDamage * DMG_MULT); // Damage attacker

              // Effect
              Game.PlayEffect(EffectName.Blood, arg.HitPosition);
              Game.PlaySound("MeleeHitSharp", Vector2.Zero);
            }
          }

          private static void Draw(Vector2 v) {
            Game.PlayEffect(EffectName.BloodTrail, v);
          }
        }

        // GRABBY HANDS - dsafxP - Danila015
        public class GrabbyHands : Powerup {
          private const float EFFECT_DISTANCE = 5;

          private WeaponItemType[] EmptyWeaponItemTypes {
            get {
              List<WeaponItemType> weaponItemTypes = new List<WeaponItemType> {
                WeaponItemType.Melee,
                WeaponItemType.Rifle,
                WeaponItemType.Handgun,
                WeaponItemType.Powerup,
                WeaponItemType.Thrown
              };

              weaponItemTypes.Remove(Player.CurrentMeleeWeapon.WeaponItemType);
              weaponItemTypes.Remove(Player.CurrentPrimaryWeapon.WeaponItemType);
              weaponItemTypes.Remove(Player.CurrentSecondaryWeapon.WeaponItemType);
              weaponItemTypes.Remove(Player.CurrentPowerupItem.WeaponItemType);
              weaponItemTypes.Remove(Player.CurrentThrownItem.WeaponItemType);

              return weaponItemTypes.ToArray();
            }
          }

          private IPlayer[] ActiveEnemies {
            get {
              return Game.GetPlayers()
                .Where(p => (p.GetTeam() != Player.GetTeam() ||
                    p.GetTeam() == PlayerTeam.Independent) && !p.IsDead &&
                  p.IsInputEnabled)
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
            Time = 14000; // 14 s
          }

          protected override void Activate() { }

          public override void Update(float dlt, float dltSecs) {
            WeaponItemType[] emptyWeaponItemTypes = EmptyWeaponItemTypes;
            IPlayer[] enemies = ActiveEnemies;

            if (emptyWeaponItemTypes.Any() && enemies.Any())
              foreach (IPlayer enemy in enemies) {
                WeaponItemType toGrab = emptyWeaponItemTypes
                  .FirstOrDefault(s => enemy.CurrentWeaponDrawn == s);

                if (toGrab != null) {
                  Vector2 enemyPos = enemy.GetWorldPosition();
                  Vector2 playerPos = Player.GetWorldPosition();

                  IObjectWeaponItem weapon = enemy.Disarm(toGrab,
                    Vector2Helper.DirectionTo(playerPos, enemyPos), true);

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

          public override void TimeOut() {
            // Play effects indicating expiration of powerup
            Game.PlaySound("StrengthBoostStop", Vector2.Zero);
          }

          private static void Draw(Vector2 v) {
            Game.PlayEffect(EffectName.ItemGleam, v);
          }
        }

        // BLITZKRIEG - dsafxP
        public class Blitzkrieg : Powerup {
          private const float ATTACK_COOLDOWN = 250;
          private const float THROW_ANGULAR_VELOCITY = 50;
          private const float THROW_SPEED = 10;

          private static readonly Vector2 _offset = new Vector2(0, 24);
          private static readonly PlayerModifiers _setMod = new PlayerModifiers() {
            ExplosionDamageTakenModifier = 0,
            ImpactDamageTakenModifier = 0.25f
          };
          private static readonly string[] _throwableIDs = {
        "WpnGrenadesThrown",
        "WpnMolotovsThrown",
        //"WpnC4Thrown",
        "WpnMineThrown"
      };

          private readonly List<IObject> _explosives = new List<IObject>();
          private PlayerModifiers _modifiers;

          private static string RandomThrowableID {
            get {
              return _throwableIDs[_rng.Next(_throwableIDs.Length)];
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
              return "BLITZKRIEG";
            }
          }

          public override string Author {
            get {
              return "dsafxP";
            }
          }

          public IObject[] Explosives {
            get {
              _explosives.RemoveAll(item => item == null || item.IsRemoved);

              return _explosives.ToArray();
            }
          }

          public Blitzkrieg(IPlayer player) : base(player) {
            Time = 19000; // 19 s
          }

          protected override void Activate() {
            _modifiers = Player.GetModifiers(); // Store original player modifiers

            _modifiers.CurrentHealth = -1;
            _modifiers.CurrentEnergy = -1;

            Player.SetModifiers(_setMod);
          }

          public override void Update(float dlt, float dltSecs) {
            if (Player.IsBurning)
              Player.ClearFire();

            if (Time % ATTACK_COOLDOWN == 0) {
              Throw(true);
            }
          }

          public override void TimeOut() {
            Game.PlaySound("C4Detonate", Vector2.Zero);
          }

          public override void OnEnabled(bool enabled) {
            if (!enabled) {
              Player.SetModifiers(_modifiers);

              foreach (IObject exp in Explosives) {
                Game.PlayEffect(EffectName.DestroyDefault, exp.GetWorldPosition());
                Game.PlaySound("DestroyDefault", Vector2.Zero);

                exp.Remove();
              }
            }
          }

          private IObject Throw(bool missile = false) {
            IPlayer closestEnemy = ClosestEnemy;

            if (closestEnemy == null)
              return null;

            Vector2 playerPos = Player.GetWorldPosition() + _offset;
            Vector2 throwVel = Vector2Helper.DirectionTo(playerPos, closestEnemy
              .GetWorldPosition()) * THROW_SPEED;

            IObject thrown = Game.CreateObject(RandomThrowableID, playerPos, 0,
              throwVel, THROW_ANGULAR_VELOCITY, Player.FacingDirection);

            thrown.TrackAsMissile(missile);

            Game.PlayEffect(EffectName.TraceSpawner, Vector2.Zero, thrown.UniqueID,
              EffectName.ItemGleam, 2f);

            _explosives.Add(thrown);

            return thrown;
          }
        }

        // STARRED - Tomo
        public class Star : Powerup {
          private const float EFFECT_COOLDOWN = 50;
          private const float THROW_COOLDOWN = 100;
          private const float PUSH_FORCE = 7;
          private const float PUSH_DMG = 16;

          private static readonly PlayerCommand _playerCommand = new PlayerCommand(PlayerCommandType.Fall);
          private static readonly PlayerModifiers _starMod = new PlayerModifiers() {
            EnergyConsumptionModifier = 0,
            ProjectileCritChanceTakenModifier = 0,
            ExplosionDamageTakenModifier = 0,
            ProjectileDamageTakenModifier = 0,
            MeleeDamageTakenModifier = 0,
            ImpactDamageTakenModifier = 0,
            MeleeStunImmunity = 1,
            CanBurn = 0,
            RunSpeedModifier = 2,
            SprintSpeedModifier = 2
          };

          private static readonly string[] _colors = {
        "ClothingRed",
        "ClothingOrange",
        "ClothingYellow",
        "ClothingGreen",
        "ClothingBlue",
        "ClothingPurple"
      };

          private static readonly string[] _lightColors = {
        "ClothingLightRed",
        "ClothingLightOrange",
        "ClothingLightYellow",
        "ClothingLightGreen",
        "ClothingLightBlue",
        "ClothingLightPurple"
      };

          private int _rainbowIndex = 0;

          private IProfile _profile;
          private PlayerModifiers _modifiers;

          private int RainbowIndex {
            get {
              _rainbowIndex = (_rainbowIndex + 1) % _colors.Length;

              return _rainbowIndex;
            }
          }

          private IPlayer[] PlayersToPush {
            get {
              return Game.GetObjectsByArea<IPlayer>(Player.GetAABB())
                .Where(p => !p.IsDead && p != Player &&
                  (p.GetTeam() != Player.GetTeam() || p.GetTeam() == PlayerTeam.Independent))
                .ToArray();
            }
          }

          public override string Name {
            get {
              return "STARRED";
            }
          }

          public override string Author {
            get {
              return "Tomo";
            }
          }

          public Star(IPlayer player) : base(player) {
            Time = 9000; // 9 s
          }

          protected override void Activate() {
            _profile = Player.GetProfile(); // Store profile

            _modifiers = Player.GetModifiers(); // Store original player modifiers

            _modifiers.CurrentHealth = -1;
            _modifiers.CurrentEnergy = -1;

            Player.SetModifiers(_starMod);
          }

          public override void Update(float dlt, float dltSecs) {
            if (Time % EFFECT_COOLDOWN == 0) {
              PointShape.Random(Draw, Player.GetAABB(), _rng);

              Player.SetProfile(ColorProfile(Player.GetProfile(),
                _colors[RainbowIndex], _lightColors[_rainbowIndex]));
            }

            if (Time % THROW_COOLDOWN == 0)
              foreach (IPlayer toPush in PlayersToPush) {
                toPush.SetInputEnabled(false);
                toPush.AddCommand(_playerCommand);

                Events.UpdateCallback.Start((float _dlt) => {
                  toPush.SetInputEnabled(true);
                }, 1, 1);

                Vector2 toPushPos = toPush.GetWorldPosition();

                toPush.SetWorldPosition(toPushPos + (Vector2Helper.Up * 2)); // Sticky feet

                toPush.SetLinearVelocity(new Vector2(-toPush.FacingDirection * PUSH_FORCE, PUSH_FORCE));

                toPush.DealDamage(PUSH_DMG, Player.UniqueID);

                Game.PlaySound("PlayerDiveCatch", Vector2.Zero);
                Game.PlayEffect(EffectName.Smack, toPushPos);
              }
          }

          public override void TimeOut() {
            // Play effects indicating expiration of powerup
            Game.PlaySound("StrengthBoostStop", Vector2.Zero);
            Game.PlayEffect(EffectName.PlayerLandFull, Player.GetWorldPosition());
          }

          public override void OnEnabled(bool enabled) {
            if (!enabled) { // Restore player
              Player.SetModifiers(_modifiers);
              Player.SetProfile(_profile);
            }
          }

          private static void Draw(Vector2 v) {
            Game.PlayEffect(EffectName.ItemGleam, v);
          }

          private static IProfile ColorProfile(IProfile pr, string col, string colI) {
            if (pr.Accesory != null)
              pr.Accesory = new IProfileClothingItem(pr.Accessory.Name, col, colI);

            if (pr.ChestOver != null)
              pr.ChestOver = new IProfileClothingItem(pr.ChestOver.Name, col, colI);

            if (pr.ChestUnder != null)
              pr.ChestUnder = new IProfileClothingItem(pr.ChestUnder.Name, col, colI);

            if (pr.Feet != null)
              pr.Feet = new IProfileClothingItem(pr.Feet.Name, col, colI);

            if (pr.Hands != null)
              pr.Hands = new IProfileClothingItem(pr.Hands.Name, col, colI);

            if (pr.Head != null)
              pr.Head = new IProfileClothingItem(pr.Head.Name, col, colI);

            if (pr.Legs != null)
              pr.Legs = new IProfileClothingItem(pr.Legs.Name, col, colI);

            if (pr.Waist != null)
              pr.Waist = new IProfileClothingItem(pr.Waist.Name, col, colI);

            return pr;
          }
        }

        // OVERHEAL - Danila015
        public class Overheal : Powerup {
          private const float REGEN_COOLDOWN = 100;
          private const float REGEN = 5;
          private const float CANCEL_DMG = 15;

          private const int OVERHEAL = 1;

          private static readonly PlayerDamageEventType[] _allowedTypes = {
        PlayerDamageEventType.Melee,
        PlayerDamageEventType.Projectile,
        PlayerDamageEventType.Missile
      };

          private Events.PlayerDamageCallback _damageCallback = null;

          public override string Name {
            get {
              return "OVERHEAL";
            }
          }

          public override string Author {
            get {
              return "Danila015";
            }
          }

          public Overheal(IPlayer player) : base(player) {
            Time = 40000; // 40 s
          }

          protected override void Activate() { }

          public override void Update(float dlt, float dltSecs) {
            if (Time % REGEN_COOLDOWN == 0) {
              float health = Player.GetHealth();

              if (health < Player.GetMaxHealth()) {
                PointShape.Random(Draw, Player.GetAABB(), _rng);

                Game.PlaySound("GetHealthSmall", Vector2.Zero, 0.15f);

                Player.SetHealth(health + REGEN);
              } else {
                // Apply overheal
                PlayerModifiers mod = Player.GetModifiers();

                mod.MaxHealth += OVERHEAL;
                mod.CurrentHealth += OVERHEAL;

                Player.SetModifiers(mod);

                // Effect
                Game.PlaySound("Syringe", Vector2.Zero, 0.25f);
                PointShape.Random(DrawOverheal, Player.GetAABB(), _rng);
              }
            }
          }

          public override void TimeOut() {
            Game.PlaySound("PlayerLeave", Player.GetWorldPosition());
          }

          public override void OnEnabled(bool enabled) {
            if (enabled) {
              _damageCallback = Events.PlayerDamageCallback.Start(OnPlayerDamage);
            } else {
              _damageCallback.Stop();
              _damageCallback = null;
            }
          }

          private void OnPlayerDamage(IPlayer player, PlayerDamageArgs args) {
            if (player != Player)
              return;

            if (_allowedTypes.Any(at => args.DamageType == at)) {
              Player.DealDamage(CANCEL_DMG);

              Time = 0; // Player has been damaged by an opponent, stop
            }
          }

          private static void Draw(Vector2 v) {
            Game.PlayEffect(EffectName.BloodTrail, v);
            Game.PlayEffect(EffectName.Smack, v);
          }

          private static void DrawOverheal(Vector2 v) {
            Game.PlayEffect(EffectName.Blood, v);
          }
        }

        // BERSERK - Danila015
        public class Berserk : Powerup {
          private const float EFFECT_COOLDOWN = 100;
          private const float TIME = 35000;

          private static readonly PlayerModifiers _berserkMod = new PlayerModifiers() {
            CurrentHealth = 1,
            ProjectileCritChanceTakenModifier = 0,
            ExplosionDamageTakenModifier = 0,
            ProjectileDamageTakenModifier = 0,
            MeleeDamageTakenModifier = 0,
            ImpactDamageTakenModifier = 0,
            CanBurn = 0
          };

          private static readonly Vector2 _jumpVel = new Vector2(0, 12);

          private PlayerModifiers _modifiers; // Stores original player modifiers

          public override string Name {
            get {
              return "BERSERK";
            }
          }

          public override string Author {
            get {
              return "Danila015";
            }
          }

          public Berserk(IPlayer player) : base(player) {
            Time = TIME; // 35 s
          }

          protected override void Activate() {
            _modifiers = Player.GetModifiers(); // Store original player modifiers

            _modifiers.CurrentHealth = -1;
            _modifiers.CurrentEnergy = -1;

            Player.SetModifiers(_berserkMod);
            Player.SetStrengthBoostTime(TIME);
            Player.SetSpeedBoostTime(TIME);
          }

          public override void Update(float dlt, float dltSecs) {
            if (Player.KeyPressed(VirtualKey.JUMP) &&
              !Player.IsInMidAir && !Player.IsDisabled) {
              Player.SetLinearVelocity(_jumpVel);

              Game.PlaySound("LogoSlam", Vector2.Zero);
              Game.PlayEffect(EffectName.Dig, Player.GetWorldPosition());

              Game.PlayEffect(EffectName.TraceSpawner, Vector2.Zero,
                Player.UniqueID, EffectName.ImpactDefault, 2f);
            }

            if (Time % EFFECT_COOLDOWN == 0) {
              PointShape.Random(Draw, Player.GetAABB(), _rng);
            }
          }

          public override void OnEnabled(bool enabled) {
            if (!enabled) {
              Player.SetModifiers(_modifiers);

              Player.SetHealth(_berserkMod.CurrentHealth);
            }
          }

          private static void Draw(Vector2 v) {
            Game.PlayEffect(EffectName.Blood, v);
            Game.PlayEffect(EffectName.ItemGleam, v);
            Game.PlayEffect(EffectName.WoodParticles, v);
          }
        }

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

        // TELEKINESIS - dsafxP
        public class Telekinesis : Powerup {
          private const float EFFECT_COOLDOWN = 50;
          private const float VORTEX_FORCE = 5;
          private const float THROW_FORCE = 9;
          private const float BULLET_FORCE = 200;
          private const float LAUNCH_FORCE = 37;
          private const float TRACK_THROW_SIZE = 30;

          private static readonly Vector2 _vortexOffset = new Vector2(0, 32);
          private static readonly Vector2 _rayCastEndOffset = new Vector2(56, -1);
          private static readonly Vector2 _rayCastStartOffset = new Vector2(0, -1);

          private static readonly RayCastInput _rayCastInput = new RayCastInput(true) {
            AbsorbProjectile = RayCastFilterMode.Any,
            ProjectileHit = RayCastFilterMode.True
          };

          private readonly List<IObject> _thrown = new List<IObject>();

          private Events.PlayerKeyInputCallback _keyCallback = null;
          private Events.ObjectCreatedCallback _objectCreatedCallback = null;

          private IObject _sticky = null;

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

          private Vector2 RayCastEndOffset {
            get {
              Vector2 v = _rayCastEndOffset;
              v.X *= Player.FacingDirection;

              return v;
            }
          }

          private Vector2 RayCastStartOffset {
            get {
              Vector2 v = _rayCastStartOffset;
              v.X *= Player.FacingDirection;

              return v;
            }
          }

          private RayCastResult RayCast {
            get {
              Vector2 playerPos = Player.GetWorldPosition();

              Vector2 rayCastStart = playerPos + RayCastStartOffset;
              Vector2 rayCastEnd = playerPos + RayCastEndOffset;

              Game.DrawLine(rayCastStart, rayCastEnd, Color.Red);

              return Game.RayCast(rayCastStart, rayCastEnd, _rayCastInput)[0];
            }
          }

          private IObject FrontObject {
            get {
              RayCastResult result = RayCast;
              IObject hit = result.HitObject;

              if (hit == null)
                return null;

              if (hit.GetBodyType() == BodyType.Dynamic && hit.Destructable)
                return hit;

              return null;
            }
          }

          private IPlayer ClosestEnemy {
            get {
              List<IPlayer> enemies = Game.GetPlayers()
                .Where(p => (p.GetTeam() != Player.GetTeam() ||
                  p.GetTeam() == PlayerTeam.Independent) && !p.IsDead &&
                  p != _sticky && p != Player)
                .ToList();

              Vector2 playerPos = Player.GetWorldPosition();

              enemies.Sort((p1, p2) => Vector2.Distance(p1.GetWorldPosition(), playerPos)
                .CompareTo(Vector2.Distance(p2.GetWorldPosition(), playerPos)));

              return enemies.FirstOrDefault();
            }
          }

          private Area TrackThrowArea {
            get {
              Area playerArea = Player.GetAABB();

              playerArea.SetDimensions(TRACK_THROW_SIZE, TRACK_THROW_SIZE);

              return playerArea;
            }
          }

          public IObject[] Thrown {
            get {
              _thrown.RemoveAll(item => item == null || item.IsRemoved ||
                !item.IsMissile);

              return _thrown.ToArray();
            }
          }

          public override string Name {
            get {
              return "TELEKINESIS";
            }
          }

          public override string Author {
            get {
              return "dsafxP";
            }
          }

          public Telekinesis(IPlayer player) : base(player) {
            Time = 27000; // 27 s
          }

          protected override void Activate() { }

          public override void Update(float dlt, float dltSecs) {
            if (_sticky != null) {
              Vector2 pos = Player.GetWorldPosition() + _vortexOffset;
              Vector2 stickyPos = _sticky.GetWorldPosition();

              _sticky.SetLinearVelocity(Vector2Helper.DirectionTo(stickyPos, pos) * VORTEX_FORCE);

              _sticky.SetAngularVelocity(1);

              if (Time % EFFECT_COOLDOWN == 0)
                Game.PlayEffect(EffectName.ImpactDefault, stickyPos);
            }

            Vector2 inputDirection = InputDirection;

            foreach (IObject thrown in Thrown) {
              thrown.SetLinearVelocity(inputDirection * THROW_FORCE);
              thrown.SetAngularVelocity(THROW_FORCE);
            }

            foreach (IProjectile fired in Game.GetProjectiles()
              .Where(p => p.OwnerPlayerID == Player.UniqueID)) {
              fired.Direction = inputDirection;
              fired.Velocity = inputDirection != Vector2.Zero ? inputDirection * BULLET_FORCE :
                fired.Direction * BULLET_FORCE;
            }
          }

          public override void TimeOut() {
            Game.PlaySound("StrengthBoostStop", Vector2.Zero);
          }

          public override void OnEnabled(bool enabled) {
            if (enabled) {
              _keyCallback = Events.PlayerKeyInputCallback.Start(OnPlayerKeyInput);
              _objectCreatedCallback = Events.ObjectCreatedCallback.Start(OnObjectCreated);
            } else {
              _keyCallback.Stop();
              _keyCallback = null;

              _objectCreatedCallback.Stop();
              _objectCreatedCallback = null;
            }
          }

          private void OnPlayerKeyInput(IPlayer player, VirtualKeyInfo[] keyEvents) {
            if (player != Player)
              return;

            foreach (VirtualKeyInfo pressed in keyEvents
              .Where(k => k.Event == VirtualKeyEvent.Pressed && k.Key == VirtualKey.ACTIVATE)) {
              if (_sticky != null) {
                _sticky.TrackAsMissile(true);

                IPlayer closestEnemy = ClosestEnemy;

                _sticky.SetLinearVelocity(closestEnemy != null ?
                  Vector2Helper.DirectionTo(_sticky.GetWorldPosition(),
                    closestEnemy.GetWorldPosition()) * LAUNCH_FORCE :
                  Vector2.Zero);

                _sticky.SetAngularVelocity(LAUNCH_FORCE);

                Game.PlayEffect(EffectName.Smack, _sticky.GetWorldPosition());
                Game.PlaySound("MeleeSwing", Vector2.Zero);

                _sticky = null;
              } else {
                _sticky = FrontObject;

                if (_sticky != null) {
                  Game.PlayEffect(EffectName.BulletHit, _sticky.GetWorldPosition());
                  Game.PlaySound("Draw1", Vector2.Zero);
                }
              }
            }
          }

          private void OnObjectCreated(IObject[] objs) {
            _thrown.AddRange(objs
              .Where(o => o.IsMissile && TrackThrowArea
                .Intersects(o.GetAABB())));
          }
        }

        // ARSENAL - dsafxP
        public class Arsenal : Powerup {
          private static readonly PlayerModifiers _arsenalMod = new PlayerModifiers() {
            ItemDropMode = 1
          };

          private static readonly Vector2 _rayCastEndOffset = new Vector2(0, -64);
          private static readonly Vector2 _rayCastStartOffset = new Vector2(0, -2);

          private static readonly RayCastInput _rayCastInput = new RayCastInput(true) {
            AbsorbProjectile = RayCastFilterMode.True,
            ProjectileHit = RayCastFilterMode.True,
            BlockFire = RayCastFilterMode.True,
            BlockMelee = RayCastFilterMode.True,
            BlockExplosions = RayCastFilterMode.True
          };

          private static WeaponItem RandomWeapon {
            get {
              WeaponItem w = Game.GetRandomWeaponItem();

              while (w == WeaponItem.STREETSWEEPER)
                w = Game.GetRandomWeaponItem();

              return w;
            }
          }

          private Vector2 RayCastEndOffset {
            get {
              Vector2 v = _rayCastEndOffset;
              v.X *= Player.FacingDirection;

              return v;
            }
          }

          private Vector2 RayCastStartOffset {
            get {
              Vector2 v = _rayCastStartOffset;
              v.X *= Player.FacingDirection;

              return v;
            }
          }

          private RayCastResult RayCast {
            get {
              Vector2 playerPos = Player.GetWorldPosition();

              Vector2 rayCastStart = playerPos + RayCastStartOffset;
              Vector2 rayCastEnd = playerPos + RayCastEndOffset;

              Game.DrawLine(rayCastStart, rayCastEnd, Color.Red);

              return Game.RayCast(rayCastStart, rayCastEnd, _rayCastInput)[0];
            }
          }

          private Vector2 Ground {
            get {
              return RayCast.Position;
            }
          }

          private PlayerModifiers _modifiers; // Stores original player modifiers

          private IObjectAmmoStashTrigger _stash;

          private WeaponItemType[] EmptyWeaponItemTypes {
            get {
              List<WeaponItemType> weaponItemTypes = new List<WeaponItemType> {
                WeaponItemType.Melee,
                WeaponItemType.Rifle,
                WeaponItemType.Handgun,
                //WeaponItemType.Powerup,
                WeaponItemType.Thrown
              };

              weaponItemTypes.Remove(Player.CurrentMeleeWeapon.WeaponItemType);
              weaponItemTypes.Remove(Player.CurrentPrimaryWeapon.WeaponItemType);
              weaponItemTypes.Remove(Player.CurrentSecondaryWeapon.WeaponItemType);
              //weaponItemTypes.Remove(Player.CurrentPowerupItem.WeaponItemType);
              weaponItemTypes.Remove(Player.CurrentThrownItem.WeaponItemType);

              return weaponItemTypes.ToArray();
            }
          }

          public override string Name {
            get {
              return "ARSENAL";
            }
          }

          public override string Author {
            get {
              return "dsafxP";
            }
          }

          public Arsenal(IPlayer player) : base(player) {
            Time = 7000; // 7 s
          }

          protected override void Activate() {
            _modifiers = Player.GetModifiers(); // Store original player modifiers

            _modifiers.CurrentHealth = -1;
            _modifiers.CurrentEnergy = -1;

            Player.SetModifiers(_arsenalMod);

            Update(0, 0);

            Player.GiveWeaponItem(GetRandomWeaponFromType(WeaponItemType.Powerup));

            // Create stash
            Vector2 ground = Ground;

            if (ground != Vector2.Zero) {
              Game.PlayEffect(EffectName.Dig, ground);

              _stash = (IObjectAmmoStashTrigger)Game.CreateObject("AmmoStash00", ground);
            }
          }

          public override void TimeOut() {
            Game.PlaySound("DestroyMetal", Vector2.Zero, 1);
            Game.PlayEffect(EffectName.Sparks, Player.GetWorldPosition());
          }

          public override void Update(float dlt, float dltSecs) {
            // Give weapons for each empty slot
            foreach (WeaponItemType empty in EmptyWeaponItemTypes)
              Player.GiveWeaponItem(GetRandomWeaponFromType(empty));
          }

          public override void OnEnabled(bool enabled) {
            if (!enabled) {
              // Restore original player modifiers
              Player.SetModifiers(_modifiers);

              if (_stash != null)
                _stash.Destroy();
            }
          }

          private static WeaponItem GetRandomWeaponFromType(WeaponItemType type) {
            WeaponItem w = RandomWeapon;

            IObjectWeaponItem wItem = Game.SpawnWeaponItem(w,
              Vector2.Zero, false, float.Epsilon);

            while (wItem.WeaponItemType != type) {
              w = RandomWeapon;

              wItem = Game.SpawnWeaponItem(w,
                Vector2.Zero, false, float.Epsilon);
            }

            return w;
          }
        }

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

        // DRONE - dsafxP
        public class Drone : Powerup {
          private const float SPEED = 6;

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
            Streetsweeper = (IObjectStreetsweeper)Game.CreateObject("Streetsweeper", Player.GetWorldPosition());

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

        // OVERCHARGE - dsafxP - Danila015 - Eiga
        public class Overcharge : Powerup {
          private const float CHARGE_INTENSITY = 0.33f; // Charges * Value
          private const float CHARGE_DELAY = 6000;
          private const string CHARGE_TEXT = "+{0}"; // 0 for charges

          private Events.PlayerMeleeActionCallback _meleeActionCallback = null;

          private PlayerModifiers _modifiers; // Stores original player modifiers

          private float _elapsed = 0;
          private int _charges = 0;

          private int Charges {
            get {
              return _charges;
            }
            set {
              _charges = value;

              float charge = _charges * CHARGE_INTENSITY;

              if (_charges > 0) {
                _elapsed += CHARGE_DELAY;

                Game.PlayEffect(EffectName.CustomFloatText, Player.GetWorldPosition(),
                  string.Format(CHARGE_TEXT, _charges));

                Game.PlaySound("GetAmmoSmall", Vector2.Zero, Math.Min(charge, 1));
              }

              Player.SetModifiers(new PlayerModifiers() {
                MeleeForceModifier = Math.Max(charge, 1)
              });
            }
          }

          public bool Depleted {
            get {
              return _elapsed <= 0;
            }
          }

          public override string Name {
            get {
              return "OVERCHARGE";
            }
          }

          public override string Author {
            get {
              return "dsafxP - Danila015 - Eiga";
            }
          }

          public Overcharge(IPlayer player) : base(player) {
            Time = 33000; // 33 s
          }

          protected override void Activate() {
            _modifiers = Player.GetModifiers(); // Store original player modifiers

            _modifiers.CurrentHealth = -1;
            _modifiers.CurrentEnergy = -1;
          }

          public override void Update(float dlt, float dltSecs) {
            _elapsed = Math.Max(_elapsed - dlt, 0);

            if (Depleted && Charges != 0) {
              Charges = 0;

              Game.PlaySound("ElectricSparks", Vector2.Zero);
              Game.PlayEffect(EffectName.Electric, Player.GetWorldPosition());
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

              // Restore original player modifiers
              Player.SetModifiers(_modifiers);
            }
          }

          private void OnPlayerMeleeAction(IPlayer player, PlayerMeleeHitArg[] args) {
            if (player != Player)
              return;

            IEnumerable<IPlayer> stunned = args
              .Where(a => a.IsPlayer)
              .Select(p => (IPlayer)p.HitObject)
              .Where(p => (p.IsFalling || p.IsStaggering) && !p.IsDead);

            if (stunned.Any())
              Charges += stunned.Count();
          }
        }

        // VAMPIRISM - Danila015
        public class Vampirism : Powerup {
          private const float EFFECT_COOLDOWN = 50;
          private const float HEAL_MULT = 0.77f;
          private const float BLEED_DMG = 2;
          private const float BLEED_COOLDOWN = 250;
          private const float CORPSE_DMG_MULT = 3;
          private const uint BLEED_TIME = 6000;

          private static readonly Vector2 _offset = new Vector2(0, 12);

          private readonly List<IPlayer> _bleeding = new List<IPlayer>();

          private Events.PlayerMeleeActionCallback _meleeActionCallback = null;

          private IPlayer[] Bleeding {
            get {
              _bleeding.RemoveAll(p => p == null || p.IsRemoved || p.IsDead);

              return _bleeding.ToArray();
            }
          }

          public override string Name {
            get {
              return "VAMPIRISM";
            }
          }

          public override string Author {
            get {
              return "Danila015";
            }
          }

          public Vampirism(IPlayer player) : base(player) {
            Time = 21000; // 21 s
          }

          protected override void Activate() { }

          public override void Update(float dlt, float dltSecs) {
            if (Time % EFFECT_COOLDOWN == 0)
              Game.PlayEffect(EffectName.BloodTrail, Player.GetWorldPosition() + _offset);

            if (Time % BLEED_COOLDOWN == 0)
              foreach (IPlayer bleeding in Bleeding) {
                bleeding.DealDamage(BLEED_DMG);

                Game.PlayEffect(EffectName.Blood, bleeding.GetWorldPosition());
                Game.PlaySound("ImpactFlesh", Vector2.Zero, 0.5f);
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

              _bleeding.Clear();
            }
          }

          private void OnPlayerMeleeAction(IPlayer player, PlayerMeleeHitArg[] args) {
            if (player != Player)
              return;

            foreach (PlayerMeleeHitArg arg in args
              .Where(a => a.IsPlayer)) {
              float dmg = arg.HitDamage;

              Player.SetHealth(Player.GetHealth() + dmg * HEAL_MULT);

              IPlayer hit = (IPlayer)arg.HitObject;

              if (hit.IsDead) {
                hit.SetHealth(hit.GetHealth() + dmg); // Null dmg so it isn't dealt twice

                hit.DealDamage(dmg * CORPSE_DMG_MULT);
              }

              if (!_bleeding.Contains(hit)) {
                _bleeding.Add(hit);

                Events.UpdateCallback.Start((float _dlt) => {
                  _bleeding.Remove(hit);
                }, BLEED_TIME, 1);
              }
            }
          }
        }

        // TRI-STRIKE - dsafxP - Eiga
        public class Strike : Powerup {
          private const float EFFECT_COOLDOWN = 50;
          private const float EFFECT_SEPARATION = 5;
          private const float STAND_DMG_MULT = 0.33f;
          private const float STAND_FORCE = 2;

          private static readonly PlayerCommand _playerCommand = new PlayerCommand(PlayerCommandType.Fall);

          private static readonly Vector2[] _offsets = {
            new Vector2(-10, 0),
            new Vector2(10, 0),
            new Vector2(0, 20)
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

              IObjectWeaponItem disarmed = hit.Disarm(WeaponItemType.Melee);

              if (!hit.IsBlocking) {
                hit.DealDamage(arg.HitDamage * STAND_DMG_MULT);

                hit.SetWorldPosition(hit.GetWorldPosition() + (Vector2Helper.Up * 2)); // Sticky feet

                hit.SetLinearVelocity(hit.GetLinearVelocity() +
                  new Vector2(STAND_FORCE * -hit.FacingDirection, STAND_FORCE));

                Game.PlaySound("PlayerDive", Vector2.Zero);

                PointShape.Polygon(Draw2, EffectPositions, EFFECT_SEPARATION);
              } else if (disarmed != null)
                disarmed.Destroy();
            }
          }

          private static void Draw(Vector2 pos) {
            Game.PlayEffect(EffectName.ItemGleam, pos);
          }

          private static void Draw2(Vector2 pos) {
            Game.PlayEffect(EffectName.Smack, pos);
          }
        }

        // ACE PITCHER - dsafxP
        public class Pitcher : Powerup {
          private const float AMMO_REGEN_COOLDOWN = 1000;
          private const float AMMO_REGEN = 0.1f; // MaxTotalAmmo * Value
          private const float TRACK_THROW_SIZE = 30;
          private const float THROW_VEL_MULT = 1.5f;
          private const float BLEED_DMG = 2;
          private const float BLEED_COOLDOWN = 250;
          private const uint BLEED_TIME = 3000;

          private static readonly WeaponItemType[] _rangedTypes = {
        WeaponItemType.Handgun,
        WeaponItemType.Rifle
      };

          private readonly List<IPlayer> _bleeding = new List<IPlayer>();

          private Events.ObjectCreatedCallback _objectCreatedCallback = null;
          private Events.ProjectileCreatedCallback _projectileCreatedCallback = null;
          private Events.PlayerWeaponAddedActionCallback _weaponAddedCallback = null;
          private Events.ProjectileHitCallback _projectileHitCallback = null;

          private bool HasRangedWeapon {
            get {
              return Player.CurrentPrimaryWeapon.WeaponItem != WeaponItem.NONE ||
                Player.CurrentSecondaryWeapon.WeaponItem != WeaponItem.NONE;
            }
          }

          private Area TrackThrowArea {
            get {
              Area playerArea = Player.GetAABB();

              playerArea.SetDimensions(TRACK_THROW_SIZE, TRACK_THROW_SIZE);

              return playerArea;
            }
          }

          private IPlayer[] Bleeding {
            get {
              _bleeding.RemoveAll(p => p == null || p.IsRemoved || p.IsDead);

              return _bleeding.ToArray();
            }
          }

          public override string Name {
            get {
              return "ACE PITCHER";
            }
          }

          public override string Author {
            get {
              return "dsafxP";
            }
          }

          public Pitcher(IPlayer player) : base(player) {
            Time = 27000; // 27 s
          }

          protected override void Activate() {
            if (HasRangedWeapon)
              Player.GiveWeaponItem(WeaponItem.LAZER);
          }

          public override void Update(float dlt, float dltSecs) {
            if (Time % AMMO_REGEN_COOLDOWN == 0 && HasRangedWeapon) {
              RifleWeaponItem rifleWeaponItem = Player.CurrentPrimaryWeapon;

              Player.SetCurrentPrimaryWeaponAmmo((int)(rifleWeaponItem.TotalAmmo + (rifleWeaponItem.MaxTotalAmmo * AMMO_REGEN)));

              HandgunWeaponItem handgunWeaponItem = Player.CurrentSecondaryWeapon;

              Player.SetCurrentSecondaryWeaponAmmo((int)(handgunWeaponItem.TotalAmmo + (handgunWeaponItem.MaxTotalAmmo * AMMO_REGEN)));

              Game.PlayEffect(EffectName.CustomFloatText, Player.GetWorldPosition(), "+AMMO");

              Game.PlaySound("GetAmmoSmall", Vector2.Zero, 0.5f);
            }

            if (Time % BLEED_COOLDOWN == 0)
              foreach (IPlayer bleeding in Bleeding) {
                bleeding.DealDamage(BLEED_DMG);

                Game.PlayEffect(EffectName.Blood, bleeding.GetWorldPosition());
                Game.PlaySound("ImpactFlesh", Vector2.Zero, 0.5f);
              }
          }

          public override void TimeOut() {
            Game.PlaySound("StrengthBoostStop", Vector2.Zero);
          }

          public override void OnEnabled(bool enabled) {
            if (enabled) {
              _objectCreatedCallback = Events.ObjectCreatedCallback.Start(OnObjectCreated);
              _projectileCreatedCallback = Events.ProjectileCreatedCallback.Start(OnProjectileCreated);
              _weaponAddedCallback = Events.PlayerWeaponAddedActionCallback.Start(OnPlayerWeaponAddedAction);
              _projectileHitCallback = Events.ProjectileHitCallback.Start(OnProjectileHit);
            } else {
              _objectCreatedCallback.Stop();
              _objectCreatedCallback = null;

              _projectileCreatedCallback.Stop();
              _projectileCreatedCallback = null;

              _weaponAddedCallback.Stop();
              _weaponAddedCallback = null;

              _projectileHitCallback.Stop();
              _projectileHitCallback = null;

              _bleeding.Clear();
            }
          }

          private void OnObjectCreated(IObject[] objs) {
            foreach (IObject obj in objs) {
              if (!obj.IsMissile && !TrackThrowArea
             .Intersects(obj.GetAABB()))
                continue;

              obj.SetLinearVelocity(obj.GetLinearVelocity() * THROW_VEL_MULT);
              obj.SetAngularVelocity(obj.GetAngularVelocity() * THROW_VEL_MULT);

              Game.PlayEffect(EffectName.TraceSpawner, Vector2.Zero,
                obj.UniqueID, EffectName.ItemGleam, 2f);
            }
          }

          private void OnProjectileCreated(IProjectile[] projectiles) {
            foreach (IProjectile proj in projectiles) {
              if (proj.OwnerPlayerID != Player.UniqueID)
                continue;

              proj.Direction = Player.AimVector;
            }
          }

          private void OnPlayerWeaponAddedAction(IPlayer player, PlayerWeaponAddedArg arg) {
            if (player != Player)
              return;

            if (_rangedTypes.Any(t => arg.WeaponItemType == t))
              Player.GiveWeaponItem(WeaponItem.LAZER);
          }

          private void OnProjectileHit(IProjectile projectile, ProjectileHitArgs args) {
            if (projectile.OwnerPlayerID != Player.UniqueID)
              return;

            IPlayer hit = Game.GetPlayer(args.HitObjectID);

            if (hit != null) {
              if (!_bleeding.Contains(hit)) {
                _bleeding.Add(hit);

                Events.UpdateCallback.Start((float _dlt) => {
                  _bleeding.Remove(hit);
                }, BLEED_TIME, 1);
              }
            }
          }
        }

        // LAGGER - dsafxP
        public class Lagger : Powerup {
          private const float TP_COOLDOWN = 500;
          private const float SAVE_POS_COOLDOWN = 1000;
          private const float PING_INTENSITY = 0.01f; // Ping * Value = 1

          private static readonly string[] _effectNames = {
            "0",
            "null",
            "NaN",
            "N/A",
            "undefined",
            "nullReference",
            "exception",
            "error",
            "unknown",
            "none",
            "missing",
            "invalid",
            "failure",
            "0x0",
            "false",
            "empty",
            "nil",
            "void"
          };

          private PlayerModifiers _modifiers;
          private Vector2 _lastPos = Vector2.Zero;

          private float PingFactor {
            get {
              float p = Player.GetUser()
                .Ping * PING_INTENSITY;

              return p < 1 ? -2 : p;
            }
          }

          private PlayerModifiers CurrentModifiers {
            get {
              float pingFactor = PingFactor;

              return new PlayerModifiers() {
                EnergyRechargeModifier = pingFactor,
                ProjectileDamageDealtModifier = pingFactor,
                ProjectileCritChanceDealtModifier = pingFactor,
                MeleeDamageDealtModifier = pingFactor,
                MeleeForceModifier = pingFactor,
                RunSpeedModifier = pingFactor,
                SprintSpeedModifier = pingFactor
              };
            }
          }

          public override string Name {
            get {
              return "LAGGER";
            }
          }

          public override string Author {
            get {
              return "dsafxP";
            }
          }

          public Lagger(IPlayer player) : base(player) {
            Time = 16000; // 16 s
          }

          protected override void Activate() {
            _lastPos = Player.GetWorldPosition();
            _modifiers = Player.GetModifiers();

            _modifiers.CurrentHealth = -1;
            _modifiers.CurrentEnergy = -1;
          }

          public override void Update(float dlt, float dltSecs) {
            if (Time % SAVE_POS_COOLDOWN == 0)
              _lastPos = Player.GetWorldPosition();
            else if (Time % TP_COOLDOWN == 0) {
              Player.SetWorldPosition(_lastPos);

              Game.PlayEffect(EffectName.CustomFloatText, _lastPos,
                _effectNames[_rng.Next(_effectNames.Length)]);
            }

            Player.SetModifiers(CurrentModifiers);
          }

          public override void TimeOut() {
            Game.PlaySound("StrengthBoostStop", Vector2.Zero);
          }

          public override void OnEnabled(bool enabled) {
            if (!enabled) {
              Player.SetModifiers(_modifiers);
            }
          }
        }

        // RECONSTRUCTOR - Danila015
        public class Reconstructor : Powerup {
          private const float EFFECT_COOLDOWN = 250;
          private const float HEAL_MULT = 0.24f;
          private const float OBJ_DMG_MULT = 7;
          private const float MAGNET_AREA_SIZE = 75;
          private const float MAGNET_FORCE = 2;
          private const float MAGNET_ANGULAR_SPEED = 1.5f;
          private const PlayerHitEffect HIT_EFFECT = PlayerHitEffect.Metal;

          private static readonly string[] _debrisNames = {
        "Debris",
        "Giblet"
      };

          public override string Author {
            get {
              return "Danila015";
            }
          }

          public override string Name {
            get {
              return "RECONSTRUCTOR";
            }
          }

          private Events.PlayerMeleeActionCallback _meleeActionCallback = null;

          private IProfile _profile;
          private PlayerModifiers _modifiers;
          private PlayerHitEffect _hitEffect;

          private Area MagnetArea {
            get {
              Area playerArea = Player.GetAABB();

              playerArea.SetDimensions(MAGNET_AREA_SIZE, MAGNET_AREA_SIZE);

              return playerArea;
            }
          }

          private IObject[] DebrisInMagnet {
            get {
              return Game.GetObjectsByArea(MagnetArea)
                .Where(o => _debrisNames.Any(d => o.Name.Contains(d)) &&
                o.Destructable)
                .ToArray();
            }
          }

          private IObject[] EatDebris {
            get {
              return Game.GetObjectsByArea(Player.GetAABB())
                .Where(o => _debrisNames.Any(d => o.Name.Contains(d)) &&
                o.Destructable)
                .ToArray();
            }
          }

          private IProfile MetalProfile {
            get {
              IProfile playerProfile = Player.GetProfile();

              playerProfile.Skin = new IProfileClothingItem(string.Format("Normal{0}",
              playerProfile.Gender == Gender.Male ? string.Empty : "_fem"), "Skin5");

              return ColorProfile(playerProfile, "ClothingLightGray", "ClothingLightGray");
            }
          }

          public Reconstructor(IPlayer player) : base(player) {
            Time = 24000; // 24 s
          }

          protected override void Activate() {
            _profile = Player.GetProfile();
            _hitEffect = Player.GetHitEffect();
            _modifiers = Player.GetModifiers();

            _modifiers.CurrentHealth = -1;
            _modifiers.CurrentEnergy = -1;

            Player.SetProfile(MetalProfile);
            Player.SetHitEffect(HIT_EFFECT);
          }

          public override void Update(float dlt, float dltSecs) {
            bool effect = Time % EFFECT_COOLDOWN == 0;

            if (effect) {
              PointShape.Random(Draw, Player.GetAABB(), _rng);
            }

            foreach (IObject deb in EatDebris) {
              float heal = deb.GetHealth() * HEAL_MULT;
              float healed = Player.GetHealth() + heal;

              if (healed > Player.GetMaxHealth()) {
                PlayerModifiers mod = Player.GetModifiers();

                mod.CurrentHealth += heal;
                mod.MaxHealth += (int)heal;

                Player.SetModifiers(mod);
              } else {
                Player.SetHealth(healed);
              }

              deb.Destroy();
            }

            foreach (IObject deb in DebrisInMagnet) {
              Vector2 debPos = deb.GetWorldPosition();

              deb.SetLinearVelocity(Vector2Helper.DirectionTo(debPos,
                Player.GetWorldPosition()) * MAGNET_FORCE);

              deb.SetAngularVelocity(MAGNET_ANGULAR_SPEED);

              if (effect)
                Game.PlayEffect(EffectName.ItemGleam, debPos);
            }
          }

          public override void TimeOut() {
            Game.PlaySound("DestroyMetal", Vector2.Zero);
            Game.PlayEffect(EffectName.Sparks, Player.GetWorldPosition());
          }

          public override void OnEnabled(bool enabled) {
            if (enabled) {
              _meleeActionCallback = Events.PlayerMeleeActionCallback.Start(OnPlayerMeleeAction);
            } else {
              Player.SetProfile(_profile);
              Player.SetHitEffect(_hitEffect);
              Player.SetModifiers(_modifiers);

              _meleeActionCallback.Stop();

              _meleeActionCallback = null;
            }
          }

          private static void Draw(Vector2 v) {
            Game.PlayEffect(EffectName.ItemGleam, v);
          }

          private static IProfile ColorProfile(IProfile pr, string col, string colI) {
            if (pr.Accesory != null)
              pr.Accesory = new IProfileClothingItem(pr.Accessory.Name, col, colI);

            if (pr.ChestOver != null)
              pr.ChestOver = new IProfileClothingItem(pr.ChestOver.Name, col, colI);

            if (pr.ChestUnder != null)
              pr.ChestUnder = new IProfileClothingItem(pr.ChestUnder.Name, col, colI);

            if (pr.Feet != null)
              pr.Feet = new IProfileClothingItem(pr.Feet.Name, col, colI);

            if (pr.Hands != null)
              pr.Hands = new IProfileClothingItem(pr.Hands.Name, col, colI);

            if (pr.Head != null)
              pr.Head = new IProfileClothingItem(pr.Head.Name, col, colI);

            if (pr.Legs != null)
              pr.Legs = new IProfileClothingItem(pr.Legs.Name, col, colI);

            if (pr.Waist != null)
              pr.Waist = new IProfileClothingItem(pr.Waist.Name, col, colI);

            return pr;
          }

          private void OnPlayerMeleeAction(IPlayer player, PlayerMeleeHitArg[] args) {
            if (player != Player)
              return;

            foreach (PlayerMeleeHitArg arg in args) {
              if (arg.IsPlayer)
                continue;

              IObject hit = arg.HitObject;
              float dmg = arg.HitDamage;

              hit.SetHealth(hit.GetHealth() + dmg); // Null dmg so it isn't dealt twice

              hit.DealDamage(dmg * OBJ_DMG_MULT);

              hit.SetAngularVelocity(MAGNET_ANGULAR_SPEED);

              Game.PlaySound("ChainsawStartup", Vector2.Zero, 0.6f);
            }
          }
        }

        // ONE-TAP - Danila015
        public class Onetap : Powerup {
          private const float EFFECT_COOLDOWN = 50;
          private const float EFFECT_SEPARATION = 50;
          private const float EFFECT_SIZE = 40;
          private const float SHAKE_TIME = 1000;
          private const float SHAKE_INTENSITY = 4;
          private const float BLOCK_TIME_PENALTY = 3000;

          private const float EFFECT_RADIUS = EFFECT_SIZE / 2;

          private Events.PlayerMeleeActionCallback _meleeActionCallback = null;

          public override string Name {
            get {
              return "ONE-TAP";
            }
          }

          public override string Author {
            get {
              return "Danila015";
            }
          }

          public Onetap(IPlayer player) : base(player) {
            Time = 10000;
          }

          protected override void Activate() { }

          public override void Update(float dlt, float dltSecs) {
            if (Time % EFFECT_COOLDOWN == 0) {
              Draw(Player.GetWorldPosition());
            }
          }

          public override void TimeOut() {
            Game.PlaySound("StrengthBoostStop", Vector2.Zero);

            Game.PlayEffect(EffectName.Gib, Player.GetWorldPosition());
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

              if (hit.IsBlocking) {
                Time -= BLOCK_TIME_PENALTY;

                continue;
              }

              hit.Gib();

              Game.PlayEffect(EffectName.CameraShaker, Vector2.Zero,
                SHAKE_INTENSITY, SHAKE_TIME, true);

              Time = 0;
            }
          }

          private void Draw(Vector2 pos) {
            PointShape.Circle(v => {
              Game.PlayEffect(EffectName.BloodTrail, Vector2Helper.Rotated(v - pos,
                  (float)(Time % 1500 * (MathHelper.TwoPI / 1500))) +
                pos);
            }, pos, EFFECT_RADIUS, EFFECT_SEPARATION);
          }
        }
      }
    }
  }
}