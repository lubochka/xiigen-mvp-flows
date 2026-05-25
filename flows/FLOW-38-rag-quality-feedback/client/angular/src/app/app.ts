import { CommonModule, DOCUMENT } from '@angular/common';
import { Component, Inject, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

type ViewerRole = 'platform_admin' | 'platform_support' | 'tenant_admin';
type Language = 'en' | 'he';
type ScreenState = 'outcomes' | 'quality' | 'rules' | 'signals';

interface FeedbackRow {
  label: string;
  state: string;
  detail: string;
}

interface FeedbackSignal {
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
  readonly screenState = signal<ScreenState>('outcomes');
  readonly moduleName = signal('RAG Quality Feedback');
  readonly readinessScore = signal(91);

  readonly feedbackRows: FeedbackRow[] = [
    { label: 'Cycle outcome', state: 'classified', detail: 'Completed learning cycles are grouped into successful, wasted, or escalated outcomes.' },
    { label: 'Pattern quality', state: 'adjusted', detail: 'Accepted retrieval patterns receive a fixed positive delta while wasted cycles reduce trust.' },
    { label: 'Distilled guidance', state: 'seeded', detail: 'Successful cycles can publish concise guidance for future retrieval context.' }
  ];

  readonly feedbackSignals: FeedbackSignal[] = [
    { label: 'Concurrency', value: '409', detail: 'stale cycle and pattern writers receive current version evidence for retry' },
    { label: 'Idempotency', value: 'cached', detail: 'repeated request keys replay the same quality update body' },
    { label: 'Outbox', value: 'retained', detail: 'timeout paths leave retry events for the worker' }
  ];

  readonly pageTitle = computed(() => this.language() === 'he' ? 'משוב איכות חיפוש חכם' : this.moduleName());
  readonly roleTestId = computed(() => `role-${this.role().replaceAll('_', '-')}-rag-quality-feedback`);
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
    if (stateParam && ['outcomes', 'quality', 'rules', 'signals'].includes(stateParam)) this.screenState.set(stateParam);
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
      'Learning feedback console': 'מסוף משוב למידה',
      'Role': 'תפקיד',
      'Outcomes': 'תוצאות',
      'Quality': 'איכות',
      'Rules': 'כללים',
      'Signals': 'אותות',
      'Cycles reviewed': 'מחזורים שנבדקו',
      'Pattern updates': 'עדכוני תבנית',
      'Readiness': 'מוכנות'
    };
    return dictionary[label] ?? label;
  }

  summary(): string {
    if (this.role() === 'tenant_admin') return 'Tenant admins can review learning outcomes and approve quality updates for their tenant patterns.';
    if (this.role() === 'platform_support') return 'Platform support can inspect outcome history, quality changes, and timeout evidence without mutation controls.';
    return 'Platform admins manage cycle outcome classification, quality updates, and distilled guidance across tenants.';
  }

  roleLabel(value: ViewerRole): string {
    return value.replaceAll('_', ' ');
  }
}
