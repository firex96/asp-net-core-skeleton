<script lang="ts">
	import { onMount } from 'svelte';
	import { ApiError, sendTwoFactorEmail, type TwoFactorProvider } from './auth.js';
	import { finishTwoFactor } from './session.svelte.js';

	// 로그인 두 번째 단계 — 계정의 수단(인증 앱 또는 메일 코드)으로 확인하고, 그 수단을 쓸 수 없으면 복구 코드로 한다.
	// 메일 코드는 이 단계에 들어오면 한 번 보내고, 다시 받을 수 있다.
	let {
		providers,
		onDone,
		onRestart
	}: { providers: TwoFactorProvider[]; onDone: () => Promise<void>; onRestart: () => void } = $props();

	const primary = $derived(providers.find((provider) => provider !== 'recovery') ?? 'recovery');
	let chosen = $state<TwoFactorProvider | null>(null);
	const method = $derived(chosen ?? primary);
	let code = $state('');
	let rememberBrowser = $state(false);
	let error = $state<ApiError | null>(null);
	let notice = $state<string | null>(null);
	let busy = $state(false);

	const lead = $derived(
		method === 'authenticator'
			? '인증 앱에 보이는 6자리 코드를 입력해 주세요.'
			: method === 'email'
				? '메일로 받은 6자리 코드를 입력해 주세요.'
				: '복구 코드 하나를 입력해 주세요. 한 번 쓴 코드는 다시 쓸 수 없어요.'
	);

	onMount(() => {
		if (primary === 'email') {
			void sendCode(false);
		}
	});

	async function sendCode(again: boolean) {
		error = null;
		try {
			await sendTwoFactorEmail();
			notice = again ? '확인 코드를 다시 보냈어요. 메일함을 확인해 주세요.' : '메일로 확인 코드를 보냈어요. 메일함을 확인해 주세요.';
		} catch (caught) {
			error = caught instanceof ApiError ? caught : ApiError.unexpected();
		}
	}

	function choose(next: TwoFactorProvider) {
		chosen = next;
		code = '';
		error = null;
		notice = null;
	}

	async function submit(event: SubmitEvent) {
		event.preventDefault();
		busy = true;
		error = null;
		try {
			await finishTwoFactor(method, code, method !== 'recovery' && rememberBrowser);
			await onDone();
		} catch (caught) {
			error = caught instanceof ApiError ? caught : ApiError.unexpected();
		} finally {
			busy = false;
		}
	}
</script>

<h1>2단계 인증</h1>
<p class="lead">{lead}</p>

{#if notice}
	<p class="alert alert-info" role="status">{notice}</p>
{/if}

<form class="form" onsubmit={submit}>
	<label class="field">
		{method === 'recovery' ? '복구 코드' : '확인 코드'}
		<input
			name="code"
			autocomplete="one-time-code"
			inputmode={method === 'recovery' ? 'text' : 'numeric'}
			required
			bind:value={code}
		/>
	</label>
	{#if method !== 'recovery'}
		<label class="checkbox">
			<input type="checkbox" bind:checked={rememberBrowser} />
			이 브라우저 기억하기
		</label>
	{/if}

	{#if error}
		<p class="alert alert-error" role="alert">{error.message}</p>
	{/if}

	<button type="submit" class="btn btn-primary btn-block" disabled={busy}>확인</button>
</form>

<p class="links">
	{#if method === 'recovery'}
		{#if primary !== 'recovery'}
			<button type="button" class="link-button" onclick={() => choose(primary)}>
				{primary === 'authenticator' ? '인증 앱 코드로 확인하기' : '메일 코드로 확인하기'}
			</button>
		{/if}
	{:else}
		{#if method === 'email'}
			<button type="button" class="link-button" onclick={() => sendCode(true)}>코드 다시 받기</button>
		{/if}
		{#if providers.includes('recovery')}
			<button type="button" class="link-button" onclick={() => choose('recovery')}>복구 코드로 확인하기</button>
		{/if}
	{/if}
	<button type="button" class="link-button" onclick={onRestart}>처음부터 다시 로그인</button>
</p>
