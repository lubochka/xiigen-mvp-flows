import { Routes } from '@angular/router';
import { App } from './app';

export const FLOW_02_ROUTES: Routes = [
  {
    path: 'marketplace-payments/questionnaire',
    component: App,
  },
  {
    path: 'marketplace-payments/matching',
    component: App,
  },
  {
    path: 'marketplace-payments/personalization',
    component: App,
  },
];

export const routes: Routes = FLOW_02_ROUTES;
