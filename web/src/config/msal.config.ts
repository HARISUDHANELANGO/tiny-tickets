import {
  PublicClientApplication,
  BrowserCacheLocation,
  LogLevel,
} from '@azure/msal-browser';

export const msalConfig = {
  auth: {
    clientId: 'd754916c-6408-4ecd-9dfd-68d64993ecae',
    authority:
      'https://login.microsoftonline.com/0784cbd8-29cf-49ad-ae0e-4ebd47bf32d1',
    redirectUri: window.location.origin,
  },
  cache: {
    cacheLocation: BrowserCacheLocation.LocalStorage,
    storeAuthStateInCookie: false,
  },
  system: {
    loggerOptions: {
      logLevel: LogLevel.Warning,
      loggerCallback: () => {},
    },
  },
};

export const protectedResources = {
  tinyTicketsApi: {
    endpoint:
      'https://tinytickets-api-hbh5h6etb4hvfjgw.centralindia-01.azurewebsites.net',
    scopes: ['api://f2cea967-6192-44ae-aedc-1e6b6a994e5e/access_as_user'],
  },
};

export const msalInstance = new PublicClientApplication(msalConfig);
