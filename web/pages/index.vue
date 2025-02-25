<template>
    <div class="relative min-h-screen flex items-center justify-center bg-gradient-to-r from-blue-500 to-purple-600">

        <div class="absolute inset-0 overflow-hidden">
            <div class="absolute w-72 h-72 bg-white opacity-10 rounded-full top-10 left-10 blur-xl animate-pulse"></div>
            <div class="absolute w-96 h-96 bg-white opacity-10 rounded-full bottom-10 right-10 blur-xl animate-pulse">
            </div>
        </div>

        <div class="relative z-10 w-full max-w-4xl p-8 text-center justify-center items-center flex flex-col text-white">
            <img src="/images/logo.svg" class="w-72" />
            <p class="text-lg mt-8 opacity-80">Upload and share files that expire automatically!</p>

            <div
                class="mt-10 w-full max-w-lg bg-white bg-opacity-20 backdrop-blur-lg rounded-2xl shadow-xl p-6 text-gray-800 mx-auto transition-all hover:scale-105">
                <h2 class="text-xl font-semibold text-gray-900 text-center">Upload & Share Instantly</h2>

                <div class="relative border-2 border-dashed border-gray-300 rounded-xl p-6 text-center cursor-pointer transition-all hover:border-purple-500 mt-4 flex flex-col items-center"
                    @dragover.prevent="dragging = true" @dragleave.prevent="dragging = false" @drop.prevent="handleDrop"
                    @click="triggerFileInput" :class="{ 'border-purple-500 bg-blue-50': dragging }">
                    <input type="file" ref="fileInput" class="hidden" @change="handleFile"
                        accept=".jpg,.png,.pdf,.txt" />

                    <div v-if="!file" class="flex flex-col items-center">
                        <IconCloudUpload />
                        <p class="text-gray-600">{{ dragging ? "Drop your file here" : "Click or Drag & Drop to Upload"
                        }}</p>
                        <small class="text-gray-500">Allowed: JPG, PNG, PDF, TXT</small>
                    </div>

                    <div v-else class="flex items-center justify-between w-full bg-white p-3 rounded-md shadow-sm">
                        <p class="text-purple-600 font-semibold truncate w-full">{{ file.name }}</p>
                        <button @click.stop="removeFile" class="text-red-500 border rounded-full w-4 h-4 flex items-center justify-center border-transparent cursor-pointer hover:border-red-500">
                            <IconX class="w-14" />
                        </button>
                    </div>
                </div>

                <div class="mt-4">
                    <label class="block text-gray-700 font-medium mb-2">Expiry Duration</label>
                    <select v-model="expiry" class="w-full p-3 border rounded-md focus:ring focus:ring-purple-300">
                        <option value="1">One Minute</option>
                        <option value="5">Five Minutes</option>
                        <option value="10">Ten Minutes</option>
                        <option value="30">Thirty Minutes</option>
                        <option value="60">One Hour</option>
                        <option value="240">Four Hours</option>
                        <option value="1440">One Day</option>
                    </select>

                </div>

                <button @click="uploadFile" class="w-full p-3 text-white font-bold rounded-lg flex justify-center items-center transition-all 
         bg-gradient-to-r from-blue-500 to-purple-600 hover:from-blue-600 hover:to-purple-700 
         mt-4 disabled:opacity-50 cursor-pointer" :disabled="loading || !file">
                    <svg v-if="loading" class="animate-spin h-5 w-5 mr-2 text-white" viewBox="0 0 24 24">
                        <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4">
                        </circle>
                        <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 0116 0H4z"></path>
                    </svg>
                    {{ loading ? "Uploading..." : "Upload File" }}
                </button>



                <transition name="fade">
                    <div v-if="fileUrl" class="p-4 bg-green-100 border border-green-300 rounded-lg text-center mt-4">
                        <p class="text-green-700 font-semibold">File Uploaded Successfully!</p>
                        <div class="mt-2 flex items-center justify-between bg-white border p-2 rounded-md">
                            <input type="text" :value="fileUrl" readonly
                                class="w-full text-sm text-gray-700 outline-none" />
                            <button @click="copyToClipboard" class="ml-2 text-blue-500 hover:text-blue-600">
                                📋 Copy
                            </button>
                        </div>
                    </div>
                </transition>
            </div>
        </div>
    </div>
</template>

<script setup>
import { ref } from "vue";
import { IconCloudUpload, IconX } from '@tabler/icons-vue'

const file = ref(null);
const fileUrl = ref("");
const expiry = ref(10);
const loading = ref(false);
const dragging = ref(false);
const fileInput = ref(null);
const apiUrl = useRuntimeConfig().public.apiBase;

const handleFile = (event) => {
    file.value = event.target.files[0];
};

const handleDrop = (event) => {
    event.preventDefault();
    file.value = event.dataTransfer.files[0];
    dragging.value = false;
};

const removeFile = () => {
    file.value = null;
};

const triggerFileInput = () => {
    fileInput.value.click();
};

const uploadFile = async () => {
  if (!file.value) return;
  
  loading.value = true;
  
  const formData = new FormData();
  formData.append("file", file.value);
  formData.append("ExpiryDuration", expiry.value);

  try {
const response = await fetch(`${apiUrl}/File/upload`, {
      method: "POST",
      body: formData,
    });

    const result = await response.json();
    if (result.data) {
      fileUrl.value = result.data.FileAccessUrl;
      
      file.value = null;
      expiry.value = 10;
      
      window.location.href = `/file/${result.data.id}`;

    console.log(result)
    }
  } catch (error) {
    console.error("Upload failed:", error);
  } finally {
    loading.value = false;
  }
};


const copyToClipboard = () => {
    navigator.clipboard.writeText(fileUrl.value);
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