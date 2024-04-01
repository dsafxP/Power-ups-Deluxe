private static void PlayPowerupEffect(Vector2 pos) {
  Game.PlaySound("LogoSlam", pos, 1);
  Game.PlaySound("MuffledExplosion", pos, 1);

  Game.PlayEffect("EXP", pos);

  Game.PlayEffect("CAM_S", pos, 1f, 1000f, true);
}

public static void OnPowerupSyringe(TriggerArgs args) {
  const WeaponItem POWERUP_WEAPONITEM = WeaponItem.STRENGTHBOOST;

  IObjectActivateTrigger caller = args.Caller as IObjectActivateTrigger;
  IPlayer sender = args.Sender as IPlayer;

  Vector2 offset = new Vector2(0, 26);

  if (sender.CurrentPowerupItem.WeaponItem == POWERUP_WEAPONITEM) {
    Game.PlayEffect("CFTXT", sender.GetWorldPosition() + offset, "CAN'T PICKUP");

    return;
  }

  // Remove syringe
  caller.GetHighlightObject()
    .Remove();

  sender.GiveWeaponItem(POWERUP_WEAPONITEM);

  Game.PlayEffect("CFTXT", sender.GetWorldPosition() + offset, "POWER-UP BOOST");

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
          Type powerUpType = Powerups.GetRandomPowerupType();

          Powerups.Powerup powerUp = (Powerups.Powerup) Activator.CreateInstance(powerUpType, sender); // Activate random powerup

          Game.ShowChatMessage(string.Format("{0} - {1}", powerUp.Name, powerUp.Author), Color.Yellow, sender.UserIdentifier);

          PlayPowerupEffect(sender.GetWorldPosition());

          Game.PlayEffect("CFTXT", sender.GetWorldPosition() + offset, powerUp.Name);

          if (POWERUP_WEAPONITEM == WeaponItem.STRENGTHBOOST)
            sender.SetStrengthBoostTime(0);

          if (POWERUP_WEAPONITEM == WeaponItem.SPEEDBOOST)
            sender.SetSpeedBoostTime(0);

          weaponRemovedActionCallback.Stop();

          weaponRemovedActionCallback = null;
        }
      }
    });
}

public static class Powerups {
  private static readonly ObjectAITargetData boxTargetData = new ObjectAITargetData(500, ObjectAITargetMode.MeleeOnly);
  private static readonly Random _rng = new Random();
  private static readonly List < IObject > _boxes = new List < IObject > ();

  private static Events.ObjectCreatedCallback _objectCreatedCallback = null;
  private static Events.ObjectTerminatedCallback _objectTerminateCallback = null;

  public static bool Enabled {
    get {
      return _objectCreatedCallback != null && _objectTerminateCallback != null;
    }
    set {
      if (value != Enabled)
        if (value) {
          _objectCreatedCallback = Events.ObjectCreatedCallback.Start(OnObjectCreated);
          _objectTerminateCallback = Events.ObjectTerminatedCallback.Start(OnObjectTerminated);
        } else {
          _objectCreatedCallback.Stop();

          _objectCreatedCallback = null;

          _objectTerminateCallback.Stop();

          _objectTerminateCallback = null;
        }
    }
  }

  public static float SpawnChance = 100;

  public static IObject CreatePowerupBox(Vector2 pos) {
    // Create box
    IObject box = Game.CreateObject("CardboardBox00", pos);

    // Make box targetable by bots
    box.SetTargetAIData(boxTargetData);

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

    // Add to list
    _boxes.Add(box);

    return box;
  }

  public static IObject CreatePowerupSyringe(Vector2 pos) {
    const uint PICKUP_CHECK_COOLDOWN = 500;
    const float PICKUP_AREA = 25;
    Vector2 offset = new Vector2(2, 3); // effect offset

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
    syringe.SetTargetAIData(boxTargetData);

    // Make pickupable for bots
    Events.UpdateCallback updateCallback = null;

    updateCallback = Events.UpdateCallback.Start((float dlt) => {
      if (syringe == null) {
        updateCallback.Stop();

        return;
      }

      if (syringe.IsRemoved) {
        updateCallback.Stop();

        return;
      }

      // Play effect
      Game.PlayEffect("GLM", syringe.GetWorldPosition() + offset);

      // Get nearby bots
      Area area = syringe.GetAABB();

      area.SetDimensions(PICKUP_AREA, PICKUP_AREA);

      IPlayer activator = Game.GetObjectsByArea < IPlayer > (area)
        .FirstOrDefault(p => p.IsBot && !p.IsDead && p.IsInputEnabled);

      if (activator != null) // Activate if bot found
        OnPowerupSyringe(new TriggerArgs(activateTrigger, activator, false));
    }, PICKUP_CHECK_COOLDOWN);

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

  private static void OnObjectCreated(IObject[] objs) {
    // Get random supply crates
    foreach(IObject supplyCrate in objs
      .Where(o => o.Name == "SupplyCrate00" && _rng.Next(101) < SpawnChance)) {
      CreatePowerupBox(supplyCrate.GetWorldPosition());

      supplyCrate.Remove();
    }
  }

  private static void OnObjectTerminated(IObject[] objs) {
    // Get powerup boxes
    foreach(IObject box in objs
      .Where(o => _boxes.Contains(o))) {
      CreatePowerupSyringe(box.GetWorldPosition());

      Game.PlaySound("DestroyWood", Vector2.Zero);

      _boxes.Remove(box);
    }
  }

  /// <summary>
  /// Represents a base power-up that can be activated and updated over time.
  /// </summary>
  public abstract class Powerup {
    // Interval for the main update callback event
    private
    const uint COOLDOWN = 0;

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