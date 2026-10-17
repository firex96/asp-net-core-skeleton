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
		let user;
		try {
			user = session.user === undefined ? await refreshSession() : session.user;
		} catch (caught) {
			// 로그인했는지 모른다 — 로그인 화면으로 보내지 않고 알린다
			error = caught instanceof ApiError ? caught : ApiError.unexpected();
			return;
		}
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
			error = caught instanceof ApiError ? caught : ApiError.unexpected();
		}
	});
</script>

<svelte:head>
	<title>접근 허용</title>
</svelte:head>

<main class="card">
	{#if details}
		<h1>{details.displayName}에 접근을 허용할까요?</h1>
		<p class="lead">허용하면 이 앱이 회원님 계정으로 아래 일을 할 수 있어요.</p>
		<ul class="list">
			{#each details.scopes as scope (scope.name)}
				<li>
					<span>{scope.description}</span>
					<code class="muted">{scope.name}</code>
				</li>
			{/each}
		</ul>
		<form method="post" action="/connect/authorize">
			{#each parameters as [name, value] (name)}
				<input type="hidden" {name} {value} />
			{/each}
			<div class="actions">
				<button type="submit" name="consent" value="deny" class="btn">거부</button>
				<button type="submit" name="consent" value="accept" class="btn btn-primary">허용</button>
			</div>
		</form>
		<p class="hint">한 번 허용하면 다음부터는 묻지 않아요.</p>
	{:else if !error}
		<p class="muted">불러오는 중이에요…</p>
	{/if}
	{#if error}
		<p class="alert alert-error" role="alert">{error.message}</p>
	{/if}
</main>
