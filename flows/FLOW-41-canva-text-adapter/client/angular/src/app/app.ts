import { CommonModule, DOCUMENT } from '@angular/common';
import { Component, Inject, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

type ViewerRole = 'platform_admin' | 'platform_support' | 'tenant_admin';
type Language = 'en' | 'he';
type ScreenState = 'read' | 'style' | 'write' | 'publication';

interface PushRow {
  label: string;
  state: string;
  detail: string;
}

interface PushSignal {
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
  readonly screenState = signal<ScreenState>('read');
  readonly moduleName = signal('Canva Text Adapter');
  readonly readinessScore = signal(100);

  readonly pushRows: PushRow[] = [
    { label: 'Read adapter', state: 'pure mapping', detail: 'Canva content, size, alignment, color, width, and height become the shared text contract without storage side effects.' },
    { label: 'Style bridge', state: 'Figma-shaped', detail: 'Canva font weights and alignment values normalize into the canonical shared style vocabulary.' },
    { label: 'Write payload', state: 'failure counted', detail: 'Shared styles translate back to Canva payloads and writer failures increment the failed count without aborting the batch.' }
  ];

  readonly pushSignals: PushSignal[] = [
    { label: 'Task types', value: '4', detail: 'read adapter, style bridge, write payload, and local registry readback' },
    { label: 'Coverage rules', value: '7', detail: 'live-mode portability, global approval, pricing, read/write mapping, package evidence, and SDK boundary' },
    { label: 'Package', value: '1.0.1', detail: '@xiigen/canva-text-elements-adapter with STEP-1 invariants included' }
  ];

  readonly pageTitle = computed(() => this.language() === 'he' ? '×’×©×¨ ×¢×“×›×•× ×™ ×œ×§×•×—' : this.moduleName());
  readonly roleTestId = computed(() => `role-${this.role().replaceAll('_', '-')}-canva-text-adapter`);
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
    if (stateParam && ['read', 'style', 'write', 'publication'].includes(stateParam)) this.screenState.set(stateParam);
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
      'Canva Text Adapter console': '×ž×¡×•×£ ×¢×“×›×•× ×™ ×œ×§×•×—',
      'Role': '×ª×¤×§×™×“',
      'Language': '×©×¤×”',
      'Read': '×§×¨×™××”',
      'Style': '×¡×’× ×•×Ÿ',
      'Write': '×›×ª×™×‘×”',
      'Publication': '×¤×¨×¡×•×',
      'Mapped elements': '×¨×›×™×‘×™× ×ž×ž×•×¤×™×',
      'Package checks': '×‘×“×™×§×•×ª ×—×‘×™×œ×”',
      'Readiness': '×ž×•×›× ×•×ª'
    };
    return dictionary[label] ?? label;
  }

  summary(): string {
    if (this.role() === 'tenant_admin') return 'Tenant admins can review their Canva adapter package, run tenant marketplace readback, and inspect their own publication evidence.';
    if (this.role() === 'platform_support') return 'Platform support can inspect adapter mapping, package evidence, and write failure counts without mutation controls.';
    return 'Platform admins approve the Canva adapter package, verify local registry readback, and monitor the C5 mapping contract before tenant adoption.';
  }

  roleLabel(value: ViewerRole): string {
    return value.replaceAll('_', ' ');
  }
}
