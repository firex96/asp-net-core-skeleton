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
			error = caught instanceof ApiError ? caught : ApiError.unexpected();
		} finally {
			busy = false;
		}
	}
</script>

<svelte:head>
	<title>비밀번호 찾기</title>
</svelte:head>

<main class="card">
	<h1>비밀번호 찾기</h1>
	{#if sent}
		<p class="alert alert-info" role="status">
			{email}로 가입한 계정이 있다면 비밀번호 재설정 링크를 보냈어요. 메일함을 확인해 주세요. 링크는 2시간 동안 한 번만
			쓸 수 있어요.
		</p>
	{:else}
		<p class="lead">가입한 이메일을 입력하면 비밀번호를 다시 정할 수 있는 링크를 보내 드려요.</p>
		<form class="form" onsubmit={submit}>
			<label class="field">
				이메일
				<input
					name="email"
					type="email"
					autocomplete="username"
					placeholder="name@example.com"
					required
					bind:value={email}
				/>
			</label>
			{#if error}
				<p class="alert alert-error" role="alert">{error.message}</p>
			{/if}
			<button type="submit" class="btn btn-primary btn-block" disabled={busy}>재설정 링크 받기</button>
		</form>
	{/if}
	<p class="links"><a href="/login">로그인으로 돌아가기</a></p>
</main>
