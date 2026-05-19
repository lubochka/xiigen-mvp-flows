import { Routes } from '@angular/router';
import { App } from './app';

export const FLOW_02_ROUTES: Routes = [
  {
    path: 'event-attendance/questionnaire',
    component: App,
  },
  {
    path: 'event-attendance/matching',
    component: App,
  },
  {
    path: 'event-attendance/personalization',
    component: App,
  },
];

export const routes: Routes = FLOW_02_ROUTES;

