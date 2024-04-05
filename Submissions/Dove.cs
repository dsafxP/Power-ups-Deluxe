private static readonly Random _rng = new Random();

// SUPER DOVE - Luminous
public class Dove : Powerup {
  private const uint CLEAN_DELAY = 10000; // ms
  private const float ATTACK_COOLDOWN = 503;
  private const float SPEED = 4.35f;
  private const float DMG_MULT = 20;

  private static Events.PlayerDamageCallback DamageCallback;
  private static int DovesCount = 0;

  private int m_lastEggID;
  private int m_dialogueID;
  
  private IObject m_dove;
  private IObject m_block;
  
  private IObjectRevoluteJoint m_joint;
  
  private Vector2 m_lastSavedVelocity;
  private Vector2 m_lastPosition;
  
  private int m_facingDirection;
  
  private bool m_nameTagVisible;
  
  private CameraFocusMode m_focusMode;
  
  private float m_elapsed = ATTACK_COOLDOWN;
  private float m_eggTimeElapsed = 160;
  
  private Events.ObjectDamageCallback m_doveDamageCallback;

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

  public Dove(IPlayer player) : base(player) {
    Time = 10000;
  }

  private void EggAsMissile() {
    IObject egg = Game.GetObject(m_lastEggID);

    if (egg != null) {
      egg.TrackAsMissile(true);
      
      Events.UpdateCallback.Start((float _dlt) => {
        if (egg != null)
          egg.Destroy();
      }, CLEAN_DELAY, 1);
    }

    m_lastEggID = 0;
  }

  public override void Update(float dlt, float dltSecs) {
    if (m_dove == null || m_dove.IsRemoved) {
      Enabled = false;
      return;
    }

    m_lastPosition = m_dove.GetWorldPosition();

    if (m_lastEggID != 0) {
      m_eggTimeElapsed -= dlt;

      if (m_eggTimeElapsed <= 0) {
        EggAsMissile();
      }
    }

    m_elapsed -= dlt;

    if (m_elapsed <= 0) {
      m_eggTimeElapsed = 160;
      m_elapsed += ATTACK_COOLDOWN;
      Vector2 vector = m_lastPosition - new Vector2(0, 2);

      Game.PlayEffect("BulletHitCloth", vector);
      Game.PlaySound("Baseball", Vector2.Zero);

      IObject egg = Game.CreateObject("CrumpledPaper00", vector, 0, m_lastSavedVelocity, -m_facingDirection);

      egg.CustomID = "Egg";
      m_lastEggID = egg.UniqueID;
    }

    m_lastSavedVelocity = Vector2.Zero;
    bool left = Player.KeyPressed(VirtualKey.AIM_RUN_LEFT);

    if (left ^ Player.KeyPressed(VirtualKey.AIM_RUN_RIGHT)) {
      if (left) {
        m_lastSavedVelocity.X -= 1;
        m_facingDirection = -1;
      } else {
        m_lastSavedVelocity.X += 1;
        m_facingDirection = 1;
      }
    }

    if (Player.KeyPressed(VirtualKey.JUMP) || Player.KeyPressed(VirtualKey.AIM_CLIMB_UP)) {
      m_lastSavedVelocity.Y += 1;
    }

    if (Player.KeyPressed(VirtualKey.AIM_CLIMB_DOWN)) {
      m_lastSavedVelocity.Y -= 1;
    }
    
    // Bot support
    if (Player.IsBot) {
      m_lastSavedVelocity = new Vector2(_rng.Next(-1, 2), _rng.Next(-1, 2));
    }

    if (m_lastSavedVelocity == Vector2.Zero) {
      m_joint.SetTargetObjectA(m_dove);
    } else {
      m_joint.SetTargetObjectA(null);
      m_lastSavedVelocity = Vector2.Normalize(m_lastSavedVelocity) * SPEED;
    }

    m_dove.SetFaceDirection(m_facingDirection);
    m_dove.SetLinearVelocity(m_lastSavedVelocity);
  }

  private static void OnEggHit(IPlayer hit, PlayerDamageArgs args) {
    if (args.DamageType == PlayerDamageEventType.Missile) {
      IObject egg = Game.GetObject(args.SourceID);

      if (egg.CustomID == "Egg") {
        hit.DealDamage(args.Damage * DMG_MULT);

        Game.PlayEffect("CFTXT", egg.GetWorldPosition(), "*BAM*");

        egg.Destroy();
      }
    }
  }

  private void OnDoveDamage(IObject obj, ObjectDamageArgs args) {
    if (obj == m_dove) {
      m_dove.SetHealth(m_dove.GetMaxHealth());
      Player.DealDamage(args.Damage);
    }
  }

  protected override void Activate() {
    Game.PlaySound("Wings", Vector2.Zero);

    m_focusMode = Player.GetCameraSecondaryFocusMode();

    Player.SetCameraSecondaryFocusMode(CameraFocusMode.Ignore);

    m_dove = Game.CreateObject("Dove00", Player.GetWorldPosition() + new Vector2(0, 10));
    
    m_dove.SetTargetAIData(new ObjectAITargetData(500, Player.GetTeam())); // Targetable by bots
    
    m_block = Game.CreateObject("InvisibleBlock", new Vector2(100, 5000));
    m_joint = (IObjectRevoluteJoint) Game.CreateObject("RevoluteJoint", m_dove.GetWorldPosition());

    Player.SetWorldPosition(new Vector2(100, 5000));

    m_nameTagVisible = Player.GetNametagVisible();

    Player.SetNametagVisible(false);
    Player.SetInputMode(PlayerInputMode.ReadOnly);

    string name = Player.Name;

    if (name.Length > 10) {
      name = name.Substring(0, 10);
      name += "...";
    }

    m_dialogueID = Game.CreateDialogue(name, GetTeamColor(Player.GetTeam()), m_dove, "", 9900, false).ID;
  }

  public override void OnEnabled(bool enabled) {
    if (enabled) {
      m_doveDamageCallback = Events.ObjectDamageCallback.Start(OnDoveDamage);
      DovesCount++;

      if (DamageCallback == null) {
        //Game.ShowChatMessage("DMG CALLBACK ENABLED", Color.Red);
        DamageCallback = Events.PlayerDamageCallback.Start(OnEggHit);
      }

      return;
    }
    
    IDialogue diag = Game.GetDialogues()
    .FirstOrDefault(d => d.ID == m_dialogueID);
    
    if (diag != null) {
      diag.Close();
    }

    Game.PlaySound("StrengthBoostStop", Vector2.Zero);
    Game.PlaySound("Wings", Vector2.Zero);
    
    --DovesCount;

    if (DovesCount == 0) {
      //Game.ShowChatMessage("DMG CALLBACK DISABLED", Color.Red);
      DamageCallback.Stop();
      DamageCallback = null;
    }

    if (m_lastEggID != 0) {
      EggAsMissile();
    }

    m_doveDamageCallback.Stop();

    m_doveDamageCallback = null;
    
    m_block.Remove();
    m_dove.Remove();
    m_joint.Remove();

    Player.SetWorldPosition(m_lastPosition + new Vector2(0, 4));

    m_lastSavedVelocity.Normalize();

    Player.SetInputMode(PlayerInputMode.Enabled);
    Player.SetNametagVisible(m_nameTagVisible);
    Player.SetCameraSecondaryFocusMode(m_focusMode);
    Player.SetLinearVelocity(new Vector2(0, 2));
  }
  
  private static Color GetTeamColor(PlayerTeam team) {
    switch(team) {
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