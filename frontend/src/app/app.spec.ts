import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { App } from './app';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideRouter([])]
    }).compileComponents();
  });

  it('renders the AssetFlow workspace navigation', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.brand-copy strong')?.textContent).toContain('AssetFlow');
    expect(compiled.textContent).toContain('Overview & Inventory');
    expect(compiled.textContent).toContain('Asset Assistant');
  });
});
