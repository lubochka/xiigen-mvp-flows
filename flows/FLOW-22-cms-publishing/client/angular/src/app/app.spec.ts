import { TestBed } from '@angular/core/testing';
import { App } from './app';

describe('CMS Publishing App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [App] }).compileComponents();
  });

  it('renders the CMS publishing workspace without internal protocol vocabulary', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Editorial operations');
    expect(text).toContain('Immutable content releases');
    expect(text).not.toMatch(/BFA|DNA-[1-9]|AF station|DataProcessResult|CF-[0-9]/);
  });

  it('shows approval workflow for Hebrew RTL evidence', () => {
    const fixture = TestBed.createComponent(App);
    const component = fixture.componentInstance;

    component.applyLanguage('he');
    component.setScreenState('approvals');
    component.setRole('legal_reviewer');
    fixture.detectChanges();

    expect(document.documentElement.lang).toBe('he');
    expect(document.documentElement.dir).toBe('rtl');
    expect(fixture.nativeElement.querySelector('[data-testid="approvals-page"]')).toBeTruthy();
    expect(fixture.nativeElement.textContent).toContain('Legal review');
  });

  it('keeps anonymous users on public reader copy', () => {
    const fixture = TestBed.createComponent(App);
    const component = fixture.componentInstance;

    component.setRole('anonymous');
    fixture.detectChanges();

    expect(component.publicView()).toBe(true);
    expect(fixture.nativeElement.textContent).toContain('published pages only');
  });

  it('renders analytics evidence for aggregate metrics', () => {
    const fixture = TestBed.createComponent(App);
    const component = fixture.componentInstance;

    component.setScreenState('analytics');
    component.setRole('content_editor');
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[data-testid="analytics-page"]')).toBeTruthy();
    expect(fixture.nativeElement.textContent).toContain('Append-only aggregate metrics');
    expect(fixture.nativeElement.textContent).toContain('aggregate only');
  });

  it('keeps publisher controls available for scheduled releases', () => {
    const fixture = TestBed.createComponent(App);
    const component = fixture.componentInstance;

    component.setRole('publisher');
    component.setScheduledItems(8);
    fixture.detectChanges();

    expect(component.canPublish()).toBe(true);
    expect(fixture.nativeElement.textContent).toContain('8');
  });
});
