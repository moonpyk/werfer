# Werfer

Werfer is a Clockify client that sucks less, simply _werfing_ time-tracking entries.

Werfer was born out of frustration. Hopefully it makes the people who use it a little happier.

## Keyboard shortcuts

| Shortcut     | Action                                                                                              |
|--------------|-----------------------------------------------------------------------------------------------------|
| `Ctrl+Space` | Start the timer, or stop it when it runs                                                            |
| `F5`         | Fetch the running entry and the week's history from Clockify (they may have changed elsewhere)      |
| `Ctrl+L`     | Clear the description (timer page)                                                                  |
| `Ctrl+N`     | New blank entry: stop the timer if it runs, clear the description and the project/task (timer page) |
| `Ctrl+,`     | Open the settings                                                                                   |
| `Enter`      | New line in the description; saves an edit or a split, applies a new start time                     |
| `Escape`     | Close an edit or a split without saving                                                             |

The description being typed is saved first, so `Ctrl+Space` right after typing keeps it. The same list is on the
About page, in the `…` menu.

## Future work

- **CI: track known NativeAOT trim warnings.** The AOT publish (`task publish-aot`, run with
  `TrimmerSingleWarn=false`) reports known, accepted warnings, e.g. Serilog's IL2072 in
  `PropertyValueConverter.CreatePropertyValue` (`{@Object}` destructuring, unused here). The CI should
  compare them against a committed baseline and fail on any new one, instead of hiding them.

## Out of scope

Werfer starts and stops entries, nothing more. These will not be added, whatever other desktop clients do:

- **Automatic time tracking:** recording which apps and websites are used to suggest entries. Logging everything
  someone does on their computer is intrusive, not far from spying on them: a time tracker has no business doing it.
- **Idle detection:** asking what to do with the time spent away from the keyboard.
- **Pomodoro timer and break reminders.**
- **Screenshots and activity monitoring.**
- **Reports, invoices, expenses, approvals and scheduling:** the Clockify web app does them.
- **Offline mode:** starting a timer without network and syncing later. Merging entries changed on both sides is a
  minefield.

## Pronunciation

The app name comes from the German word _Werfer_ (as in _Flammenwerfer_), which is pronounced _verfeur_ in French.

## Sponsor

The development of Werfer is sponsored by [repply](https://www.repply.fr), my employer, who gave me some time and
resources to work on it.
