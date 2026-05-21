import { Routes } from '@angular/router';
import { App } from './app';

export const FLOW_02_ROUTES: Routes = [
  {
    path: 'durable-sagas-compliance/questionnaire',
    component: App,
  },
  {
    path: 'durable-sagas-compliance/matching',
    component: App,
  },
  {
    path: 'durable-sagas-compliance/personalization',
    component: App,
  },
];

export const routes: Routes = FLOW_02_ROUTES;
