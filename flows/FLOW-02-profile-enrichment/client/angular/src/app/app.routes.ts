import { Routes } from '@angular/router';
import { App } from './app';

export const FLOW_02_ROUTES: Routes = [
  {
    path: 'profile-enrichment/questionnaire',
    component: App,
  },
  {
    path: 'profile-enrichment/matching',
    component: App,
  },
  {
    path: 'profile-enrichment/personalization',
    component: App,
  },
];

export const routes: Routes = FLOW_02_ROUTES;
