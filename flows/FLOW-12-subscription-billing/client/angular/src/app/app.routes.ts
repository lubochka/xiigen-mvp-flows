import { Routes } from '@angular/router';
import { App } from './app';

export const FLOW_02_ROUTES: Routes = [
  {
    path: 'subscription-billing/questionnaire',
    component: App,
  },
  {
    path: 'subscription-billing/matching',
    component: App,
  },
  {
    path: 'subscription-billing/personalization',
    component: App,
  },
];

export const routes: Routes = FLOW_02_ROUTES;
