import { Routes } from '@angular/router';
import { App } from './app';

export const FLOW_02_ROUTES: Routes = [
  {
    path: 'friend-request-social-feed/questionnaire',
    component: App,
  },
  {
    path: 'friend-request-social-feed/matching',
    component: App,
  },
  {
    path: 'friend-request-social-feed/personalization',
    component: App,
  },
];

export const routes: Routes = FLOW_02_ROUTES;


