using System.Runtime.Versioning;
using System.Runtime.InteropServices.JavaScript;

namespace NativeWebView.Platform.Browser;

[SupportedOSPlatform("browser")]
internal static partial class BrowserNativeWebViewInterop
{
    private static bool s_installed;

    private const string InstallScript = """
        (() => {
          if (globalThis.__nativeWebViewBrowser) {
            return;
          }

          const eventMarker = "__nativeWebViewEvent";
          const hostMessageMarker = "__nativeWebViewHostMessage";

          const safeString = (value) => value == null ? "" : String(value);
          const safeJson = (value) => {
            try {
              return JSON.stringify(value);
            } catch (error) {
              return JSON.stringify(safeString(value));
            }
          };

          const installBridge = (frame) => {
            let frameWindow = null;
            try {
              frameWindow = frame.contentWindow;
            } catch (error) {
              return false;
            }

            if (!frameWindow) {
              return false;
            }

            try {
              const chromeRoot = frameWindow.chrome = frameWindow.chrome || {};
              const webview = chromeRoot.webview = chromeRoot.webview || {};
              if (webview.__nativeBridgeReady) {
                return true;
              }

              webview.__nativeBridgeReady = true;
              const listeners = webview.__listeners = webview.__listeners || [];

              webview.postMessage = (message) => {
                if (typeof message === "string") {
                  frameWindow.parent.postMessage({
                    [eventMarker]: true,
                    kind: "message",
                    format: "string",
                    value: message
                  }, "*");
                  return;
                }

                frameWindow.parent.postMessage({
                  [eventMarker]: true,
                  kind: "message",
                  format: "json",
                  value: safeJson(message)
                }, "*");
              };

              webview.addEventListener = (type, listener) => {
                if (type !== "message" || typeof listener !== "function" || listeners.includes(listener)) {
                  return;
                }

                listeners.push(listener);
              };

              webview.removeEventListener = (type, listener) => {
                if (type !== "message") {
                  return;
                }

                const index = listeners.indexOf(listener);
                if (index >= 0) {
                  listeners.splice(index, 1);
                }
              };

              webview.__dispatchMessage = (format, value) => {
                let payload = value;
                if (format === "json") {
                  try {
                    payload = JSON.parse(value);
                  } catch (error) {
                    payload = value;
                  }
                }

                const event = { data: payload };
                for (const listener of [...listeners]) {
                  try {
                    listener(event);
                  } catch (error) {
                    console.error(error);
                  }
                }

                frameWindow.dispatchEvent(new MessageEvent("message", { data: payload }));
              };

              if (!frameWindow.__nativeWebViewBridgeParentListener) {
                frameWindow.__nativeWebViewBridgeParentListener = (event) => {
                  const data = event.data;
                  if (!data || data[hostMessageMarker] !== true || event.source !== frameWindow.parent) {
                    return;
                  }

                  webview.__dispatchMessage(data.kind === "json" ? "json" : "string", data.value);
                };

                frameWindow.addEventListener("message", frameWindow.__nativeWebViewBridgeParentListener);
              }

              if (!frameWindow.__nativeWebViewOriginalOpen) {
                frameWindow.__nativeWebViewOriginalOpen = typeof frameWindow.open === "function"
                  ? frameWindow.open.bind(frameWindow)
                  : null;
              }

              const dispatchNewWindow = (url) => {
                frameWindow.parent.postMessage({
                  [eventMarker]: true,
                  kind: "newWindow",
                  url: safeString(url)
                }, "*");
              };

              const createPopupProxy = (initialUrl) => {
                let currentUrl = safeString(initialUrl);
                const navigate = (nextUrl) => {
                  currentUrl = safeString(nextUrl);
                  dispatchNewWindow(currentUrl);
                };

                const location = {
                  assign: (value) => navigate(value),
                  replace: (value) => navigate(value),
                  toString: () => currentUrl,
                  valueOf: () => currentUrl
                };

                Object.defineProperty(location, "href", {
                  configurable: true,
                  enumerable: true,
                  get: () => currentUrl,
                  set: (value) => navigate(value)
                });

                const popupProxy = {
                  blur: () => {},
                  close: () => {
                    popupProxy.closed = true;
                  },
                  closed: false,
                  focus: () => {},
                  opener: frameWindow,
                  postMessage: () => {}
                };

                Object.defineProperty(popupProxy, "location", {
                  configurable: true,
                  enumerable: true,
                  get: () => location,
                  set: (value) => navigate(value)
                });

                return popupProxy;
              };

              frameWindow.open = (url, target, features) => {
                const normalizedUrl = safeString(url);
                dispatchNewWindow(normalizedUrl);
                return createPopupProxy(normalizedUrl);
              };

              return true;
            } catch (error) {
              return false;
            }
          };

          const normalizeMessage = (data) => {
            if (data && typeof data === "object" && data[eventMarker] === true) {
              return JSON.stringify(data);
            }

            if (typeof data === "string") {
              return JSON.stringify({
                kind: "message",
                format: "string",
                value: data
              });
            }

            return JSON.stringify({
              kind: "message",
              format: "json",
              value: safeJson(data)
            });
          };

          globalThis.__nativeWebViewBrowser = {
            createFrame: () => {
              const frame = globalThis.document.createElement("iframe");
              frame.style.border = "0";
              frame.style.margin = "0";
              frame.style.padding = "0";
              frame.style.width = "100%";
              frame.style.height = "100%";
              frame.style.display = "block";
              frame.style.background = "transparent";
              frame.referrerPolicy = "strict-origin-when-cross-origin";
              frame.src = "about:blank";
              return frame;
            },
            releaseFrame: (frame) => {
              if (!frame) {
                return;
              }

              try {
                frame.src = "about:blank";
              } catch (error) {
              }
            },
            subscribeFrameEvents: (frame, onLoad, onMessage) => {
              const loadHandler = () => {
                installBridge(frame);
                if (typeof onLoad === "function") {
                  onLoad(globalThis.__nativeWebViewBrowser.getCurrentUrl(frame));
                }
              };

              const messageHandler = (event) => {
                try {
                  if (!frame || event.source !== frame.contentWindow) {
                    return;
                  }
                } catch (error) {
                  return;
                }

                if (typeof onMessage === "function") {
                  onMessage(normalizeMessage(event.data));
                }
              };

              frame.addEventListener("load", loadHandler);
              globalThis.addEventListener("message", messageHandler);
              return { frame, loadHandler, messageHandler };
            },
            unsubscribeFrameEvents: (subscription) => {
              if (!subscription) {
                return;
              }

              if (subscription.frame && subscription.loadHandler) {
                subscription.frame.removeEventListener("load", subscription.loadHandler);
              }

              if (subscription.messageHandler) {
                globalThis.removeEventListener("message", subscription.messageHandler);
              }
            },
            navigate: (frame, url) => {
              frame.src = safeString(url);
            },
            reload: (frame) => {
              try {
                frame.contentWindow.location.reload();
              } catch (error) {
                try {
                  const current = frame.src;
                  frame.src = current;
                } catch (innerError) {
                }
              }
            },
            stop: (frame) => {
              try {
                frame.contentWindow.stop();
              } catch (error) {
              }
            },
            goBack: (frame) => {
              try {
                frame.contentWindow.history.back();
              } catch (error) {
              }
            },
            goForward: (frame) => {
              try {
                frame.contentWindow.history.forward();
              } catch (error) {
              }
            },
            focus: (frame) => {
              try {
                frame.focus();
              } catch (error) {
              }
            },
            setZoomFactor: (frame, zoomFactor) => {
              const normalized = Number.isFinite(zoomFactor) && zoomFactor > 0 ? zoomFactor : 1;
              frame.style.zoom = normalized === 1 ? "" : String(normalized);
            },
            getCurrentUrl: (frame) => {
              try {
                if (frame.contentWindow && frame.contentWindow.location) {
                  return frame.contentWindow.location.href || frame.src || "";
                }
              } catch (error) {
              }

              try {
                return frame.src || "";
              } catch (error) {
                return "";
              }
            },
            executeScript: (frame, script) => {
              const frameWindow = frame.contentWindow;
              if (!frameWindow) {
                return null;
              }

              const result = frameWindow.eval(script);
              if (typeof result === "undefined") {
                return null;
              }

              try {
                return JSON.stringify(result);
              } catch (error) {
                return JSON.stringify(safeString(result));
              }
            },
            postWebMessage: (frame, kind, value) => {
              const frameWindow = frame.contentWindow;
              if (!frameWindow) {
                return;
              }

              frameWindow.postMessage({
                [hostMessageMarker]: true,
                kind: kind === "json" ? "json" : "string",
                value
              }, "*");
            },
            openPopup: (url, title) => {
              const popup = globalThis.open(
                safeString(url),
                safeString(title) || "_blank",
                "popup=yes,width=480,height=720,resizable=yes,scrollbars=yes");
              return popup || null;
            },
            closePopup: (popup) => {
              if (!popup) {
                return;
              }

              try {
                popup.close();
              } catch (error) {
              }
            },
            isPopupClosed: (popup) => {
              try {
                return !popup || popup.closed === true;
              } catch (error) {
                return true;
              }
            },
            getPopupUrl: (popup) => {
              try {
                if (!popup || !popup.location) {
                  return "";
                }

                return popup.location.href || "";
              } catch (error) {
                return null;
              }
            },
            getHostOrigin: () => {
              try {
                return globalThis.location && globalThis.location.origin
                  ? globalThis.location.origin
                  : "";
              } catch (error) {
                return "";
              }
            }
          };
        })();
        """;

