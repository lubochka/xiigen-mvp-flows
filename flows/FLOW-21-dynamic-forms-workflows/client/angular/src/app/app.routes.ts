import { Routes } from '@angular/router';
import { App } from './app';

export const FLOW_02_ROUTES: Routes = [
  {
    path: 'dynamic-forms-workflows/questionnaire',
    component: App,
  },
  {
    path: 'dynamic-forms-workflows/matching',
    component: App,
  },
  {
    path: 'dynamic-forms-workflows/personalization',
    component: App,
  },
];

export const routes: Routes = FLOW_02_ROUTES;

