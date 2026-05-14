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

  return 'webgl';
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
  const { runMain, getConfig } = await dotnet.create();
  await runMain(getConfig().mainAssemblyName, [chooseBackend()]);
} catch (error) {
  showStartupError(error);
}
