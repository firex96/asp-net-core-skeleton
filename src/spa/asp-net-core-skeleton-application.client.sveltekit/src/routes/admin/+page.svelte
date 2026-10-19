<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import {
		MANAGE_PERMISSION,
		addMember,
		assignRole,
		createGroup,
		createRole,
		deleteGroup,
		deleteRole,
		findUser,
		listGroupRoles,
		listGroups,
		listMembers,
		listPermissions,
		listRoles,
		removeMember,
		unassignRole,
		updateGroup,
		updateRole,
		type Member,
		type Permission,
		type Role,
		type UserGroup
	} from '#lib/admin.js';
	import { ApiError } from '#lib/auth.js';
	import { refreshSession, session } from '#lib/session.svelte.js';

	// 권한 관리 — 사람은 그룹에, 그룹에는 역할을, 역할에는 권한 코드를 준다(authorization.manage 권한). 바꾸면 다음 요청부터 반영된다.
	// 마지막 관리자를 없애는 변경은 서버가 409 로 막고, 그 안내를 그대로 보인다.
	let allowed = $state<boolean | null>(null);
	let groups = $state<UserGroup[]>([]);
	let roles = $state<Role[]>([]);
	let permissions = $state<Permission[]>([]);
	let error = $state<ApiError | null>(null);
	let notice = $state<string | null>(null);

	let newGroupName = $state('');
	let newGroupDescription = $state('');

	let group = $state<UserGroup | null>(null);
	let groupName = $state('');
	let groupDescription = $state('');
	let members = $state<Member[]>([]);
	let groupRoleIds = $state<number[]>([]);
	let memberEmail = $state('');

	let editingRole = $state<Role | null>(null);
	let roleName = $state('');
	let roleDescription = $state('');
	let rolePermissions = $state<string[]>([]);

	onMount(async () => {
		let user;
		try {
			user = session.user === undefined ? await refreshSession() : session.user;
		} catch (caught) {
			// 로그인했는지 모른다 — 로그인 화면으로 보내지 않고 알린다
			error = caught instanceof ApiError ? caught : ApiError.unexpected();
			return;
		}
		if (!user) {
			await goto(`/login?returnUrl=${encodeURIComponent('/admin')}`);
			return;
		}

		allowed = user.permissions.includes(MANAGE_PERMISSION);
		if (allowed) {
			await run(async () => {
				[groups, roles, permissions] = await Promise.all([listGroups(), listRoles(), listPermissions()]);
			});
		}
	});

	async function run(action: () => Promise<void>, done?: string) {
		error = null;
		notice = null;
		try {
			await action();
			notice = done ?? null;
		} catch (caught) {
			error = caught instanceof ApiError ? caught : ApiError.unexpected();
		}
	}

	const reloadGroups = async () => (groups = await listGroups());
	const reloadRoles = async () => (roles = await listRoles());

	/** 실패를 삼키지 않는다 — 부르는 쪽의 run 이 알린다. */
	async function loadGroup(target: UserGroup) {
		[members, groupRoleIds] = await Promise.all([
			listMembers(target.id),
			listGroupRoles(target.id).then((assigned) => assigned.map((role) => role.id))
		]);
		group = target;
		groupName = target.name;
		groupDescription = target.description;
	}

	const openGroup = (target: UserGroup) => run(() => loadGroup(target));

	const submitNewGroup = (event: SubmitEvent) => {
		event.preventDefault();
		return run(async () => {
			const created = await createGroup(newGroupName, newGroupDescription);
			newGroupName = '';
			newGroupDescription = '';
			await reloadGroups();
			await loadGroup(created);
		}, '그룹을 만들었어요.');
	};

	const saveGroup = (event: SubmitEvent) => {
		event.preventDefault();
		return run(async () => {
			group = await updateGroup(group!.id, groupName, groupDescription);
			await reloadGroups();
		}, '그룹을 저장했어요.');
	};

	const removeGroup = () =>
		run(async () => {
			await deleteGroup(group!.id);
			group = null;
			await reloadGroups();
		}, '그룹을 지웠어요.');

	const submitMember = (event: SubmitEvent) => {
		event.preventDefault();
		return run(async () => {
			const user = await findUser(memberEmail);
			await addMember(group!.id, user.userId);
			memberEmail = '';
			members = await listMembers(group!.id);
		}, '구성원을 넣었어요.');
	};

	const dropMember = (member: Member) =>
		run(async () => {
			await removeMember(group!.id, member.userId);
			members = await listMembers(group!.id);
		});

	const toggleRole = (role: Role, checked: boolean) =>
		run(async () => {
			await (checked ? assignRole(group!.id, role.id) : unassignRole(group!.id, role.id));
			groupRoleIds = (await listGroupRoles(group!.id)).map((assigned) => assigned.id);
		});

	function editRole(role: Role | null) {
		editingRole = role;
		roleName = role?.name ?? '';
		roleDescription = role?.description ?? '';
		rolePermissions = [...(role?.permissions ?? [])];
	}

	function toggleRolePermission(code: string, checked: boolean) {
		rolePermissions = checked ? [...rolePermissions, code] : rolePermissions.filter((permission) => permission !== code);
	}

	const saveRole = (event: SubmitEvent) => {
		event.preventDefault();
		const role = { name: roleName, description: roleDescription, permissions: rolePermissions };
		return run(
			async () => {
				await (editingRole ? updateRole(editingRole.id, role) : createRole(role));
				editRole(null);
				await reloadRoles();
			},
			editingRole ? '역할을 저장했어요.' : '역할을 만들었어요.'
		);
	};

	const removeRole = (role: Role) =>
		run(async () => {
			await deleteRole(role.id);
			if (editingRole?.id === role.id) {
				editRole(null);
			}
			await reloadRoles();
		}, '역할을 지웠어요.');
