<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { page } from '$app/state';
	import {
		ApiError,
		externalLoginMessage,
		getTwoFactorChallenge,
		isServerPath,
		listProviders,
		resendConfirmation,
		safeReturnUrl,
		type ExternalProvider,
		type TwoFactorProvider
	} from '#lib/auth.js';
	import TwoFactorStep from '#lib/TwoFactorStep.svelte';
	import { refreshSession, session, signIn, signInWithPasskey, signUp } from '#lib/session.svelte.js';
	import { isPasskeyCancelled, isPasskeySupported } from '#lib/webauthn.js';

	// 로그인과 가입을 한 화면에서 전환한다. 로그인에 성공하면 returnUrl(같은 출처 경로만)로 돌아간다 — OAuth 서버의 authorize 같은
	// 서버 주소면 브라우저 이동으로 간다. 이미 로그인돼 있으면 곧바로 돌아간다. 가입하면 확인 메일 안내를 보이고 로그인 화면으로 돌아온다
	// (이메일을 확인해야 로그인할 수 있다). 2단계 인증을 켠 계정은 두 번째 단계(TwoFactorStep)를 거친다 — 소셜 로그인 뒤에는
	// 서버가 /login?step=two-factor 로 보낸다. 소셜 로그인은 공급자마다 폼 POST 로 시작한다(연결된 계정만).
	let mode = $state<'login' | 'register'>('login');
	let email = $state('');
	let password = $state('');
	let confirmPassword = $state('');
	let error = $state<ApiError | null>(null);
	let busy = $state(false);
	let providers = $state<ExternalProvider[]>([]);
	let providersFailed = $state(false);
	let message = $state<string | null>(null);
	let twoFactor = $state<TwoFactorProvider[] | null>(null);
	let passkeySupported = $state(false);

	const returnUrl = $derived(safeReturnUrl(page.url.searchParams.get('returnUrl')));
	const notice = $derived(
		externalLoginMessage(page.url.searchParams.get('error'), page.url.searchParams.get('provider')) ??
			(page.url.searchParams.get('reset') === 'done'
				? '비밀번호를 바꿨어요. 새 비밀번호로 로그인해 주세요.'
				: null)
	);

	onMount(async () => {
		// 현재 사용자를 못 읽으면 로그인 안 된 것으로 보고 화면을 그대로 준비한다 — 로그인 폼 · 소셜 로그인이 막히지 않게.
		const user = session.user === undefined ? await refreshSession().catch(() => null) : session.user;
		if (user) {
			await continueTo(returnUrl);
			return;
		}

		if (page.url.searchParams.get('step') === 'two-factor') {
			try {
				twoFactor = await getTwoFactorChallenge();
				if (!twoFactor) {
					message = '로그인 시간이 지났어요. 처음부터 다시 로그인해 주세요.';
				}
			} catch (caught) {
				error = caught instanceof ApiError ? caught : ApiError.unexpected();
			}
		}

		passkeySupported = isPasskeySupported();
		providers = await listProviders().catch(() => {
			providersFailed = true;
			return [];
		});
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
		message = null;
	}

	async function submit(event: SubmitEvent) {
		event.preventDefault();
		if (mode === 'register' && password !== confirmPassword) {
			error = new ApiError(400, undefined, '비밀번호가 서로 달라요. 다시 입력해 주세요.');
			return;
		}

		busy = true;
		error = null;
		message = null;
		try {
			if (mode === 'login') {
				twoFactor = await signIn(email, password);
				if (!twoFactor) {
					await continueTo(returnUrl);
				}
			} else {
				await signUp(email, password);
				mode = 'login';
				password = '';
				confirmPassword = '';
				message = `${email}로 확인 메일을 보냈어요. 메일의 링크를 열어 이메일을 확인한 뒤 로그인해 주세요.`;
			}
		} catch (caught) {
			error = caught instanceof ApiError ? caught : ApiError.unexpected();
		} finally {
			busy = false;
		}
	}

	async function loginWithPasskey() {
		busy = true;
		error = null;
		message = null;
		try {
			await signInWithPasskey();
			await continueTo(returnUrl);
		} catch (caught) {
			error = isPasskeyCancelled(caught)
				? new ApiError(0, undefined, '패스키 로그인을 취소했어요.')
				: caught instanceof ApiError
					? caught
					: ApiError.unexpected();
		} finally {
			busy = false;
		}
	}

	function restart() {
		twoFactor = null;
		password = '';
		message = null;
	}

	async function resend() {
		error = null;
		try {
			await resendConfirmation(email);
			message = `${email}로 확인 메일을 다시 보냈어요. 메일함을 확인해 주세요.`;
		} catch (caught) {
			error = caught instanceof ApiError ? caught : ApiError.unexpected();
		}
	}
</script>

<svelte:head>
	<title>{twoFactor ? '2단계 인증' : mode === 'login' ? '로그인' : '회원가입'}</title>
</svelte:head>

<main class="card">
	{#if twoFactor}
		<TwoFactorStep providers={twoFactor} onDone={() => continueTo(returnUrl)} onRestart={restart} />
	{:else}
	<h1>{mode === 'login' ? '로그인' : '회원가입'}</h1>
	<p class="lead">{mode === 'login' ? '이메일과 비밀번호로 로그인해 주세요.' : '이메일로 새 계정을 만들어요.'}</p>

	{#if message ?? notice}
		<p class="alert alert-info" role="status">{message ?? notice}</p>
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
				{#if error.code === 'EMAIL_NOT_CONFIRMED_ERROR'}
					<p><button type="button" class="link-button" onclick={resend}>확인 메일 다시 받기</button></p>
				{/if}
			</div>
		{/if}

		<button type="submit" class="btn btn-primary btn-block" disabled={busy}>
			{mode === 'login' ? '로그인' : '가입하기'}
		</button>
	</form>

	{#if mode === 'login'}
		{#if passkeySupported || providers.length > 0}
			<div class="divider">또는</div>
			<div class="social">
				{#if passkeySupported}
					<button type="button" class="btn btn-block" disabled={busy} onclick={loginWithPasskey}>패스키로 로그인</button>
				{/if}
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
		{#if providersFailed}
			<p class="alert alert-error" role="alert">소셜 로그인 목록을 불러오지 못했어요. 잠시 뒤 새로고침해 주세요.</p>
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
	{/if}
</main>
