<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { page } from '$app/state';
	import {
		ApiError,
		externalLoginMessage,
		listLogins,
		listProviders,
		providerName,
		unlink,
		type ExternalLogin,
		type ExternalProvider
	} from '#lib/auth.js';
	import { refreshSession, session } from '#lib/session.svelte.js';
	import PasskeySettings from '#lib/PasskeySettings.svelte';
	import TwoFactorSettings from '#lib/TwoFactorSettings.svelte';

	// 내 계정 — 권한, 2단계 인증, 패스키, 연결된 소셜 계정. 연결은 공급자마다 폼 POST 로 시작하고, 돌아오면 ?linked= · ?error= 로 결과를 받는다.
	let providers = $state<ExternalProvider[]>([]);
	let logins = $state<ExternalLogin[]>([]);
	let error = $state<ApiError | null>(null);
	let notice = $state<string | null>(null);

	const linked = $derived(new Set(logins.map((login) => login.provider)));

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
			await goto(`/login?returnUrl=${encodeURIComponent('/account')}`);
			return;
		}

		const linkedProvider = page.url.searchParams.get('linked');
		notice = linkedProvider
			? `${providerName(linkedProvider)} 계정을 연결했어요. 이제 ${providerName(linkedProvider)}로 로그인할 수 있어요.`
			: externalLoginMessage(page.url.searchParams.get('error'), page.url.searchParams.get('provider'));
		try {
			await load();
		} catch (caught) {
			error = caught instanceof ApiError ? caught : ApiError.unexpected();
		}
	});

	async function load() {
		[providers, logins] = await Promise.all([listProviders(), listLogins()]);
	}

	async function remove(provider: string) {
		error = null;
		notice = null;
		try {
			await unlink(provider);
			await load();
		} catch (caught) {
			error = caught instanceof ApiError ? caught : ApiError.unexpected();
		}
	}
</script>

<svelte:head>
	<title>내 계정</title>
</svelte:head>

<main class="card card-wide">
	<h1>내 계정</h1>
	{#if session.user}
		<p class="lead">{session.user.email}</p>

		<h2 class="section-title">권한</h2>
		{#if session.user.permissions.length === 0}
			<p class="muted">아직 받은 권한이 없어요.</p>
		{:else}
			<ul class="tags">
				{#each session.user.permissions as permission (permission)}
					<li class="tag"><code>{permission}</code></li>
				{/each}
			</ul>
		{/if}

		<TwoFactorSettings />

		<PasskeySettings />

		<h2 class="section-title">연결된 계정</h2>
		<p class="muted">연결해 두면 비밀번호 없이 그 계정으로 로그인할 수 있어요.</p>
		{#if notice}
			<p class="alert alert-info" role="status">{notice}</p>
		{/if}
		{#if error}
			<p class="alert alert-error" role="alert">{error.message}</p>
		{/if}
		<ul class="list">
			{#each providers as provider (provider.name)}
				<li>
					<span>
						<strong>{provider.displayName}</strong>
						<span class="muted"> · {linked.has(provider.name) ? '연결됨' : '연결 안 됨'}</span>
					</span>
					{#if linked.has(provider.name)}
						<button type="button" class="btn btn-small" onclick={() => remove(provider.name)}>연결 끊기</button>
					{:else}
						<form method="post" action={`/api/auth/external/${encodeURIComponent(provider.name)}/link`}>
							<button type="submit" class="btn btn-primary btn-small">연결하기</button>
						</form>
					{/if}
				</li>
			{/each}
		</ul>
	{:else if error}
		<p class="alert alert-error" role="alert">{error.message}</p>
	{/if}
</main>
