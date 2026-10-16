<script setup lang="ts">
// 권한 관리 — 사람은 그룹에, 그룹에는 역할을, 역할에는 권한 코드를 준다(authorization.manage 권한). 바꾸면 다음 요청부터 반영된다.
// 마지막 관리자를 없애는 변경은 서버가 409 로 막고, 그 안내를 그대로 보인다.
const session = useSession()
const allowed = ref<boolean | null>(null)
const groups = ref<UserGroup[]>([])
const roles = ref<Role[]>([])
const permissions = ref<Permission[]>([])
const error = ref<ApiError | null>(null)
const notice = ref<string | null>(null)

const newGroupName = ref('')
const newGroupDescription = ref('')

const group = ref<UserGroup | null>(null)
const groupName = ref('')
const groupDescription = ref('')
const members = ref<Member[]>([])
const groupRoleIds = ref<number[]>([])
const memberEmail = ref('')

const editingRole = ref<Role | null>(null)
const roleName = ref('')
const roleDescription = ref('')
const rolePermissions = ref<string[]>([])

useHead({ title: '권한 관리' })

onMounted(async () => {
  const user = session.user.value === undefined ? await session.refresh() : session.user.value
  if (!user) {
    await navigateTo(`/login?returnUrl=${encodeURIComponent('/admin')}`)
    return
  }

  allowed.value = user.permissions.includes(MANAGE_PERMISSION)
  if (allowed.value) {
    await run(async () => {
      [groups.value, roles.value, permissions.value] = await Promise.all([listGroups(), listRoles(), listPermissions()])
    })
  }
})

async function run(action: () => Promise<void>, done?: string) {
  error.value = null
  notice.value = null
  try {
    await action()
    notice.value = done ?? null
  }
  catch (caught) {
    error.value = caught instanceof ApiError ? caught : ApiError.unexpected()
  }
}

async function reloadGroups() {
  groups.value = await listGroups()
}

async function reloadRoles() {
  roles.value = await listRoles()
}

async function openGroup(target: UserGroup) {
  await run(async () => {
    [members.value, groupRoleIds.value] = await Promise.all([
      listMembers(target.id),
      listGroupRoles(target.id).then(assigned => assigned.map(role => role.id)),
    ])
    group.value = target
    groupName.value = target.name
    groupDescription.value = target.description
  })
}

const submitNewGroup = () => run(async () => {
  const created = await createGroup(newGroupName.value, newGroupDescription.value)
  newGroupName.value = ''
  newGroupDescription.value = ''
  await reloadGroups()
  await openGroup(created)
}, '그룹을 만들었어요.')

const saveGroup = () => run(async () => {
  group.value = await updateGroup(group.value!.id, groupName.value, groupDescription.value)
  await reloadGroups()
}, '그룹을 저장했어요.')

const removeGroup = () => run(async () => {
  await deleteGroup(group.value!.id)
  group.value = null
  await reloadGroups()
}, '그룹을 지웠어요.')

const submitMember = () => run(async () => {
  const user = await findUser(memberEmail.value)
  await addMember(group.value!.id, user.userId)
  memberEmail.value = ''
  members.value = await listMembers(group.value!.id)
}, '구성원을 넣었어요.')

const dropMember = (member: Member) => run(async () => {
  await removeMember(group.value!.id, member.userId)
  members.value = await listMembers(group.value!.id)
})

const toggleRole = (role: Role, checked: boolean) => run(async () => {
  await (checked ? assignRole(group.value!.id, role.id) : unassignRole(group.value!.id, role.id))
  groupRoleIds.value = (await listGroupRoles(group.value!.id)).map(assigned => assigned.id)
})

function editRole(role: Role | null) {
  editingRole.value = role
  roleName.value = role?.name ?? ''
  roleDescription.value = role?.description ?? ''
  rolePermissions.value = [...(role?.permissions ?? [])]
}

function toggleRolePermission(code: string, checked: boolean) {
  rolePermissions.value = checked ? [...rolePermissions.value, code] : rolePermissions.value.filter(permission => permission !== code)
}

function saveRole() {
  const role = { name: roleName.value, description: roleDescription.value, permissions: rolePermissions.value }
  const editing = editingRole.value
  return run(async () => {
    await (editing ? updateRole(editing.id, role) : createRole(role))
    editRole(null)
    await reloadRoles()
  }, editing ? '역할을 저장했어요.' : '역할을 만들었어요.')
}

const removeRole = (role: Role) => run(async () => {
  await deleteRole(role.id)
  if (editingRole.value?.id === role.id) {
    editRole(null)
  }
  await reloadRoles()
}, '역할을 지웠어요.')

function checked(event: Event): boolean {
  return (event.target as HTMLInputElement).checked
}
</script>

