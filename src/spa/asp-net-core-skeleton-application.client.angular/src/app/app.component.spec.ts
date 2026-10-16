import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { AppComponent } from './app.component';

describe('AppComponent', () => {
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AppComponent],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])]
    }).compileComponents();
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('shows a login link when signed out', async () => {
    const fixture = TestBed.createComponent(AppComponent);
    fixture.detectChanges();
    httpMock.expectOne('/api/auth/me').flush(null, { status: 401, statusText: 'Unauthorized' });
    await fixture.whenStable();
    fixture.detectChanges();

    const header = (fixture.nativeElement as HTMLElement).querySelector('header')!;
    expect(header.textContent).toContain('로그인');
  });

  it('shows the user and a logout button when signed in', async () => {
    const fixture = TestBed.createComponent(AppComponent);
    fixture.detectChanges();
    httpMock.expectOne('/api/auth/me').flush({ id: 1, email: 'user@example.com', permissions: [] });
    await fixture.whenStable();
    fixture.detectChanges();

    const header = (fixture.nativeElement as HTMLElement).querySelector('header')!;
    expect(header.textContent).toContain('user@example.com');
    expect(header.textContent).toContain('로그아웃');
  });
});
