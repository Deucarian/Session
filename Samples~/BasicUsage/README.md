# Basic Session Usage

Open `BasicUsage.unity` and enter Play Mode. The scene uses local fake login and
refresh services to exercise session creation, restore, refresh, logout, and
store clearing without an API dependency.

Use the controller's inspector actions or wire its public methods to your own UI.
The sample stores only its isolated demonstration session in `PlayerPrefs`; clear
the sample store when you are finished testing restore behavior.
