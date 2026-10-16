<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { page } from '$app/state';
	import { ApiError, safeReturnUrl } from '#lib/auth.js';
	import { refreshSession, session, signIn, signUp } from '#lib/session.svelte.js';

	// 로그인과 가입을 한 화면에서 전환한다. 성공하면 returnUrl(같은 출처 경로만)로 돌아간다.
	let mode = $state<'login' | 'register'>('login');
	let email = $state('');
	let password = $state('');
	let confirmPassword = $state('');
	let error = $state<ApiError | null>(null);
	let busy = $state(false);

	const returnUrl = $derived(safeReturnUrl(page.url.searchParams.get('returnUrl')));

	onMount(async () => {
		const user = session.user === undefined ? await refreshSession() : session.user;
		if (user) {
			await goto(returnUrl);
		}
	});

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
			await goto(returnUrl);
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

<main>
	<h1>{mode === 'login' ? 'Log in' : 'Create account'}</h1>

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

	<p>
		<button type="button" class="link" onclick={toggleMode}>
			{mode === 'login' ? 'Need an account? Create one' : 'Already have an account? Log in'}
		</button>
	</p>
</main>

<style>
	main {
		max-width: 24rem;
		margin: 2rem auto;
	}

	form {
		display: flex;
		flex-direction: column;
		gap: 0.75rem;
	}

	label {
		display: flex;
		flex-direction: column;
		gap: 0.25rem;
	}

	.error {
		color: #b00020;
	}

	.link {
		background: none;
		border: none;
		color: inherit;
		text-decoration: underline;
		cursor: pointer;
	}
</style>
