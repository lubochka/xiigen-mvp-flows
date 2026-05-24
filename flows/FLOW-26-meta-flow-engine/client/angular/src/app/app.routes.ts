import { Routes } from '@angular/router';
import { App } from './app';

export const FLOW_02_ROUTES: Routes = [
  {
    path: 'meta-flow-engine/questionnaire',
    component: App,
  },
  {
    path: 'meta-flow-engine/matching',
    component: App,
  },
  {
    path: 'meta-flow-engine/personalization',
    component: App,
  },
];

export const routes: Routes = FLOW_02_ROUTES;



