import { Routes } from '@angular/router';
import { App } from './app';

export const FLOW_02_ROUTES: Routes = [
  {
    path: 'completion-gamification/questionnaire',
    component: App,
  },
  {
    path: 'completion-gamification/matching',
    component: App,
  },
  {
    path: 'completion-gamification/personalization',
    component: App,
  },
];

export const routes: Routes = FLOW_02_ROUTES;


