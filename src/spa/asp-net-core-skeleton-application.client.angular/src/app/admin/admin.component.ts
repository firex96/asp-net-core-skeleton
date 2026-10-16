import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

import { ApiError } from '../auth/api-error';
import { AuthService } from '../auth/auth.service';
import { AdminService, MANAGE_PERMISSION, Member, Permission, Role, UserGroup } from './admin.service';

/**
 * 권한 관리 — 사람은 그룹에, 그룹에는 역할을, 역할에는 권한 코드를 준다(authorization.manage 권한). 바꾸면 다음 요청부터 반영된다.
 * 마지막 관리자를 없애는 변경은 서버가 409 로 막고, 그 안내를 그대로 보인다.
 */
@Component({
  selector: 'app-admin',
  imports: [FormsModule],
  template: `
    <main class="card card-wide">
      <h1>권한 관리</h1>
      <p class="lead">사람은 그룹에, 그룹에는 역할을, 역할에는 권한을 줘요. 바꾸면 다음 요청부터 반영돼요.</p>

      @if (allowed() === false) {
        <p class="alert alert-error" role="alert">이 화면을 볼 권한이 없어요.</p>
      } @else if (allowed()) {
        @if (notice(); as notice) {
          <p class="alert alert-info" role="status">{{ notice }}</p>
        }
        @if (error(); as error) {
          <p class="alert alert-error" role="alert">{{ error.message }}</p>
        }

        <h2 class="section-title">그룹</h2>
        <ul class="list">
          @for (item of groups(); track item.id) {
            <li>
              <span>
                <strong>{{ item.name }}</strong>
                @if (item.description) {
                  <span class="muted"> · {{ item.description }}</span>
                }
              </span>
              <button type="button" class="btn btn-small" (click)="openGroup(item)">열기</button>
            </li>
          }
        </ul>
        <form class="form" (ngSubmit)="submitNewGroup()">
          <label class="field">
            새 그룹 이름
            <input name="newGroupName" required maxlength="100" [(ngModel)]="newGroupName" />
          </label>
          <label class="field">
            새 그룹 설명
            <input name="newGroupDescription" maxlength="500" [(ngModel)]="newGroupDescription" />
          </label>
          <button type="submit" class="btn btn-primary">그룹 만들기</button>
        </form>

        @if (group(); as current) {
          <h2 class="section-title">{{ current.name }} 그룹</h2>
          <form class="form" (ngSubmit)="saveGroup()">
            <label class="field">
              그룹 이름
              <input name="groupName" required maxlength="100" [(ngModel)]="groupName" />
            </label>
            <label class="field">
              그룹 설명
              <input name="groupDescription" maxlength="500" [(ngModel)]="groupDescription" />
            </label>
            <div class="button-row">
              <button type="submit" class="btn btn-primary">그룹 저장</button>
              <button type="button" class="btn" (click)="removeGroup()">그룹 지우기</button>
            </div>
          </form>

          <h3 class="subsection-title">구성원</h3>
          @if (members().length === 0) {
            <p class="muted">아직 구성원이 없어요.</p>
          } @else {
            <ul class="list">
              @for (member of members(); track member.userId) {
                <li>
                  <span>{{ member.email }}</span>
                  <button type="button" class="btn btn-small" (click)="dropMember(member)">빼기</button>
                </li>
              }
            </ul>
          }
          <form class="form" (ngSubmit)="submitMember()">
            <label class="field">
              구성원 이메일
              <input name="memberEmail" type="email" required placeholder="name@example.com" [(ngModel)]="memberEmail" />
            </label>
            <button type="submit" class="btn btn-primary">구성원 추가</button>
          </form>

          <h3 class="subsection-title">역할</h3>
          <ul class="list">
            @for (role of roles(); track role.id) {
              <li>
                <label class="checkbox">
                  <input type="checkbox" [checked]="groupRoleIds().includes(role.id)" (change)="toggleRole(role, $any($event.target).checked)" />
                  {{ role.name }}
                </label>
                <span class="muted">{{ role.permissions.join(', ') || '권한 없음' }}</span>
              </li>
            }
          </ul>
        }

        <h2 class="section-title">역할</h2>
        <ul class="list">
          @for (role of roles(); track role.id) {
            <li>
              <span>
                <strong>{{ role.name }}</strong>
                <span class="muted"> · {{ role.permissions.join(', ') || '권한 없음' }}</span>
              </span>
              <span class="button-row">
                <button type="button" class="btn btn-small" (click)="editRole(role)">고치기</button>
                <button type="button" class="btn btn-small" (click)="removeRole(role)">지우기</button>
              </span>
            </li>
          }
        </ul>
        <form class="form" (ngSubmit)="saveRole()">
          <label class="field">
            역할 이름
            <input name="roleName" required maxlength="100" [(ngModel)]="roleName" />
          </label>
          <label class="field">
            역할 설명
            <input name="roleDescription" maxlength="500" [(ngModel)]="roleDescription" />
          </label>
          <fieldset class="permissions">
            <legend>권한</legend>
            @for (permission of permissions(); track permission.code) {
              <label class="checkbox">
                <input type="checkbox" [checked]="rolePermissions().includes(permission.code)"
                       (change)="toggleRolePermission(permission.code, $any($event.target).checked)" />
                <code>{{ permission.code }}</code>
                {{ permission.description }}
              </label>
            }
          </fieldset>
          <div class="button-row">
            <button type="submit" class="btn btn-primary">{{ editingRole() ? '역할 저장' : '역할 만들기' }}</button>
            @if (editingRole()) {
              <button type="button" class="btn" (click)="editRole(null)">취소</button>
            }
          </div>
        </form>
      }
    </main>
  `,
  changeDetection: ChangeDetectionStrategy.Eager
})
export class AdminComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly admin = inject(AdminService);
  private readonly router = inject(Router);

  readonly allowed = signal<boolean | null>(null);
  readonly groups = signal<UserGroup[]>([]);
  readonly roles = signal<Role[]>([]);
  readonly permissions = signal<Permission[]>([]);
  readonly error = signal<ApiError | null>(null);
  readonly notice = signal<string | null>(null);
  readonly group = signal<UserGroup | null>(null);
  readonly members = signal<Member[]>([]);
  readonly groupRoleIds = signal<number[]>([]);
  readonly editingRole = signal<Role | null>(null);
  readonly rolePermissions = signal<string[]>([]);

  newGroupName = '';
  newGroupDescription = '';
  groupName = '';
  groupDescription = '';
  memberEmail = '';
  roleName = '';
  roleDescription = '';

  async ngOnInit() {
    const user = this.auth.user() === undefined ? await this.auth.refresh() : this.auth.user();
    if (!user) {
      await this.router.navigateByUrl(`/login?returnUrl=${encodeURIComponent('/admin')}`);
      return;
    }

    this.allowed.set(user.permissions.includes(MANAGE_PERMISSION));
    if (this.allowed()) {
      await this.run(async () => {
        const [groups, roles, permissions] = await Promise.all([this.admin.listGroups(), this.admin.listRoles(), this.admin.listPermissions()]);
        this.groups.set(groups);
        this.roles.set(roles);
        this.permissions.set(permissions);
      });
    }
  }

  openGroup(target: UserGroup) {
    return this.run(async () => {
      const [members, roles] = await Promise.all([this.admin.listMembers(target.id), this.admin.listGroupRoles(target.id)]);
      this.members.set(members);
      this.groupRoleIds.set(roles.map(role => role.id));
      this.group.set(target);
      this.groupName = target.name;
      this.groupDescription = target.description;
    });
  }

  submitNewGroup() {
    return this.run(async () => {
      const created = await this.admin.createGroup(this.newGroupName, this.newGroupDescription);
      this.newGroupName = '';
      this.newGroupDescription = '';
      await this.reloadGroups();
      await this.openGroup(created);
    }, '그룹을 만들었어요.');
  }

  saveGroup() {
    return this.run(async () => {
      this.group.set(await this.admin.updateGroup(this.group()!.id, this.groupName, this.groupDescription));
      await this.reloadGroups();
    }, '그룹을 저장했어요.');
  }

  removeGroup() {
    return this.run(async () => {
      await this.admin.deleteGroup(this.group()!.id);
      this.group.set(null);
      await this.reloadGroups();
    }, '그룹을 지웠어요.');
  }

  submitMember() {
    return this.run(async () => {
      const user = await this.admin.findUser(this.memberEmail);
      await this.admin.addMember(this.group()!.id, user.userId);
      this.memberEmail = '';
      this.members.set(await this.admin.listMembers(this.group()!.id));
    }, '구성원을 넣었어요.');
  }

  dropMember(member: Member) {
    return this.run(async () => {
      await this.admin.removeMember(this.group()!.id, member.userId);
      this.members.set(await this.admin.listMembers(this.group()!.id));
    });
  }

  toggleRole(role: Role, checked: boolean) {
    return this.run(async () => {
      const groupId = this.group()!.id;
      await (checked ? this.admin.assignRole(groupId, role.id) : this.admin.unassignRole(groupId, role.id));
      this.groupRoleIds.set((await this.admin.listGroupRoles(groupId)).map(assigned => assigned.id));
    });
  }

  editRole(role: Role | null) {
    this.editingRole.set(role);
    this.roleName = role?.name ?? '';
    this.roleDescription = role?.description ?? '';
    this.rolePermissions.set([...(role?.permissions ?? [])]);
  }

  toggleRolePermission(code: string, checked: boolean) {
    this.rolePermissions.update(current => (checked ? [...current, code] : current.filter(permission => permission !== code)));
  }

  saveRole() {
    const role = { name: this.roleName, description: this.roleDescription, permissions: this.rolePermissions() };
    const editing = this.editingRole();
    return this.run(async () => {
      await (editing ? this.admin.updateRole(editing.id, role) : this.admin.createRole(role));
      this.editRole(null);
      await this.reloadRoles();
    }, editing ? '역할을 저장했어요.' : '역할을 만들었어요.');
  }

  removeRole(role: Role) {
    return this.run(async () => {
      await this.admin.deleteRole(role.id);
      if (this.editingRole()?.id === role.id) {
        this.editRole(null);
      }
      await this.reloadRoles();
    }, '역할을 지웠어요.');
  }

  private async reloadGroups() {
    this.groups.set(await this.admin.listGroups());
  }

  private async reloadRoles() {
    this.roles.set(await this.admin.listRoles());
  }

  private async run(action: () => Promise<void>, done?: string) {
    this.error.set(null);
    this.notice.set(null);
    try {
      await action();
      this.notice.set(done ?? null);
    } catch (error) {
      this.error.set(error instanceof ApiError ? error : ApiError.from(error));
    }
  }
}