    public static void EnsureInstalled()
    {
        if (s_installed)
        {
            return;
        }

        Evaluate(InstallScript);
        s_installed = true;
    }

    [JSImport("globalThis.eval")]
    private static partial void Evaluate(string script);

    [JSImport("globalThis.__nativeWebViewBrowser.createFrame")]
    public static partial JSObject CreateFrame();

    [JSImport("globalThis.__nativeWebViewBrowser.releaseFrame")]
    public static partial void ReleaseFrame(JSObject frame);

    [JSImport("globalThis.__nativeWebViewBrowser.subscribeFrameEvents")]
    public static partial JSObject SubscribeFrameEvents(
        JSObject frame,
        [JSMarshalAs<JSType.Function<JSType.String>>] Action<string?> onLoad,
        [JSMarshalAs<JSType.Function<JSType.String>>] Action<string> onMessage);

    [JSImport("globalThis.__nativeWebViewBrowser.unsubscribeFrameEvents")]
    public static partial void UnsubscribeFrameEvents(JSObject subscription);

    [JSImport("globalThis.__nativeWebViewBrowser.navigate")]
    public static partial void Navigate(JSObject frame, string url);

    [JSImport("globalThis.__nativeWebViewBrowser.reload")]
    public static partial void Reload(JSObject frame);

