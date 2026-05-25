import { CommonModule, DOCUMENT } from '@angular/common';
import { Component, Inject, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

type ViewerRole = 'platform_admin' | 'platform_support' | 'tenant_admin';
type Language = 'en' | 'he';
type ScreenState = 'features' | 'signals' | 'decisions' | 'adapters' | 'simulations';

interface FeatureRow {
  label: string;
  state: string;
  detail: string;
}

interface FeatureSignal {
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
  readonly screenState = signal<ScreenState>('features');
  readonly moduleName = signal('Feature Registry');
  readonly readinessScore = signal(94);

  readonly featureRows: FeatureRow[] = [
    { label: 'FT-ID extraction', state: 'cataloged', detail: 'FeatureExtractor assigns stable FT identifiers from source topology and design evidence.' },
    { label: 'Porting decision gate', state: 'guarded', detail: 'portingCandidate=false resolves to PROHIBITED before cost, adapter, or simulation work starts.' },
    { label: 'Adapter simulation', state: 'verified', detail: 'Approved candidates produce adapter plans and simulator readback evidence before publication.' }
  ];

  readonly featureSignals: FeatureSignal[] = [
    { label: 'Concurrency', value: '409', detail: 'stale FT writers receive current version evidence for retry' },
    { label: 'Idempotency', value: 'cached', detail: 'repeated X-Idempotency-Key values replay the same registry body' },
    { label: 'Outbox', value: 'retained', detail: 'timeout paths leave retry events for the worker' }
  ];

  readonly pageTitle = computed(() => this.language() === 'he' ? 'מרשם יכולות' : this.moduleName());
  readonly roleTestId = computed(() => `role-${this.role().replaceAll('_', '-')}-feature-registry`);
  readonly publicView = computed(() => false);
  readonly canOperate = computed(() => this.role() === 'platform_admin');
  readonly canInspect = computed(() => true);

  constructor(@Inject(DOCUMENT) private readonly documentRef: Document) {
    const params = new URLSearchParams(this.documentRef.defaultView?.location.search ?? '');
    const roleParam = params.get('role') as ViewerRole | null;
    const stateParam = params.get('state') as ScreenState | null;
    const langParam = params.get('lang') as Language | null;
    const moduleParam = params.get('module');
    if (roleParam && this.roles.includes(roleParam)) this.role.set(roleParam);
    if (stateParam && ['features', 'signals', 'decisions', 'adapters', 'simulations'].includes(stateParam)) this.screenState.set(stateParam);
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
      'Feature registry console': 'מסוף מרשם יכולות',
      'Role': 'תפקיד',
      'Features': 'יכולות',
      'Signals': 'אותות',
      'Decisions': 'החלטות',
      'Adapters': 'מתאמים',
      'Simulations': 'סימולציות',
      'FT records': 'רשומות יכולת',
      'Candidates': 'מועמדים',
      'Readiness': 'מוכנות'
    };
    return dictionary[label] ?? label;
  }

  summary(): string {
    if (this.role() === 'tenant_admin') return 'Tenant admins see which registered capabilities are active for their tenant, including porting status, without mutation controls.';
    if (this.role() === 'platform_support') return 'Platform support can inspect FT records, signal history, porting decisions, adapters, and simulator evidence without mutation controls.';
    return 'Platform admins manage FT records, classify porting candidates, approve adapter generation, and verify simulator evidence before a capability moves downstream.';
  }

  roleLabel(value: ViewerRole): string {
    return value.replaceAll('_', ' ');
  }
}
