import { CommonModule, DOCUMENT } from '@angular/common';
import { Component, Inject, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

type ViewerRole = 'platform_admin' | 'platform_support' | 'tenant_admin';
type Language = 'en' | 'he';
type ScreenState = 'run' | 'scope' | 'judge' | 'actions' | 'patterns' | 'evidence';

interface AgentRow {
  label: string;
  state: string;
  detail: string;
}

interface AgentSignal {
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
  readonly screenState = signal<ScreenState>('run');
  readonly moduleName = signal('Platform Agent');
  readonly readinessScore = signal(100);

  readonly agentRows: AgentRow[] = [
    { label: 'Master run context', state: 'guarded', detail: 'Agent runs start only from the master platform context and reject tenant-local execution.' },
    { label: 'Scope switch audit', state: 'audited', detail: 'Cross-tenant actions write an audit record before the bounded scope switch executes.' },
    { label: 'Super judge', state: 'cost-gated', detail: 'No platform evidence means defer to the normal review result with zero model calls.' },
    { label: 'Pattern contribution', state: 'sanitized', detail: 'Tenant-derived patterns must strip tenant identifiers before any shared write.' }
  ];

  readonly agentSignals: AgentSignal[] = [
    { label: 'Task types', value: 'T650-T656', detail: 'orchestrate, scope, enrich, judge, publish, contribute, chat' },
    { label: 'Rule coverage', value: '17', detail: 'scope audit, cost gate, sanitizer, idempotency, ordering, concurrency' },
    { label: 'Cascade evidence', value: 'Tier D', detail: 'source flow plus three-tenant marketplace adaptation chain' }
  ];

  readonly pageTitle = computed(() => this.language() === 'he' ? '???? ?????????' : this.moduleName());
  readonly roleTestId = computed(() => `role-${this.role().replaceAll('_', '-')}-platform-agent`);
  readonly canOperate = computed(() => this.role() === 'platform_admin');
  readonly canInspect = computed(() => true);

  constructor(@Inject(DOCUMENT) private readonly documentRef: Document) {
    const params = new URLSearchParams(this.documentRef.defaultView?.location.search ?? '');
    const roleParam = params.get('role') as ViewerRole | null;
    const stateParam = params.get('state') as ScreenState | null;
    const langParam = params.get('lang') as Language | null;
    const moduleParam = params.get('module');
    if (roleParam && this.roles.includes(roleParam)) this.role.set(roleParam);
    if (stateParam && ['run', 'scope', 'judge', 'actions', 'patterns', 'evidence'].includes(stateParam)) this.screenState.set(stateParam);
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
      'Platform Agent console': '???? ???? ?????????',
      'Role': '?????',
      'Language': '???',
      'Run': '????',
      'Scope': '????',
      'Judge': '?????',
      'Actions': '??????',
      'Patterns': '??????',
      'Evidence': '?????',
      'Agent sessions': '????? ????',
      'Rule coverage': '????? ?????',
      'Readiness': '??????'
    };
    return dictionary[label] ?? label;
  }

  summary(): string {
    if (this.role() === 'tenant_admin') return 'Tenant admins inspect adapted action proposals and confirm that tenant-derived patterns stay private unless sharing was approved.';
    if (this.role() === 'platform_support') return 'Platform support reviews agent sessions, scope-switch audits, and super-judge outcomes without mutation controls.';
    return 'Platform admins run the agent, approve bounded tenant actions, review super-judge decisions, and publish sanitized reusable patterns.';
  }

  roleLabel(value: ViewerRole): string {
    return value.replaceAll('_', ' ');
  }
}