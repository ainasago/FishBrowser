#!/usr/bin/env python3
"""
生成美观的浏览器指纹 icon
"""
from PIL import Image, ImageDraw, ImageFont
import os

# 创建 256x256 的图像
size = 256
img = Image.new('RGBA', (size, size), (0, 0, 0, 0))
draw = ImageDraw.Draw(img)

# 背景渐变色（从深蓝到浅蓝）
for y in range(size):
    # 计算渐变色
    r = int(30 + (100 - 30) * (y / size))
    g = int(100 + (180 - 100) * (y / size))
    b = int(200 + (220 - 200) * (y / size))
    draw.line([(0, y), (size, y)], fill=(r, g, b, 255))

# 绘制圆形边框
border_width = 8
draw.ellipse(
    [(border_width, border_width), (size - border_width, size - border_width)],
    outline=(255, 255, 255, 255),
    width=border_width
)

# 绘制指纹纹理（多个同心圆）
center_x, center_y = size // 2, size // 2
colors = [
    (255, 200, 0, 200),    # 金色
    (255, 150, 0, 180),    # 橙色
    (255, 100, 0, 160),    # 深橙色
]

# 绘制指纹纹理
for i, color in enumerate(colors):
    radius = 40 + i * 25
    draw.ellipse(
        [(center_x - radius, center_y - radius), (center_x + radius, center_y + radius)],
        outline=color,
        width=4
    )

# 绘制中心点
center_radius = 12
draw.ellipse(
    [(center_x - center_radius, center_y - center_radius),
     (center_x + center_radius, center_y + center_radius)],
    fill=(255, 255, 255, 255)
)

# 添加小的装饰元素（代表数据点）
dot_positions = [
    (center_x - 60, center_y - 40),
    (center_x + 60, center_y - 40),
    (center_x - 70, center_y + 50),
    (center_x + 70, center_y + 50),
]

for x, y in dot_positions:
    draw.ellipse([(x - 6, y - 6), (x + 6, y + 6)], fill=(255, 255, 255, 220))

# 保存为 PNG
output_dir = os.path.dirname(os.path.abspath(__file__))
png_path = os.path.join(output_dir, 'fingerprint_icon.png')
img.save(png_path, 'PNG')
print(f"✅ PNG icon saved: {png_path}")

# 转换为 ICO（多个尺寸）
ico_sizes = [(16, 16), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)]
ico_images = []

for ico_size in ico_sizes:
    resized = img.resize(ico_size, Image.Resampling.LANCZOS)
    ico_images.append(resized)

ico_path = os.path.join(output_dir, 'fingerprint_icon.ico')
ico_images[0].save(ico_path, 'ICO', sizes=ico_sizes, append_images=ico_images[1:])
print(f"✅ ICO icon saved: {ico_path}")

print("\n📋 Icon generation complete!")
print(f"   PNG: {png_path}")
print(f"   ICO: {ico_path}")
