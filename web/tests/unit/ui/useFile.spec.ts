import { describe, expect, test, beforeEach, vi } from 'vitest';
import { ref, type Ref } from 'vue';
import { useFile } from '@/composables/ui/useFile';

describe('useFile', () => {
    let file: Ref<File | null>;
    let mockFile: File;
    let composable: ReturnType<typeof useFile>;

    beforeEach(() => {
        file = ref<File | null>(null);
        mockFile = new File(['test content'], 'test.txt', { type: 'text/plain' });
        composable = useFile({ file });
    });

    test('should initialize correctly', () => {
        expect(composable.file.value).toBeNull();
        expect(composable.dragging.value).toBeFalsy();
        expect(composable.fileInput.value).toBeNull();
        expect(composable.handleDrop).toBeDefined();
        expect(composable.handleFile).toBeDefined();
        expect(composable.removeFile).toBeDefined();
        expect(composable.triggerFileInput).toBeDefined();
    });

    test('handleFile() updates file correctly', () => {
        composable.handleFile({ target: { files: [mockFile] } } as unknown as Event);

        expect(file.value).toStrictEqual(mockFile);
        expect(composable.file.value).toStrictEqual(mockFile);
    });

    test('handleDrop() updates file when dragging a file', () => {
        const event = {
            preventDefault: vi.fn(),
            dataTransfer: {
                files: [mockFile]
            }
        } as unknown as DragEvent;

        composable.handleDrop(event);

        expect(event.preventDefault).toHaveBeenCalled();
        expect(file.value).toStrictEqual(mockFile);
        expect(composable.file.value).toStrictEqual(mockFile);
        expect(composable.dragging.value).toBeFalsy()
    });

    test('removeFile() sets file to null', () => {
        composable.handleFile({ target: { files: [mockFile] } } as unknown as Event);

        expect(file.value).toStrictEqual(mockFile);
        expect(composable.file.value).toStrictEqual(mockFile);

        composable.removeFile();

        expect(file.value).toBeNull();
        expect(composable.file.value).toBeNull();
    });

    test('triggerFileInput() triggers file selection input', () => {
        const mockInput = { click: vi.fn() } as unknown as HTMLInputElement

        composable.fileInput.value = mockInput

        composable.triggerFileInput()

        expect(mockInput.click).toHaveBeenCalled()
    })
});
