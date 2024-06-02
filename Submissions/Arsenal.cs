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
      HashSet < WeaponItemType > weaponItemTypes = new HashSet < WeaponItemType > {
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

      _stash = (IObjectAmmoStashTrigger) Game.CreateObject("AmmoStash00", ground);
    }
  }

  public override void TimeOut() {
    Game.PlaySound("DestroyMetal", Vector2.Zero, 1);
    Game.PlayEffect(EffectName.Sparks, Player.GetWorldPosition());
  }

  public override void Update(float dlt, float dltSecs) {
    // Give weapons for each empty slot
    foreach(WeaponItemType empty in EmptyWeaponItemTypes)
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