<script setup lang="ts">
const session = useSession()
const route = useRoute()

// 계정 화면들(로그인 · 비밀번호 찾기 · 재설정 · 이메일 확인)에서는 돌아올 곳을 싣지 않는다 — 링크의 일회용 코드를 다른 주소로 옮기지 않는다.
const loginLink = computed(() =>
  ['/login', '/forgot-password', '/reset-password', '/confirm-email'].includes(route.path)
    ? { path: '/login' }
    : { path: '/login', query: { returnUrl: route.fullPath } },
)

// 현재 사용자를 못 읽으면 로그인 버튼을 보인다 — 세션 값은 그대로 둬서(미확인) 화면들이 다시 묻게 한다.
const sessionFailed = ref(false)
const logoutFailed = ref(false)

onMounted(() => {
  session.refresh().catch((error) => {
    console.error(error)
    sessionFailed.value = true
  })
})

async function logOut() {
  logoutFailed.value = false
  try {
    await session.signOut()
  }
  catch {
    logoutFailed.value = true
    return
  }
  await navigateTo('/')
}
</script>

<template>
  <div>
    <NuxtRouteAnnouncer />
    <header class="app-header">
      <NuxtLink to="/" class="brand">
        AspNetCoreSkeletonApplication
      </NuxtLink>
      <nav class="nav">
        <template v-if="session.user.value">
          <NuxtLink v-if="session.user.value.permissions.includes(MANAGE_PERMISSION)" to="/admin">
            관리
          </NuxtLink>
          <NuxtLink to="/account">
            {{ session.user.value.email }}
          </NuxtLink>
          <button type="button" class="btn btn-small" @click="logOut">
            로그아웃
          </button>
        </template>
        <NuxtLink v-else-if="(session.user.value === null || sessionFailed) && route.path !== '/login'" :to="loginLink" class="btn btn-primary btn-small">
          로그인
        </NuxtLink>
      </nav>
    </header>
    <p v-if="logoutFailed" class="alert alert-error" role="alert">
      로그아웃하지 못했어요. 잠시 후 다시 시도해 주세요.
    </p>
    <NuxtPage />
  </div>
</template>
