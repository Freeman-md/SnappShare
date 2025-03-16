<template>
    <div class="relative min-h-screen flex items-center justify-center bg-gradient-to-r from-blue-500 to-purple-600">

        <div class="absolute inset-0 overflow-hidden">
            <div class="absolute w-72 h-72 bg-white opacity-10 rounded-full top-10 left-10 blur-xl animate-pulse" />
            <div class="absolute w-96 h-96 bg-white opacity-10 rounded-full bottom-10 right-10 blur-xl animate-pulse" />
        </div>

        <div
            class="relative z-10 w-full max-w-4xl p-8 text-center justify-center items-center flex flex-col text-white">
            <img src="/images/logo.svg" class="w-72">
            <p class="text-lg mt-8 opacity-80">Upload and share files that expire automatically!</p>

            <div
                class="mt-10 w-full max-w-lg bg-white bg-opacity-20 backdrop-blur-lg rounded-2xl shadow-xl p-6 text-gray-800 mx-auto transition-all hover:scale-105">
                <h2 class="text-xl font-semibold text-gray-900 text-center">Upload & Share Instantly</h2>

                <div class="relative border-2 border-dashed border-gray-300 rounded-xl p-6 text-center cursor-pointer transition-all hover:border-purple-500 mt-4 flex flex-col items-center"
                    :class="{ 'border-purple-500 bg-blue-50': dragging }" @dragover.prevent="dragging = true"
                    @dragleave.prevent="dragging = false" @drop.prevent="handleDrop" @click="triggerFileInput">
                    <input ref="fileInput" type="file" class="hidden" @change="handleFile">

                    <div v-if="!file" class="flex flex-col items-center">
                        <IconCloudUpload />
                        <p class="text-gray-600">{{ dragging ? "Drop your file here" : "Click or Drag & Drop to Upload"
                        }}</p>
                        <small class="text-gray-500">Allowed: Images, Documents, Videos; Max File Size (5MB)</small>
                    </div>

                    <div v-else class="flex items-center justify-between w-full bg-white p-3 rounded-md shadow-sm">
                        <p class="text-purple-600 font-semibold truncate w-full">{{ file.name }}</p>
                        <button
                            class="text-red-500 border rounded-full w-4 h-4 flex items-center justify-center border-transparent cursor-pointer hover:border-red-500"
                            @click.stop="removeFile">
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

                <button
                    class="w-full p-3 text-white font-bold rounded-lg flex justify-center items-center transition-all relative overflow-hidden mt-4 disabled:opacity-50 cursor-pointer
         bg-gradient-to-r from-blue-500 to-purple-600 hover:from-blue-600 hover:to-purple-700 in-active:cursor-pointer disabled:active:cursor-progress"
                    :disabled="loading || !file" @click="uploadFile">
                    <div
class="absolute top-0 left-0 h-full transition-all" :style="{
                        width: uploadProgress + '%',
                        background: 'linear-gradient(to right, #155dfc, #9810fa)',
                    }" />

                    <span class="relative z-10">
                        {{ loading ? `Uploading... ${uploadProgress}%` : "Upload File" }}
                    </span>
                </button>


                <small v-show="errorMessage" class="text-red-500">{{ errorMessage }}</small>
            </div>
        </div>
    </div>
</template>

<script setup>
import { IconCloudUpload, IconX } from '@tabler/icons-vue'
import { useUpload } from '~/composables/api/useUpload';
import { useFile } from "~/composables/ui/useFile"

const file = ref(null);
const expiry = ref(10);
const { public: { apiBase } } = useRuntimeConfig();

const {
    dragging,
    fileInput,
    handleFile,
    handleDrop,
    removeFile,
    triggerFileInput
} = useFile({
    file
})

const { loading, errorMessage, uploadProgress, uploadFile } = useUpload({
    file,
    expiry,
    apiUrl: apiBase
})

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