import { Routes } from '@angular/router';
import { App } from './app';

export const FLOW_02_ROUTES: Routes = [
  {
    path: 'ads-platform/questionnaire',
    component: App,
  },
  {
    path: 'ads-platform/matching',
    component: App,
  },
  {
    path: 'ads-platform/personalization',
    component: App,
  },
];

export const routes: Routes = FLOW_02_ROUTES;
