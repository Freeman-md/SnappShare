import { ref } from "vue"
import { useRouter } from 'vue-router'
import axios, { type AxiosError, type AxiosProgressEvent } from "axios"

export const useUpload = ({ file, expiry, apiUrl }: {
    file: Ref<File | null>,
    expiry: Ref<number>,
    apiUrl: string
}) => {
    const router = useRouter()

    const loading = ref(false);
    const errorMessage = ref("");
    const uploadProgress = ref(0)

    const validateFile = (file: File): boolean => {
        if (!file) return false;
    
        if (file.size > 5 * 1024 * 1024) {
            errorMessage.value = "File must not be greater than 5MB";
            return false;
        }
    
        return true;
    };
    
    const sendFileToServer = async (file: File, expiry: number) => {
        const formData = new FormData();
        formData.append("file", file);
        formData.append("ExpiryDuration", expiry.toString());
    
        return axios.post(`${apiUrl}/File/upload`, formData, {
            headers: { "Content-Type": "multipart/form-data" },
            onUploadProgress: (progressEvent: AxiosProgressEvent) => {
                uploadProgress.value = Math.round(
                    progressEvent.total ? (progressEvent.loaded / progressEvent.total) * 100 : 0
                );
            },
        });
    };
    
    const handleUploadError = (error: AxiosError) => {
        if (axios.isAxiosError(error) && !error.response) {
            errorMessage.value = error.code === "ERR_NETWORK"
                ? "Network Error. Please check your internet and try again"
                : (error as Error).message;
            return;
        }
    
        const response = error?.response?.data as { errors?: Record<string, string[]> };
        if (response?.errors) {
            errorMessage.value = (Object.values(response.errors).flat()[0] as string) || "An error occurred.";
        } else {
            errorMessage.value = (error as Error).message || "An unknown error occurred."; 
        }
    
        setTimeout(() => (errorMessage.value = ""), 3000);
    };
    
    
    const uploadFile = async () => {
        if (!file.value || !validateFile(file.value)) return;
    
        loading.value = true;
    
        try {
            const { data } = await sendFileToServer(file.value, expiry.value);

            if (data) {
                router.push(`/file/${data.data.id}`);
                file.value = null;
            }
        } catch (error) {
            handleUploadError(error as AxiosError);
        } finally {
            loading.value = false;
            uploadProgress.value = 0;
        }
    };    

    return {
        loading,
        errorMessage,
        uploadProgress,
        uploadFile
    }
}