[Added] Implemented real `NativeWebDialog` runtime paths on Windows and Linux by hosting the existing WebView2/WebKitGTK runtimes inside native desktop dialog windows.
[Added] Replaced `WebAuthenticationBroker` stubs with real runtime-backed authentication flows on Windows, macOS, Linux, iOS, Android, and Browser.
[Fixed] Updated the platform implementation-status matrix, docs, and tests so dialog/auth support is reported as runtime-implemented where the repo now ships real native paths.
