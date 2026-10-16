<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { page } from '$app/state';
	import {
		ApiError,
		externalLoginMessage,
		listLogins,
		listProviders,
		unlink,
		type ExternalLogin,
		type ExternalProvider
	} from '#lib/auth.js';
	import { refreshSession, session } from '#lib/session.svelte.js';

	// 내 계정 — 권한과 연결된 소셜 계정. 연결은 공급자마다 폼 POST 로 시작하고, 돌아오면 ?linked= · ?error= 로 결과를 받는다.
	let providers = $state<ExternalProvider[]>([]);
	let logins = $state<ExternalLogin[]>([]);
	let error = $state<ApiError | null>(null);
	let notice = $state<string | null>(null);

	const linked = $derived(new Set(logins.map((login) => login.provider)));

	onMount(async () => {
		const user = session.user === undefined ? await refreshSession() : session.user;
		if (!user) {
			await goto(`/login?returnUrl=${encodeURIComponent('/account')}`);
			return;
		}

		const linkedProvider = page.url.searchParams.get('linked');
		notice = linkedProvider
			? `Your ${linkedProvider} account is linked. You can now log in with it.`
			: externalLoginMessage(page.url.searchParams.get('error'), page.url.searchParams.get('provider'));
		await load();
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
			error = caught instanceof ApiError ? caught : new ApiError(0, undefined, 'The request failed. Please try again.');
		}
	}
</script>

<svelte:head>
	<title>Account</title>
</svelte:head>

<main class="page">
	<h1>Account</h1>
	{#if session.user}
		<p>{session.user.email}</p>
		<h2>Permissions</h2>
		{#if session.user.permissions.length === 0}
			<p>None</p>
		{:else}
			<ul>
				{#each session.user.permissions as permission (permission)}
					<li><code>{permission}</code></li>
				{/each}
			</ul>
		{/if}

		<h2>Linked accounts</h2>
		{#if notice}
			<p class="notice" role="status">{notice}</p>
		{/if}
		{#if error}
			<p class="error" role="alert">{error.message}</p>
		{/if}
		<ul class="providers">
			{#each providers as provider (provider.name)}
				<li>
					<span>{provider.displayName}</span>
					{#if linked.has(provider.name)}
						<button type="button" onclick={() => remove(provider.name)}>Unlink</button>
					{:else}
						<form method="post" action={`/api/auth/external/${encodeURIComponent(provider.name)}/link`}>
							<button type="submit">Link</button>
						</form>
					{/if}
				</li>
			{/each}
		</ul>
	{/if}
</main>
