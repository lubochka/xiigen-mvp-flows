import { Routes } from '@angular/router';
import { App } from './app';

export const FLOW_44_ROUTES: Routes = [
  {
    path: 'history-bootstrap/filter',
    component: App,
  },
  {
    path: 'history-bootstrap/style',
    component: App,
  },
  {
    path: 'history-bootstrap/storage',
    component: App,
  },
];

export const routes: Routes = FLOW_44_ROUTES;

