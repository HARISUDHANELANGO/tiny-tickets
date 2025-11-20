import {
  PublicClientApplication,
  BrowserCacheLocation,
  LogLevel,
} from '@azure/msal-browser';

export const msalConfig = {
  auth: {
    clientId: 'd88dadfa-39af-4ef8-bcd5-8e9b60ce8fb8',
    authority:
      'https://login.microsoftonline.com/0784cbd8-29cf-49ad-ae0e-4ebd47bf32d1/v2.0',
    redirectUri: window.location.origin,
    navigateToLoginRequestUrl: false,
  },

  cache: {
    cacheLocation: BrowserCacheLocation.LocalStorage,
    storeAuthStateInCookie: false,
  },
};

export const protectedResources = {
  tinyTicketsApi: {
    endpoint:
      'https://tinytickets-api-hbh5h6etb4hvfjgw.centralindia-01.azurewebsites.net',
    scopes: ['api://9768865e-83af-4a37-8785-b3a547a4e220/access_as_user'],
  },
};

export const msalInstance = new PublicClientApplication(msalConfig);
