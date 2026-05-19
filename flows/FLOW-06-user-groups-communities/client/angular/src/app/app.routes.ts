import { Routes } from '@angular/router';
import { App } from './app';

export const FLOW_02_ROUTES: Routes = [
  {
    path: 'user-groups-communities/questionnaire',
    component: App,
  },
  {
    path: 'user-groups-communities/matching',
    component: App,
  },
  {
    path: 'user-groups-communities/personalization',
    component: App,
  },
];

export const routes: Routes = FLOW_02_ROUTES;


