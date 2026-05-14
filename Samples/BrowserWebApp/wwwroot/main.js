import { dotnet } from './_framework/dotnet.js';

const canvas = document.getElementById('canvas');
if (canvas instanceof HTMLCanvasElement) {
  canvas.width = 1280;
  canvas.height = 720;
}

function chooseBackend() {
  const requested = new URLSearchParams(globalThis.location.search).get('backend');
  if (requested === 'webgl' || requested === 'webgpu') {
    return requested;
  }

  return globalThis.navigator && globalThis.navigator.gpu ? 'webgpu' : 'webgl';
}

try {
  const { runMain, getConfig } = await dotnet.create();
  await runMain(getConfig().mainAssemblyName, [chooseBackend()]);
} catch (error) {
  const message = error instanceof Error ? error.stack || error.message : String(error);
  console.error(message);
  document.body.innerHTML = `<pre style="margin:16px;color:#fff;background:#300;padding:16px;white-space:pre-wrap">${message}</pre>`;
}
