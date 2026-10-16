<script setup lang="ts">
// 메일 링크(/reset-password?email=…&code=…)로 들어와 새 비밀번호를 정한다. 성공하면 로그인 화면으로 간다.
const route = useRoute()
const email = typeof route.query.email === 'string' ? route.query.email : ''
const code = typeof route.query.code === 'string' ? route.query.code : ''
const password = ref('')
const confirmPassword = ref('')
const busy = ref(false)
const error = ref<ApiError | null>(null)

useHead({ title: '비밀번호 재설정' })

async function submit() {
  if (password.value !== confirmPassword.value) {
    error.value = new ApiError(400, undefined, '비밀번호가 서로 달라요. 다시 입력해 주세요.')
    return
  }

  busy.value = true
  error.value = null
  try {
    await resetPassword(email, code, password.value)
    await navigateTo('/login?reset=done')
  }
  catch (caught) {
    error.value = caught instanceof ApiError ? caught : ApiError.unexpected()
  }
  finally {
    busy.value = false
  }
}
</script>

<template>
  <main class="card">
    <h1>새 비밀번호 정하기</h1>
    <p class="lead">
      {{ email }} 계정에서 쓸 새 비밀번호를 입력해 주세요.
    </p>
    <form class="form" @submit.prevent="submit">
      <label class="field">
        새 비밀번호
        <input v-model="password" name="password" type="password" autocomplete="new-password" required>
      </label>
      <label class="field">
        새 비밀번호 확인
        <input v-model="confirmPassword" name="confirmPassword" type="password" autocomplete="new-password" required>
      </label>
      <p class="hint">
        10자 이상, 영문 대문자 · 소문자와 숫자, 특수 문자를 섞어 주세요.
      </p>
      <div v-if="error" class="alert alert-error" role="alert">
        <p>{{ error.message }}</p>
        <ul v-if="error.fieldErrors.length > 0">
          <li v-for="message in error.fieldErrors" :key="message">
            {{ message }}
          </li>
        </ul>
        <p v-if="error.code === 'INVALID_TOKEN_ERROR'">
          <NuxtLink to="/forgot-password">
            재설정 링크 다시 받기
          </NuxtLink>
        </p>
      </div>
      <button type="submit" class="btn btn-primary btn-block" :disabled="busy">
        비밀번호 바꾸기
      </button>
    </form>
  </main>
</template>
