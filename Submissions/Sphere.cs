// SPHERE - dsafxP
public class Sphere : Powerup {
  private const uint EFFECT_COOLDOWN = 50;
  private const float SPHERE_SIZE = 100;

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

  private IObject[] MissilesInSphere {
    get {
      return Game.GetObjectsByArea(SphereArea)
        .Where(o => o.IsMissile)
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

  public Sphere(IPlayer player): base(player) {
    Time = 24000; // 24 s
  }

  public override void Update(float dlt, float dltSecs) {
    if (Time % EFFECT_COOLDOWN == 0) {
      Draw(Player.GetWorldPosition());

      Game.DrawArea(SphereArea, Color.Red);
    }

    foreach(IProjectile projs in ProjectilesInSphere) {
      projs.Direction *= -1;
      projs.CritChanceDealtModifier = 100;
      projs.PowerupBounceActive = true;

      Game.PlayEffect("Electric", projs.Position);
      Game.PlaySound("ShellBounce", Vector2.Zero, 1);
      Game.PlaySound("ElectricSparks", Vector2.Zero, 1);
    }
  }

  public override void TimeOut() {
    // Play sound effect indicating expiration of powerup
    Game.PlaySound("StrengthBoostStop", Vector2.Zero);
  }

  protected override void Activate() {}

  private void Draw(Vector2 pos) {
    PointShape.Circle(v => {
      Game.PlayEffect("GLM", Vector2Helper.Rotated(v - pos,
          (float)(Time % 1500 * (MathHelper.TwoPI / 1500))) +
        pos);
    }, pos, SPHERE_SIZE / 2, 45);
  }

  private PlayerTeam GetTeamOrDefault(IPlayer player,
    PlayerTeam defaultTeam = PlayerTeam.Independent) {
    return player != null ? player.GetTeam() : defaultTeam;
  }
}