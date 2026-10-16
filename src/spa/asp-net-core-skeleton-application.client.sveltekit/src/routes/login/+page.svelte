<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { page } from '$app/state';
	import {
		ApiError,
		externalLoginMessage,
		isServerPath,
		listProviders,
		safeReturnUrl,
		type ExternalProvider
	} from '#lib/auth.js';
	import { refreshSession, session, signIn, signUp } from '#lib/session.svelte.js';

	// 로그인과 가입을 한 화면에서 전환한다. 성공하면 returnUrl(같은 출처 경로만)로 돌아간다 — OAuth 서버의 authorize 같은
	// 서버 주소면 브라우저 이동으로 간다. 이미 로그인돼 있으면 곧바로 돌아간다. 소셜 로그인은 공급자마다 폼 POST 로 시작한다(연결된 계정만).
	let mode = $state<'login' | 'register'>('login');
	let email = $state('');
	let password = $state('');
	let confirmPassword = $state('');
	let error = $state<ApiError | null>(null);
	let busy = $state(false);
	let providers = $state<ExternalProvider[]>([]);

	const returnUrl = $derived(safeReturnUrl(page.url.searchParams.get('returnUrl')));
	const notice = $derived(
		externalLoginMessage(page.url.searchParams.get('error'), page.url.searchParams.get('provider')) ??
			(page.url.searchParams.get('reset') === 'done'
				? 'Your password was changed. Log in with the new password.'
				: null)
	);

	onMount(async () => {
		const user = session.user === undefined ? await refreshSession() : session.user;
		if (user) {
			await continueTo(returnUrl);
			return;
		}

		providers = await listProviders().catch(() => []);
	});

	async function continueTo(target: string) {
		if (isServerPath(target)) {
			window.location.assign(target);
			return;
		}

		await goto(target);
	}

	function toggleMode() {
		mode = mode === 'login' ? 'register' : 'login';
		error = null;
	}

	async function submit(event: SubmitEvent) {
		event.preventDefault();
		if (mode === 'register' && password !== confirmPassword) {
			error = new ApiError(400, undefined, 'Passwords do not match.');
			return;
		}

		busy = true;
		error = null;
		try {
			await (mode === 'login' ? signIn(email, password) : signUp(email, password));
			await continueTo(returnUrl);
		} catch (caught) {
			error = caught instanceof ApiError ? caught : new ApiError(0, undefined, 'The request failed. Please try again.');
		} finally {
			busy = false;
		}
	}
</script>

<svelte:head>
	<title>{mode === 'login' ? 'Log in' : 'Create account'}</title>
</svelte:head>

<main class="page">
	<h1>{mode === 'login' ? 'Log in' : 'Create account'}</h1>

	{#if notice}
		<p class="notice" role="status">{notice}</p>
	{/if}

	<form onsubmit={submit}>
		<label>
			Email
			<input name="email" type="email" autocomplete="username" required bind:value={email} />
		</label>
		<label>
			Password
			<input
				name="password"
				type="password"
				autocomplete={mode === 'login' ? 'current-password' : 'new-password'}
				required
				bind:value={password}
			/>
		</label>
		{#if mode === 'register'}
			<label>
				Confirm password
				<input
					name="confirmPassword"
					type="password"
					autocomplete="new-password"
					required
					bind:value={confirmPassword}
				/>
			</label>
		{/if}

		{#if error}
			<div class="error" role="alert">
				<p>{error.message}</p>
				{#if error.fieldErrors.length > 0}
					<ul>
						{#each error.fieldErrors as message (message)}
							<li>{message}</li>
						{/each}
					</ul>
				{/if}
			</div>
		{/if}

		<button type="submit" disabled={busy}>{mode === 'login' ? 'Log in' : 'Create account'}</button>
	</form>

	{#if mode === 'login'}
		<p><a href="/forgot-password">Forgot your password?</a></p>

		{#if providers.length > 0}
			<div class="social">
				{#each providers as provider (provider.name)}
					<form
						method="post"
						action={`/api/auth/external/${encodeURIComponent(provider.name)}/login?returnUrl=${encodeURIComponent(returnUrl)}`}
					>
						<button type="submit">Continue with {provider.displayName}</button>
					</form>
				{/each}
			</div>
		{/if}
	{/if}

	<p>
		<button type="button" class="link" onclick={toggleMode}>
			{mode === 'login' ? 'Need an account? Create one' : 'Already have an account? Log in'}
		</button>
	</p>
</main>
