<script lang="ts">
	import { onMount } from 'svelte';
	import { ApiError, addPasskey, listPasskeys, passkeyCreationOptions, removePasskey, type Passkey } from './auth.js';
	import { createPasskey, isPasskeyCancelled, isPasskeySupported } from './webauthn.js';

	// 내 계정의 패스키 — 이 기기(지문 · 얼굴 · PIN)나 보안 키로 비밀번호 없이 로그인한다. 등록은 브라우저의 패스키 창을 거친다.
	let passkeys = $state<Passkey[]>([]);
	let supported = $state(false);
	let name = $state('');
	let error = $state<ApiError | null>(null);
	let notice = $state<string | null>(null);
	let busy = $state(false);

	onMount(async () => {
		supported = isPasskeySupported();
		passkeys = await listPasskeys();
	});

	async function add(event: SubmitEvent) {
		event.preventDefault();
		busy = true;
		error = null;
		notice = null;
		try {
			const created = await addPasskey(await createPasskey(await passkeyCreationOptions()), name);
			notice = `패스키 '${created.name}'를 등록했어요. 다음부터 비밀번호 없이 로그인할 수 있어요.`;
			name = '';
			passkeys = await listPasskeys();
		} catch (caught) {
			error = isPasskeyCancelled(caught)
				? new ApiError(0, undefined, '패스키 등록을 취소했어요.')
				: caught instanceof ApiError
					? caught
					: ApiError.unexpected();
		} finally {
			busy = false;
		}
	}

	async function remove(passkey: Passkey) {
		error = null;
		notice = null;
		try {
			await removePasskey(passkey.id);
			passkeys = await listPasskeys();
		} catch (caught) {
			error = caught instanceof ApiError ? caught : ApiError.unexpected();
		}
	}
</script>

<h2 class="section-title">패스키</h2>
<p class="muted">패스키를 등록하면 비밀번호 없이 이 기기의 지문 · 얼굴 · PIN 으로 로그인할 수 있어요.</p>

{#if notice}
	<p class="alert alert-info" role="status">{notice}</p>
{/if}
{#if error}
	<p class="alert alert-error" role="alert">{error.message}</p>
{/if}

{#if passkeys.length > 0}
	<ul class="list">
		{#each passkeys as passkey (passkey.id)}
			<li>
				<span>
					<strong>{passkey.name}</strong>
					<span class="muted"> · {new Date(passkey.createdAt).toLocaleDateString('ko-KR')} 등록</span>
				</span>
				<button type="button" class="btn btn-small" onclick={() => remove(passkey)}>지우기</button>
			</li>
		{/each}
	</ul>
{/if}

{#if supported}
	<form class="form" onsubmit={add}>
		<label class="field">
			패스키 이름
			<input name="passkeyName" placeholder="예: 회사 노트북" maxlength="50" bind:value={name} />
		</label>
		<button type="submit" class="btn btn-primary" disabled={busy}>패스키 추가</button>
	</form>
{:else}
	<p class="muted">이 브라우저는 패스키를 지원하지 않아요.</p>
{/if}
