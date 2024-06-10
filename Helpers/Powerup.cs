using SFDGameScriptInterface;

namespace PowerupsDeluxe {
  public partial class GameScript : GameScriptInterface {
    /// <summary>
    /// Represents a base power-up that can be activated and updated over time.
    /// </summary>
    public abstract class Powerup {
      // Interval for the main update callback event
      private const uint COOLDOWN = 0;

      // Main update callback event
      private Events.UpdateCallback _updateCallback = null;

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
        Enabled = true;
        Activate();
      }

      /// <summary>
      /// Updates the power-up with the specified time delta.
      /// </summary>
      /// <param name="dlt">The time delta since the last update.</param>
      private void Update(float dlt) {
        // Check if the player is still valid
        if (Player == null || Player.IsRemoved || Player.IsDead) {
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
      public abstract void Update(float dlt, float dltSecs);

      /// <summary>
      /// Virtual method called when the power-up times out.
      /// </summary>
      public abstract void TimeOut();

      /// <summary>
      /// Virtual method called when the power-up is enabled or disabled. Called by
      /// the constructor.
      /// </summary>
      public abstract void OnEnabled(bool enabled);
    }
  }
}