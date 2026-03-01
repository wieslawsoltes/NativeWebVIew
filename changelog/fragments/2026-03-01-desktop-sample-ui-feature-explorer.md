[Added] Reworked `NativeWebView.Sample.Desktop` into an Avalonia desktop feature explorer that hosts the `NativeWebView` control and exposes navigation, script execution, web messaging, printing, focus movement, cookie/command manager checks, dialog operations, and authentication broker actions.
[Changed] Preserved deterministic backend contract checks by moving matrix logic into `DesktopSampleSmokeRunner` and adding `--smoke` mode.
[Changed] Updated desktop smoke script to run sample in smoke mode (`dotnet run ... -- --smoke`) while default sample run launches the interactive Avalonia UI.
[Docs] Added desktop sample run instructions to README and quickstart documentation.
