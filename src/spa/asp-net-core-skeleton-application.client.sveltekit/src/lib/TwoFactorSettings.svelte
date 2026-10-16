<script lang="ts">
	import { onMount } from 'svelte';
	import {
		ApiError,
		disableTwoFactor,
		enableEmailTwoFactor,
		forgetBrowser,
		getTwoFactorStatus,
		regenerateRecoveryCodes,
		setupAuthenticator,
		verifyAuthenticator,
		type AuthenticatorSetup,
		type TwoFactorStatus
	} from './auth.js';

	// 내 계정의 2단계 인증 — 인증 앱(QR 등록 → 코드 확인) 또는 메일 코드로 켜고, 켤 때 받은 복구 코드는 이 화면에서 한 번만 보여 준다.
	// 끄려면 비밀번호를 다시 넣는다. 수단을 바꾸려면 끈 뒤에 다시 켠다.
	let status = $state<TwoFactorStatus | null>(null);
	let setup = $state<AuthenticatorSetup | null>(null);
	let recoveryCodes = $state<string[] | null>(null);
	let code = $state('');
	let password = $state('');
	let error = $state<ApiError | null>(null);
	let busy = $state(false);

	onMount(load);

	async function load() {
		status = await getTwoFactorStatus();
	}

	async function run(action: () => Promise<void>) {
		busy = true;
		error = null;
		try {
			await action();
			await load();
		} catch (caught) {
			error = caught instanceof ApiError ? caught : ApiError.unexpected();
		} finally {
			busy = false;
		}
	}

	const startAuthenticator = () =>
		run(async () => {
			recoveryCodes = null;
			code = '';
			setup = await setupAuthenticator();
		});

	const confirmAuthenticator = (event: SubmitEvent) => {
		event.preventDefault();
		return run(async () => {
			recoveryCodes = await verifyAuthenticator(code);
			setup = null;
		});
	};

	const enableEmail = () =>
		run(async () => {
			recoveryCodes = await enableEmailTwoFactor();
		});

	const newRecoveryCodes = () =>
		run(async () => {
			recoveryCodes = await regenerateRecoveryCodes();
		});

	const forget = () => run(forgetBrowser);

	const disable = (event: SubmitEvent) => {
		event.preventDefault();
		return run(async () => {
			await disableTwoFactor(password);
			password = '';
			recoveryCodes = null;
		});
	};
</script>

<h2 class="section-title">2단계 인증</h2>

{#if recoveryCodes}
	<div class="alert alert-info" role="status">
		<p>
			복구 코드예요. 인증 앱이나 메일을 쓸 수 없을 때 하나씩 쓸 수 있어요. 이 화면을 떠나면 다시 볼 수 없으니 안전한 곳에 적어
			두세요.
		</p>
		<ul class="codes">
			{#each recoveryCodes as recoveryCode (recoveryCode)}
				<li><code>{recoveryCode}</code></li>
			{/each}
		</ul>
	</div>
{/if}

{#if error}
	<p class="alert alert-error" role="alert">{error.message}</p>
{/if}

{#if status && !status.enabled}
	<p class="muted">로그인할 때 비밀번호 다음에 확인 코드를 한 번 더 넣어요.</p>
	{#if setup}
		<p>인증 앱(Google Authenticator, Microsoft Authenticator 등)으로 QR 코드를 찍거나 키를 직접 입력해 주세요.</p>
		<img class="qr" src={setup.qrCodeDataUri} alt="인증 앱 등록 QR 코드" />
		<p class="muted">키 <code data-testid="shared-key">{setup.sharedKey}</code></p>
		<form class="form" onsubmit={confirmAuthenticator}>
			<label class="field">
				인증 앱의 6자리 코드
				<input name="code" autocomplete="one-time-code" inputmode="numeric" required bind:value={code} />
			</label>
			<div class="button-row">
				<button type="submit" class="btn btn-primary" disabled={busy}>확인하고 켜기</button>
				<button type="button" class="btn" onclick={() => (setup = null)}>취소</button>
			</div>
		</form>
	{:else}
		<div class="button-row">
			<button type="button" class="btn btn-primary btn-small" disabled={busy} onclick={startAuthenticator}>
				인증 앱으로 켜기
			</button>
			<button type="button" class="btn btn-small" disabled={busy} onclick={enableEmail}>메일 코드로 켜기</button>
		</div>
	{/if}
{:else if status}
	<p>
		켜져 있어요 — {status.method === 'authenticator' ? '인증 앱' : '메일 코드'}로 확인해요. 남은 복구 코드는 {status.recoveryCodesLeft}개예요.
	</p>
	{#if status.browserRemembered}
		<p class="muted">이 브라우저는 기억돼 있어서 확인 코드를 묻지 않아요.</p>
	{/if}
	<div class="button-row">
		<button type="button" class="btn btn-small" disabled={busy} onclick={newRecoveryCodes}>복구 코드 새로 받기</button>
		{#if status.browserRemembered}
			<button type="button" class="btn btn-small" disabled={busy} onclick={forget}>이 브라우저 기억 지우기</button>
		{/if}
	</div>
	<form class="form" onsubmit={disable}>
		<label class="field">
			비밀번호
			<input name="password" type="password" autocomplete="current-password" required bind:value={password} />
		</label>
		<button type="submit" class="btn" disabled={busy}>2단계 인증 끄기</button>
	</form>
{/if}
