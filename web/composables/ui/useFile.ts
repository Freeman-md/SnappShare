import { ref } from "vue";

export const useFile = ({
    file
} : {
    file: Ref<File | null>
}) => {
    const dragging = ref(false);
    const fileInput = ref<HTMLElement | null>(null);

    const handleFile = (event: Event) => {
        const target = event.target as HTMLInputElement;

        if (!target.files) return

        file.value = target.files[0];
    };

    const handleDrop = (event: DragEvent) => {
        event.preventDefault();

        if (event.dataTransfer) {
            file.value = event.dataTransfer.files[0];
        }

        dragging.value = false;
    };

    const removeFile = () => {
        file.value = null;
    };

    const triggerFileInput = () => {
        if (fileInput.value) {
            fileInput.value.click();
        }
    };

    return {
        file,
        dragging,
        fileInput,
        handleFile,
        handleDrop,
        removeFile,
        triggerFileInput
    }
}