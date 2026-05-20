import { Routes } from '@angular/router';
import { App } from './app';

export const FLOW_02_ROUTES: Routes = [
  {
    path: 'marketplace/questionnaire',
    component: App,
  },
  {
    path: 'marketplace/matching',
    component: App,
  },
  {
    path: 'marketplace/personalization',
    component: App,
  },
];

export const routes: Routes = FLOW_02_ROUTES;



