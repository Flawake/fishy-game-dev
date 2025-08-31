from PIL import Image, ImageTk, ImageFilter, ImageChops
from tkinter import filedialog, colorchooser
import tkinter as tk
from math import sqrt

class ImageProcessorApp:
    def __init__(self, root):
        self.root = root
        self.root.title("Image Processor")
        self.root.geometry("1200x900")  # large window

        self.image = None
        self.processed_image = None
        self.tk_preview = None
        self.input_path = None
        self.target_color = (255, 255, 255)  # default white

        # --- Top buttons ---
        top_frame = tk.Frame(root)
        top_frame.pack(pady=10)

        self.open_btn = tk.Button(top_frame, text="Open Image", command=self.open_image)
        self.open_btn.pack(side="left", padx=5)

        self.save_btn = tk.Button(top_frame, text="Save Image", command=self.save_image, state="disabled")
        self.save_btn.pack(side="left", padx=5)

        self.color_btn = tk.Button(top_frame, text="Pick Color", command=self.pick_color)
        self.color_btn.pack(side="left", padx=5)

        # --- Mode selection ---
        self.mode = tk.StringVar(value="bgremove")
        mode_frame = tk.Frame(root)
        mode_frame.pack(pady=10)
        tk.Label(mode_frame, text="Mode:").pack(side="left", padx=5)
        tk.Radiobutton(mode_frame, text="Remove Color", variable=self.mode, value="bgremove",
                       command=self.update_controls).pack(side="left", padx=5)
        tk.Radiobutton(mode_frame, text="Blur Filter", variable=self.mode, value="blur",
                       command=self.update_controls).pack(side="left", padx=5)

        # --- Control sliders frame ---
        self.slider_frame = tk.Frame(root)
        self.slider_frame.pack(fill="x", padx=10, pady=5)

        self.control_slider = tk.Scale(self.slider_frame, from_=0, to=50, orient="horizontal",
                                       label="Tolerance", command=self.update_preview)
        self.control_slider.set(10)
        self.control_slider.pack(side="left", fill="x", expand=True, padx=5)

        self.feather_slider = tk.Scale(self.slider_frame, from_=0, to=20, orient="horizontal",
                                       label="Feather", command=self.update_preview)
        self.feather_slider.set(2)
        self.feather_slider.pack(side="left", fill="x", expand=True, padx=5)

        # --- Preview ---
        self.preview_label = tk.Label(root, bg="black")
        self.preview_label.pack(expand=True, fill="both", padx=10, pady=10)

    # ---------- Utils ----------
    def file_dialog(self, action="open"):
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

    def remove_color_with_mask(self, img, target_color=(255, 255, 255), tolerance=30, feather=2):
        img = img.convert("RGBA")
        r, g, b, a = img.split()
        tr, tg, tb = target_color
        mask_data = []
        for rp, gp, bp in zip(r.getdata(), g.getdata(), b.getdata()):
            dist = sqrt((rp - tr)**2 + (gp - tg)**2 + (bp - tb)**2)
            mask_data.append(int(255 if dist <= tolerance else 0))
        mask = Image.new("L", img.size)
        mask.putdata(mask_data)
        mask = mask.filter(ImageFilter.GaussianBlur(feather))
        a = ImageChops.subtract(a, mask)
        img.putalpha(a)
        return img

    def apply_blur(self, img, radius):
        img = img.convert("RGBA")
        r, g, b, a = img.split()
        rgb_img = Image.merge("RGB", (r, g, b))
        blurred_rgb = rgb_img.filter(ImageFilter.GaussianBlur(radius))
        blurred_rgba = Image.merge("RGBA", (*blurred_rgb.split(), a))
        return blurred_rgba

    # ---------- UI actions ----------
    def open_image(self):
        path = self.file_dialog("open")
        if not path:
            return
        self.input_path = path
        self.image = Image.open(path)
        self.update_preview()
        self.save_btn.config(state="normal")

    def pick_color(self):
        color = colorchooser.askcolor(color=self.target_color)
        if color[0]:
            self.target_color = tuple(int(c) for c in color[0])
            self.update_preview()

    def update_controls(self):
        if self.mode.get() == "bgremove":
            self.control_slider.config(label="Tolerance", from_=0, to=100)
            self.control_slider.set(30)
            self.feather_slider.config(label="Feather", from_=0, to=20)
            self.feather_slider.set(2)
            self.feather_slider.pack(side="left", fill="x", expand=True, padx=5)
        elif self.mode.get() == "blur":
            self.control_slider.config(label="Blur Radius", from_=0, to=100)
            self.control_slider.set(10)
            self.feather_slider.pack_forget()
        self.update_preview()

    def update_preview(self, event=None):
        if not self.image:
            return
        preview = self.image.copy()
        if self.mode.get() == "bgremove":
            tolerance = self.control_slider.get()
            feather = self.feather_slider.get()
            self.processed_image = self.remove_color_with_mask(preview, self.target_color, tolerance, feather)
        elif self.mode.get() == "blur":
            radius = self.control_slider.get()
            preview.thumbnail((900, 700))
            self.processed_image = self.apply_blur(preview, radius)
        self.tk_preview = ImageTk.PhotoImage(self.processed_image)
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
