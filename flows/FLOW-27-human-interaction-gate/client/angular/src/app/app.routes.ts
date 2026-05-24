import { Routes } from '@angular/router';
import { App } from './app';

export const FLOW_02_ROUTES: Routes = [
  {
    path: 'human-interaction-gate/questionnaire',
    component: App,
  },
  {
    path: 'human-interaction-gate/matching',
    component: App,
  },
  {
    path: 'human-interaction-gate/personalization',
    component: App,
  },
];

export const routes: Routes = FLOW_02_ROUTES;



