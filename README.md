# Werfer

Werfer is a Clockify client that sucks less, simply _werfing_ time-tracking entries.

## Future work

- **CI: track known NativeAOT trim warnings.** The AOT publish (`task publish-aot`, run with
  `TrimmerSingleWarn=false`) reports known, accepted warnings, e.g. Serilog's IL2072 in
  `PropertyValueConverter.CreatePropertyValue` (`{@Object}` destructuring, unused here). The CI should
  compare them against a committed baseline and fail on any new one, instead of hiding them.
