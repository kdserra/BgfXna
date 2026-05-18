import { dotnet } from './_framework/dotnet.js';

const canvas = document.getElementById('canvas');
if (canvas instanceof HTMLCanvasElement) {
  canvas.width = 1280;
  canvas.height = 720;
}

globalThis.BgfXna_showManagedError = message => {
  const text = String(message);
  console.error(text);
  document.body.innerHTML = `<pre style="margin:16px;color:#fff;background:#300;padding:16px;white-space:pre-wrap">${text}</pre>`;
};

function chooseBackend() {
  const requested = new URLSearchParams(globalThis.location.search).get('backend');
  if (requested === 'webgl' || requested === 'webgpu') {
    return requested;
  }

  return globalThis.navigator?.gpu ? 'webgpu' : 'webgl';
}

function describeError(error) {
  const lines = [];
  const seen = new Set();

  function append(value, label) {
    if (value === null || value === undefined) {
      return;
    }

    if (typeof value !== 'object') {
      lines.push(`${label}: ${String(value)}`);
      return;
    }

    if (seen.has(value)) {
      return;
    }

    seen.add(value);
    lines.push(`${label}: ${Object.prototype.toString.call(value)}`);

    for (const name of Object.getOwnPropertyNames(value)) {
      try {
        const property = value[name];
        if (typeof property === 'object' && property !== null) {
          append(property, `${label}.${name}`);
        } else {
          lines.push(`${label}.${name}: ${String(property)}`);
        }
      } catch (propertyError) {
        lines.push(`${label}.${name}: <unreadable: ${propertyError}>`);
      }
    }
  }

  append(error, 'error');
  if (error instanceof Error) {
    lines.unshift(error.stack || error.message || String(error));
  } else {
    lines.unshift(String(error));
  }

  return [...new Set(lines)].join('\n');
}

function showStartupError(error) {
  const message = describeError(error);
  console.error(error);
  document.body.innerHTML = `<pre style="margin:16px;color:#fff;background:#300;padding:16px;white-space:pre-wrap">${message}</pre>`;
}

globalThis.addEventListener('error', event => showStartupError(event.error || event.message));
globalThis.addEventListener('unhandledrejection', event => showStartupError(event.reason));

try {
  const { runMain, getConfig } = await dotnet
    .withModuleConfig({
      onRuntimeInitialized: function() {
        console.log("Emscripten runtime initialized. Applying robust keepalive patch...");
        const m = this || globalThis.Module || (typeof Module !== 'undefined' ? Module : null);
        if (m) {
          console.log("Found Emscripten Module object:", m);
          // 1. Manually push one keepalive to prevent underflow
          if (typeof m.runtimeKeepalivePush === 'function') {
            m.runtimeKeepalivePush();
            console.log("Successfully pushed initial keepalive.");
          }
          // 2. Wrap runtimeKeepalivePop in a try-catch to prevent aborting on underflow
          if (typeof m.runtimeKeepalivePop === 'function') {
            const originalPop = m.runtimeKeepalivePop;
            m.runtimeKeepalivePop = function() {
              try {
                return originalPop.apply(this, arguments);
              } catch (e) {
                console.warn("Caught and suppressed keepalive pop assertion:", e);
              }
            };
            console.log("Successfully wrapped runtimeKeepalivePop.");
          }
        } else {
          console.warn("Could not find Emscripten Module object to patch keepalive.");
        }
      }
    })
    .create();
  await runMain(getConfig().mainAssemblyName, [chooseBackend()]);
} catch (error) {
  showStartupError(error);
}
