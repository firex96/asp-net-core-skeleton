import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, firstValueFrom, map } from 'rxjs';

import { ApiError } from '../auth/api-error';

/** 관리 화면이 다루는 그룹 · 역할 · 권한(`/api/authorization`, 권한 `authorization.manage`). */
export interface UserGroup {
  id: number;
  name: string;
  description: string;
}

export interface Member {
  userId: number;
  email: string;
}

export interface Role {
  id: number;
  name: string;
  description: string;
  permissions: string[];
}

export interface Permission {
  code: string;
  description: string;
}

export interface RoleSave {
  name: string;
  description: string;
  permissions: string[];
}

export const MANAGE_PERMISSION = 'authorization.manage';

const base = '/api/authorization';

@Injectable({ providedIn: 'root' })
export class AdminService {
  private readonly http = inject(HttpClient);

  listGroups = () => this.call(this.http.get<UserGroup[]>(`${base}/groups`));
  createGroup = (name: string, description: string) => this.call(this.http.post<UserGroup>(`${base}/groups`, { name, description }));
  updateGroup = (id: number, name: string, description: string) =>
    this.call(this.http.put<UserGroup>(`${base}/groups/${id}`, { name, description }));
  deleteGroup = (id: number) => this.call(this.http.delete<void>(`${base}/groups/${id}`));

  listMembers = (groupId: number) => this.call(this.http.get<Member[]>(`${base}/groups/${groupId}/members`));
  addMember = (groupId: number, userId: number) => this.call(this.http.put<void>(`${base}/groups/${groupId}/members/${userId}`, null));
  removeMember = (groupId: number, userId: number) => this.call(this.http.delete<void>(`${base}/groups/${groupId}/members/${userId}`));

  listGroupRoles = (groupId: number) => this.call(this.http.get<Role[]>(`${base}/groups/${groupId}/roles`));
  assignRole = (groupId: number, roleId: number) => this.call(this.http.put<void>(`${base}/groups/${groupId}/roles/${roleId}`, null));
  unassignRole = (groupId: number, roleId: number) => this.call(this.http.delete<void>(`${base}/groups/${groupId}/roles/${roleId}`));

  listRoles = () => this.call(this.http.get<Role[]>(`${base}/roles`));
  createRole = (role: RoleSave) => this.call(this.http.post<Role>(`${base}/roles`, role));
  updateRole = (id: number, role: RoleSave) => this.call(this.http.put<Role>(`${base}/roles/${id}`, role));
  deleteRole = (id: number) => this.call(this.http.delete<void>(`${base}/roles/${id}`));

  listPermissions = () => this.call(this.http.get<Permission[]>(`${base}/permissions`));

  /** 그룹에 넣을 사람을 이메일(정확히 일치)로 찾는다. 없으면 404 ApiError. */
  findUser = (email: string) =>
    this.call(this.http.get<{ id: number; email: string }>(`${base}/users`, { params: { email } })
      .pipe(map((user): Member => ({ userId: user.id, email: user.email }))));

  private async call<T>(request: Observable<T>): Promise<T> {
    try {
      return await firstValueFrom(request);
    } catch (error) {
      throw ApiError.from(error);
    }
  }
}
