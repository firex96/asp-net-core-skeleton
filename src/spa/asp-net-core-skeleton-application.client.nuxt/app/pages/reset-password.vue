<script setup lang="ts">
// 메일 링크(/reset-password?email=…&code=…)로 들어와 새 비밀번호를 정한다. 성공하면 로그인 화면으로 간다.
const route = useRoute()
const email = typeof route.query.email === 'string' ? route.query.email : ''
const code = typeof route.query.code === 'string' ? route.query.code : ''
const password = ref('')
const confirmPassword = ref('')
const busy = ref(false)
const error = ref<ApiError | null>(null)

useHead({ title: 'Reset password' })

async function submit() {
  if (password.value !== confirmPassword.value) {
    error.value = new ApiError(400, undefined, 'Passwords do not match.')
    return
  }

  busy.value = true
  error.value = null
  try {
    await resetPassword(email, code, password.value)
    await navigateTo('/login?reset=done')
  }
  catch (caught) {
    error.value = caught instanceof ApiError ? caught : new ApiError(0, undefined, 'The request failed. Please try again.')
  }
  finally {
    busy.value = false
  }
}
</script>

<template>
  <main class="page">
    <h1>Reset password</h1>
    <p>For {{ email }}</p>
    <form @submit.prevent="submit">
      <label>
        New password
        <input v-model="password" name="password" type="password" autocomplete="new-password" required>
      </label>
      <label>
        Confirm new password
        <input v-model="confirmPassword" name="confirmPassword" type="password" autocomplete="new-password" required>
      </label>
      <div v-if="error" class="error" role="alert">
        <p>{{ error.message }}</p>
        <ul v-if="error.fieldErrors.length > 0">
          <li v-for="message in error.fieldErrors" :key="message">
            {{ message }}
          </li>
        </ul>
        <p v-if="error.code === 'INVALID_TOKEN_ERROR'">
          <NuxtLink to="/forgot-password">
            Request a new link
          </NuxtLink>
        </p>
      </div>
      <button type="submit" :disabled="busy">
        Change password
      </button>
    </form>
  </main>
</template>
