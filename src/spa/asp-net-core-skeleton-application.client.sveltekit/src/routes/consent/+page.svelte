<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { page } from '$app/state';
	import { ApiError, getConsent, type ConsentDetails } from '#lib/auth.js';
	import { refreshSession, session } from '#lib/session.svelte.js';

	// OAuth 동의 — 인가 서버가 /consent?…(authorize 요청 그대로) 로 보낸다. 허락 · 거절은 같은 요청을 consent=accept|deny 와 함께
	// /connect/authorize 로 폼 POST 해 인가 서버가 처리한다(허락은 저장돼 다시 묻지 않는다).
	const parameters = [...page.url.searchParams.entries()];
	let details = $state<ConsentDetails | null>(null);
	let error = $state<ApiError | null>(null);

	onMount(async () => {
		const user = session.user === undefined ? await refreshSession() : session.user;
		if (!user) {
			await goto(`/login?returnUrl=${encodeURIComponent(page.url.pathname + page.url.search)}`);
			return;
		}

		try {
			details = await getConsent(
				page.url.searchParams.get('client_id') ?? '',
				page.url.searchParams.get('scope') ?? ''
			);
		} catch (caught) {
			error = caught instanceof ApiError ? caught : new ApiError(0, undefined, 'The request failed. Please try again.');
		}
	});
</script>

<svelte:head>
	<title>Allow access?</title>
</svelte:head>

<main class="page">
	<h1>Allow access?</h1>
	{#if details}
		<p><strong>{details.displayName}</strong> wants to:</p>
		<ul>
			{#each details.scopes as scope (scope.name)}
				<li>{scope.description} <code>{scope.name}</code></li>
			{/each}
		</ul>
		<form method="post" action="/connect/authorize">
			{#each parameters as [name, value] (name)}
				<input type="hidden" {name} {value} />
			{/each}
			<button type="submit" name="consent" value="accept">Allow</button>
			<button type="submit" name="consent" value="deny">Deny</button>
		</form>
	{/if}
	{#if error}
		<p class="error" role="alert">{error.message}</p>
	{/if}
</main>
