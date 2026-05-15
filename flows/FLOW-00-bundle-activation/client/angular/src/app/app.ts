import { CommonModule, DOCUMENT } from '@angular/common';
import { Component, Inject, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

type ViewerRole = 'tenant_admin' | 'platform_admin' | 'platform_support' | 'anonymous';
type BundleStatus = 'active' | 'degraded' | 'pending' | 'failed';

interface BundleRow {
  id: string;
  name: string;
  status: BundleStatus;
  version: string;
  requiredFlowCount: number;
  tenantLabel: string;
}

@Component({
  selector: 'app-root',
  imports: [CommonModule, FormsModule],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  readonly roles: ViewerRole[] = ['tenant_admin', 'platform_admin', 'platform_support', 'anonymous'];
  readonly role = signal<ViewerRole>('tenant_admin');
  readonly wizardStep = signal(1);
  readonly selectedBundleId = signal('BDL-marketplace-pack');
  readonly dryRunMode = signal(true);
  readonly activationMessage = signal<string | null>(null);
  readonly language = signal<'en' | 'he'>('en');

  readonly bundles: BundleRow[] = [
    { id: 'BDL-core-platform-v2', name: 'Core Platform v2', status: 'active', version: '2.1.3', requiredFlowCount: 7, tenantLabel: 'Platform' },
    { id: 'BDL-marketplace-pack', name: 'Marketplace Pack', status: 'pending', version: '1.4.0', requiredFlowCount: 4, tenantLabel: 'Acme Corp' },
    { id: 'BDL-compliance-gate', name: 'Compliance Gate', status: 'degraded', version: '3.0.1', requiredFlowCount: 3, tenantLabel: 'Northwind' },
    { id: 'BDL-experimental-x', name: 'Experimental X', status: 'failed', version: '0.9.2', requiredFlowCount: 0, tenantLabel: 'Tessera' }
  ];

  readonly activeBundle = computed(() =>
    this.bundles.find(bundle => bundle.id === this.selectedBundleId()) ?? this.bundles[0]
  );

  constructor(@Inject(DOCUMENT) private readonly document: Document) {
    const params = new URLSearchParams(this.document.defaultView?.location.search ?? '');
    const requestedRole = params.get('role');
    const requestedLanguage = params.get('lang') === 'he' ? 'he' : 'en';
    if (this.isViewerRole(requestedRole)) {
      this.role.set(requestedRole);
    }
    this.applyLanguage(requestedLanguage);
  }

  setRole(value: ViewerRole): void {
    this.role.set(value);
    this.activationMessage.set(null);
  }

  setSelectedBundleId(value: string): void {
    this.selectedBundleId.set(value);
  }

  setDryRunMode(value: boolean): void {
    this.dryRunMode.set(value);
  }

  continueToConfigure(): void {
    this.wizardStep.set(2);
  }

  backToSelect(): void {
    this.wizardStep.set(1);
    this.activationMessage.set(null);
  }

  activateBundle(): void {
    const bundle = this.activeBundle();
    const mode = this.dryRunMode() ? 'DRY_RUN' : 'FULL';
    this.activationMessage.set(`${bundle.name} activation started in ${mode}. Validation, dry-run, state write, and outbox notification are tracked.`);
    this.wizardStep.set(3);
  }

  resetWizard(): void {
    this.wizardStep.set(1);
    this.activationMessage.set(null);
  }

  applyLanguage(value: 'en' | 'he'): void {
    this.language.set(value);
    this.document.documentElement.lang = value;
    this.document.documentElement.dir = value === 'he' ? 'rtl' : 'ltr';
  }

  statusLabel(status: BundleStatus): string {
    return status.charAt(0).toUpperCase() + status.slice(1);
  }

  statusClass(status: BundleStatus): string {
    return `status status-${status}`;
  }

  private isViewerRole(value: string | null): value is ViewerRole {
    return value === 'tenant_admin' ||
      value === 'platform_admin' ||
      value === 'platform_support' ||
      value === 'anonymous';
  }
}
