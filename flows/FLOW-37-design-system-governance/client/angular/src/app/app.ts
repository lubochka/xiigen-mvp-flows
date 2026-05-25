import { CommonModule, DOCUMENT } from '@angular/common';
import { Component, Inject, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

type ViewerRole = 'platform_admin' | 'platform_support' | 'tenant_admin';
type Language = 'en' | 'he';
type ScreenState = 'classification' | 'prompts' | 'debt' | 'compatibility';

interface GovernanceRow {
  label: string;
  state: string;
  detail: string;
}

interface GovernanceSignal {
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
  readonly screenState = signal<ScreenState>('classification');
  readonly moduleName = signal('Design System Governance');
  readonly readinessScore = signal(94);

  readonly governanceRows: GovernanceRow[] = [
    { label: 'Stack compatibility', state: 'approved', detail: 'Target stack, token set, and prompt frame are reviewed together before downstream generation.' },
    { label: 'Prompt exclusions', state: 'guarded', detail: 'Known incompatibilities are excluded from the genesis prompt before a shell is generated.' },
    { label: 'Design debt review', state: 'measured', detail: 'Complexity, token consistency, and accessibility risk are scored as a single release signal.' }
  ];

  readonly governanceSignals: GovernanceSignal[] = [
    { label: 'Concurrency', value: '409', detail: 'stale design-system writers receive current version evidence for retry' },
    { label: 'Idempotency', value: 'cached', detail: 'repeated request keys replay the same governance body' },
    { label: 'Outbox', value: 'retained', detail: 'timeout paths leave retry events for the worker' }
  ];

  readonly pageTitle = computed(() => this.language() === 'he' ? 'ממשל מערכת עיצוב' : this.moduleName());
  readonly roleTestId = computed(() => `role-${this.role().replaceAll('_', '-')}-design-system-governance`);
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
    if (stateParam && ['classification', 'prompts', 'debt', 'compatibility'].includes(stateParam)) this.screenState.set(stateParam);
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
      'Design governance console': 'מסוף ממשל עיצוב',
      'Role': 'תפקיד',
      'Classification': 'סיווג',
      'Prompts': 'הנחיות',
      'Debt': 'חוב עיצוב',
      'Compatibility': 'תאימות',
      'Reviewed systems': 'מערכות שנבדקו',
      'Prompt frames': 'מסגרות הנחיה',
      'Readiness': 'מוכנות'
    };
    return dictionary[label] ?? label;
  }

  summary(): string {
    if (this.role() === 'tenant_admin') return 'Tenant admins can register and review the design systems owned by their tenant before generated surfaces ship.';
    if (this.role() === 'platform_support') return 'Platform support can inspect compatibility, prompt exclusions, and debt signals without mutation controls.';
    return 'Platform admins manage design-system classifications, prompt exclusions, token consistency, and release readiness across tenants.';
  }

  roleLabel(value: ViewerRole): string {
    return value.replaceAll('_', ' ');
  }
}
