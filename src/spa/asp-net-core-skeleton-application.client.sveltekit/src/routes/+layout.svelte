<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { page } from '$app/state';
	import '../app.css';
	import favicon from '#lib/assets/favicon.svg';
	import { MANAGE_PERMISSION } from '#lib/admin.js';
	import { refreshSession, session, signOut } from '#lib/session.svelte.js';
	import type { LayoutProps } from './$types';

	let { children }: LayoutProps = $props();

	// 현재 사용자를 못 읽으면 로그인 버튼을 보인다 — 세션 값은 그대로 둬서(미확인) 화면들이 다시 묻게 한다.
	let sessionFailed = $state(false);
	let logoutFailed = $state(false);

	// 계정 화면들(로그인 · 비밀번호 찾기 · 재설정 · 이메일 확인)에서는 돌아올 곳을 싣지 않는다 — 링크의 일회용 코드를 다른 주소로 옮기지 않는다.
	const loginHref = $derived(
		['/login', '/forgot-password', '/reset-password', '/confirm-email'].includes(page.url.pathname)
			? '/login'
			: `/login?returnUrl=${encodeURIComponent(page.url.pathname + page.url.search)}`
	);

	onMount(() => {
		refreshSession().catch((error) => {
			console.error(error);
			sessionFailed = true;
		});
	});

	async function logOut() {
		logoutFailed = false;
		try {
			await signOut();
		} catch {
			logoutFailed = true;
			return;
		}
		await goto('/');
	}
</script>

<svelte:head>
	<link rel="icon" href={favicon} />
</svelte:head>

<header class="app-header">
	<a href="/" class="brand">AspNetCoreSkeletonApplication</a>
	<nav class="nav">
		{#if session.user}
			{#if session.user.permissions.includes(MANAGE_PERMISSION)}
				<a href="/admin">관리</a>
			{/if}
			<a href="/account">{session.user.email}</a>
			<button type="button" class="btn btn-small" onclick={logOut}>로그아웃</button>
		{:else if (session.user === null || sessionFailed) && page.url.pathname !== '/login'}
			<a href={loginHref} class="btn btn-primary btn-small">로그인</a>
		{/if}
	</nav>
</header>
{#if logoutFailed}
	<p class="alert alert-error" role="alert">로그아웃하지 못했어요. 잠시 후 다시 시도해 주세요.</p>
{/if}

{@render children()}
