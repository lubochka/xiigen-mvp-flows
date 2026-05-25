import { CommonModule, DOCUMENT } from '@angular/common';
import { Component, Inject, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

type ViewerRole = 'platform_admin' | 'platform_support' | 'tenant_admin';
type Language = 'en' | 'he';
type ScreenState = 'filter' | 'style' | 'write' | 'storage' | 'publication';

interface AdapterRow {
  label: string;
  state: string;
  detail: string;
}

interface AdapterSignal {
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
  readonly screenState = signal<ScreenState>('filter');
  readonly moduleName = signal('Framer Text Bridge');
  readonly readinessScore = signal(100);

  readonly adapterRows: AdapterRow[] = [
    { label: 'Text node filter', state: 'isolated', detail: 'Mixed Framer selections are reduced to text nodes before they enter the shared XIIGen text model.' },
    { label: 'Typography mapping', state: 'deterministic', detail: 'Font weights normalize to Framer style names from Thin through Black with stable size, line-height, and letter-spacing values.' },
    { label: 'Paint conversion', state: 'solid', detail: 'Hex, short-hex, rgb, and rgba values become solid paint payloads with bounded opacity.' },
    { label: 'Plugin data storage', state: 'facade-only', detail: 'Adapter state is serialized through getPluginData and setPluginData semantics with no browser storage fallback.' }
  ];

  readonly adapterSignals: AdapterSignal[] = [
    { label: 'Text nodes', value: '1 / mixed', detail: 'non-text frames are ignored during adapter reads' },
    { label: 'Coverage rules', value: '7', detail: 'filter, style mapping, plugin data, SDK boundary, package, live auth, and scope' },
    { label: 'Package', value: '1.0.0', detail: '@xiigen/framer-text-elements-adapter with STEP-1 invariants included' }
  ];

  readonly pageTitle = computed(() => this.language() === 'he' ? 'מתאם טקסט Framer' : this.moduleName());
  readonly roleTestId = computed(() => `role-${this.role().replaceAll('_', '-')}-framer-adapter`);
  readonly canOperate = computed(() => this.role() === 'platform_admin' || this.role() === 'tenant_admin');
  readonly canInspect = computed(() => true);

  constructor(@Inject(DOCUMENT) private readonly documentRef: Document) {
    const params = new URLSearchParams(this.documentRef.defaultView?.location.search ?? '');
    const roleParam = params.get('role') as ViewerRole | null;
    const stateParam = params.get('state') as ScreenState | null;
    const langParam = params.get('lang') as Language | null;
    const moduleParam = params.get('module');
    if (roleParam && this.roles.includes(roleParam)) this.role.set(roleParam);
    if (stateParam && ['filter', 'style', 'write', 'storage', 'publication'].includes(stateParam)) this.screenState.set(stateParam);
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
      'Framer Text Elements Adapter console': 'מסוף מתאם הטקסט של Framer',
      'Role': 'תפקיד',
      'Language': 'שפה',
      'Filter': 'סינון',
      'Style': 'סגנון',
      'Write': 'כתיבה',
      'Storage': 'אחסון',
      'Publication': 'פרסום',
      'Mapped text nodes': 'צמתי טקסט שמופו',
      'Package checks': 'בדיקות חבילה',
      'Readiness': 'מוכנות'
    };
    return dictionary[label] ?? label;
  }

  summary(): string {
    if (this.role() === 'tenant_admin') return 'Tenant admins review their Framer text package, run marketplace readback, and inspect text-node conversion evidence before adoption.';
    if (this.role() === 'platform_support') return 'Platform support inspects filter results, package evidence, plugin-data storage, and tenant-scoped run records without mutation controls.';
    return 'Platform admins approve the Framer Text Elements Adapter package, verify local registry readback, and monitor adapter parity before tenant adoption.';
  }

  roleLabel(value: ViewerRole): string {
    return value.replaceAll('_', ' ');
  }
}
