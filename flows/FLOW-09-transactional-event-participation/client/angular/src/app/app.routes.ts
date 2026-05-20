import { Routes } from '@angular/router';
import { App } from './app';

export const FLOW_02_ROUTES: Routes = [
  {
    path: 'transactional-event-participation/questionnaire',
    component: App,
  },
  {
    path: 'transactional-event-participation/matching',
    component: App,
  },
  {
    path: 'transactional-event-participation/personalization',
    component: App,
  },
];

export const routes: Routes = FLOW_02_ROUTES;
