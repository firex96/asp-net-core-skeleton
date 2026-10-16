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

  /** 화면을 띄우고 시작할 때 읽는 소셜 공급자 목록에 답한다. */
  async function create(providers: { name: string; displayName: string }[] = []) {
    const fixture = TestBed.createComponent(LoginComponent);
    fixture.detectChanges();
    await new Promise(resolve => setTimeout(resolve));
    httpMock.expectOne('/api/auth/external/providers').flush(providers);
    await new Promise(resolve => setTimeout(resolve));
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture;
  }

  it('shows the server message when login fails', async () => {
    const fixture = await create();
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
    const fixture = await create();
    const component = fixture.componentInstance;
    component.toggleMode();
    component.email = 'user@example.com';
    component.password = 'Passw0rd!';
    component.confirmPassword = 'different';

    await component.submit();

    expect(component.error()?.message).toBe('비밀번호가 서로 달라요. 다시 입력해 주세요.');
  });

  it('asks to confirm the email after sign-up instead of signing in', async () => {
    const fixture = await create();
    const component = fixture.componentInstance;
    component.toggleMode();
    component.email = 'new@example.com';
    component.password = 'Passw0rd!-long';
    component.confirmPassword = 'Passw0rd!-long';

    const submitted = component.submit();
    httpMock.expectOne('/api/auth/register').flush(null, { status: 201, statusText: 'Created' });
    await submitted;
    fixture.detectChanges();

    expect(component.mode()).toBe('login');
    expect((fixture.nativeElement as HTMLElement).querySelector('[role=status]')?.textContent).toContain('new@example.com로 확인 메일을 보냈어요');
  });

  it('offers to resend the confirmation mail to an unconfirmed account', async () => {
    const fixture = await create();
    const component = fixture.componentInstance;
    component.email = 'new@example.com';
    component.password = 'Passw0rd!-long';
    const submitted = component.submit();
    httpMock.expectOne('/api/auth/login').flush(
      { status: 403, code: 'EMAIL_NOT_CONFIRMED_ERROR', detail: 'not confirmed' },
      { status: 403, statusText: 'Forbidden' });
    await submitted;
    fixture.detectChanges();

    const resend = (fixture.nativeElement as HTMLElement).querySelector('[role=alert] button') as HTMLButtonElement;
    expect(resend.textContent).toContain('확인 메일 다시 받기');
    resend.click();
    const request = httpMock.expectOne('/api/auth/email/resend');
    expect(request.request.body).toEqual({ email: 'new@example.com' });
    request.flush(null, { status: 202, statusText: 'Accepted' });
    await fixture.whenStable();
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).querySelector('[role=status]')?.textContent).toContain('다시 보냈어요');
  });

  it('returns to a same-origin returnUrl after login', async () => {
    const navigate = vi.spyOn(router, 'navigateByUrl').mockResolvedValue(true);
    queryParams = { returnUrl: '/products' };
    const fixture = await create();
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

  it('offers a form post per social provider that keeps the returnUrl', async () => {
    queryParams = { returnUrl: '/products' };
    const fixture = await create([{ name: 'Google', displayName: 'Google' }]);

    const form = (fixture.nativeElement as HTMLElement).querySelector('.social form') as HTMLFormElement;
    expect(form.getAttribute('method')).toBe('post');
    // FormsModule 의 NgForm 은 브라우저 기본 제출을 막는다 — 서버로 가는 폼은 ngNoForm 이어야 한다(실제 제출은 브라우저로 확인)
    expect(form.hasAttribute('ngnoform')).toBe(true);
    expect(form.getAttribute('action')).toBe('/api/auth/external/Google/login?returnUrl=%2Fproducts');
  });

  it('explains an unlinked social account', async () => {
    queryParams = { error: 'EXTERNAL_LOGIN_NOT_LINKED_ERROR', provider: 'Kakao' };
    const fixture = await create();

    expect((fixture.nativeElement as HTMLElement).querySelector('[role=status]')?.textContent).toContain('아직 연결하지 않은 카카오 계정');
  });

  it('tells when the social providers cannot be loaded', async () => {
    const fixture = TestBed.createComponent(LoginComponent);
    fixture.detectChanges();
    await new Promise(resolve => setTimeout(resolve));
    httpMock.expectOne('/api/auth/external/providers').flush(null, { status: 500, statusText: 'Server Error' });
    await new Promise(resolve => setTimeout(resolve));
    await fixture.whenStable();
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).querySelector('[role=alert]')?.textContent).toContain('소셜 로그인 목록을 불러오지 못했어요');
  });

  it('still loads the social providers when the current user cannot be read', async () => {
    TestBed.inject(AuthService).user.set(undefined);
    const fixture = TestBed.createComponent(LoginComponent);
    fixture.detectChanges();
    await new Promise(resolve => setTimeout(resolve));
    httpMock.expectOne('/api/auth/me').flush(null, { status: 500, statusText: 'Server Error' });
    await new Promise(resolve => setTimeout(resolve));
    httpMock.expectOne('/api/auth/external/providers').flush([{ name: 'Google', displayName: 'Google' }]);
    await new Promise(resolve => setTimeout(resolve));
    await fixture.whenStable();
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).querySelector('.social button[type=submit]')?.textContent).toContain('Google로 계속하기');
  });
});
