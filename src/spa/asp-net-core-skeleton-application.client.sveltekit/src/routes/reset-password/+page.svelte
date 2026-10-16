<script lang="ts">
	import { goto } from '$app/navigation';
	import { page } from '$app/state';
	import { ApiError, resetPassword } from '#lib/auth.js';

	// 메일 링크(/reset-password?email=…&code=…)로 들어와 새 비밀번호를 정한다. 성공하면 로그인 화면으로 간다.
	const email = page.url.searchParams.get('email') ?? '';
	const code = page.url.searchParams.get('code') ?? '';
	let password = $state('');
	let confirmPassword = $state('');
	let busy = $state(false);
	let error = $state<ApiError | null>(null);

	async function submit(event: SubmitEvent) {
		event.preventDefault();
		if (password !== confirmPassword) {
			error = new ApiError(400, undefined, 'Passwords do not match.');
			return;
		}

		busy = true;
		error = null;
		try {
			await resetPassword(email, code, password);
			await goto('/login?reset=done');
		} catch (caught) {
			error = caught instanceof ApiError ? caught : new ApiError(0, undefined, 'The request failed. Please try again.');
		} finally {
			busy = false;
		}
	}
</script>

<svelte:head>
	<title>Reset password</title>
</svelte:head>

<main class="page">
	<h1>Reset password</h1>
	<p>For {email}</p>
	<form onsubmit={submit}>
		<label>
			New password
			<input name="password" type="password" autocomplete="new-password" required bind:value={password} />
		</label>
		<label>
			Confirm new password
			<input
				name="confirmPassword"
				type="password"
				autocomplete="new-password"
				required
				bind:value={confirmPassword}
			/>
		</label>
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
				{#if error.code === 'INVALID_TOKEN_ERROR'}
					<p><a href="/forgot-password">Request a new link</a></p>
				{/if}
			</div>
		{/if}
		<button type="submit" disabled={busy}>Change password</button>
	</form>
</main>
