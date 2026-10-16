<script setup lang="ts">
// 로그인과 가입을 한 화면에서 전환한다. 성공하면 returnUrl(같은 출처 경로만)로 돌아간다.
const session = useSession()
const route = useRoute()

const mode = ref<'login' | 'register'>('login')
const email = ref('')
const password = ref('')
const confirmPassword = ref('')
const error = ref<ApiError | null>(null)
const busy = ref(false)

const returnUrl = computed(() => safeReturnUrl(typeof route.query.returnUrl === 'string' ? route.query.returnUrl : null))
const title = computed(() => (mode.value === 'login' ? 'Log in' : 'Create account'))

useHead({ title })

onMounted(async () => {
  const user = session.user.value === undefined ? await session.refresh() : session.user.value
  if (user) {
    await navigateTo(returnUrl.value)
  }
})

function toggleMode() {
  mode.value = mode.value === 'login' ? 'register' : 'login'
  error.value = null
}

async function submit() {
  if (mode.value === 'register' && password.value !== confirmPassword.value) {
    error.value = new ApiError(400, undefined, 'Passwords do not match.')
    return
  }

  busy.value = true
  error.value = null
  try {
    await (mode.value === 'login' ? session.signIn(email.value, password.value) : session.signUp(email.value, password.value))
    await navigateTo(returnUrl.value)
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
  <main>
    <h1>{{ title }}</h1>

    <form @submit.prevent="submit">
      <label>
        Email
        <input v-model="email" name="email" type="email" autocomplete="username" required>
      </label>
      <label>
        Password
        <input
          v-model="password"
          name="password"
          type="password"
          :autocomplete="mode === 'login' ? 'current-password' : 'new-password'"
          required
        >
      </label>
      <label v-if="mode === 'register'">
        Confirm password
        <input v-model="confirmPassword" name="confirmPassword" type="password" autocomplete="new-password" required>
      </label>

      <div v-if="error" class="error" role="alert">
        <p>{{ error.message }}</p>
        <ul v-if="error.fieldErrors.length > 0">
          <li v-for="message in error.fieldErrors" :key="message">
            {{ message }}
          </li>
        </ul>
      </div>

      <button type="submit" :disabled="busy">
        {{ title }}
      </button>
    </form>

    <p>
      <button type="button" class="link" @click="toggleMode">
        {{ mode === 'login' ? 'Need an account? Create one' : 'Already have an account? Log in' }}
      </button>
    </p>
  </main>
</template>

<style scoped>
main {
  max-width: 24rem;
  margin: 2rem auto;
}

form {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

label {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.error {
  color: #b00020;
}

.link {
  background: none;
  border: none;
  color: inherit;
  text-decoration: underline;
  cursor: pointer;
}
</style>
