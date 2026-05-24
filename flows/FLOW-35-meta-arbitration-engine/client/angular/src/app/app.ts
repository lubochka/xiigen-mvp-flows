import { CommonModule, DOCUMENT } from '@angular/common';
import { Component, Inject, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

type ViewerRole = 'platform_admin' | 'platform_support';
type Language = 'en' | 'he';
type ScreenState = 'intake' | 'panel' | 'verdicts' | 'briefings' | 'audit';

interface ArbitrationRow {
  label: string;
  state: string;
  detail: string;
}

interface ArbitrationSignal {
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
  readonly roles: ViewerRole[] = ['platform_admin', 'platform_support'];
  readonly role = signal<ViewerRole>('platform_admin');
  readonly language = signal<Language>('en');
  readonly screenState = signal<ScreenState>('intake');
  readonly moduleName = signal('Meta Arbitration Engine');
  readonly readinessScore = signal(96);

  readonly arbitrationRows: ArbitrationRow[] = [
    { label: 'Conflict intake', state: 'captured', detail: 'round id, competing verdicts, policy set, and arbiter count are recorded before decision writes' },
    { label: 'Meta panel', state: 'weighted', detail: 'policy weights and platform rules select accept, escalate, or halt without tenant payload scope' },
    { label: 'Decision log', state: 'append only', detail: 'each escalation or halt writes a briefing before emitting the CloudEvents envelope' }
  ];

  readonly arbitrationSignals: ArbitrationSignal[] = [
    { label: 'Concurrency', value: '409', detail: 'stale decision writers are rejected with the current version for retry' },
    { label: 'Escalation', value: 'briefed', detail: 'every escalate or halt decision creates operator options in the briefing collection' },
    { label: 'Outbox', value: 'retained', detail: 'timeout and partial-failure paths keep retry evidence for the worker' }
  ];

  readonly pageTitle = computed(() => this.language() === 'he' ? 'מנוע בוררות מטא' : this.moduleName());
  readonly roleTestId = computed(() => `role-${this.role().replaceAll('_', '-')}-meta-arbitration-engine`);
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
    if (stateParam && ['intake', 'panel', 'verdicts', 'briefings', 'audit'].includes(stateParam)) this.screenState.set(stateParam);
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
      'Arbitration control room': 'חדר בקרת בוררות',
      'Role': 'תפקיד',
      'Intake': 'קליטה',
      'Panel': 'פאנל',
      'Verdicts': 'הכרעות',
      'Briefings': 'תדריכים',
      'Audit': 'ביקורת',
      'Conflicts': 'קונפליקטים',
      'Arbiters': 'בוררים',
      'Readiness': 'מוכנות'
    };
    return dictionary[label] ?? label;
  }

  summary(): string {
    if (this.role() === 'platform_support') return 'Platform support can inspect conflict rounds, verdict comparisons, escalation briefings, and append-only audit records without mutation controls.';
    return 'Platform admins resolve conflicting arbiter verdicts, enforce meta policies, and publish escalation briefings before downstream generation continues.';
  }

  roleLabel(value: ViewerRole): string {
    return value.replaceAll('_', ' ');
  }
}

