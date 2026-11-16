declare global {
  interface Window {
    __ASSETFLOW_CONFIG__?: {
      apiUrl: string;
    };
  }
}

export const environment = {
  production: true,
  apiUrl: typeof window === 'undefined'
    ? '/api'
    : window.__ASSETFLOW_CONFIG__?.apiUrl ?? '/api'
};
