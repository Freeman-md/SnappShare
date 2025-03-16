import { beforeEach, describe, expect, test, vi } from "vitest";
import { ref } from "vue";
import axios from "axios";
import { useUpload } from "~/composables/api/useUpload";
import { useRouter } from "vue-router";

vi.mock("axios");

const mockRouterPush = vi.fn()

vi.mock("vue-router", () => ({
    useRouter: () => ({
        push: mockRouterPush,
    }),
}));

describe('useUpload', () => {
    let file: Ref<File | null>;
    let expiry: Ref<number>;
    let mockFile: File
    let composable: ReturnType<typeof useUpload>;

    beforeEach(() => {
        file = ref(null);
        expiry = ref(10);
        mockFile = new File(["file content"], "test.txt", { type: "text/plain" })
        composable = useUpload({
            file, 
            expiry,
            apiUrl: "https://mock-api.com"
        });

        vi.resetAllMocks();
    });

    test('should initialize correctly', () => {
        expect(composable.loading.value).toBeFalsy();
        expect(composable.uploadProgress.value).toBe(0);
        expect(composable.errorMessage.value).toBe("");
        expect(composable.uploadFile).toBeDefined();
    });

    test('uploadFile() rejects files larger than 5MB', async () => {
        const largeFile = new File(['File content'], 'large-file.png', { type: 'image/png' });
        Object.defineProperty(largeFile, 'size', {
            value: 6 * 1024 * 1024
        });

        file.value = largeFile;
        await composable.uploadFile();

        expect(composable.errorMessage.value).toBeTruthy(); 
        expect(composable.loading.value).toBe(false);
    });

    test('sendFileToServer() calls API with correct payload', async () => {
        file.value = mockFile;

        axios.post.mockResolvedValue({ data: { data: { id: "123ABC" } } });

        await composable.uploadFile();

        expect(axios.post).toHaveBeenCalledWith(
            "https://mock-api.com/File/upload",
            expect.any(FormData),
            expect.objectContaining({
                headers: { "Content-Type": "multipart/form-data" },
            })
        );
    });

    test('handleUploadError() sets correct error messages for network failure', async () => {
        file.value = mockFile;

        axios.post.mockRejectedValue({ code: "ERR_NETWORK", message: "Network Error" });

        await composable.uploadFile();

        expect(composable.errorMessage.value).toBe("Network Error");
    });

    test('uploadFile() resets state after upload and calls router.push()', async () => {
        file.value = mockFile;

        axios.post.mockResolvedValue({ data: { data: { id: "123ABC" } } });

        await composable.uploadFile();

        expect(composable.loading.value).toBe(false);
        expect(composable.uploadProgress.value).toBe(0);
        expect(file.value).toBeNull();

        expect(mockRouterPush).toHaveBeenCalledOnce();
        expect(mockRouterPush).toHaveBeenCalledWith("/file/123ABC");
    });
});
