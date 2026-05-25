import { CommonModule, DOCUMENT } from '@angular/common';
import { Component, Inject, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

type ViewerRole = 'platform_admin' | 'platform_support' | 'tenant_admin';
type Language = 'en' | 'he';
type ScreenState = 'connections' | 'bridge' | 'keepalive' | 'audit';

interface PushRow {
  label: string;
  state: string;
  detail: string;
}

interface PushSignal {
  label: string;
  value: string;
  detail: string;
}

@Component({
  selector: 'app-root',
  imports: [CommonModule, FormsModule],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  readonly roles: ViewerRole[] = ['platform_admin', 'platform_support', 'tenant_admin'];
  readonly role = signal<ViewerRole>('platform_admin');
  readonly language = signal<Language>('en');
  readonly screenState = signal<ScreenState>('connections');
  readonly moduleName = signal('Client Push');
  readonly readinessScore = signal(94);

  readonly pushRows: PushRow[] = [
    { label: 'SSE connection manager', state: 'tenant scoped', detail: 'Correlation ids bind every browser connection to the current TenantContext before delivery can happen.' },
    { label: 'Flow event bridge', state: 'guarded', detail: 'Platform events can reach registered clients only after outbox append and user-facing domain checks.' },
    { label: 'Keepalive scheduler', state: 'self cleaning', detail: 'Dropped connections are removed before heartbeat pings are emitted to active clients.' }
  ];

  readonly pushSignals: PushSignal[] = [
    { label: 'Task types', value: '3', detail: 'connection manager, event bridge, and keepalive scheduler' },
    { label: 'Delivery rules', value: '6', detail: 'domain isolation, tenant scoping, concurrency, replay, validation, and retained timeout evidence' },
    { label: 'Transport', value: 'SSE', detail: 'SignalR is out of scope for parity; the source flow is server-sent events' }
  ];

  readonly pageTitle = computed(() => this.language() === 'he' ? 'גשר עדכוני לקוח' : this.moduleName());
  readonly roleTestId = computed(() => `role-${this.role().replaceAll('_', '-')}-client-push`);
  readonly publicView = computed(() => false);
  readonly canOperate = computed(() => this.role() === 'platform_admin' || this.role() === 'tenant_admin');
  readonly canInspect = computed(() => true);

  constructor(@Inject(DOCUMENT) private readonly documentRef: Document) {
    const params = new URLSearchParams(this.documentRef.defaultView?.location.search ?? '');
    const roleParam = params.get('role') as ViewerRole | null;
    const stateParam = params.get('state') as ScreenState | null;
    const langParam = params.get('lang') as Language | null;
    const moduleParam = params.get('module');
    if (roleParam && this.roles.includes(roleParam)) this.role.set(roleParam);
    if (stateParam && ['connections', 'bridge', 'keepalive', 'audit'].includes(stateParam)) this.screenState.set(stateParam);
    this.applyLanguage(langParam === 'he' ? 'he' : 'en');
    if (moduleParam) this.moduleName.set(moduleParam);
  }

  setRole(value: ViewerRole): void {
    this.role.set(value);
  }

  setScreenState(value: ScreenState): void {
    this.screenState.set(value);
  }

  applyLanguage(value: Language): void {
    this.language.set(value);
    this.documentRef.documentElement.lang = value;
    this.documentRef.documentElement.dir = value === 'he' ? 'rtl' : 'ltr';
  }

  translated(label: string): string {
    if (this.language() === 'en') return label;
    const dictionary: Record<string, string> = {
      'Client push console': 'מסוף עדכוני לקוח',
      'Role': 'תפקיד',
      'Language': 'שפה',
      'Connections': 'חיבורים',
      'Bridge': 'גשר',
      'Keepalive': 'פעימות',
      'Audit': 'בקרה',
      'Active channels': 'ערוצים פעילים',
      'Delivery checks': 'בדיקות מסירה',
      'Readiness': 'מוכנות'
    };
    return dictionary[label] ?? label;
  }

  summary(): string {
    if (this.role() === 'tenant_admin') return 'Tenant admins can register tenant-scoped SSE channels, inspect delivery outcomes, and run keepalive checks for their tenant.';
    if (this.role() === 'platform_support') return 'Platform support can inspect connection health, retained timeout evidence, and delivery audit trails without mutation controls.';
    return 'Platform admins manage Client Push connections, event bridging, keepalive cleanup, and tenant-safe delivery audit across the platform.';
  }

  roleLabel(value: ViewerRole): string {
    return value.replaceAll('_', ' ');
  }
}
