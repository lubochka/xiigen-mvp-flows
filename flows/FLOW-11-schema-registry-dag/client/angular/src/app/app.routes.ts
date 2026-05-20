import { Routes } from '@angular/router';
import { App } from './app';

export const FLOW_02_ROUTES: Routes = [
  {
    path: 'schema-registry-dag/questionnaire',
    component: App,
  },
  {
    path: 'schema-registry-dag/matching',
    component: App,
  },
  {
    path: 'schema-registry-dag/personalization',
    component: App,
  },
];

export const routes: Routes = FLOW_02_ROUTES;
