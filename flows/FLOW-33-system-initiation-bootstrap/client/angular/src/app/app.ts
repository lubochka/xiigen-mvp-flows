import { CommonModule, DOCUMENT } from '@angular/common';
import { Component, Inject, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

type ViewerRole = 'anonymous' | 'platform_admin' | 'platform_support';
type Language = 'en' | 'he';
type ScreenState = 'plan' | 'registries' | 'gaps' | 'smoke' | 'audit';

interface BootstrapRow {
  label: string;
  state: string;
  detail: string;
}

interface BootstrapSignal {
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
  readonly roles: ViewerRole[] = ['anonymous', 'platform_admin', 'platform_support'];
  readonly role = signal<ViewerRole>('platform_admin');
  readonly language = signal<Language>('en');
  readonly screenState = signal<ScreenState>('plan');
  readonly moduleName = signal('System Initiation Bootstrap');
  readonly readinessScore = signal(96);

  readonly bootstrapRows: BootstrapRow[] = [
    { label: 'Plan bundle', state: 'imported', detail: 'schemas, factories, task types, skills, and governance rules are loaded as versioned registries' },
    { label: 'Coverage scan', state: 'evaluated', detail: 'required factories are compared with installed providers before generation starts' },
    { label: 'Smoke flow', state: 'scheduled', detail: 'FLOW-SMOKE-01 proves registry, document, queue, and dashboard wiring before normal mode' }
  ];

  readonly bootstrapSignals: BootstrapSignal[] = [
    { label: 'Registry writes', value: 'versioned', detail: 'stale writers receive 409 and must retry against the current bootstrap version' },
    { label: 'Gap strategy', value: 'self-extend', detail: 'missing factories emit CapabilityGapDetected for the T58 extension loop' },
    { label: 'Outbox', value: 'retained', detail: 'timeout and partial-failure paths keep retry evidence for the worker' }
  ];

  readonly pageTitle = computed(() => this.language() === 'he' ? 'אתחול מערכת' : this.moduleName());
  readonly roleTestId = computed(() => `role-${this.role().replaceAll('_', '-')}-system-initiation-bootstrap`);
  readonly publicView = computed(() => this.role() === 'anonymous');
  readonly canOperate = computed(() => this.role() === 'platform_admin');
  readonly canInspect = computed(() => this.role() === 'platform_admin' || this.role() === 'platform_support');

  constructor(@Inject(DOCUMENT) private readonly documentRef: Document) {
    const params = new URLSearchParams(this.documentRef.defaultView?.location.search ?? '');
    const roleParam = params.get('role') as ViewerRole | null;
    const stateParam = params.get('state') as ScreenState | null;
    const langParam = params.get('lang') as Language | null;
    const moduleParam = params.get('module');
    if (roleParam && this.roles.includes(roleParam)) this.role.set(roleParam);
    if (stateParam && ['plan', 'registries', 'gaps', 'smoke', 'audit'].includes(stateParam)) this.screenState.set(stateParam);
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
      'Bootstrap control room': 'חדר בקרת אתחול',
      'Role': 'תפקיד',
      'Plan': 'תוכנית',
      'Registries': 'רישומים',
      'Gaps': 'פערים',
      'Smoke': 'בדיקה',
      'Audit': 'ביקורת',
      'Runs': 'הרצות',
      'Factories': 'מפעלים',
      'Readiness': 'מוכנות'
    };
    return dictionary[label] ?? label;
  }

  summary(): string {
    if (this.publicView()) return 'Anonymous first-run users see only the safe bootstrap entry point and cannot mutate registries.';
    if (this.role() === 'platform_support') return 'Platform support can inspect registry imports, capability gaps, smoke evidence, and audit records without starting a run.';
    return 'Platform admins start the bootstrap run, import the plan bundle, scan factory coverage, and promote the system into normal mode.';
  }

  roleLabel(value: ViewerRole): string {
    return value.replaceAll('_', ' ');
  }
}
