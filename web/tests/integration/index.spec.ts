import { describe, expect, test, vi } from "vitest";
import { mount } from "@vue/test-utils";
import Index from "~/pages/index.vue";
import axios from "axios";

vi.mock("axios");

const mockedAxiosPost = vi.mocked(axios.post);

const mockRouterPush = vi.fn();
vi.mock("vue-router", () => ({
    useRouter: () => ({ push: mockRouterPush }),
}));

describe('IndexPage - File Upload Flow', () => {
    test('User selects a file → UI updates correctly (filename appears)', async () => {
        const wrapper = mount(Index)

        const fileInput = wrapper.find("input[type='file']")

        const mockFile = new File(["File Content"], "file.txt", { type: "text/plain" })

        const dataTransfer = new DataTransfer()
        dataTransfer.items.add(mockFile)
        
        Object.defineProperty(fileInput.element, "files", {
            value: dataTransfer.files
        })
        
        await fileInput.trigger("change")

        expect(wrapper.text()).toContain("file.txt")
    })

    test('Drag-and-drop file → UI updates correctly', async () => {
        const wrapper = mount(Index);
    
        const dropZone = wrapper.find('#dropzone')
        const mockFile = new File(["File Content"], "dropped-file.txt", { type: "text/plain" });
    
        const dataTransfer = new DataTransfer();
        dataTransfer.items.add(mockFile);
    
        await dropZone.trigger("dragover", { dataTransfer });
    
        await dropZone.trigger("drop", { dataTransfer });
    
        expect(wrapper.text()).toContain("dropped-file.txt");
    });

    test('Clicking "Remove File" resets file state', async () => {
        const wrapper = mount(Index);
        const fileInput = wrapper.find("input[type='file']");
    
        const mockFile = new File(["File Content"], "file.txt", { type: "text/plain" });
        const dataTransfer = new DataTransfer();
        dataTransfer.items.add(mockFile);
    
        Object.defineProperty(fileInput.element, "files", { value: dataTransfer.files });
        await fileInput.trigger("change");
    
        expect(wrapper.text()).toContain("file.txt");

        const removeButton = wrapper.find("#remove-file-button");
        await removeButton.trigger("click");
    
        expect(wrapper.text()).not.toContain("file.txt");
    });    

    test('Clicking "Upload File" triggers API request', async () => {
        const wrapper = mount(Index);
        const fileInput = wrapper.find("input[type='file']");
        const uploadButton = wrapper.find("#upload-file-button");
    
        const mockFile = new File(["File Content"], "file.txt", { type: "text/plain" });
        const dataTransfer = new DataTransfer();
        dataTransfer.items.add(mockFile);
    
        Object.defineProperty(fileInput.element, "files", { value: dataTransfer.files });
        await fileInput.trigger("change");
    
        mockedAxiosPost.mockResolvedValue({ data: { data: { id: "123ABC" } } });
    
        await uploadButton.trigger("click");
    
        expect(axios.post).toHaveBeenCalled();
    });

    test("Error message displays when API fails", async () => {
        const wrapper = mount(Index);
        const fileInput = wrapper.find("input[type='file']");
        const uploadButton = wrapper.find("#upload-file-button");
    
        const mockFile = new File(["File Content"], "file.txt", { type: "text/plain" });
        const dataTransfer = new DataTransfer();
        dataTransfer.items.add(mockFile);
    
        Object.defineProperty(fileInput.element, "files", { value: dataTransfer.files });
        await fileInput.trigger("change");
    
        mockedAxiosPost.mockRejectedValueOnce(new Error("Network Error"));
    
        await uploadButton.trigger("click");

        await nextTick();
    
        const errorMessageElement = wrapper.find("#error-message");
        expect(errorMessageElement.text()).toBe("Network Error");
    });

    test('Navigates to /file/:id after successful upload', async () => {
        const wrapper = mount(Index);
        const fileInput = wrapper.find("input[type='file']");
        const uploadButton = wrapper.find("#upload-file-button");
    
        const mockFile = new File(["File Content"], "file.txt", { type: "text/plain" });
        const dataTransfer = new DataTransfer();
        dataTransfer.items.add(mockFile);
    
        Object.defineProperty(fileInput.element, "files", { value: dataTransfer.files });
        await fileInput.trigger("change");
    
        mockedAxiosPost.mockResolvedValue({ data: { data: { id: "123ABC" } } });
    
        await uploadButton.trigger("click");
    
        expect(mockRouterPush).toHaveBeenCalledWith("/file/123ABC");
    });
    
    
})