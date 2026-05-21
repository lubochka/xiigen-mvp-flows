import { Routes } from '@angular/router';
import { App } from './app';

export const FLOW_02_ROUTES: Routes = [
  {
    path: 'visual-flow-engine/questionnaire',
    component: App,
  },
  {
    path: 'visual-flow-engine/matching',
    component: App,
  },
  {
    path: 'visual-flow-engine/personalization',
    component: App,
  },
];

export const routes: Routes = FLOW_02_ROUTES;
