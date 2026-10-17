<script setup lang="ts">
// 내 계정의 패스키 — 이 기기(지문 · 얼굴 · PIN)나 보안 키로 비밀번호 없이 로그인한다. 등록은 브라우저의 패스키 창을 거친다.
const passkeys = ref<Passkey[]>([])
const supported = ref(false)
const name = ref('')
const error = ref<ApiError | null>(null)
const notice = ref<string | null>(null)
const busy = ref(false)

onMounted(async () => {
  supported.value = isPasskeySupported()
  try {
    passkeys.value = await listPasskeys()
  }
  catch (caught) {
    error.value = caught instanceof ApiError ? caught : ApiError.unexpected()
  }
})

async function add() {
  busy.value = true
  error.value = null
  notice.value = null
  try {
    const created = await addPasskey(await createPasskey(await passkeyCreationOptions()), name.value)
    notice.value = `패스키 '${created.name}'를 등록했어요. 다음부터 비밀번호 없이 로그인할 수 있어요.`
    name.value = ''
    passkeys.value = await listPasskeys()
  }
  catch (caught) {
    error.value = isPasskeyCancelled(caught)
      ? new ApiError(0, undefined, '패스키 등록을 취소했어요.')
      : caught instanceof ApiError ? caught : ApiError.unexpected()
  }
  finally {
    busy.value = false
  }
}

async function remove(passkey: Passkey) {
  error.value = null
  notice.value = null
  try {
    await removePasskey(passkey.id)
    passkeys.value = await listPasskeys()
  }
  catch (caught) {
    error.value = caught instanceof ApiError ? caught : ApiError.unexpected()
  }
}
</script>

<template>
  <h2 class="section-title">
    패스키
  </h2>
  <p class="muted">
    패스키를 등록하면 비밀번호 없이 이 기기의 지문 · 얼굴 · PIN 으로 로그인할 수 있어요.
  </p>

  <p v-if="notice" class="alert alert-info" role="status">
    {{ notice }}
  </p>
  <p v-if="error" class="alert alert-error" role="alert">
    {{ error.message }}
  </p>

  <ul v-if="passkeys.length > 0" class="list">
    <li v-for="passkey in passkeys" :key="passkey.id">
      <span>
        <strong>{{ passkey.name }}</strong>
        <span class="muted"> · {{ new Date(passkey.createdAt).toLocaleDateString('ko-KR') }} 등록</span>
      </span>
      <button type="button" class="btn btn-small" @click="remove(passkey)">
        지우기
      </button>
    </li>
  </ul>

  <form v-if="supported" class="form" @submit.prevent="add">
    <label class="field">
      패스키 이름
      <input v-model="name" name="passkeyName" placeholder="예: 회사 노트북" maxlength="50">
    </label>
    <button type="submit" class="btn btn-primary" :disabled="busy">
      패스키 추가
    </button>
  </form>
  <p v-else class="muted">
    이 브라우저는 패스키를 지원하지 않아요.
  </p>
</template>
