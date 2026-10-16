<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { page } from '$app/state';
	import '../app.css';
	import favicon from '#lib/assets/favicon.svg';
	import { refreshSession, session, signOut } from '#lib/session.svelte.js';
	import type { LayoutProps } from './$types';

	let { children }: LayoutProps = $props();

	// 계정 화면들(로그인 · 비밀번호 찾기 · 재설정)에서는 돌아올 곳을 싣지 않는다 — 재설정 주소의 일회용 코드를 다른 주소로 옮기지 않는다.
	const loginHref = $derived(
		['/login', '/forgot-password', '/reset-password'].includes(page.url.pathname)
			? '/login'
			: `/login?returnUrl=${encodeURIComponent(page.url.pathname + page.url.search)}`
	);

	onMount(() => {
		refreshSession().catch((error) => console.error(error));
	});

	async function logOut() {
		await signOut();
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
			<a href="/account">{session.user.email}</a>
			<button type="button" class="btn btn-small" onclick={logOut}>로그아웃</button>
		{:else if session.user === null && page.url.pathname !== '/login'}
			<a href={loginHref} class="btn btn-primary btn-small">로그인</a>
		{/if}
	</nav>
</header>

{@render children()}
