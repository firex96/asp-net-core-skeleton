<script lang="ts">
	import { onMount } from 'svelte';
	import { page } from '$app/state';
	import { ApiError, confirmEmail } from '#lib/auth.js';

	// 메일 링크(/confirm-email?userId=…&code=…)로 들어와 이메일을 확인한다. 로그인은 확인한 뒤 따로 한다.
	let status = $state<'pending' | 'done' | 'failed'>('pending');
	let error = $state<ApiError | null>(null);

	onMount(async () => {
		try {
			await confirmEmail(page.url.searchParams.get('userId') ?? '', page.url.searchParams.get('code') ?? '');
			status = 'done';
		} catch (caught) {
			error = caught instanceof ApiError ? caught : ApiError.unexpected();
			status = 'failed';
		}
	});
</script>

<svelte:head>
	<title>이메일 확인</title>
</svelte:head>

<main class="card">
	<h1>이메일 확인</h1>
	{#if status === 'pending'}
		<p class="muted">확인하는 중이에요…</p>
	{:else if status === 'done'}
		<p class="alert alert-info" role="status">이메일을 확인했어요. 이제 로그인할 수 있어요.</p>
		<a href="/login" class="btn btn-primary btn-block">로그인하기</a>
	{:else}
		<p class="alert alert-error" role="alert">{error?.message}</p>
		<p class="links"><a href="/login">로그인으로 돌아가기</a></p>
	{/if}
</main>
