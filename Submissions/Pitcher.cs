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

  private readonly List < IPlayer > _bleeding = new List < IPlayer > ();

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
      foreach(IPlayer bleeding in Bleeding) {
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
    foreach(IObject obj in objs) {
      if (!obj.IsMissile && !TrackThrowArea
        .Intersects(obj.GetAABB()))
        continue;

      obj.SetLinearVelocity(obj.GetLinearVelocity() * THROW_VEL_MULT);
      obj.SetAngularVelocity(obj.GetAngularVelocity() * THROW_VEL_MULT);

      Game.PlayEffect(EffectName.TraceSpawner, Vector2.Zero,
        obj.UniqueID, EffectName.ItemGleam, 2 f);
    }
  }

  private void OnProjectileCreated(IProjectile[] projectiles) {
    foreach(IProjectile proj in projectiles) {
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