import { CommonModule, DOCUMENT } from '@angular/common';
import { Component, Inject, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

type ViewerRole =
  | 'anonymous'
  | 'tenant_user'
  | 'tenant_admin'
  | 'platform_admin'
  | 'platform_support'
  | 'human_decision_maker';
type Language = 'en' | 'he';
type ScreenState = 'queue' | 'claims' | 'completion';

interface GateRow {
  task: string;
  state: string;
  guard: string;
}

interface ClaimRow {
  signal: string;
  value: string;
  rule: string;
}

@Component({
  selector: 'app-root',
  imports: [CommonModule, FormsModule],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  readonly roles: ViewerRole[] = [
    'anonymous',
    'tenant_user',
    'tenant_admin',
    'platform_admin',
    'platform_support',
    'human_decision_maker'
  ];
  readonly role = signal<ViewerRole>('tenant_admin');
  readonly language = signal<Language>('en');
  readonly screenState = signal<ScreenState>('queue');
  readonly moduleName = signal('Human Interaction Gate');
  readonly readinessScore = signal(97);

  readonly gateRows: GateRow[] = [
    { task: 'Approve generated flow promotion', state: 'waiting for user', guard: 'blocks dependent nodes' },
    { task: 'Review tenant policy exception', state: 'quorum partial', guard: 'answers are preserved' },
    { task: 'Resolve inactive approver', state: 'needs admin resolution', guard: 'all required reviewers accounted for' }
  ];

  readonly claimRows: ClaimRow[] = [
    { signal: 'Claim lifetime', value: '5 min', rule: 'expired claims cannot submit answers' },
    { signal: 'Notification dedupe', value: 'one active notice', rule: 'task, channel, recipient' },
    { signal: 'Audit mode', value: 'append only', rule: 'reassignment writes immutable history' }
  ];

  readonly pageTitle = computed(() => this.language() === 'he' ? 'שער החלטות אנושי' : this.moduleName());
  readonly roleTestId = computed(() => `role-${this.role().replaceAll('_', '-')}-human-interaction-gate`);
  readonly publicView = computed(() => this.role() === 'anonymous');
  readonly canAnswer = computed(() => ['tenant_user', 'tenant_admin', 'platform_admin', 'human_decision_maker'].includes(this.role()));
  readonly canConfigure = computed(() => ['tenant_admin', 'platform_admin'].includes(this.role()));
  readonly canInspect = computed(() => ['platform_support', 'tenant_admin', 'platform_admin', 'human_decision_maker'].includes(this.role()));
  readonly canApprove = computed(() => ['tenant_admin', 'platform_admin', 'human_decision_maker'].includes(this.role()));

  constructor(@Inject(DOCUMENT) private readonly documentRef: Document) {
    const params = new URLSearchParams(this.documentRef.defaultView?.location.search ?? '');
    const roleParam = params.get('role') as ViewerRole | null;
    const stateParam = params.get('state') as ScreenState | null;
    const langParam = params.get('lang') as Language | null;
    const moduleParam = params.get('module');
    if (roleParam && this.roles.includes(roleParam)) this.role.set(roleParam);
    if (stateParam && ['queue', 'claims', 'completion'].includes(stateParam)) this.screenState.set(stateParam);
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
      'Human approval console': 'מסוף אישורים אנושי',
      'Role': 'תפקיד',
      'Queue': 'תור',
      'Claims': 'שיוכים',
      'Completion': 'השלמה',
      'Open tasks': 'משימות פתוחות',
      'Stress checks': 'בדיקות עומס',
      'Readiness': 'מוכנות'
    };
    return dictionary[label] ?? label;
  }

  summary(): string {
    if (this.publicView()) return 'Public visitors see only a safe explanation of why human review may delay an action.';
    if (this.canApprove()) return 'Approvers resolve tenant or platform gates only after claim, answer, and dependency checks are satisfied.';
    if (this.canInspect()) return 'Support reviewers inspect gate history, claim ownership, and blocked dependency state without changing tasks.';
    return 'This role can view its own pending approval tasks and cannot see cross-tenant gate data.';
  }

  roleLabel(value: ViewerRole): string {
    return value.replaceAll('_', ' ');
  }
}
