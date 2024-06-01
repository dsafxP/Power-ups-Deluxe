public void OnStartup() {
  Powerups.Enabled = true;
  
  Events.UserMessageCallback.Start(HandleCommand);
  
  Config.Update();
}

public void HandleCommand(UserMessageCallbackArgs args) {
  IUser user = args.User;
  
  if (!args.IsCommand)
    return;
  
  switch(args.Command) {
    case "PD_CRATE_CHANCE": {
      if (!user.IsModerator && !user.IsHost) {
        Game.ShowChatMessage("You don't have enough perms to execute this command.", 
        Color.Red, user.UserIdentifier);
    
        break;
      }
      
      float crateChance;
      
      if (float.TryParse(args.CommandArguments.Trim(), out crateChance)) {
        Config.SpecialCrateChance = crateChance;
        
        Game.ShowChatMessage(string.Format("Set special crate chance to {0}.", Config.SpecialCrateChance), 
        Color.Green, user.UserIdentifier);
      } else {
        Game.ShowChatMessage("Specify a valid number.", 
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
      
      IUser target = GetUser(args.CommandArguments.Trim());
      IPlayer targetPlayer = target != null ? target.GetPlayer() : user.GetPlayer();
      
      if (targetPlayer != null) {
        OnPowerupSyringe(new TriggerArgs(null, targetPlayer, false));
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
    sender = Game.GetObjectsByArea < IPlayer > (caller.GetAABB())
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

          Powerups.Powerup powerUp = (Powerups.Powerup) Activator.CreateInstance(powerUpType, sender); // Activate random powerup

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
  private static readonly Random _rng = new Random();
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

  public static float SpawnChance = 100;

  public static SupplyBox CreatePowerupBox(Vector2 pos) {
    // Create box
    IObject box = Game.CreateObject("CardboardBox00", pos);

    // Create helmet
    Vector2 helmOffset = new Vector2(2, -0.5f);

    IObject helm = Game.CreateObject("Helmet00", pos + helmOffset);

    // Create weld joint
    IObjectWeldJoint weldJoint = (IObjectWeldJoint) Game.CreateObject("WeldJoint", pos);

    // Set weld joint targets
    weldJoint.AddTargetObject(box);
    weldJoint.AddTargetObject(helm);

    // Create destroy targets
    IObjectDestroyTargets destroyTargets = (IObjectDestroyTargets) Game.CreateObject("DestroyTargets", pos);

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
    IObjectActivateTrigger activateTrigger = (IObjectActivateTrigger) Game.CreateObject("ActivateTrigger", pos);

    // Set ActivateTrigger
    activateTrigger.SetBodyType(BodyType.Dynamic);
    activateTrigger.SetHighlightObject(syringe);
    activateTrigger.SetScriptMethod("OnPowerupSyringe");

    // Create weld joint
    IObjectWeldJoint weldJoint = (IObjectWeldJoint) Game.CreateObject("WeldJoint", pos);

    // Set weld joint targets
    weldJoint.AddTargetObject(syringe);
    weldJoint.AddTargetObject(activateTrigger);

    // Create destroy targets
    IObjectDestroyTargets destroyTargets = (IObjectDestroyTargets) Game.CreateObject("DestroyTargets", pos);

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

    Type[] nestedPowerups = typeof (AvailablePowerups).GetNestedTypes();

    Type[] instantiableTypes = nestedPowerups.Where(t =>
      //t.BaseType == typeof(Powerup) &&
      t.GetConstructors().Any(c =>
        c.GetParameters().Length == 1 &&
        c.GetParameters()[0].ParameterType == typeof (IPlayer)
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
    foreach(IObject supplyCrate in objs
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

    private readonly List < IPlayer > _activators = new List < IPlayer > ();
    private Events.UpdateCallback _updateCallback = null;

    private IPlayer Activator {
      get {
        return Game.GetObjectsByArea < IPlayer > (Trigger.GetAABB())
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
        }, (uint) Trigger.GetCooldown(), 1);
      }
    }
  }

  /// <summary>
  /// Represents a base power-up that can be activated and updated over time.
  /// </summary>
  public abstract class Powerup {
    // Interval for the main update callback event
    private const uint COOLDOWN = 0;

    // Main update callback event
    private Events.UpdateCallback _updateCallback = null;

    // Used for calculating delta time
    private float _lastUpdate;

    public abstract string Name {
      get;
    }

    public abstract string Author {
      get;
    }

    // Time left for the power-up to be active
    public float Time = 1000;

    // The player associated with this power-up
    public IPlayer Player;

    /// <summary>
    /// Gets or sets whether the power-up is enabled.
    /// </summary>
    public bool Enabled {
      get {
        return _updateCallback != null;
      }
      set {
        if (value != Enabled) {
          if (value) {
            _updateCallback = Events.UpdateCallback.Start(Update, COOLDOWN);

            OnEnabled(true);
          } else {
            _updateCallback.Stop();
            _updateCallback = null;

            OnEnabled(false);
          }
        }
      }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Powerup"/> class.
    /// </summary>
    /// <param name="player">The player associated with this power-up.</param>
    public Powerup(IPlayer player) {
      Player = player;
      _lastUpdate = Game.TotalElapsedGameTime;
      Enabled = true;
      Activate();
    }

    /// <summary>
    /// Updates the power-up with the specified time delta.
    /// </summary>
    /// <param name="dlt">The time delta since the last update.</param>
    private void Update(float dlt) {
      dlt = Game.TotalElapsedGameTime - _lastUpdate;
      _lastUpdate = Game.TotalElapsedGameTime;

      // Check if the player is still valid
      if (Player == null) {
        Enabled = false;

        return;
      }

      // Check if the player is dead or removed
      if (Player.IsDead || Player.IsRemoved) {
        Enabled = false;

        return;
      }

      // Check if the power-up has timed out
      if (Time <= 0) {
        TimeOut();

        Enabled = false;

        return;
      }

      // Update the time left for the power-up
      Time -= dlt;

      // Invoke the virtual Update method
      Update(dlt, dlt / 1000);
    }

    /// <summary>
    /// Virtual method for actions upon activating the power-up.
    /// </summary>
    protected abstract void Activate();

    /// <summary>
    /// Virtual method for updating the power-up.
    /// </summary>
    /// <param name="dlt">The time delta since the last update.</param>
    /// <param name="dltSecs">The time delta in seconds since the last
    /// update.</param>
    public virtual void Update(float dlt, float dltSecs) {
      // Implement in derived classes
    }

    /// <summary>
    /// Virtual method called when the power-up times out.
    /// </summary>
    public virtual void TimeOut() {
      // Implement in derived classes
    }

    /// <summary>
    /// Virtual method called when the power-up is enabled or disabled. Called by
    /// the constructor.
    /// </summary>
    public virtual void OnEnabled(bool enabled) {
      // Implement in derived classes
    }
  }

  public static class AvailablePowerups {} // POWER-UPS SHOULD GO HERE!
}