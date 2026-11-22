# Known issues

Problems we know about. Listed so they aren't rediscovered, not a promise that they will be fixed.

## No recovery from a failed connection at startup

The saved API key is tried once, at launch. Started offline (before Wi-Fi or VPN is up), the timer bar stays disabled
and F5 only says "Not connected to Clockify": the user must open the settings and click Connect.

The projects are loaded by the connection itself: if they fail, the connection fails, and the previous one (if any)
stays. Projects created in Clockify only show after the next connection, from the settings or a restart.

Where: `TimerViewModel.RefreshAsync`, `TimeTrackingService.ConnectAsync`.
