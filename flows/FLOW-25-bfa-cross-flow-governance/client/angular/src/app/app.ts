import { CommonModule, DOCUMENT } from '@angular/common';
import { Component, Inject, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

type ViewerRole =
  | 'anonymous'
  | 'governance_operator'
  | 'arbiter_reviewer'
  | 'flow_architect'
  | 'impact_analyst'
  | 'release_manager'
  | 'tenant_admin'
  | 'platform_admin';
type Language = 'en' | 'he';
type ScreenState = 'rules' | 'impact' | 'release';

interface RuleRow {
  gate: string;
  outcome: string;
  effect: string;
}

interface ConflictRow {
  subject: string;
  severity: 'critical' | 'high' | 'none';
  route: string;
}

interface ReleaseMetric {
  signal: string;
  value: string;
  guard: string;
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
    'governance_operator',
    'arbiter_reviewer',
    'flow_architect',
    'impact_analyst',
    'release_manager',
    'tenant_admin',
    'platform_admin'
  ];
  readonly role = signal<ViewerRole>('governance_operator');
  readonly language = signal<Language>('en');
  readonly screenState = signal<ScreenState>('rules');
  readonly moduleName = signal('BFA Cross-Flow Governance');
  readonly readinessScore = signal(98);

  readonly ruleRows: RuleRow[] = [
    { gate: 'Rule envelope', outcome: '72 rules loaded', effect: 'change scan can start' },
    { gate: 'Stress matrix', outcome: '25 checks ready', effect: 'release gate has proof' },
    { gate: 'Tenant scope', outcome: 'context only', effect: 'payload scope rejected' }
  ];

  readonly conflicts: ConflictRow[] = [
    { subject: 'Required field removal', severity: 'critical', route: 'arbitrate' },
    { subject: 'API endpoint removal', severity: 'critical', route: 'arbitrate' },
    { subject: 'Optional field addition', severity: 'none', route: 'publish' }
  ];

  readonly releaseMetrics: ReleaseMetric[] = [
    { signal: 'Impact graph', value: '3 hops', guard: 'cycle-safe traversal' },
    { signal: 'Audit mode', value: 'append only', guard: 'no overwrite path' },
    { signal: 'Queue event', value: 'after state', guard: 'persist-before-emit' }
  ];

  readonly pageTitle = computed(() => this.language() === 'he' ? '×ž×ž×©×œ ×©×™× ×•×™×™× ×‘×™×Ÿ ×–×¨×™×ž×•×ª' : this.moduleName());
  readonly roleTestId = computed(() => `role-${this.role().replaceAll('_', '-')}-bfa-cross-flow-governance`);
  readonly publicView = computed(() => this.role() === 'anonymous');
  readonly canOperate = computed(() => ['governance_operator', 'tenant_admin', 'platform_admin'].includes(this.role()));
  readonly canReview = computed(() => ['arbiter_reviewer', 'impact_analyst', 'tenant_admin', 'platform_admin'].includes(this.role()));
  readonly canApprove = computed(() => ['release_manager', 'tenant_admin', 'platform_admin'].includes(this.role()));
  readonly canArchitect = computed(() => ['flow_architect', 'impact_analyst', 'tenant_admin', 'platform_admin'].includes(this.role()));

  constructor(@Inject(DOCUMENT) private readonly documentRef: Document) {
    const params = new URLSearchParams(this.documentRef.defaultView?.location.search ?? '');
    const roleParam = params.get('role') as ViewerRole | null;
    const stateParam = params.get('state') as ScreenState | null;
    const langParam = params.get('lang') as Language | null;
    const moduleParam = params.get('module');
    if (roleParam && this.roles.includes(roleParam)) this.role.set(roleParam);
    if (stateParam && ['rules', 'impact', 'release'].includes(stateParam)) this.screenState.set(stateParam);
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
      'Governance arbiter': '×ž×ž×©×œ ×‘×“×™×§×•×ª',
      'Role': '×ª×¤×§×™×“',
      'Rules': '×›×œ×œ×™×',
      'Impact': '×”×©×¤×¢×”',
      'Release': '×©×—×¨×•×¨',
      'Active rules': '×›×œ×œ×™× ×¤×¢×™×œ×™×',
      'Stress checks': '×‘×“×™×§×•×ª ×¢×•×ž×¡',
      'Readiness': '×ž×•×›× ×•×ª'
    };
    return dictionary[label] ?? label;
  }

  summary(): string {
    if (this.publicView()) return 'Public visitors see only the governance posture and cannot inspect tenant change records.';
    if (this.canApprove()) return 'Release managers close the gate only after rule coverage, impact analysis, and stress proof are present.';
    if (this.canReview()) return 'Reviewers inspect conflicts, evidence links, and the stress matrix before any release can proceed.';
    if (this.canArchitect()) return 'Architects review dependency blast radius and verify the change path stays cross-flow safe.';
    if (this.canOperate()) return 'Operators validate the rule envelope, submit proposed changes, and keep tenant scope outside the payload.';
    return 'This role has read-only visibility into the governance posture.';
  }

  roleLabel(value: ViewerRole): string {
    return value.replaceAll('_', ' ');
  }
}