<template>
  <main class="card card-wide">
    <h1>권한 관리</h1>
    <p class="lead">
      사람은 그룹에, 그룹에는 역할을, 역할에는 권한을 줘요. 바꾸면 다음 요청부터 반영돼요.
    </p>

    <p v-if="allowed === false" class="alert alert-error" role="alert">
      이 화면을 볼 권한이 없어요.
    </p>
    <template v-else-if="allowed">
      <p v-if="notice" class="alert alert-info" role="status">
        {{ notice }}
      </p>
      <p v-if="error" class="alert alert-error" role="alert">
        {{ error.message }}
      </p>

      <h2 class="section-title">
        그룹
      </h2>
      <ul class="list">
        <li v-for="item in groups" :key="item.id">
          <span>
            <strong>{{ item.name }}</strong>
            <span v-if="item.description" class="muted"> · {{ item.description }}</span>
          </span>
          <button type="button" class="btn btn-small" @click="openGroup(item)">
            열기
          </button>
        </li>
      </ul>
      <form class="form" @submit.prevent="submitNewGroup">
        <label class="field">
          새 그룹 이름
          <input v-model="newGroupName" name="newGroupName" required maxlength="100">
        </label>
        <label class="field">
          새 그룹 설명
          <input v-model="newGroupDescription" name="newGroupDescription" maxlength="500">
        </label>
        <button type="submit" class="btn btn-primary">
          그룹 만들기
        </button>
      </form>

      <template v-if="group">
        <h2 class="section-title">
          {{ group.name }} 그룹
        </h2>
        <form class="form" @submit.prevent="saveGroup">
          <label class="field">
            그룹 이름
            <input v-model="groupName" name="groupName" required maxlength="100">
          </label>
          <label class="field">
            그룹 설명
            <input v-model="groupDescription" name="groupDescription" maxlength="500">
          </label>
          <div class="button-row">
            <button type="submit" class="btn btn-primary">
              그룹 저장
            </button>
            <button type="button" class="btn" @click="removeGroup">
              그룹 지우기
            </button>
          </div>
        </form>

        <h3 class="subsection-title">
          구성원
        </h3>
        <p v-if="members.length === 0" class="muted">
          아직 구성원이 없어요.
        </p>
        <ul v-else class="list">
          <li v-for="member in members" :key="member.userId">
            <span>{{ member.email }}</span>
            <button type="button" class="btn btn-small" @click="dropMember(member)">
              빼기
            </button>
          </li>
        </ul>
        <form class="form" @submit.prevent="submitMember">
          <label class="field">
            구성원 이메일
            <input v-model="memberEmail" name="memberEmail" type="email" required placeholder="name@example.com">
          </label>
          <button type="submit" class="btn btn-primary">
            구성원 추가
          </button>
        </form>

        <h3 class="subsection-title">
          역할
        </h3>
        <ul class="list">
          <li v-for="role in roles" :key="role.id">
            <label class="checkbox">
              <input type="checkbox" :checked="groupRoleIds.includes(role.id)" @change="toggleRole(role, checked($event))">
              {{ role.name }}
            </label>
            <span class="muted">{{ role.permissions.join(', ') || '권한 없음' }}</span>
          </li>
        </ul>
      </template>

      <h2 class="section-title">
        역할
      </h2>
      <ul class="list">
        <li v-for="role in roles" :key="role.id">
          <span>
            <strong>{{ role.name }}</strong>
            <span class="muted"> · {{ role.permissions.join(', ') || '권한 없음' }}</span>
          </span>
          <span class="button-row">
            <button type="button" class="btn btn-small" @click="editRole(role)">
              고치기
            </button>
            <button type="button" class="btn btn-small" @click="removeRole(role)">
              지우기
            </button>
          </span>
        </li>
      </ul>
      <form class="form" @submit.prevent="saveRole">
        <label class="field">
          역할 이름
          <input v-model="roleName" name="roleName" required maxlength="100">
        </label>
        <label class="field">
          역할 설명
          <input v-model="roleDescription" name="roleDescription" maxlength="500">
        </label>
        <fieldset class="permissions">
          <legend>권한</legend>
          <label v-for="permission in permissions" :key="permission.code" class="checkbox">
            <input type="checkbox" :checked="rolePermissions.includes(permission.code)" @change="toggleRolePermission(permission.code, checked($event))">
            <code>{{ permission.code }}</code>
            {{ permission.description }}
          </label>
        </fieldset>
        <div class="button-row">
          <button type="submit" class="btn btn-primary">
            {{ editingRole ? '역할 저장' : '역할 만들기' }}
          </button>
          <button v-if="editingRole" type="button" class="btn" @click="editRole(null)">
            취소
          </button>
        </div>
      </form>
    </template>
  </main>
</template>
