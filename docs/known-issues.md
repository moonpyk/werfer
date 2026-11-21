# Known issues

Problems we know about. Listed so they aren't rediscovered, not a promise that they will be fixed.

## No recovery from a failed connection at startup

The saved API key is tried once, at launch. Started offline (before Wi-Fi or VPN is up), the timer bar stays disabled
and F5 only says "Not connected to Clockify": the user must open the settings and click Connect.

Same when the connection works but the projects fail to load: F5 never reloads them, so the bar stays disabled. For
the same reason, projects created in Clockify only show after a restart.

Where: `TimerViewModel.RefreshAsync`, `TimeTrackingService.ConnectAsync`.
