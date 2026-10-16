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
			error = new ApiError(400, undefined, '비밀번호가 서로 달라요. 다시 입력해 주세요.');
			return;
		}

		busy = true;
		error = null;
		try {
			await resetPassword(email, code, password);
			await goto('/login?reset=done');
		} catch (caught) {
			error = caught instanceof ApiError ? caught : ApiError.unexpected();
		} finally {
			busy = false;
		}
	}
</script>

<svelte:head>
	<title>비밀번호 재설정</title>
</svelte:head>

<main class="card">
	<h1>새 비밀번호 정하기</h1>
	<p class="lead">{email} 계정에서 쓸 새 비밀번호를 입력해 주세요.</p>
	<form class="form" onsubmit={submit}>
		<label class="field">
			새 비밀번호
			<input name="password" type="password" autocomplete="new-password" required bind:value={password} />
		</label>
		<label class="field">
			새 비밀번호 확인
			<input
				name="confirmPassword"
				type="password"
				autocomplete="new-password"
				required
				bind:value={confirmPassword}
			/>
		</label>
		<p class="hint">10자 이상, 영문 대문자 · 소문자와 숫자, 특수 문자를 섞어 주세요.</p>
		{#if error}
			<div class="alert alert-error" role="alert">
				<p>{error.message}</p>
				{#if error.fieldErrors.length > 0}
					<ul>
						{#each error.fieldErrors as message (message)}
							<li>{message}</li>
						{/each}
					</ul>
				{/if}
				{#if error.code === 'INVALID_TOKEN_ERROR'}
					<p><a href="/forgot-password">재설정 링크 다시 받기</a></p>
				{/if}
			</div>
		{/if}
		<button type="submit" class="btn btn-primary btn-block" disabled={busy}>비밀번호 바꾸기</button>
	</form>
</main>
