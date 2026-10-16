<script lang="ts">
	import { ApiError, forgotPassword } from '#lib/auth.js';

	// 재설정 링크를 메일로 받는다. 계정이 있든 없든 같은 안내를 보인다(가입 여부를 드러내지 않는다).
	let email = $state('');
	let sent = $state(false);
	let busy = $state(false);
	let error = $state<ApiError | null>(null);

	async function submit(event: SubmitEvent) {
		event.preventDefault();
		busy = true;
		error = null;
		try {
			await forgotPassword(email);
			sent = true;
		} catch (caught) {
			error = caught instanceof ApiError ? caught : new ApiError(0, undefined, 'The request failed. Please try again.');
		} finally {
			busy = false;
		}
	}
</script>

<svelte:head>
	<title>Forgot password</title>
</svelte:head>

<main class="page">
	<h1>Forgot password</h1>
	{#if sent}
		<p role="status">
			If an account exists for {email}, we sent a link to reset the password. The link works once within 2 hours.
		</p>
	{:else}
		<form onsubmit={submit}>
			<label>
				Email
				<input name="email" type="email" autocomplete="username" required bind:value={email} />
			</label>
			{#if error}
				<p class="error" role="alert">{error.message}</p>
			{/if}
			<button type="submit" disabled={busy}>Send reset link</button>
		</form>
	{/if}
	<p><a href="/login">Back to log in</a></p>
</main>
