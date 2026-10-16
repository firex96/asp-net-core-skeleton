<script setup lang="ts">
// 메일 링크(/confirm-email?userId=…&code=…)로 들어와 이메일을 확인한다. 로그인은 확인한 뒤 따로 한다.
const route = useRoute()
const status = ref<'pending' | 'done' | 'failed'>('pending')
const error = ref<ApiError | null>(null)

useHead({ title: '이메일 확인' })

onMounted(async () => {
  try {
    await confirmEmail(queryValue('userId'), queryValue('code'))
    status.value = 'done'
  }
  catch (caught) {
    error.value = caught instanceof ApiError ? caught : ApiError.unexpected()
    status.value = 'failed'
  }
})

function queryValue(name: string): string {
  const value = route.query[name]
  return typeof value === 'string' ? value : ''
}
</script>

<template>
  <main class="card">
    <h1>이메일 확인</h1>
    <p v-if="status === 'pending'" class="muted">
      확인하는 중이에요…
    </p>
    <template v-else-if="status === 'done'">
      <p class="alert alert-info" role="status">
        이메일을 확인했어요. 이제 로그인할 수 있어요.
      </p>
      <NuxtLink to="/login" class="btn btn-primary btn-block">
        로그인하기
      </NuxtLink>
    </template>
    <template v-else>
      <p class="alert alert-error" role="alert">
        {{ error?.message }}
      </p>
      <p class="links">
        <NuxtLink to="/login">
          로그인으로 돌아가기
        </NuxtLink>
      </p>
    </template>
  </main>
</template>
