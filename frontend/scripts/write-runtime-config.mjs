import { writeFile } from 'node:fs/promises';
import { resolve } from 'node:path';

const configuredApiUrl = process.env.ASSETFLOW_API_URL?.trim();
const isVercelBuild = process.env.VERCEL === '1';

if (isVercelBuild && !configuredApiUrl) {
  throw new Error('ASSETFLOW_API_URL must be set in the Vercel project environment.');
}

let apiUrl = '/api';

if (configuredApiUrl) {
  let parsedApiUrl;
  try {
    parsedApiUrl = new URL(configuredApiUrl);
  } catch {
    throw new Error('ASSETFLOW_API_URL must be an absolute HTTP or HTTPS URL ending in /api.');
  }

  if (
    !['http:', 'https:'].includes(parsedApiUrl.protocol) ||
    parsedApiUrl.username ||
    parsedApiUrl.password ||
    parsedApiUrl.search ||
    parsedApiUrl.hash ||
    !parsedApiUrl.pathname.replace(/\/+$/, '').endsWith('/api')
  ) {
    throw new Error('ASSETFLOW_API_URL must be an absolute HTTP or HTTPS URL ending in /api, without credentials or query parameters.');
  }

  apiUrl = configuredApiUrl.replace(/\/+$/, '');
}

const runtimeConfig = `window.__ASSETFLOW_CONFIG__ = Object.freeze(${JSON.stringify({ apiUrl })});\n`;
await writeFile(resolve('public/runtime-config.js'), runtimeConfig, 'utf8');
console.log(`Generated frontend runtime configuration for ${apiUrl}.`);
