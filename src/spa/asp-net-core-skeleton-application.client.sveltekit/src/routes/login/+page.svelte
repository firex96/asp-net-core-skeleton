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
				? '비밀번호를 바꿨어요. 새 비밀번호로 로그인해 주세요.'
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
			error = new ApiError(400, undefined, '비밀번호가 서로 달라요. 다시 입력해 주세요.');
			return;
		}

		busy = true;
		error = null;
		try {
			await (mode === 'login' ? signIn(email, password) : signUp(email, password));
			await continueTo(returnUrl);
		} catch (caught) {
			error = caught instanceof ApiError ? caught : ApiError.unexpected();
		} finally {
			busy = false;
		}
	}
</script>

<svelte:head>
	<title>{mode === 'login' ? '로그인' : '회원가입'}</title>
</svelte:head>

<main class="card">
	<h1>{mode === 'login' ? '로그인' : '회원가입'}</h1>
	<p class="lead">{mode === 'login' ? '이메일과 비밀번호로 로그인해 주세요.' : '이메일로 새 계정을 만들어요.'}</p>

	{#if notice}
		<p class="alert alert-info" role="status">{notice}</p>
	{/if}

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
		<label class="field">
			비밀번호
			<input
				name="password"
				type="password"
				autocomplete={mode === 'login' ? 'current-password' : 'new-password'}
				required
				bind:value={password}
			/>
		</label>
		{#if mode === 'register'}
			<label class="field">
				비밀번호 확인
				<input
					name="confirmPassword"
					type="password"
					autocomplete="new-password"
					required
					bind:value={confirmPassword}
				/>
			</label>
			<p class="hint">10자 이상, 영문 대문자 · 소문자와 숫자, 특수 문자를 섞어 주세요.</p>
		{/if}

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
			</div>
		{/if}

		<button type="submit" class="btn btn-primary btn-block" disabled={busy}>
			{mode === 'login' ? '로그인' : '가입하기'}
		</button>
	</form>

	{#if mode === 'login'}
		{#if providers.length > 0}
			<div class="divider">또는</div>
			<div class="social">
				{#each providers as provider (provider.name)}
					<form
						method="post"
						action={`/api/auth/external/${encodeURIComponent(provider.name)}/login?returnUrl=${encodeURIComponent(returnUrl)}`}
					>
						<button type="submit" class={`btn btn-block btn-${provider.name.toLowerCase()}`}>
							{provider.displayName}로 계속하기
						</button>
					</form>
				{/each}
			</div>
		{/if}

		<p class="links">
			<a href="/forgot-password">비밀번호를 잊으셨나요?</a>
			<button type="button" class="link-button" onclick={toggleMode}>회원가입</button>
		</p>
	{:else}
		<p class="links">
			<span class="muted">이미 계정이 있나요?</span>
			<button type="button" class="link-button" onclick={toggleMode}>로그인</button>
		</p>
	{/if}
</main>
