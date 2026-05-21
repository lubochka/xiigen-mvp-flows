import { Routes } from '@angular/router';
import { App } from './app';

export const FLOW_02_ROUTES: Routes = [
  {
    path: 'data-warehouse-analytics/questionnaire',
    component: App,
  },
  {
    path: 'data-warehouse-analytics/matching',
    component: App,
  },
  {
    path: 'data-warehouse-analytics/personalization',
    component: App,
  },
];

export const routes: Routes = FLOW_02_ROUTES;
