import { PublicClientApplication, LogLevel } from '@azure/msal-browser';

export const msalConfig = {
  auth: {
    clientId: 'd754916c-6408-4ecd-9dfd-68d64993ecae',
    authority:
      'https://login.microsoftonline.com/0784cbd8-29cf-49ad-ae0e-4ebd47bf32d1',
    redirectUri: 'https://salmon-rock-08a479500.3.azurestaticapps.net',
  },
  cache: {
    cacheLocation: 'localStorage',
    storeAuthStateInCookie: false,
  },
  system: {
    loggerOptions: {
      logLevel: LogLevel.Warning,
      loggerCallback: () => {},
    },
  },
};

export const msalInstance = new PublicClientApplication(msalConfig);
