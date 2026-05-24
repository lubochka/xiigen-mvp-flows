import { Routes } from '@angular/router';
import { App } from './app';

export const FLOW_02_ROUTES: Routes = [
  {
    path: 'bfa-cross-flow-governance/questionnaire',
    component: App,
  },
  {
    path: 'bfa-cross-flow-governance/matching',
    component: App,
  },
  {
    path: 'bfa-cross-flow-governance/personalization',
    component: App,
  },
];

export const routes: Routes = FLOW_02_ROUTES;


