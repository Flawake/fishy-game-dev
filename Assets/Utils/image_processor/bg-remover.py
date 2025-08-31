from PIL import Image, ImageTk, ImageFilter
import tkinter as tk
from tkinter import filedialog


class ImageProcessorApp:
    def __init__(self, root):
        self.root = root
        self.root.title("Image Processor")
        self.root.geometry("1000x800")

        self.image = None
        self.processed_image = None
        self.tk_preview = None
        self.input_path = None

        # Top buttons
        top_frame = tk.Frame(root)
        top_frame.pack(pady=10)

        self.open_btn = tk.Button(top_frame, text="Open Image", command=self.open_image)
        self.open_btn.pack(side="left", padx=5)

        self.save_btn = tk.Button(top_frame, text="Save Image", command=self.save_image, state="disabled")
        self.save_btn.pack(side="left", padx=5)

        # Mode selection
        self.mode = tk.StringVar(value="bgremove")
        mode_frame = tk.Frame(root)
        mode_frame.pack(pady=10)
        tk.Label(mode_frame, text="Mode:").pack(side="left", padx=5)
        tk.Radiobutton(mode_frame, text="Remove White BG", variable=self.mode, value="bgremove",
                       command=self.update_controls).pack(side="left", padx=5)
        tk.Radiobutton(mode_frame, text="Blur Filter", variable=self.mode, value="blur",
                       command=self.update_controls).pack(side="left", padx=5)

        # Control slider
        self.control_slider = tk.Scale(root, from_=0, to=50, orient="horizontal", label="Tolerance", command=self.update_preview)
        self.control_slider.set(10)
        self.control_slider.pack(fill="x", padx=10)

        # Preview
        self.preview_label = tk.Label(root, bg="black")
        self.preview_label.pack(expand=True, fill="both", padx=10, pady=10)

    # ---------- Utils ----------
    def file_dialog(self, action="open"):
        """Unified file dialog helper"""
        filetypes = [
            ("PNG files", "*.png"),
            ("JPEG files", "*.jpg *.jpeg"),
            ("Bitmap files", "*.bmp"),
            ("GIF files", "*.gif"),
            ("All files", "*.*"),
        ]
        if action == "open":
            return filedialog.askopenfilename(filetypes=filetypes)
        elif action == "save":
            return filedialog.asksaveasfilename(defaultextension=".png", filetypes=filetypes)

    def make_white_transparent(self, img, tolerance):
        img = img.convert("RGBA")
        new_data = []
        for r, g, b, a in img.getdata():
            if r >= 255 - tolerance and g >= 255 - tolerance and b >= 255 - tolerance:
                new_data.append((255, 255, 255, 0))
            else:
                new_data.append((r, g, b, a))
        out = Image.new("RGBA", img.size)
        out.putdata(new_data)
        return out

    def apply_blur(self, img, radius):
        return img.filter(ImageFilter.GaussianBlur(radius))

    # ---------- UI actions ----------
    def open_image(self):
        path = self.file_dialog("open")
        if not path:
            return
        self.input_path = path
        self.image = Image.open(path)
        self.update_preview()
        self.save_btn.config(state="normal")

    def update_controls(self):
        if self.mode.get() == "bgremove":
            self.control_slider.config(label="Tolerance", from_=0, to=50)
            self.control_slider.set(10)
        elif self.mode.get() == "blur":
            self.control_slider.config(label="Blur Radius", from_=0, to=100)
            self.control_slider.set(2)
        self.update_preview()

    def update_preview(self, event=None):
        if not self.image:
            return
        if self.mode.get() == "bgremove":
            tolerance = self.control_slider.get()
            self.processed_image = self.make_white_transparent(self.image, tolerance)
        elif self.mode.get() == "blur":
            radius = self.control_slider.get()
            self.processed_image = self.apply_blur(self.image, radius)

        preview = self.processed_image.copy()
        preview.thumbnail((900, 700))
        self.tk_preview = ImageTk.PhotoImage(preview)
        self.preview_label.config(image=self.tk_preview)

    def save_image(self):
        if not self.processed_image:
            return
        path = self.file_dialog("save")
        if path:
            self.processed_image.save(path)
            print(f"✅ Saved to {path}")


def main():
    root = tk.Tk()
    app = ImageProcessorApp(root)
    root.mainloop()


if __name__ == "__main__":
    main()
