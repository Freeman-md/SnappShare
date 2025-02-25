<template>
  <div class="min-h-screen flex items-center justify-center bg-gradient-to-r from-blue-500 to-purple-600">
    <div class="relative z-10 max-w-sm sm:max-w-xl w-full bg-white p-8 rounded-2xl shadow-2xl text-center transition-all transform hover:scale-105">

      <div v-if="loading" class="flex items-center justify-center mt-4">
        <svg class="animate-spin h-10 w-10 text-blue-500" viewBox="0 0 24 24">
          <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
          <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 0116 0H4z"></path>
        </svg>
      </div>

      <template v-else>
        <div v-if="!isExpired" class="text-gray-800">
          <h1 class="text-2xl font-bold">File Expires In</h1>
          <p class="text-5xl font-extrabold bg-clip-text text-transparent 
              bg-gradient-to-r from-blue-500 to-purple-400 animate-pulse mt-2">
            {{ countdown }}
          </p>
        </div>

        <div v-else class="text-red-500 text-xl font-semibold fade-in">
          <h1>⚠️ File Expired</h1>
          <p class="text-gray-700">This file is no longer available.</p>
        </div>

        <div v-if="fileUrl && !isExpired" class="mt-6 space-y-4">
          <div class="mt-4 flex space-x-4 items-center justify-center">
            <button type="button" @click="shareFile"
              class="flex space-x-2 items-center border border-gray-200 p-2 rounded-full cursor-pointer">
              <IconShare width="20" /> <span>Share</span>
            </button>
            <a :href="fileUrl" target="_blank" rel="noopener"
              class="flex space-x-2 items-center p-2 rounded-full bg-gradient-to-r from-blue-500 to-purple-600 text-white">
              👀 View File
            </a>
          </div>
        </div>
      </template>

    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, watchEffect } from "vue";
import { useRoute } from "vue-router";
import { useAsyncData } from "#app";
import { IconShare } from '@tabler/icons-vue';

const route = useRoute();
const fileId = route.params.id;
const fileUrl = ref("");
const expiresAt = ref(null);
const remainingTime = ref(0);
const loading = ref(true);

const { data, status } = await useAsyncData(`file-${fileId}`, async () => {
  const response = await fetch(`http://localhost:5028/file/${fileId}`);
  return response.json();
});

onMounted(() => {
  watchEffect(() => {
    if (data.value?.data) {
      fileUrl.value = data.value.data.originalUrl;
      expiresAt.value = new Date(data.value.data.expiresAt);
      loading.value = false;

      updateRemainingTime();
      setInterval(updateRemainingTime, 1000);
    }
  });
});

const updateRemainingTime = () => {
  if (!expiresAt.value) {
    remainingTime.value = 0;
    return;
  }
  const now = new Date();
  remainingTime.value = Math.max(0, Math.floor((expiresAt.value - now) / 1000));
};

const isExpired = computed(() => !loading.value && remainingTime.value === 0);

const countdown = computed(() => {
  const seconds = remainingTime.value;
  if (seconds <= 0) return "00:00:00";

  const hours = String(Math.floor(seconds / 3600)).padStart(2, '0');
  const minutes = String(Math.floor((seconds % 3600) / 60)).padStart(2, '0');
  const secs = String(seconds % 60).padStart(2, '0');

  return `${hours}:${minutes}:${secs}`;
});

const shareFile = () => {
  if (navigator.share) {
    navigator.share({
      title: "Check out this file on SnappShare",
      url: window.location.href
    }).catch(err => console.error("Sharing failed:", err));
  } else {
    alert("Your browser does not support Web Share API.");
  }
};
</script>

<style>
.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.5s ease-in-out;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}
</style>