    [JSImport("globalThis.__nativeWebViewBrowser.stop")]
    public static partial void Stop(JSObject frame);

    [JSImport("globalThis.__nativeWebViewBrowser.goBack")]
    public static partial void GoBack(JSObject frame);

    [JSImport("globalThis.__nativeWebViewBrowser.goForward")]
    public static partial void GoForward(JSObject frame);

    [JSImport("globalThis.__nativeWebViewBrowser.focus")]
    public static partial void Focus(JSObject frame);

    [JSImport("globalThis.__nativeWebViewBrowser.setZoomFactor")]
    public static partial void SetZoomFactor(JSObject frame, double zoomFactor);

    [JSImport("globalThis.__nativeWebViewBrowser.getCurrentUrl")]
    public static partial string? GetCurrentUrl(JSObject frame);

    [JSImport("globalThis.__nativeWebViewBrowser.executeScript")]
    public static partial string? ExecuteScript(JSObject frame, string script);

    [JSImport("globalThis.__nativeWebViewBrowser.postWebMessage")]
    public static partial void PostWebMessage(JSObject frame, string kind, string value);

    [JSImport("globalThis.__nativeWebViewBrowser.openPopup")]
    public static partial JSObject? OpenPopup(string url, string title);

    [JSImport("globalThis.__nativeWebViewBrowser.closePopup")]
    public static partial void ClosePopup(JSObject popup);

    [JSImport("globalThis.__nativeWebViewBrowser.isPopupClosed")]
    public static partial bool IsPopupClosed(JSObject popup);

    [JSImport("globalThis.__nativeWebViewBrowser.getPopupUrl")]
    public static partial string? GetPopupUrl(JSObject popup);

    [JSImport("globalThis.__nativeWebViewBrowser.getHostOrigin")]
    public static partial string? GetHostOrigin();
}
