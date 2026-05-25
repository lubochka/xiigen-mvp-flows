import { CommonModule, DOCUMENT } from '@angular/common';
import { Component, Inject, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

type ViewerRole = 'platform_admin' | 'platform_support' | 'tenant_admin';
type Language = 'en' | 'he';
type ScreenState = 'seed' | 'retrieve' | 'summarize' | 'orchestrate' | 'evidence';

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
  readonly roles: ViewerRole[] = ['platform_admin', 'platform_support', 'tenant_admin'];
  readonly role = signal<ViewerRole>('platform_admin');
  readonly language = signal<Language>('en');
  readonly screenState = signal<ScreenState>('seed');
  readonly moduleName = signal('History Bootstrap');
  readonly readinessScore = signal(100);

  readonly bootstrapRows: BootstrapRow[] = [
    { label: 'Architecture corpus', state: 'approved', detail: 'Only approved history-seeds and architecture documents can enter the bootstrap pipeline.' },
    { label: 'Global patterns', state: 'global', detail: 'Architecture patterns are forced to GLOBAL knowledge scope before retrieval.' },
    { label: 'RAG summaries', state: 'grouped', detail: 'Pattern summaries are grouped by pattern type and written before any queue signal.' },
    { label: 'Completion record', state: 'idempotent', detail: 'A bootstrap run id can be replayed without duplicating state or events.' }
  ];

  readonly bootstrapSignals: BootstrapSignal[] = [
    { label: 'Task types', value: 'T601-T604', detail: 'seed, retrieve, summarize, orchestrate' },
    { label: 'Rule coverage', value: '7', detail: 'bootstrap prerequisite, global scope, routing, outbox, idempotency, tenant dashboard' },
    { label: 'Cascade evidence', value: 'Tier D', detail: 'source flow plus three-tenant marketplace adaptation chain' }
  ];

  readonly pageTitle = computed(() => this.language() === 'he' ? 'אתחול היסטוריית הארכיטקטורה' : this.moduleName());
  readonly roleTestId = computed(() => `role-${this.role().replaceAll('_', '-')}-history-bootstrap`);
  readonly canOperate = computed(() => this.role() === 'platform_admin' || this.role() === 'tenant_admin');
  readonly canInspect = computed(() => true);

  constructor(@Inject(DOCUMENT) private readonly documentRef: Document) {
    const params = new URLSearchParams(this.documentRef.defaultView?.location.search ?? '');
    const roleParam = params.get('role') as ViewerRole | null;
    const stateParam = params.get('state') as ScreenState | null;
    const langParam = params.get('lang') as Language | null;
    const moduleParam = params.get('module');
    if (roleParam && this.roles.includes(roleParam)) this.role.set(roleParam);
    if (stateParam && ['seed', 'retrieve', 'summarize', 'orchestrate', 'evidence'].includes(stateParam)) this.screenState.set(stateParam);
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
      'History Bootstrap console': 'מסוף אתחול היסטוריה',
      'Role': 'תפקיד',
      'Language': 'שפה',
      'Seed': 'זריעה',
      'Retrieve': 'אחזור',
      'Summarize': 'סיכום',
      'Orchestrate': 'תזמור',
      'Evidence': 'ראיות',
      'Architecture records': 'רשומות ארכיטקטורה',
      'Rule coverage': 'כיסוי כללים',
      'Readiness': 'מוכנות'
    };
    return dictionary[label] ?? label;
  }

  summary(): string {
    if (this.role() === 'tenant_admin') return 'Tenant admins run a FREEDOM-gated bootstrap readback for their adapted module and confirm the history memory remains tenant safe.';
    if (this.role() === 'platform_support') return 'Platform support inspects architecture corpus seeding, summary counts, and completion evidence without mutation controls.';
    return 'Platform admins seed architecture history, retrieve global patterns, summarize philosophy groups, and certify the bootstrap completion record.';
  }

  roleLabel(value: ViewerRole): string {
    return value.replaceAll('_', ' ');
  }
}
