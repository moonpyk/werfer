# Werfer

Werfer is a Clockify client that sucks less, simply _werfing_ time-tracking entries.

## Keyboard shortcuts

| Shortcut     | Action                                                                                              |
| ------------ | --------------------------------------------------------------------------------------------------- |
| `Ctrl+Space` | Start the timer, or stop it when it runs                                                            |
| `F5`         | Fetch the running entry from Clockify (it may have been started or stopped elsewhere)               |
| `Ctrl+L`     | Clear the description (timer page)                                                                  |
| `Ctrl+N`     | New blank entry: stop the timer if it runs, clear the description and the project/task (timer page) |
| `Enter`      | New line in the description                                                                         |

The description being typed is saved first, so `Ctrl+Space` right after typing keeps it.

## Future work

- **CI: track known NativeAOT trim warnings.** The AOT publish (`task publish-aot`, run with
  `TrimmerSingleWarn=false`) reports known, accepted warnings, e.g. Serilog's IL2072 in
  `PropertyValueConverter.CreatePropertyValue` (`{@Object}` destructuring, unused here). The CI should
  compare them against a committed baseline and fail on any new one, instead of hiding them.
