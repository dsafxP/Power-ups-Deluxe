// VORTEX - dsafxP
public class Vortex : Powerup {
  private const uint VORTEX_COOLDOWN = 250;
  private const float VORTEX_AREA_SIZE = 100;
  private const float VORTEX_FORCE = 5;

  private static readonly PlayerCommand _playerCommand = new PlayerCommand(PlayerCommandType.Fall);
  private static readonly Type[] _objTypes = {
    typeof (IObjectSupplyCrate),
    typeof (IObjectStreetsweeperCrate),
    typeof (IObjectWeaponItem)
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
      return Game.GetObjectsByArea < IPlayer > (VortexArea)
        .Where(p => (p.GetTeam() == PlayerTeam.Independent || p.GetTeam() != Player.GetTeam()) &&
          !p.IsDisabled && p != Player)
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

  public Vortex(IPlayer player): base(player) {
    Time = 17000; // 17 s
  }

  public override void Update(float dlt, float dltSecs) {
    if (Time % 50 == 0) // every 50ms
      Draw(Player.GetWorldPosition());

    if (Time % VORTEX_COOLDOWN == 0) { // every 250ms
      Game.DrawArea(VortexArea, Color.Red);

      foreach(IPlayer pulled in PlayersInVortex) {
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

      foreach(IObject pulled in ObjectsInVortex) {
        pulled.SetLinearVelocity(Vector2Helper.DirectionTo(pulled.GetWorldPosition(),
          Player.GetWorldPosition()) * VORTEX_FORCE);

        Game.PlaySound("PlayerDive", Vector2.Zero);
      }
    }
  }

  protected override void Activate() {}

  public override void TimeOut() {
    // Play sound effect indicating expiration of powerup
    Game.PlaySound("StrengthBoostStop", Vector2.Zero);
  }

  // This cool effect was made by Danger Ross!
  private void Draw(Vector2 pos) {
    PointShape.Swirl(
      (v => Game.PlayEffect(EffectName.ItemGleam,
        Vector2Helper.Rotated(v - pos,
          (float)(Time % 1500 * (MathHelper.TwoPI / 1500))) +
        pos)),
      pos, // Center Position
      5, // Initial Radius
      VORTEX_AREA_SIZE / 2, // End Radius
      2, // Rotations
      45 // Point count
    );
  }
}
