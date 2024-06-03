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

  public Clone(IPlayer player): base(player) {
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
      List < PlayerTeam > AvailableTeams = new List < PlayerTeam > {
        PlayerTeam.Team1,
        PlayerTeam.Team2,
        PlayerTeam.Team3,
        PlayerTeam.Team4
      };

      foreach(IPlayer player in Game.GetPlayers()) {
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