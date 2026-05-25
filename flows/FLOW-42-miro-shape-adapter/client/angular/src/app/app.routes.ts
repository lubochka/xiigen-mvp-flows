import { Routes } from '@angular/router';
import { App } from './app';

export const FLOW_29_ROUTES: Routes = [
  {
    path: 'miro-shape-adapter/routing',
    component: App,
  },
  {
    path: 'miro-shape-adapter/graph',
    component: App,
  },
  {
    path: 'miro-shape-adapter/promotion',
    component: App,
  },
];

export const routes: Routes = FLOW_29_ROUTES;
