<script setup lang="ts">
// 재설정 링크를 메일로 받는다. 계정이 있든 없든 같은 안내를 보인다(가입 여부를 드러내지 않는다).
const email = ref('')
const sent = ref(false)
const busy = ref(false)
const error = ref<ApiError | null>(null)

useHead({ title: '비밀번호 찾기' })

async function submit() {
  busy.value = true
  error.value = null
  try {
    await forgotPassword(email.value)
    sent.value = true
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
    <h1>비밀번호 찾기</h1>
    <p v-if="sent" class="alert alert-info" role="status">
      {{ email }}로 가입한 계정이 있다면 비밀번호 재설정 링크를 보냈어요. 메일함을 확인해 주세요. 링크는 2시간 동안 한 번만 쓸 수 있어요.
    </p>
    <template v-else>
      <p class="lead">
        가입한 이메일을 입력하면 비밀번호를 다시 정할 수 있는 링크를 보내 드려요.
      </p>
      <form class="form" @submit.prevent="submit">
        <label class="field">
          이메일
          <input v-model="email" name="email" type="email" autocomplete="username" placeholder="name@example.com" required>
        </label>
        <p v-if="error" class="alert alert-error" role="alert">
          {{ error.message }}
        </p>
        <button type="submit" class="btn btn-primary btn-block" :disabled="busy">
          재설정 링크 받기
        </button>
      </form>
    </template>
    <p class="links">
      <NuxtLink to="/login">
        로그인으로 돌아가기
      </NuxtLink>
    </p>
  </main>
</template>
