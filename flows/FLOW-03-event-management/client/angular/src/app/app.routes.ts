import { Routes } from '@angular/router';
import { App } from './app';

export const FLOW_02_ROUTES: Routes = [
  {
    path: 'event-management/questionnaire',
    component: App,
  },
  {
    path: 'event-management/matching',
    component: App,
  },
  {
    path: 'event-management/personalization',
    component: App,
  },
];

export const routes: Routes = FLOW_02_ROUTES;

