[Added] Introduced a platform implementation status matrix so callers can distinguish current repo runtime support from broader backend capability contracts.
[Added] Implemented the embedded Windows NativeWebView control runtime with WebView2 child hosting, real native handles, and per-instance proxy application through AdditionalBrowserArguments.
[Added] Implemented the embedded Linux NativeWebView control runtime with GTK3/WebKitGTK child hosting on X11, real native handles, and per-instance proxy application through WebsiteDataManager.
[Fixed] Corrected platform status documentation and diagnostics so Windows, macOS, and Linux are reported as implemented embedded control targets and iOS becomes the next bring-up phase.
