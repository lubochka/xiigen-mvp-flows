import { CommonModule, DOCUMENT } from '@angular/common';
import { Component, Inject, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

type ViewerRole = 'tenant_admin' | 'platform_admin' | 'platform_support';
type Language = 'en' | 'he';
type ScreenState = 'provisioning' | 'health' | 'offboarding';

interface LifecycleRow {
  label: string;
  state: string;
  detail: string;
}

interface LifecycleSignal {
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
  readonly roles: ViewerRole[] = ['tenant_admin', 'platform_admin', 'platform_support'];
  readonly role = signal<ViewerRole>('platform_admin');
  readonly language = signal<Language>('en');
  readonly screenState = signal<ScreenState>('provisioning');
  readonly moduleName = signal('Tenant Lifecycle Manager');
  readonly readinessScore = signal(97);

  readonly researchRows: LifecycleRow[] = [
    { label: 'Provisioning run', state: 'provisioning', detail: 'region, plan, quota, and policy inheritance captured before outbox dispatch' },
    { label: 'Health score', state: 'healthy', detail: 'support can inspect probes without mutating lifecycle controls' },
    { label: 'Offboarding window', state: 'scheduled', detail: 'audit and retention steps remain visible before tenant archival' }
  ];

  readonly ragSignals: LifecycleSignal[] = [
    { label: 'Concurrency', value: 'If-Match', detail: 'stale lifecycle writers receive a retryable 409' },
    { label: 'Idempotency', value: 'required', detail: 'replayed provisioning calls return cached 200 responses' },
    { label: 'Timeout path', value: 'retained', detail: 'partial fabric failures keep an outbox record for retry' }
  ];

  readonly pageTitle = computed(() => this.language() === 'he' ? 'ניהול מחזור חיי דייר' : this.moduleName());
  readonly roleTestId = computed(() => `role-${this.role().replaceAll('_', '-')}-tenant-lifecycle-manager`);
  readonly publicView = computed(() => false);
  readonly canResearch = computed(() => this.role() === 'platform_admin');
  readonly canConfigure = computed(() => this.role() === 'platform_admin');
  readonly canInspect = computed(() => this.role() === 'platform_admin' || this.role() === 'platform_support' || this.role() === 'tenant_admin');

  constructor(@Inject(DOCUMENT) private readonly documentRef: Document) {
    const params = new URLSearchParams(this.documentRef.defaultView?.location.search ?? '');
    const roleParam = params.get('role') as ViewerRole | null;
    const stateParam = params.get('state') as ScreenState | null;
    const langParam = params.get('lang') as Language | null;
    const moduleParam = params.get('module');
    if (roleParam && this.roles.includes(roleParam)) this.role.set(roleParam);
    if (stateParam && ['provisioning', 'health', 'offboarding'].includes(stateParam)) this.screenState.set(stateParam);
    this.applyLanguage(langParam === 'he' ? 'he' : 'en');
    if (moduleParam) this.moduleName.set(moduleParam);
  }

  setRole(value: ViewerRole): void {
    this.role.set(value);
  }

  setScreenState(value: ScreenState): void {
    this.screenState.set(value);
  }

  setReadinessScore(value: number | string): void {
    this.readinessScore.set(Number(value));
  }

  applyLanguage(value: Language): void {
    this.language.set(value);
    this.documentRef.documentElement.lang = value;
    this.documentRef.documentElement.dir = value === 'he' ? 'rtl' : 'ltr';
  }

  translated(label: string): string {
    if (this.language() === 'en') return label;
    const dictionary: Record<string, string> = {
      'Research workspace': 'מרחב מחזור חיים',
      'Role': 'תפקיד',
      'Routing': 'הקמה',
      'Graph': 'בריאות',
      'Promotion': 'סגירה',
      'Research runs': 'ריצות מחזור חיים',
      'Stress checks': 'בדיקות שמירה',
      'Readiness': 'מוכנות'
    };
    return dictionary[label] ?? label;
  }

  summary(): string {
    if (this.role() === 'platform_admin') return 'Platform admins provision tenants, enforce quota policy, inspect health, and schedule offboarding.';
    if (this.role() === 'platform_support') return 'Support reviewers inspect lifecycle state and health timelines without engine mutation controls.';
    return 'Tenant admins see their own tenant lifecycle status without platform-wide controls.';
  }

  roleLabel(value: ViewerRole): string {
    return value.replaceAll('_', ' ');
  }
}
