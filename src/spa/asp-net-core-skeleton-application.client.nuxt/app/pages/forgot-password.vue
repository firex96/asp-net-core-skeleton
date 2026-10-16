<script setup lang="ts">
// 재설정 링크를 메일로 받는다. 계정이 있든 없든 같은 안내를 보인다(가입 여부를 드러내지 않는다).
const email = ref('')
const sent = ref(false)
const busy = ref(false)
const error = ref<ApiError | null>(null)

useHead({ title: 'Forgot password' })

async function submit() {
  busy.value = true
  error.value = null
  try {
    await forgotPassword(email.value)
    sent.value = true
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
    <h1>Forgot password</h1>
    <p v-if="sent" role="status">
      If an account exists for {{ email }}, we sent a link to reset the password. The link works once within 2 hours.
    </p>
    <form v-else @submit.prevent="submit">
      <label>
        Email
        <input v-model="email" name="email" type="email" autocomplete="username" required>
      </label>
      <p v-if="error" class="error" role="alert">
        {{ error.message }}
      </p>
      <button type="submit" :disabled="busy">
        Send reset link
      </button>
    </form>
    <p>
      <NuxtLink to="/login">
        Back to log in
      </NuxtLink>
    </p>
  </main>
</template>