</script>

<svelte:head>
	<title>권한 관리</title>
</svelte:head>

<main class="card card-wide">
	<h1>권한 관리</h1>
	<p class="lead">사람은 그룹에, 그룹에는 역할을, 역할에는 권한을 줘요. 바꾸면 다음 요청부터 반영돼요.</p>

	{#if allowed === false}
		<p class="alert alert-error" role="alert">이 화면을 볼 권한이 없어요.</p>
	{:else if allowed === null && error}
		<p class="alert alert-error" role="alert">{error.message}</p>
	{:else if allowed}
		{#if notice}
			<p class="alert alert-info" role="status">{notice}</p>
		{/if}
		{#if error}
			<p class="alert alert-error" role="alert">{error.message}</p>
		{/if}

		<h2 class="section-title">그룹</h2>
		<ul class="list">
			{#each groups as item (item.id)}
				<li>
					<span>
						<strong>{item.name}</strong>
						{#if item.description}<span class="muted"> · {item.description}</span>{/if}
					</span>
					<button type="button" class="btn btn-small" onclick={() => openGroup(item)}>열기</button>
				</li>
			{/each}
		</ul>
		<form class="form" onsubmit={submitNewGroup}>
			<label class="field">
				새 그룹 이름
				<input name="newGroupName" required maxlength="100" bind:value={newGroupName} />
			</label>
			<label class="field">
				새 그룹 설명
				<input name="newGroupDescription" maxlength="500" bind:value={newGroupDescription} />
			</label>
			<button type="submit" class="btn btn-primary">그룹 만들기</button>
		</form>

		{#if group}
			<h2 class="section-title">{group.name} 그룹</h2>
			<form class="form" onsubmit={saveGroup}>
				<label class="field">
					그룹 이름
					<input name="groupName" required maxlength="100" bind:value={groupName} />
				</label>
				<label class="field">
					그룹 설명
					<input name="groupDescription" maxlength="500" bind:value={groupDescription} />
				</label>
				<div class="button-row">
					<button type="submit" class="btn btn-primary">그룹 저장</button>
					<button type="button" class="btn" onclick={removeGroup}>그룹 지우기</button>
				</div>
			</form>

			<h3 class="subsection-title">구성원</h3>
			{#if members.length === 0}
				<p class="muted">아직 구성원이 없어요.</p>
			{:else}
				<ul class="list">
					{#each members as member (member.userId)}
						<li>
							<span>{member.email}</span>
							<button type="button" class="btn btn-small" onclick={() => dropMember(member)}>빼기</button>
						</li>
					{/each}
				</ul>
			{/if}
			<form class="form" onsubmit={submitMember}>
				<label class="field">
					구성원 이메일
					<input name="memberEmail" type="email" required placeholder="name@example.com" bind:value={memberEmail} />
				</label>
				<button type="submit" class="btn btn-primary">구성원 추가</button>
			</form>

			<h3 class="subsection-title">역할</h3>
			<ul class="list">
				{#each roles as role (role.id)}
					<li>
						<label class="checkbox">
							<input
								type="checkbox"
								checked={groupRoleIds.includes(role.id)}
								onchange={(event) => toggleRole(role, event.currentTarget.checked)}
							/>
							{role.name}
						</label>
						<span class="muted">{role.permissions.join(', ') || '권한 없음'}</span>
					</li>
				{/each}
			</ul>
		{/if}

		<h2 class="section-title">역할</h2>
		<ul class="list">
			{#each roles as role (role.id)}
				<li>
					<span>
						<strong>{role.name}</strong>
						<span class="muted"> · {role.permissions.join(', ') || '권한 없음'}</span>
					</span>
					<span class="button-row">
						<button type="button" class="btn btn-small" onclick={() => editRole(role)}>고치기</button>
						<button type="button" class="btn btn-small" onclick={() => removeRole(role)}>지우기</button>
					</span>
				</li>
			{/each}
		</ul>
		<form class="form" onsubmit={saveRole}>
			<label class="field">
				역할 이름
				<input name="roleName" required maxlength="100" bind:value={roleName} />
			</label>
			<label class="field">
				역할 설명
				<input name="roleDescription" maxlength="500" bind:value={roleDescription} />
			</label>
			<fieldset class="permissions">
				<legend>권한</legend>
				{#each permissions as permission (permission.code)}
					<label class="checkbox">
						<input
							type="checkbox"
							checked={rolePermissions.includes(permission.code)}
							onchange={(event) => toggleRolePermission(permission.code, event.currentTarget.checked)}
						/>
						<code>{permission.code}</code>
						{permission.description}
					</label>
				{/each}
			</fieldset>
			<div class="button-row">
				<button type="submit" class="btn btn-primary">{editingRole ? '역할 저장' : '역할 만들기'}</button>
				{#if editingRole}
					<button type="button" class="btn" onclick={() => editRole(null)}>취소</button>
				{/if}
			</div>
		</form>
	{/if}
</main>
