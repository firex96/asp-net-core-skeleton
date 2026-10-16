import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap, provideRouter } from '@angular/router';

import { AuthService } from '../auth/auth.service';
import { LoginComponent } from './login.component';

describe('LoginComponent', () => {
  let httpMock: HttpTestingController;
  let router: Router;
  let queryParams: Record<string, string>;

  beforeEach(async () => {
    queryParams = {};
    await TestBed.configureTestingModule({
      imports: [LoginComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: ActivatedRoute, useValue: { snapshot: { get queryParamMap() { return convertToParamMap(queryParams); } } } }
      ]
    }).compileComponents();
    httpMock = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
    TestBed.inject(AuthService).user.set(null);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('shows the server message when login fails', async () => {
    const fixture = TestBed.createComponent(LoginComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    const component = fixture.componentInstance;
    component.email = 'user@example.com';
    component.password = 'wrong';

    const submitted = component.submit();
    httpMock.expectOne('/api/auth/login').flush(
      { status: 401, code: 'UNAUTHORIZED_ERROR', detail: 'invalid credentials' },
      { status: 401, statusText: 'Unauthorized' });
    await submitted;
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).querySelector('[role=alert]')?.textContent).toContain('invalid credentials');
  });

  it('rejects mismatched passwords before calling the server', async () => {
    const fixture = TestBed.createComponent(LoginComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    const component = fixture.componentInstance;
    component.toggleMode();
    component.email = 'user@example.com';
    component.password = 'Passw0rd!';
    component.confirmPassword = 'different';

    await component.submit();

    expect(component.error()?.message).toBe('Passwords do not match.');
  });

  it('returns to a same-origin returnUrl after login', async () => {
    const navigate = vi.spyOn(router, 'navigateByUrl').mockResolvedValue(true);
    queryParams = { returnUrl: '/products' };
    const fixture = TestBed.createComponent(LoginComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    const component = fixture.componentInstance;
    component.email = 'user@example.com';
    component.password = 'Passw0rd!';

    const submitted = component.submit();
    httpMock.expectOne('/api/auth/login').flush(null, { status: 204, statusText: 'No Content' });
    await new Promise(resolve => setTimeout(resolve));
    httpMock.expectOne('/api/auth/me').flush({ id: 1, email: 'user@example.com', permissions: [] });
    await submitted;

    expect(navigate).toHaveBeenCalledWith('/products');
  });
});
