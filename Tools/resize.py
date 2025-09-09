from PIL import Image

# Settings
input_path = "Tools/img/macpass"
output_path = input_path+"_out"
zoom_factor = 6

# Open image
img = Image.open(input_path+".png")
w, h = img.size

# Scale up
scaled_w = int(w * zoom_factor)
scaled_h = int(h * zoom_factor)
img_zoomed = img.resize((scaled_w, scaled_h), Image.LANCZOS)
print(img_zoomed)
cr = 6/4.85
img_cropped = img_zoomed.crop((5500*cr, 1500*cr, 7400*cr, 2500*cr))

# Save result
img_cropped.save(output_path+".png")
print(f"Saved zoomed+clipped image to {output_path}")
