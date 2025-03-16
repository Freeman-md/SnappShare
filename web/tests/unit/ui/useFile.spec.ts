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

    test('useFile() initializes file with null value', () => {
        expect(composable.file.value).toBeNull();
    });

    test('useFile() initializes dragging with false', () => {
        expect(composable.dragging.value).toBeFalsy();
    });

    test('handleFile() updates file correctly', () => {
        composable.handleFile({ target: { files: [mockFile] } } as unknown as Event);

        expect(file.value).toBe(mockFile);
        expect(composable.file.value).toBe(mockFile);
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
        expect(file.value).toBe(mockFile);
        expect(composable.file.value).toBe(mockFile);
        expect(composable.dragging.value).toBeFalsy()
    });

    test('removeFile() sets file to null', () => {
        composable.handleFile({ target: { files: [mockFile] } } as unknown as Event);

        expect(file.value).toBe(mockFile);
        expect(composable.file.value).toBe(mockFile);

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
