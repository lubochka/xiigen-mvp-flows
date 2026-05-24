import { CommonModule, DOCUMENT } from '@angular/common';
import { Component, Inject, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

type ViewerRole =
  | 'anonymous'
  | 'platform_support'
  | 'flow_architect'
  | 'prompt_approver'
  | 'human_decision_maker'
  | 'tenant_admin'
  | 'platform_admin';
type Language = 'en' | 'he';
type ScreenState = 'gaps' | 'genesis' | 'promotion';

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
    'platform_support',
    'flow_architect',
    'prompt_approver',
    'human_decision_maker',
    'tenant_admin',
    'platform_admin'
  ];
  readonly role = signal<ViewerRole>('platform_admin');
  readonly language = signal<Language>('en');
  readonly screenState = signal<ScreenState>('gaps');
  readonly moduleName = signal('Self-Developing Meta-Flow Engine');
  readonly readinessScore = signal(98);

  readonly ruleRows: RuleRow[] = [
    { gate: 'Capability registry', outcome: 'false positives suppressed', effect: 'only true gaps enter genesis' },
    { gate: 'Contract set', outcome: '24 task types covered', effect: 'factory and event specs stay complete' },
    { gate: 'Tenant scope', outcome: 'context only', effect: 'self-build data cannot cross tenants' }
  ];

  readonly conflicts: ConflictRow[] = [
    { subject: 'Factory id collision', severity: 'critical', route: 'regenerate' },
    { subject: 'Sandbox production leakage', severity: 'critical', route: 'hold' },
    { subject: 'Owned skill adaptation notice', severity: 'none', route: 'notify' }
  ];

  readonly releaseMetrics: ReleaseMetric[] = [
    { signal: 'Evidence bundle', value: '12 sections', guard: 'promotion cannot skip proof' },
    { signal: 'CORE stage', value: 'human gate', guard: 'no automated bypass' },
    { signal: 'GitOps branch', value: 'self-build/*', guard: 'namespace collision blocked' }
  ];

  readonly pageTitle = computed(() => this.language() === 'he' ? 'Ã—Å¾Ã—Å¾Ã—Â©Ã—Å“ Ã—Â©Ã—â„¢Ã— Ã—â€¢Ã—â„¢Ã—â„¢Ã—Â Ã—â€˜Ã—â„¢Ã—Å¸ Ã—â€“Ã—Â¨Ã—â„¢Ã—Å¾Ã—â€¢Ã—Âª' : this.moduleName());
  readonly roleTestId = computed(() => `role-${this.role().replaceAll('_', '-')}-meta-flow-engine`);
  readonly publicView = computed(() => this.role() === 'anonymous');
  readonly canOperate = computed(() => ['flow_architect', 'prompt_approver', 'tenant_admin', 'platform_admin'].includes(this.role()));
  readonly canReview = computed(() => ['platform_support', 'human_decision_maker', 'tenant_admin', 'platform_admin'].includes(this.role()));
  readonly canApprove = computed(() => ['human_decision_maker', 'tenant_admin', 'platform_admin'].includes(this.role()));
  readonly canArchitect = computed(() => ['flow_architect', 'tenant_admin', 'platform_admin'].includes(this.role()));

  constructor(@Inject(DOCUMENT) private readonly documentRef: Document) {
    const params = new URLSearchParams(this.documentRef.defaultView?.location.search ?? '');
    const roleParam = params.get('role') as ViewerRole | null;
    const stateParam = params.get('state') as ScreenState | null;
    const langParam = params.get('lang') as Language | null;
    const moduleParam = params.get('module');
    if (roleParam && this.roles.includes(roleParam)) this.role.set(roleParam);
    if (stateParam && ['gaps', 'genesis', 'promotion'].includes(stateParam)) this.screenState.set(stateParam);
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
      'Governance arbiter': 'Ã—Å¾Ã—Å¾Ã—Â©Ã—Å“ Ã—â€˜Ã—â€œÃ—â„¢Ã—Â§Ã—â€¢Ã—Âª',
      'Role': 'Ã—ÂªÃ—Â¤Ã—Â§Ã—â„¢Ã—â€œ',
      'Rules': 'Ã—â€ºÃ—Å“Ã—Å“Ã—â„¢Ã—Â',
      'Impact': 'Ã—â€Ã—Â©Ã—Â¤Ã—Â¢Ã—â€',
      'Release': 'Ã—Â©Ã—â€”Ã—Â¨Ã—â€¢Ã—Â¨',
      'Active rules': 'Ã—â€ºÃ—Å“Ã—Å“Ã—â„¢Ã—Â Ã—Â¤Ã—Â¢Ã—â„¢Ã—Å“Ã—â„¢Ã—Â',
      'Stress checks': 'Ã—â€˜Ã—â€œÃ—â„¢Ã—Â§Ã—â€¢Ã—Âª Ã—Â¢Ã—â€¢Ã—Å¾Ã—Â¡',
      'Readiness': 'Ã—Å¾Ã—â€¢Ã—â€ºÃ— Ã—â€¢Ã—Âª'
    };
    return dictionary[label] ?? label;
  }

  summary(): string {
    if (this.publicView()) return 'Public visitors see only the self-build posture and cannot inspect tenant build runs.';
    if (this.canApprove()) return 'Human decision makers approve promotion only after evidence, sandbox isolation, and BFA governance proof are present.';
    if (this.canReview()) return 'Support reviewers inspect gap evidence, generated contracts, and stress-matrix proof before escalation.';
    if (this.canArchitect()) return 'Architects review capability gaps, plan DAGs, fabric boundaries, and generated task contracts.';
    if (this.canOperate()) return 'Prompt approvers tune the self-build request while keeping tenant scope outside the payload.';
    return 'This role has read-only visibility into the self-build posture.';
  }

  roleLabel(value: ViewerRole): string {
    return value.replaceAll('_', ' ');
  }
}

