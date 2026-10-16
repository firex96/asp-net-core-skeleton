<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { page } from '$app/state';
	import favicon from '#lib/assets/favicon.svg';
	import { refreshSession, session, signOut } from '#lib/session.svelte.js';
	import type { LayoutProps } from './$types';

	let { children }: LayoutProps = $props();

	const loginHref = $derived(
		page.url.pathname === '/login'
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

<header>
	<a href="/">AspNetCoreSkeletonApplication</a>
	<nav>
		{#if session.user}
			<span class="user">{session.user.email}</span>
			<button type="button" onclick={logOut}>Log out</button>
		{:else if session.user === null}
			<a href={loginHref}>Log in</a>
		{/if}
	</nav>
</header>

{@render children()}

<style>
	header {
		display: flex;
		justify-content: space-between;
		align-items: center;
		padding: 0.75rem 2rem;
		border-bottom: 1px solid #ddd;
	}

	nav {
		display: flex;
		align-items: center;
		gap: 0.75rem;
	}
</style>
