import { Routes } from '@angular/router';
import { App } from './app';

export const FLOW_02_ROUTES: Routes = [
  {
    path: 'saas-multi-tenancy/questionnaire',
    component: App,
  },
  {
    path: 'saas-multi-tenancy/matching',
    component: App,
  },
  {
    path: 'saas-multi-tenancy/personalization',
    component: App,
  },
];

export const routes: Routes = FLOW_02_ROUTES;

