<script setup lang="ts">
const session = useSession()
const route = useRoute()

const loginLink = computed(() =>
  route.path === '/login' ? { path: '/login' } : { path: '/login', query: { returnUrl: route.fullPath } },
)

onMounted(() => {
  session.refresh().catch(error => console.error(error))
})

async function logOut() {
  await session.signOut()
  await navigateTo('/')
}
</script>

<template>
  <div>
    <NuxtRouteAnnouncer />
    <header>
      <NuxtLink to="/">
        AspNetCoreSkeletonApplication
      </NuxtLink>
      <nav>
        <template v-if="session.user.value">
          <span class="user">{{ session.user.value.email }}</span>
          <button type="button" @click="logOut">
            Log out
          </button>
        </template>
        <NuxtLink v-else-if="session.user.value === null" :to="loginLink">
          Log in
        </NuxtLink>
      </nav>
    </header>
    <NuxtPage />
  </div>
</template>

<style scoped>
header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 0.75rem 2rem;
  border-bottom: 1px solid #ddd;
}

nav {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}
</style>
