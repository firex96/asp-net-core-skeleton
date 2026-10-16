import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { ApiError } from '../auth/api-error';
import { AdminService } from './admin.service';

describe('AdminService', () => {
  let service: AdminService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(AdminService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('sends role saves as JSON and adds members with PUT', async () => {
    const created = service.createRole({ name: '편집자', description: '', permissions: ['products.manage'] });
    const createRequest = httpMock.expectOne('/api/authorization/roles');
    expect(createRequest.request.method).toBe('POST');
    expect(createRequest.request.body).toEqual({ name: '편집자', description: '', permissions: ['products.manage'] });
    createRequest.flush({ id: 2, name: '편집자', description: '', permissions: ['products.manage'] }, { status: 201, statusText: 'Created' });
    await created;

    const added = service.addMember(3, 7);
    const addRequest = httpMock.expectOne('/api/authorization/groups/3/members/7');
    expect(addRequest.request.method).toBe('PUT');
    addRequest.flush(null, { status: 204, statusText: 'No Content' });
    await added;
  });

  it('finds a user by email and reports an unknown one', async () => {
    const found = service.findUser('member@example.com');
    httpMock.expectOne('/api/authorization/users?email=member@example.com').flush({ id: 7, email: 'member@example.com' });
    expect(await found).toEqual({ userId: 7, email: 'member@example.com' });

    const missing = service.findUser('nobody@example.com').catch((caught: unknown) => caught);
    httpMock.expectOne('/api/authorization/users?email=nobody@example.com')
      .flush({ code: 'NOT_FOUND_ERROR', detail: '없어요' }, { status: 404, statusText: 'Not Found' });
    expect(await missing).toBeInstanceOf(ApiError);
  });
});
