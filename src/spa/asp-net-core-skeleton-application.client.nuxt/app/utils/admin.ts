import { ApiError } from './auth'

/** 관리 화면이 다루는 그룹 · 역할 · 권한(`/api/authorization`, 권한 `authorization.manage`). */
export interface UserGroup {
  id: number
  name: string
  description: string
}

export interface Member {
  userId: number
  email: string
}

export interface Role {
  id: number
  name: string
  description: string
  permissions: string[]
}

export interface Permission {
  code: string
  description: string
}

export interface RoleSave {
  name: string
  description: string
  permissions: string[]
}

export const MANAGE_PERMISSION = 'authorization.manage'

const base = '/api/authorization'

export const listGroups = (fetchFn: typeof fetch = fetch) => send<UserGroup[]>(fetchFn, 'GET', `${base}/groups`)
export const createGroup = (name: string, description: string, fetchFn: typeof fetch = fetch) =>
  send<UserGroup>(fetchFn, 'POST', `${base}/groups`, { name, description })
export const updateGroup = (id: number, name: string, description: string, fetchFn: typeof fetch = fetch) =>
  send<UserGroup>(fetchFn, 'PUT', `${base}/groups/${id}`, { name, description })
export const deleteGroup = (id: number, fetchFn: typeof fetch = fetch) => send<void>(fetchFn, 'DELETE', `${base}/groups/${id}`)

export const listMembers = (groupId: number, fetchFn: typeof fetch = fetch) =>
  send<Member[]>(fetchFn, 'GET', `${base}/groups/${groupId}/members`)
export const addMember = (groupId: number, userId: number, fetchFn: typeof fetch = fetch) =>
  send<void>(fetchFn, 'PUT', `${base}/groups/${groupId}/members/${userId}`)
export const removeMember = (groupId: number, userId: number, fetchFn: typeof fetch = fetch) =>
  send<void>(fetchFn, 'DELETE', `${base}/groups/${groupId}/members/${userId}`)

export const listGroupRoles = (groupId: number, fetchFn: typeof fetch = fetch) =>
  send<Role[]>(fetchFn, 'GET', `${base}/groups/${groupId}/roles`)
export const assignRole = (groupId: number, roleId: number, fetchFn: typeof fetch = fetch) =>
  send<void>(fetchFn, 'PUT', `${base}/groups/${groupId}/roles/${roleId}`)
export const unassignRole = (groupId: number, roleId: number, fetchFn: typeof fetch = fetch) =>
  send<void>(fetchFn, 'DELETE', `${base}/groups/${groupId}/roles/${roleId}`)

export const listRoles = (fetchFn: typeof fetch = fetch) => send<Role[]>(fetchFn, 'GET', `${base}/roles`)
export const createRole = (role: RoleSave, fetchFn: typeof fetch = fetch) => send<Role>(fetchFn, 'POST', `${base}/roles`, role)
export const updateRole = (id: number, role: RoleSave, fetchFn: typeof fetch = fetch) =>
  send<Role>(fetchFn, 'PUT', `${base}/roles/${id}`, role)
export const deleteRole = (id: number, fetchFn: typeof fetch = fetch) => send<void>(fetchFn, 'DELETE', `${base}/roles/${id}`)

export const listPermissions = (fetchFn: typeof fetch = fetch) => send<Permission[]>(fetchFn, 'GET', `${base}/permissions`)

/** 그룹에 넣을 사람을 이메일(정확히 일치)로 찾는다. 없으면 404 ApiError. */
export const findUser = (email: string, fetchFn: typeof fetch = fetch) =>
  send<{ id: number; email: string }>(fetchFn, 'GET', `${base}/users?email=${encodeURIComponent(email)}`).then(
    (user): Member => ({ userId: user.id, email: user.email })
  )

async function send<T>(fetchFn: typeof fetch, method: string, url: string, body?: object): Promise<T> {
  const response = await fetchFn(url, {
    method,
    headers: body ? { 'Content-Type': 'application/json' } : undefined,
    body: body ? JSON.stringify(body) : undefined
  })
  if (!response.ok) {
    throw await ApiError.from(response)
  }
  return (response.status === 204 ? undefined : await response.json()) as T
}
