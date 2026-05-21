import { Routes } from '@angular/router';
import { App } from './app';

export const FLOW_02_ROUTES: Routes = [
  {
    path: 'etl-data-integration/questionnaire',
    component: App,
  },
  {
    path: 'etl-data-integration/matching',
    component: App,
  },
  {
    path: 'etl-data-integration/personalization',
    component: App,
  },
];

export const routes: Routes = FLOW_02_ROUTES;
