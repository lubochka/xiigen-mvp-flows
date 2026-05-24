import { CommonModule, DOCUMENT } from '@angular/common';
import { Component, Inject, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

type ViewerRole =
  | 'anonymous'
  | 'public_marketplace_visitor'
  | 'tenant_user'
  | 'tenant_admin'
  | 'prompt_approver'
  | 'human_decision_maker'
  | 'agency_access'
  | 'platform_admin'
  | 'platform_support';
type Language = 'en' | 'he';
type ScreenState = 'routing' | 'graph' | 'promotion';

interface ResearchRow {
  goal: string;
  strategy: string;
  guard: string;
}

interface RagSignal {
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
  readonly roles: ViewerRole[] = [
    'anonymous',
    'public_marketplace_visitor',
    'tenant_user',
    'tenant_admin',
    'prompt_approver',
    'human_decision_maker',
    'agency_access',
    'platform_admin',
    'platform_support'
  ];
  readonly role = signal<ViewerRole>('tenant_admin');
  readonly language = signal<Language>('en');
  readonly screenState = signal<ScreenState>('routing');
  readonly moduleName = signal('Adaptive RAG Deep Research');
  readonly readinessScore = signal(98);

  readonly researchRows: ResearchRow[] = [
    { goal: 'Tenant-safe retrieval route', strategy: 'hybrid GraphRAG', guard: 'tenant scope and rollback target verified' },
    { goal: 'Prompt promotion candidate', strategy: 'quality gated', guard: 'promotion ladder advances one stage only' },
    { goal: 'Feedback learning batch', strategy: 'queued bandit update', guard: '100 runs or 24 hours before policy change' }
  ];

  readonly ragSignals: RagSignal[] = [
    { label: 'Routing policy', value: 'queued', detail: 'adaptive updates run through events, never inline' },
    { label: 'Graph edits', value: 'gated', detail: 'community rebuilds stay outside the query path' },
    { label: 'Budget caps', value: 'visible', detail: 'over-budget runs show a clear retry state' }
  ];

  readonly pageTitle = computed(() => this.language() === 'he' ? 'מחקר עומק חכם' : this.moduleName());
  readonly roleTestId = computed(() => `role-${this.role().replaceAll('_', '-')}-adaptive-rag-deep-research`);
  readonly publicView = computed(() => this.role() === 'anonymous' || this.role() === 'public_marketplace_visitor');
  readonly canResearch = computed(() => ['tenant_user', 'tenant_admin', 'prompt_approver', 'human_decision_maker', 'agency_access', 'platform_admin'].includes(this.role()));
  readonly canConfigure = computed(() => ['tenant_admin', 'prompt_approver', 'human_decision_maker', 'platform_admin'].includes(this.role()));
  readonly canInspect = computed(() => ['platform_support', 'tenant_admin', 'platform_admin'].includes(this.role()));

  constructor(@Inject(DOCUMENT) private readonly documentRef: Document) {
    const params = new URLSearchParams(this.documentRef.defaultView?.location.search ?? '');
    const roleParam = params.get('role') as ViewerRole | null;
    const stateParam = params.get('state') as ScreenState | null;
    const langParam = params.get('lang') as Language | null;
    const moduleParam = params.get('module');
    if (roleParam && this.roles.includes(roleParam)) this.role.set(roleParam);
    if (stateParam && ['routing', 'graph', 'promotion'].includes(stateParam)) this.screenState.set(stateParam);
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
      'Research workspace': 'סביבת מחקר',
      'Role': 'תפקיד',
      'Routing': 'ניתוב',
      'Graph': 'גרף',
      'Promotion': 'קידום',
      'Research runs': 'הרצות מחקר',
      'Stress checks': 'בדיקות עומס',
      'Readiness': 'מוכנות'
    };
    return dictionary[label] ?? label;
  }

  summary(): string {
    if (this.publicView()) return 'Marketplace visitors see the capability summary without tenant research controls.';
    if (this.canConfigure()) return 'Control-plane roles tune routing, graph edit gates, prompt promotion, and feedback learning safely.';
    if (this.canInspect()) return 'Support reviewers inspect retained evidence, stress results, and tenant-scoped retrieval health.';
    if (this.canResearch()) return 'Research roles submit tenant-scoped queries and inspect their own traceable results.';
    return 'This role can view the public summary only.';
  }

  roleLabel(value: ViewerRole): string {
    return value.replaceAll('_', ' ');
  }
}
